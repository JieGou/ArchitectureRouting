using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.UI.Selection ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.AutoRoutingAnemostatCommand", DefaultString = "Auto Routing\nAnemostat" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class AutoRoutingAnemostatCommand : RoutingCommandBase<AutoRoutingAnemostatCommand.PickState>
  {
    public record PickState( MechanicalSystem FasuMechanicalSytem, Connector InConnector, IList<Connector> NotInConnector, IList<Connector> AnemoConnectors ) ;

    protected override string GetTransactionNameKey()
    {
      return "TransactionName.Commands.Routing.AutoRoutingAnemostat" ;
    }

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view )
    {
      return AppCommandSettings.CreateRoutingExecutor( document, view ) ;
    }

    protected override OperationResult<PickState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var fasuFamilyInstance = SelectFasu( uiDocument ) ;
      var fasuMechanicalSystem = fasuFamilyInstance.GetConnectors().FirstOrDefault( c => c.Direction == FlowDirectionType.In )?.MEPSystem as MechanicalSystem ;
      if ( fasuMechanicalSystem == null ) {
        return OperationResult<PickState>.FailWithMessage( "UiDocument.Commands.Routing.AutoRouteAnemostat.Error.Message.Route.Failed".GetAppStringByKeyOrDefault( "Auto routing anemostat failed! Please create Duct System for FASU before route." ) ) ;
      }

      // 全てFASUのInコネクタ以外を取得
      var fasuNotInConnectors = fasu.GetConnectors().Where( connector => connector.Direction != FlowDirectionType.In && ! connector.IsConnected ).ToList() ;
      var fasuInConnector = fasu.GetConnectors().FirstOrDefault( connector => connector.Direction == FlowDirectionType.In ) ;
      if ( fasuInConnector == null || ! fasuNotInConnectors.Any() ) {
        return OperationResult<PickState>.FailWithMessage( "UiDocument.Commands.Routing.AutoRouteAnemostat.Error.inconnector.notfound".GetAppStringByKeyOrDefault( "Auto routing anemostat failed! FASU hasn't got in connector or hasn't got other connector." ) ) ;
      }

      var anemoConnectors = TTEUtil.GetAllAnemoConnectors( uiDocument.Document, fasuInConnector ) ;
      if ( anemoConnectors.Count > fasuNotInConnectors.Count ) {
        return OperationResult<PickState>.FailWithMessage( "UiDocument.Commands.Routing.AutoRouteAnemostat.Error.connector.less.than".GetAppStringByKeyOrDefault( "Auto routing anemostat failed! Because FASU's connector is less than anemostat." ) ) ;
      }

      return new OperationResult<PickState>( new PickState( fasuMechanicalSystem, fasuInConnector, fasuNotInConnectors, anemoConnectors ) ) ;
    }

    private static FamilyInstance SelectFasu( UIDocument uiDocument )
    {
      // Todo get fasu only
      var ductAccessoryFilter = new DuctAccessoryPickFilter() ;

      while ( true ) {
        var pickedObject = uiDocument.Selection.PickObject( ObjectType.Element, ductAccessoryFilter, "UiDocument.Selection.PickObject.Fasu".GetAppStringByKeyOrDefault( "Pick the FASU of a auto route Anemostat." ) ) ;
        var element = uiDocument.Document.GetElement( pickedObject.ElementId ) ;
        var fasuFamilyInstance = element as FamilyInstance ;
        if ( null == fasuFamilyInstance ) continue ;
        return fasuFamilyInstance ;
      }
    }

    private class DuctAccessoryPickFilter : ISelectionFilter
    {
      public bool AllowElement( Element e )
      {
        return ( e.Category.Id.IntegerValue.Equals( (int) BuiltInCategory.OST_DuctAccessory ) ) ;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return false ;
      }
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, PickState pickState )
    {
      document.Regenerate() ; // Apply Arent-RoundDuct-Diameter
      RouteGenerator.CorrectEnvelopes( document ) ;
      var (fasuMechanicalSytem, fasuInConnector, fasuNotInConnectors, anemoConnectors) = pickState ;
      var anemostatRouter = new AutoRoutingAnemostat( document, fasuMechanicalSytem, fasuInConnector, fasuNotInConnectors, anemoConnectors ) ;
      return anemostatRouter.Execute().EnumerateAll() ;
    }
  }
}