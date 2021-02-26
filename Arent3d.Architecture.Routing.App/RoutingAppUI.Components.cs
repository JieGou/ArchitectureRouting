using Arent3d.Architecture.Routing.App.Commands.Initialization ;
using Arent3d.Architecture.Routing.App.Commands.PassPoint ;
using Arent3d.Architecture.Routing.App.Commands.Routing ;
using Arent3d.Architecture.Routing.App.Commands.Rack ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App
{
  partial class RoutingAppUI
  {
    private const string RibbonTabName = "Routing Assist" ;

    private static readonly (string Key, string Title) InitPanel = ( Key: "arent3d.architecture.routing.init", Title: "Initialize" ) ;
    private static readonly (string Key, string Title) RoutingPanel = ( Key: "arent3d.architecture.routing.routing", Title: "Routing" ) ;
    private static readonly (string Key, string Title) PassPointPanel = ( Key: "arent3d.architecture.routing.passpoint", Title: "Pass Points" ) ;
    private static readonly (string Key, string Title) RackPanel = ( Key: "arent3d.architecture.routing.rack", Title: "Racks" ) ;


    private readonly RibbonButton _initializeCommandButton ;
    private readonly RibbonButton _showRoutingViewsCommandButton ;

    private readonly RibbonButton _pickRoutingCommandButton ;
    private readonly RibbonButton _fileRoutingCommandButton ;

    private readonly RibbonButton _pickAndReRouteCommandButton ;
    private readonly RibbonButton _allReRouteCommandButton ;
    private readonly RibbonButton _eraseSelectedRoutesCommandButton ;
    private readonly RibbonButton _eraseAllRoutesCommandButton ;
    private readonly RibbonButton _exportRoutingCommandButton ;

    private readonly RibbonButton _insertPassPointCommandButton ;

    private readonly RibbonButton _importRacksCommandButton ;
    private readonly RibbonButton _exportRacksCommandButton ;
    private readonly RibbonButton _eraseAllRacksCommandButton ;


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
        _pickAndReRouteCommandButton = routingPanel.AddButton<PickAndReRouteCommand>() ;
        _allReRouteCommandButton = routingPanel.AddButton<AllReRouteCommand>() ;
        routingPanel.AddSeparator() ;
        _eraseSelectedRoutesCommandButton = routingPanel.AddButton<EraseSelectedRoutesCommand>() ;
        _eraseAllRoutesCommandButton = routingPanel.AddButton<EraseAllRoutesCommand>() ;
        routingPanel.AddSeparator() ;
        _exportRoutingCommandButton = routingPanel.AddButton<ExportRoutingCommand>() ;
      }
      {
        var passPointPanel = tab.CreateRibbonPanel( PassPointPanel.Key, PassPointPanel.Title ) ;
        _insertPassPointCommandButton = passPointPanel.AddButton<InsertPassPointCommand>() ;
      }
      {
        var rackPanel = tab.CreateRibbonPanel( RackPanel.Key, RackPanel.Title ) ;
        _importRacksCommandButton = rackPanel.AddButton<ImportRacksCommand>() ;
        _exportRacksCommandButton = rackPanel.AddButton<ExportRacksCommand>() ;
        _eraseAllRacksCommandButton = rackPanel.AddButton<EraseAllRacksCommand>() ;
      }

      InitializeRibbon() ;
    }

    private void InitializeRibbon()
    {
      _initializeCommandButton.Enabled = true ;
      _showRoutingViewsCommandButton.Enabled = false ;

      _pickRoutingCommandButton.Enabled = false ;
      _fileRoutingCommandButton.Enabled = false ;
      _pickAndReRouteCommandButton.Enabled = false ;
      _allReRouteCommandButton.Enabled = false ;
      _eraseSelectedRoutesCommandButton.Enabled = false ;
      _eraseAllRoutesCommandButton.Enabled = false ;
      _exportRoutingCommandButton.Enabled = false ;

      _insertPassPointCommandButton.Enabled = false ;

      _importRacksCommandButton.Enabled = false ;
      _exportRacksCommandButton.Enabled = false ;
      _eraseAllRacksCommandButton.Enabled = false ;
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
      _pickAndReRouteCommandButton.Enabled = setupIsDone ;
      _allReRouteCommandButton.Enabled = setupIsDone ;
      _eraseSelectedRoutesCommandButton.Enabled = setupIsDone ;
      _eraseAllRoutesCommandButton.Enabled = setupIsDone ;
      _exportRoutingCommandButton.Enabled = setupIsDone ;

      _insertPassPointCommandButton.Enabled = setupIsDone ;

      _importRacksCommandButton.Enabled = setupIsDone ;
      _exportRacksCommandButton.Enabled = setupIsDone ;
      _eraseAllRacksCommandButton.Enabled = setupIsDone ;
    }
  }
}