using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using System.Collections.Generic ;
using ImageType = Arent3d.Revit.UI.ImageType ;
using System ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Revit ;
using System.Linq ;
using MathLib ;
using Line = Autodesk.Revit.DB.Line ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.CreateFASUAndVAVAutomaticallyCommand",
    DefaultString = "Create FASU\nAnd VAV" )]
  [Image( "resources/Initialize-16.bmp", ImageType = ImageType.Normal )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class CreateFASUAndVAVAutomaticallyCommand : IExternalCommand
  {
    private const double DistanceBetweenFASUAndVAV = 0.25 ;
    private const string HeightOfFASU = "3100" ;
    private const string HeightOfVAV = "3275" ;
    private const string DiameterOfVAV = "250" ;
    private const int RootBranchNumber = 0 ;
    private const double MinDistanceSpacesCollinear = 2.5 ;

    private enum RotationAxis
    {
      XAxis,
      YAxis
    }

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      var executor = CreateRoutingExecutor( document, commandData.View ) ;

      try {
        bool success ;
        object? state ;
        ( success, state ) = OperateUI( uiDocument, executor ) ;
        if ( state is string mes ) {
          message = mes ;
        }

        if ( success ) {
          return Result.Succeeded ;
        }

        return Result.Failed ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
    }

    private (bool Result, object? State) OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor )
    {
      ConnectorPicker.IPickResult iPickResult =
        ConnectorPicker.GetConnector( uiDocument, routingExecutor, true,
          "Dialog.Commands.Routing.CreateFASUAndVAVAutomaticallyCommand.PickConnector", null, GetAddInType() ) ;
      if ( iPickResult.PickedConnector != null &&
           CreateFASUAndVAVAutomatically( uiDocument.Document, iPickResult.PickedConnector ) == Result.Succeeded ) {
        TaskDialog.Show( "FASUとVAVの自動配置", "FASUとVAVを配置しました。" ) ;
      }

      return ( true, null ) ;
    }

    private AddInType GetAddInType() => AppCommandSettings.AddInType ;

    private RoutingExecutor CreateRoutingExecutor( Document document, View view ) =>
      AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    private static Result CreateFASUAndVAVAutomatically( Document document, Connector pickedConnector )
    {
      IList<Element> spaces = GetAllSpaces( document ) ;

      Dictionary<int, List<Element>> branchNumberToAreaDictionary = new() ;
      foreach ( Element space in spaces ) {
        space.TryGetProperty( BranchNumberParameter.BranchNumber, out int branchNumber ) ;
        if ( branchNumberToAreaDictionary.ContainsKey( branchNumber ) ) {
          branchNumberToAreaDictionary[ branchNumber ].Add( space ) ;
        }
        else {
          branchNumberToAreaDictionary.Add( branchNumber, new List<Element>() { space } ) ;
        }
      }
      
      var rotationAxis = GetRotationAxis( pickedConnector ) ;

      Dictionary<Element, double> rotationAnglesOfFASUsAndVAVs = CalculateRotationAnglesOfFASUsAndVAVs( document,
        branchNumberToAreaDictionary, pickedConnector, rotationAxis ) ;

      // Start Transaction
      using ( Transaction tr = new(document) ) {
        tr.Start( "Create FASUs and VAVs Automatically" ) ;
        foreach ( var space in spaces ) {
          // Add object to the document
          BoundingBoxXYZ boxOfSpace = space.get_BoundingBox( document.ActiveView ) ;
          if ( boxOfSpace == null ) continue ;

          var positionOfFASUAndVAV = new XYZ( ( boxOfSpace.Max.X + boxOfSpace.Min.X ) / 2,
            ( boxOfSpace.Max.Y + boxOfSpace.Min.Y ) / 2, 0 ) ;

          // Add FASU to document
          var instanceOfFASU = document.AddFASU( positionOfFASUAndVAV, space.LevelId ) ;
          ElementTransformUtils.RotateElement( document, instanceOfFASU.Id,
            Line.CreateBound( positionOfFASUAndVAV, positionOfFASUAndVAV + XYZ.BasisZ ), Math.PI / 2 ) ;
          instanceOfFASU.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).SetValueString( HeightOfFASU ) ;

          // Add VAV to document
          var instanceOfVAV = document.AddVAV( positionOfFASUAndVAV, space.LevelId ) ;
          instanceOfVAV.LookupParameter( "ダクト径" ).SetValueString( DiameterOfVAV ) ;
          instanceOfVAV.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).SetValueString( HeightOfVAV ) ;

          // Get BoundingBox of FASU and VAV
          BoundingBoxXYZ boxOfFASU = instanceOfFASU.get_BoundingBox( document.ActiveView ) ;
          if ( boxOfFASU == null ) continue ;
          BoundingBoxXYZ boxOfVAV = instanceOfVAV.get_BoundingBox( document.ActiveView ) ;
          if ( boxOfVAV == null ) continue ;

          // Move the VAV to a distance distanceBetweenFASUAndVAV from FASU
          var distanceBetweenFASUCenterAndVAVCenter = ( boxOfFASU.Max.X - boxOfFASU.Min.X ) / 2 +
                                                      ( boxOfVAV.Max.X - boxOfVAV.Min.X ) / 2 +
                                                      DistanceBetweenFASUAndVAV ;
          ElementTransformUtils.MoveElement( document, instanceOfVAV.Id,
            new XYZ( distanceBetweenFASUCenterAndVAVCenter, 0, 0 ) ) ;

          // 回転軸で見るとき、コネクターがVAVの境界ボックス内にある場合、VAVの向きを反転させる
          if ( CheckVAVTouchingConnector( document, instanceOfVAV, pickedConnector, positionOfFASUAndVAV,
            rotationAnglesOfFASUsAndVAVs[ space ], rotationAxis ) ) {
            rotationAnglesOfFASUsAndVAVs[ space ] += Math.PI ;
          }
          
          // Rotate FASU and VAV
          var idOfFASUAndVAV = new List<ElementId>
          {
            instanceOfFASU.Id,
            instanceOfVAV.Id
          } ;
          ElementTransformUtils.RotateElements( document, idOfFASUAndVAV,
            Line.CreateBound( positionOfFASUAndVAV, positionOfFASUAndVAV + XYZ.BasisZ ),
            rotationAnglesOfFASUsAndVAVs[ space ] ) ;
        }

        tr.Commit() ;
      }

      return Result.Succeeded ;
    }

    private static Dictionary<Element, double> CalculateRotationAnglesOfFASUsAndVAVs( Document document,
      Dictionary<int, List<Element>> branchNumberDict, Connector pickedConnector, RotationAxis rotationAxis )
    {
      var rotationAnglesOfFASUsAndVAVs = new Dictionary<Element, double>() ;

      // Process by group BranchNumber
      foreach ( var branchNumber in branchNumberDict.Keys ) {
        List<Element> targetSpaces = branchNumberDict[ branchNumber ] ;

        // Separate handling for RootBranchNumber
        if ( branchNumber == RootBranchNumber ) {
          foreach ( var targetSpace in targetSpaces ) {
            XYZ centerPointOfSpace = GetCenterPointOfElement( document, targetSpace ) ;
            var rotation = GetRotationAngle( pickedConnector.Origin, centerPointOfSpace, rotationAxis ) ;
            rotationAnglesOfFASUsAndVAVs.Add( targetSpace, rotation ) ;
          }
          continue ;
        }

        // Get center of spaces group
        XYZ centerPointOfSpacesGroup = GetCenterPointOfSpacesGroup( document, targetSpaces ) ;

        // Are the spaces collinear
        var areSpacesCollinear =
          AreRotatedSpacesCollinear( document, targetSpaces, centerPointOfSpacesGroup, rotationAxis ) ;

        // Calculate rotation angle of FASU and VAV in each space
        foreach ( var handleSpace in targetSpaces ) {
          if ( areSpacesCollinear ) {
            rotationAnglesOfFASUsAndVAVs[ handleSpace ] = GetRotationAngle( pickedConnector.Origin,
              centerPointOfSpacesGroup, rotationAxis ) ;
          }
          else {
            XYZ centerPointOfSpace = GetCenterPointOfElement( document, handleSpace ) ;
            rotationAnglesOfFASUsAndVAVs[ handleSpace ] =
              GetRotationAngle( centerPointOfSpacesGroup, centerPointOfSpace, rotationAxis ) ;
          }
        }
      }

      return rotationAnglesOfFASUsAndVAVs ;
    }

    private static IList<Element> GetAllSpaces( Document document )
    {
      ElementCategoryFilter filter = new(BuiltInCategory.OST_MEPSpaces) ;
      FilteredElementCollector collector = new(document) ;
      IList<Element> spaces = collector.WherePasses( filter ).WhereElementIsNotElementType().ToElements() ;
      return spaces ;
    }

    private static XYZ GetCenterPointOfSpacesGroup( Document document, List<Element> spaces )
    {
      var centerPositions = spaces.Select( element => GetCenterPointOfElement( document, element ).To3dPoint() )
        .ToArray() ;
      return new Box3d( centerPositions ).Center.ToXYZPoint() ;
    }

    private static XYZ GetCenterPointOfElement( Document document, Element element )
    {
      BoundingBoxXYZ boxOfSpace = element.get_BoundingBox( document.ActiveView ) ;
      if ( boxOfSpace != null ) {
        return boxOfSpace.ToBox3d().Center.ToXYZPoint() ;
      }

      //TODO：スペースにはBounding boxが含まれていないことがある。この場合は警告を出す必要があるか?
      return new XYZ() ;
    }

    private static RotationAxis GetRotationAxis( Connector pickedConnector )
    {
      var rotation = ( pickedConnector.Owner.Location as LocationPoint )!.Rotation ;
      if ( Math.Abs( Math.Cos( rotation ) ) >= Math.Cos( Math.PI / 4 ) ) {
        return RotationAxis.XAxis ;
      }

      return RotationAxis.YAxis ;
    }

    /// <summary>
    /// (fromPoint,toPoint)ベクトルに対応した回転角度を求める
    /// </summary>
    /// <param name="toPoint">回転ベクトルの始点</param>
    /// <param name="fromPoint">回転ベクトルの終点</param>
    /// <param name="axisOfRotation">回転軸（縦・横方向の回転のみ）</param>
    /// <returns>回転角度</returns>
    private static double GetRotationAngle( XYZ toPoint, XYZ fromPoint, RotationAxis axisOfRotation )
    {
      if ( axisOfRotation == RotationAxis.XAxis ) {
        if ( fromPoint.X <= toPoint.X ) {
          return 0 ;
        }

        return Math.PI ;
      }

      if ( fromPoint.Y <= toPoint.Y ) {
        return 0.5 * Math.PI ;
      }

      return 1.5 * Math.PI ;
    }

    private static bool AreRotatedSpacesCollinear( Document document, List<Element> spaces,
      XYZ centerPointOfSpacesGroup, RotationAxis rotationAxis )
    {
      foreach ( var space in spaces ) {
        XYZ centerPointOfSpace = GetCenterPointOfElement( document, space ) ;
        if ( rotationAxis == RotationAxis.XAxis ) {
          if ( Math.Abs( centerPointOfSpacesGroup.X - centerPointOfSpace.X ) > MinDistanceSpacesCollinear ) {
            return false ;
          }
        }
        else {
          if ( Math.Abs( centerPointOfSpacesGroup.Y - centerPointOfSpace.Y ) > MinDistanceSpacesCollinear ) {
            return false ;
          }
        }
      }
      return true ;
    }

    private static bool CheckVAVTouchingConnector( Document document, Element instanceOfVAV,
      Connector instanceOfConnector, XYZ pointOfRotation, double rotationAngle, RotationAxis axisOfRotation )
    {
      // Get BoundingBox of VAV
      BoundingBoxXYZ boxOfVAV = instanceOfVAV.get_BoundingBox( document.ActiveView ) ;
      if ( boxOfVAV == null ) return false ;
      
      // 仮にオブジェクトを回転したとき、回転軸において回転したオブジェクトの境界線の位置を求める。
      ( double leftOfVAV, double rightOfVAV ) =
        GetTheBoundariesOfTheRotatedObjectOnTheAxisOfRotation( boxOfVAV, pointOfRotation, rotationAngle,
          axisOfRotation ) ;

      if ( axisOfRotation == RotationAxis.XAxis ) {
        if ( instanceOfConnector.Origin.X >= leftOfVAV && instanceOfConnector.Origin.X <= rightOfVAV ) {
          return true ;
        }
      }
      else {
        if ( instanceOfConnector.Origin.Y >= leftOfVAV && instanceOfConnector.Origin.Y <= rightOfVAV ) {
          return true ;
        }
      }

      return false ;
    }

    private static (double, double) GetTheBoundariesOfTheRotatedObjectOnTheAxisOfRotation(
      BoundingBoxXYZ boxOfRotatingObject, XYZ pointOfRotation,
      double rotationAngle, RotationAxis axisOfRotation )
    {
      if ( axisOfRotation == RotationAxis.XAxis ) {
        if ( Math.Cos( rotationAngle ) > 0 ) {
          return ( boxOfRotatingObject.Min.X, boxOfRotatingObject.Max.X ) ;
        }
        else {
          return ( pointOfRotation.X - Math.Abs( pointOfRotation.X - boxOfRotatingObject.Max.X ),
            pointOfRotation.X + Math.Abs( pointOfRotation.X - boxOfRotatingObject.Min.X ) ) ;
        }
      }
      else {
        if ( Math.Sin( rotationAngle ) > 0 ) {
          return ( boxOfRotatingObject.Min.Y, boxOfRotatingObject.Max.Y ) ;
        }
        else {
          return ( pointOfRotation.Y - Math.Abs( pointOfRotation.Y - boxOfRotatingObject.Max.Y ),
            pointOfRotation.Y + Math.Abs( pointOfRotation.Y - boxOfRotatingObject.Min.Y ) ) ;
        }
      }
    }
  }
}