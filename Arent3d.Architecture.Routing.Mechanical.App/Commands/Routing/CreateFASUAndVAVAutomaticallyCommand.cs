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
using Arent3d.Utility ;
using Autodesk.Revit.DB.Mechanical ;
using MathLib ;
using Line = Autodesk.Revit.DB.Line ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.CreateFASUAndVAVAutomaticallyCommand", DefaultString = "Create FASU\nAnd VAV" )]
  [Image( "resources/Initialize-16.bmp", ImageType = ImageType.Normal )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class CreateFASUAndVAVAutomaticallyCommand : IExternalCommand
  {
    private const double DistanceBetweenFASUAndVAV = 0.25 ;
    private const string DiameterOfVAVForFASU250Phi = "250" ;
    private const string DiameterOfVAVForFASU300Phi = "300" ;
    private const int RootBranchNumber = 0 ;
    private const double MinDistanceSpacesCollinear = 2.5 ;
    private const string VAVAirflowName = "風量" ;
    private const int AirflowThresholdForUseDiameter250Phi = 765 ;

    private class FASUsAndVAVsInSpaceModel
    {
      public List<Element> listOfFASUs = new List<Element>() ;
      public List<Element> listOfVAVs = new List<Element>() ;
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

    private static IList<Element> GetAllSpaces( Document document )
    {
      ElementCategoryFilter filter = new(BuiltInCategory.OST_MEPSpaces) ;
      FilteredElementCollector collector = new(document) ;
      IList<Element> spaces = collector.WherePasses( filter ).WhereElementIsNotElementType().ToElements() ;
      return spaces ;
    }

    bool HasBoundingBox( Element elm )
    {
      return elm.get_BoundingBox( elm.Document.ActiveView ) != null ;
    }
    
    private (bool Result, object? State) OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor )
    {
      IList<Element> spaces = GetAllSpaces( uiDocument.Document ).Where( space => space.HasParameter( BranchNumberParameter.BranchNumber ) ).ToArray() ;

      foreach ( var space in spaces ) {
        if ( ! HasBoundingBox( space ) ) {
          return ( false, $"`{space.Name}` have not bounding box." ) ;
        }
      }

      if ( ! RoundDuctTypeExists( uiDocument.Document ) )
        return ( false, "There no RoundDuct family in the document." ) ;

      ConnectorPicker.IPickResult iPickResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, true, "Dialog.Commands.Routing.CreateFASUAndVAVAutomaticallyCommand.PickConnector", null, GetAddInType() ) ;
      if ( iPickResult.PickedConnector != null && CreateFASUAndVAVAutomatically( uiDocument.Document, iPickResult.PickedConnector, spaces ) == Result.Succeeded ) {
        TaskDialog.Show( "FASUとVAVの自動配置", "FASUとVAVを配置しました。" ) ;
      }

      return ( true, null ) ;
    }

    private AddInType GetAddInType() => AppCommandSettings.AddInType ;

    private RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    // コネクタが揃うように VAV, FASU の高さを決める
    private static void CalcFASUAndVAVHeight( Connector rootConnector, double fasuInConnectorHeight, double vavInConnectorHeight, double vavOutConnectorHeight, out double heightOfFASU, out double heightOfVAV )
    {
      var baseHeight = rootConnector.Origin.Z ;
      heightOfVAV = ( baseHeight - vavInConnectorHeight ) ;
      heightOfFASU = ( heightOfVAV + vavOutConnectorHeight - fasuInConnectorHeight ) ;
    }

    private static Result CreateFASUAndVAVAutomatically( Document document, Connector pickedConnector, IList<Element> spaces )
    {
      var maintainer = new FASUAndVAVMaintainerForTTE() ;
      var ( error, errorMessage ) = maintainer.Setup( document, pickedConnector.CoordinateSystem.BasisZ.To3dDirection() ) ;

      using ( Transaction tr = new(document) ) {
        tr.Start( "Create FASUs and VAVs Automatically" ) ;

        maintainer.Execute( pickedConnector.Origin.To3dPoint(), pickedConnector.CoordinateSystem.BasisZ.To3dDirection(), pickedConnector.Origin.Z ) ;
        tr.Commit() ;
      }

      return Result.Succeeded ;
    }

    private static double GetComponentOfRootConnectorNormal( IConnector rootConnector, LocationPoint targetConnectorPos )
    {
      var rootConnectorNormalDirection = rootConnector.CoordinateSystem.BasisZ.To3dDirection() ;
      var rootConnectorPos3d = rootConnector.Origin.To3dPoint() ;
      var targetConnectorPos3d = targetConnectorPos.Point.To3dPoint() ;
      var componentOfRootConnectorNormal = Vector3d.Dot( targetConnectorPos3d - rootConnectorPos3d, rootConnectorNormalDirection ) ;
      return componentOfRootConnectorNormal ;
    }

    private static Duct? CreateDuctConnectionFASUAndVAV( Document document, Connector connectorOfFASU, Connector connectorOfVAV, ElementId levelId )
    {
      var collector = new FilteredElementCollector( document ).OfClass( typeof( DuctType ) ).WhereElementIsElementType().AsEnumerable().OfType<DuctType>() ;
      var ductTypes = collector.Where( e => e.Shape == ConnectorProfileType.Round ).ToArray() ;
      var ductType = ductTypes.FirstOrDefault( e => e.PreferredJunctionType == JunctionType.Tee ) ?? ductTypes.FirstOrDefault() ;
      return ductType != null ? Duct.Create( document, ductType.Id, levelId, connectorOfVAV, connectorOfFASU ) : null ;
    }

    private static bool RoundDuctTypeExists( Document document )
    {
      var collector = new FilteredElementCollector( document ).OfClass( typeof( DuctType ) ).AsEnumerable().OfType<DuctType>() ;
      return collector.Any( e => e.Shape == ConnectorProfileType.Round ) ;
    }

    #region SubFunctionsForRotation

    
    private static bool IsVavLocatedBehindConnector( Document document, Element instanceOfVAV, Connector instanceOfConnector )
    {
      BoundingBoxXYZ boxOfVAV = instanceOfVAV.get_BoundingBox( document.ActiveView ) ;
      if ( boxOfVAV == null ) return false ;

      var connectorPosition = instanceOfConnector.Origin.To3dPoint() ;
      var connectorNormal = instanceOfConnector.CoordinateSystem.BasisZ.To3dDirection() ;

      // コネクタの向いている方向の成分で比較したときに、VAVのBoxの角が1つでもコネクタ位置よりも小さければ後方とみなす.
      return boxOfVAV.ToBox3d().Vertices().Any( boxCorner => Vector3d.Dot( boxCorner - connectorPosition, connectorNormal ) < 0 ) ;
    }

    #endregion
  }
}