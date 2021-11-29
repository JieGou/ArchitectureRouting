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
using Autodesk.Revit.DB.Mechanical ;
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
    private const double EastDirection = Math.PI * 0 ;
    private const double NorthDirection = Math.PI * 0.5 ;
    private const double WestDirection = Math.PI * 1 ;
    private const double SouthDirection = Math.PI * 1.5 ;
    private const int FASUConnectorId = 18 ;
    private const int VAVConnectorId = 4 ;
    private const string RoundDuctUniqueId = "dee0da15-198f-4f79-aa08-3ce71203da82-00c0cdcf" ;

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
      IList<Element> spaces = GetAllSpaces( uiDocument.Document )
        .Where( space => space.HasParameter( BranchNumberParameter.BranchNumber ) ).ToArray() ;

      foreach ( var space in spaces ) {
        if ( ! CheckSpaceHasBoundingBox( uiDocument.Document, space ) ) {
          return ( false, $"`{space.Name}` have not bounding box." ) ;
        }
      }

      if ( ! CheckDocumentHasDuctType( uiDocument.Document ) )
        return ( false, $"There no family with UniqueID `{RoundDuctUniqueId}` in the document." ) ;

      ConnectorPicker.IPickResult iPickResult =
        ConnectorPicker.GetConnector( uiDocument, routingExecutor, true,
          "Dialog.Commands.Routing.CreateFASUAndVAVAutomaticallyCommand.PickConnector", null, GetAddInType() ) ;
      if ( iPickResult.PickedConnector != null &&
           CreateFASUAndVAVAutomatically( uiDocument.Document, iPickResult.PickedConnector, spaces ) ==
           Result.Succeeded ) {
        TaskDialog.Show( "FASUとVAVの自動配置", "FASUとVAVを配置しました。" ) ;
      }

      return ( true, null ) ;
    }

    private AddInType GetAddInType() => AppCommandSettings.AddInType ;

    private RoutingExecutor CreateRoutingExecutor( Document document, View view ) =>
      AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    private static Result CreateFASUAndVAVAutomatically( Document document, Connector pickedConnector,
      IList<Element> spaces )
    {
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


          // Rotate FASU and VAV
          var idOfFASUAndVAV = new List<ElementId>
          {
            instanceOfFASU.Id,
            instanceOfVAV.Id,
            // instanceOfDuct.Id
          } ;
          ElementTransformUtils.RotateElements( document, idOfFASUAndVAV,
            Line.CreateBound( positionOfFASUAndVAV, positionOfFASUAndVAV + XYZ.BasisZ ),
            rotationAnglesOfFASUsAndVAVs[ space ] ) ;

          // 回転軸で見るとき、コネクターがVAVの境界ボックス内にある場合、VAVの向きを反転させる
          if ( CheckVAVTouchingConnector( document, instanceOfVAV, pickedConnector, rotationAxis ) ) {
            ElementTransformUtils.RotateElements( document, idOfFASUAndVAV,
              Line.CreateBound( positionOfFASUAndVAV, positionOfFASUAndVAV + XYZ.BasisZ ), Math.PI ) ;
          }

          var fasuConnector = instanceOfFASU.GetConnectors().First( c => c.Id == FASUConnectorId ) ;
          var vavConnector = instanceOfVAV.GetConnectors().First( c => c.Id == VAVConnectorId ) ;
          CreateDuctConnectFASUAndVAV( document, fasuConnector, vavConnector, space.LevelId ) ;
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
            var rotation = GetRotationAngleForFASUAndVAV( pickedConnector.Origin, centerPointOfSpace, rotationAxis ) ;
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
            rotationAnglesOfFASUsAndVAVs[ handleSpace ] = GetRotationAngleForFASUAndVAV( pickedConnector.Origin,
              centerPointOfSpacesGroup, rotationAxis ) ;
          }
          else {
            XYZ centerPointOfSpace = GetCenterPointOfElement( document, handleSpace ) ;
            rotationAnglesOfFASUsAndVAVs[ handleSpace ] =
              GetRotationAngleForFASUAndVAV( centerPointOfSpacesGroup, centerPointOfSpace, rotationAxis ) ;
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
      return boxOfSpace.ToBox3d().Center.ToXYZPoint() ;
    }

    private static RotationAxis GetRotationAxis( Connector pickedConnector )
    {
      var rotation = ( pickedConnector.Owner.Location as LocationPoint )!.Rotation ;
      return Math.Abs( Math.Cos( rotation ) ) >= Math.Cos( Math.PI / 4 ) ? RotationAxis.XAxis : RotationAxis.YAxis ;
    }

    private static double GetRotationAngleForFASUAndVAV( XYZ centerPointOfSpacesGroup, XYZ centerPointOfSpace,
      RotationAxis axisOfRotation )
    {
      if ( axisOfRotation == RotationAxis.XAxis ) {
        return centerPointOfSpace.X <= centerPointOfSpacesGroup.X ? EastDirection : WestDirection ;
      }

      return centerPointOfSpace.Y <= centerPointOfSpacesGroup.Y ? NorthDirection : SouthDirection ;
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
      Connector instanceOfConnector, RotationAxis axisOfRotation )
    {
      // Get BoundingBox of VAV
      BoundingBoxXYZ boxOfVAV = instanceOfVAV.get_BoundingBox( document.ActiveView ) ;
      if ( boxOfVAV == null ) return false ;
      if ( axisOfRotation == RotationAxis.XAxis ) {
        return instanceOfConnector.Origin.X >= boxOfVAV.Min.X && instanceOfConnector.Origin.X <= boxOfVAV.Max.X ;
      }

      return instanceOfConnector.Origin.Y >= boxOfVAV.Min.Y && instanceOfConnector.Origin.Y <= boxOfVAV.Max.Y ;
    }

    private static bool CheckSpaceHasBoundingBox( Document document, Element space )
    {
      return ( null != space.get_BoundingBox( document.ActiveView ) ) ;
    }

    private static void CreateDuctConnectFASUAndVAV( Document document, Connector connectorOfFASU,
      Connector connectorOfVAV, ElementId levelId )
    {
      FilteredElementCollector collector = new FilteredElementCollector( document ).OfClass( typeof( DuctType ) )
        .WhereElementIsElementType() ;
      var ductType = collector.First( e => e.UniqueId == RoundDuctUniqueId ) ;
      Duct.Create( document, ductType.Id, levelId, connectorOfVAV, connectorOfFASU ) ;
    }

    private static bool CheckDocumentHasDuctType( Document document )
    {
      FilteredElementCollector collector = new FilteredElementCollector( document ).OfClass( typeof( DuctType ) ) ;
      return collector.Any( e => e.UniqueId == RoundDuctUniqueId ) ;
    }
  }
}