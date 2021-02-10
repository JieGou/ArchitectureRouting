using Arent3d.Architecture.Routing.App.Commands ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App
{
  partial class RoutingAppUI
  {
    private const string RibbonTabName = "Routing Assist" ;

    private static readonly (string Key, string Title) InitPanel = ( Key: "arent3d.architecture.routing.init", Title: "Initialization" ) ;
    private static readonly (string Key, string Title) RoutingPanel = ( Key: "arent3d.architecture.routing.routing", Title: "Routing" ) ;
    private static readonly (string Key, string Title) PassPointPanel = ( Key: "arent3d.architecture.routing.passpoint", Title: "Pass Point" ) ;


    private readonly RibbonButton _initializeCommandButton ;
    private readonly RibbonButton _showRoutingViewsCommandButton ;

    private readonly RibbonButton _pickRoutingCommandButton ;
    private readonly RibbonButton _fileRoutingCommandButton ;

    private readonly RibbonButton _allReRouteCommandButton ;
    private readonly RibbonButton _deleteAllRoutesCommandButton ;
    private readonly RibbonButton _exportRoutingCommandButton ;

    private readonly RibbonButton _insertPassPointCommandButton ;


    private RoutingAppUI( UIControlledApplication application )
    {
      var tab = application.CreateRibbonTabEx( RibbonTabName ) ;
      {
        var initPanel = tab.CreateRibbonPanel( InitPanel.Key, InitPanel.Title ) ;
        _initializeCommandButton = initPanel.AddButton<InitializeCommand>() ;
        _showRoutingViewsCommandButton = initPanel.AddButton<ShowRoutingViewsCommand>() ;
      }
      {
        var routingPanel = tab.CreateRibbonPanel( RoutingPanel.Key, RoutingPanel.Title ) ;
        _pickRoutingCommandButton = routingPanel.AddButton<PickRoutingCommand>() ;
        _fileRoutingCommandButton = routingPanel.AddButton<FileRoutingCommand>() ;
        routingPanel.AddSeparator() ;
        _allReRouteCommandButton = routingPanel.AddButton<AllReRouteCommand>() ;
        _deleteAllRoutesCommandButton = routingPanel.AddButton<DeleteAllRoutesCommand>() ;
        routingPanel.AddSeparator() ;
        _exportRoutingCommandButton = routingPanel.AddButton<ExportRoutingCommand>() ;
      }
      {
        var passPointPanel = tab.CreateRibbonPanel( PassPointPanel.Key, PassPointPanel.Title ) ;
        _insertPassPointCommandButton = passPointPanel.AddButton<InsertPassPointCommand>() ;
        passPointPanel.AddSeparator() ;
      }

      InitializeRibbon() ;
    }

    private void InitializeRibbon()
    {
      _initializeCommandButton.Enabled = true ;
      _showRoutingViewsCommandButton.Enabled = false ;

      _pickRoutingCommandButton.Enabled = false ;
      _fileRoutingCommandButton.Enabled = false ;
      _allReRouteCommandButton.Enabled = false ;
      _deleteAllRoutesCommandButton.Enabled = false ;
      _exportRoutingCommandButton.Enabled = false ;

      _insertPassPointCommandButton.Enabled = false ;
    }

    public partial void UpdateUI( Document document, AppUIUpdateType updateType )
    {
      if ( updateType == AppUIUpdateType.Finish ) {
        InitializeRibbon() ;
        return ;
      }

      var setupIsDone = document.RoutingSettingsAreInitialized() ;

      _initializeCommandButton.Enabled = ! setupIsDone ;
      _showRoutingViewsCommandButton.Enabled = setupIsDone ;

      _pickRoutingCommandButton.Enabled = setupIsDone ;
      _fileRoutingCommandButton.Enabled = setupIsDone ;
      _allReRouteCommandButton.Enabled = setupIsDone ;
      _deleteAllRoutesCommandButton.Enabled = setupIsDone ;
      _exportRoutingCommandButton.Enabled = setupIsDone ;

      _insertPassPointCommandButton.Enabled = setupIsDone ;
    }
  }
}