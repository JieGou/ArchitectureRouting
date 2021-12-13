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
    private const int FASUConnectorId = 18 ;
    private const int VAVConnectorId = 4 ;
    private const string RoundDuctUniqueId = "dee0da15-198f-4f79-aa08-3ce71203da82-00c0cdcf" ;

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
        if ( ! HasBoundingBox( uiDocument.Document, space ) ) {
          return ( false, $"`{space.Name}` have not bounding box." ) ;
        }
      }

      if ( ! RoundDuctTypeExists( uiDocument.Document ) )
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

      if ( ! branchNumberToAreaDictionary.TryGetValue( 0, out var rootAreas ) )
      {
        rootAreas = new List<Element>() ;
      }
      
      // TODO VAVのファミリから取得
      var vavUpstreamConnectorNormal = new Vector3d( 1, 0, 0 ) ;
      Dictionary<Element, double> rotationAnglesOfFASUsAndVAVs = CalculateRotationAnglesOfFASUsAndVAVs( document,
        branchNumberToAreaDictionary, pickedConnector, vavUpstreamConnectorNormal ) ;

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
            instanceOfVAV.Id
          } ;
          ElementTransformUtils.RotateElements( document, idOfFASUAndVAV,
            Line.CreateBound( positionOfFASUAndVAV, positionOfFASUAndVAV + XYZ.BasisZ ),
            rotationAnglesOfFASUsAndVAVs[ space ] ) ;

          // この時点でコネクタの向きとは逆を向いている想定
          // コネクタの裏側にあるときは、ここで向きを反転する
          if ( rootAreas.Contains( space ) && IsVavLocatedBehindConnector( document, instanceOfVAV, pickedConnector ) ) {
            ElementTransformUtils.RotateElements( document, idOfFASUAndVAV,
              Line.CreateBound( positionOfFASUAndVAV, positionOfFASUAndVAV + XYZ.BasisZ ), Math.PI ) ;
          }

          // TODO : 一直線にならんでいるグループの方向修正
          
          var fasuConnector = instanceOfFASU.GetConnectors().First( c => c.Id == FASUConnectorId ) ;
          var vavConnector = instanceOfVAV.GetConnectors().First( c => c.Id == VAVConnectorId ) ;
          CreateDuctConnectionFASUAndVAV( document, fasuConnector, vavConnector, space.LevelId ) ;
        }

        tr.Commit() ;
      }

      return Result.Succeeded ;
    }

    private static Dictionary<Element, double> CalculateRotationAnglesOfFASUsAndVAVs( Document document,
      Dictionary<int, List<Element>> branchNumberDict, Connector rootConnector, Vector3d upstreamConnectorNormal )
    {
      var rotationAnglesOfFASUsAndVAVs = new Dictionary<Element, double>() ;

      var rootConnectorNormal = rootConnector.CoordinateSystem.BasisZ.To3dDirection() ;
      var orthogonalToConnectorNormal = new Vector3d( rootConnectorNormal.y, -rootConnectorNormal.x, 0 ) ;
        
      foreach ( var branchNumber in branchNumberDict.Keys ) {
        List<Element> targetSpaces = branchNumberDict[ branchNumber ] ;

        if ( branchNumber == RootBranchNumber ) {
          var rotation = GetRotationForRootSpaces( rootConnector, upstreamConnectorNormal ) ;
          foreach ( var targetSpace in targetSpaces ) {
            rotationAnglesOfFASUsAndVAVs.Add( targetSpace, rotation ) ;
          }

          continue ;
        }

        if ( AreSpacesCollinear( document, targetSpaces, orthogonalToConnectorNormal ) ) {
          var rotation = GetRotationForCollinearSpaces( document, rootConnector, targetSpaces, upstreamConnectorNormal ) ;
          foreach ( var targetSpace in targetSpaces ) {
            rotationAnglesOfFASUsAndVAVs[ targetSpace ] = rotation ;
          }

          continue ;
        }

        var spaceBoxes = targetSpaces.Select( space => space.get_BoundingBox( document.ActiveView ).ToBox3d() ).ToArray() ;
        var spacesCenter = spaceBoxes.UnionBounds()!.Value.Center ;
        
        foreach ( var space in targetSpaces ) {
          var spaceCenter = space.get_BoundingBox( document.ActiveView ).ToBox3d().Center ;
          rotationAnglesOfFASUsAndVAVs[space] = GetRotationForNonCollinearSpace( document, rootConnector, spacesCenter, spaceCenter, upstreamConnectorNormal ) ;
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

    private static double ConvertDegreeToRadian( double degreeAngle )
    {
      return degreeAngle * Math.PI / 180 ;
    }

    private static double CalcRadianAngle2D( Vector3d from, Vector3d to )
    {
      var degree = Vector3d.SignedAngle( from, to, new Vector3d( 0, 0, 1 ) ) ;
      if ( degree != 0 ) return ConvertDegreeToRadian(degree) ;
      return from == to ? 0 : Math.PI ;
    }
    
    private static double GetRotationForRootSpaces( Connector rootConnector, Vector3d upstreamConnectorNormal )
    {
      var rootConnectorNormal = rootConnector.CoordinateSystem.BasisZ.To3dDirection() ;
      if ( upstreamConnectorNormal == rootConnectorNormal ) return ConvertDegreeToRadian( 180 ) ;

      return CalcRadianAngle2D( upstreamConnectorNormal, -rootConnectorNormal ) ;
    }

    private static double GetRotationForCollinearSpaces( Document document, Connector rootConnector, IReadOnlyList<Element> spaces, Vector3d upstreamConnectorNormal )
    {
      var rootConnectorOrigin = rootConnector.Origin.To3dPoint() ;
      var rootConnectorNormal = rootConnector.CoordinateSystem.BasisZ.To3dDirection() ;
      
      var spaceBoxes = spaces.Select( space => space.get_BoundingBox( document.ActiveView ).ToBox3d() ).ToArray() ;
      var centerOfSpaces = spaceBoxes.UnionBounds()!.Value.Center ;

      // RootConnectorの法線方向基準で、RootConnectorより奥にある場合は、上流につなげるConnectorの向きを法線方向とは逆向きにする.
      var sign = Vector3d.Dot( centerOfSpaces - rootConnectorOrigin, rootConnectorNormal ) > 0 ? -1 : 1 ;
      return CalcRadianAngle2D( upstreamConnectorNormal, sign * rootConnectorNormal ) ;
    }

    private static double GetRotationForNonCollinearSpace( Document document, Connector rootConnector, Vector3d spaceGroupCenter, Vector3d spaceCenter, Vector3d upstreamConnectorNormal )
    {
      var rootConnectorNormal = rootConnector.CoordinateSystem.BasisZ.To3dDirection() ;
      var sign = Vector3d.Dot( spaceGroupCenter - spaceCenter, rootConnectorNormal ) > 0 ? 1 : -1 ;
      return CalcRadianAngle2D( upstreamConnectorNormal, sign * rootConnectorNormal ) ;
    }

    private static bool AreSpacesCollinear( Document document, IReadOnlyList<Element> spaces, Vector3d checkTargetDir2D )
    {
      var orthogonalToTargetDir = new Vector3d( checkTargetDir2D.y, -checkTargetDir2D.x, 0.0 ) ;
      
      var spaceBoxes = spaces.Select( space => space.get_BoundingBox( document.ActiveView ).ToBox3d() ).ToArray() ;
      var centerOfSpaces = spaceBoxes.UnionBounds()!.Value.Center ;
      
      return spaceBoxes.Select( box => box.Center )
        .Any( center => Math.Abs( Vector3d.Dot( centerOfSpaces - center, orthogonalToTargetDir ) ) < MinDistanceSpacesCollinear ) ; 
    }

    private static bool IsVavLocatedBehindConnector( Document document, Element instanceOfVAV, Connector instanceOfConnector )
    {
      BoundingBoxXYZ boxOfVAV = instanceOfVAV.get_BoundingBox( document.ActiveView ) ;
      if ( boxOfVAV == null ) return false ;

      var connectorPosition = instanceOfConnector.Origin.To3dPoint() ;
      var connectorNormal = instanceOfConnector.CoordinateSystem.BasisZ.To3dDirection() ;
      
      // コネクタの向いている方向の成分で比較したときに、VAVのBoxの角が1つでもコネクタ位置よりも小さければ後方とみなす.
      return boxOfVAV.ToBox3d().Vertices().Any( boxCorner => Vector3d.Dot( boxCorner - connectorPosition, connectorNormal ) < 0 ) ;
    }

    private static bool HasBoundingBox( Document document, Element element )
    {
      return null != element.get_BoundingBox( document.ActiveView ) ;
    }

    private static void CreateDuctConnectionFASUAndVAV( Document document, Connector connectorOfFASU,
      Connector connectorOfVAV, ElementId levelId )
    {
      FilteredElementCollector collector = new FilteredElementCollector( document ).OfClass( typeof( DuctType ) )
        .WhereElementIsElementType() ;
      var ductType = collector.First( e => e.UniqueId == RoundDuctUniqueId ) ;
      Duct.Create( document, ductType.Id, levelId, connectorOfVAV, connectorOfFASU ) ;
    }

    private static bool RoundDuctTypeExists( Document document )
    {
      FilteredElementCollector collector = new FilteredElementCollector( document ).OfClass( typeof( DuctType ) ) ;
      return collector.Any( e => e.UniqueId == RoundDuctUniqueId ) ;
    }
  }
}