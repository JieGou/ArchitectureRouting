using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.Electrical.App.Commands ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.PassPoint ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.BranchPoint ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Routing ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Rack ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Events ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App
{
  partial class RoutingAppUI
  {
    private const string RibbonTabNameKey = "Electrical.App.Routing.TabName"  ;

    private static readonly (string Key, string TitleKey) InitPanel = ( Key: "arent3d.architecture.routing.init", TitleKey: "Electrical.App.Panels.Routing.Initialize" ) ;
    private static readonly (string Key, string TitleKey) RoutingPanel = ( Key: "arent3d.architecture.routing.routing", TitleKey: "Electrical.App.Panels.Routing.Routing" ) ;
    private static readonly (string Key, string TitleKey) RackPanel = ( Key: "arent3d.architecture.routing.rack", TitleKey: "Electrical.App.Panels.Routing.Racks" ) ;
    private static readonly (string Key, string TitleKey) MonitorPanel = ( Key: "arent3d.architecture.routing.monitor", TitleKey: "Electrical.App.Panels.Routing.Monitor" ) ;

    private readonly RibbonButton _initializeCommandButton ;
    private readonly RibbonButton _showRoutingViewsCommandButton ;

    private readonly RibbonButton _pickRoutingCommandButton ;

    private readonly RibbonButton _pickAndReRouteCommandButton ;
    private readonly RibbonButton _allReRouteCommandButton ;

    private readonly RibbonButton _insertPassPointCommandButton ;
    private readonly RibbonButton _insertBranchPointCommandButton ; //just show dialog

    private readonly RibbonButton _eraseSelectedRoutesCommandButton ;
    private readonly RibbonButton _eraseAllRoutesCommandButton ;

    private readonly RibbonButton _replaceFromToCommandButton ; //just show dialog
    private readonly RibbonButton _showFromToWindowCommandButton ;
    private readonly RibbonButton _showFromToTreeCommandButton ;

    private readonly RibbonButton _importRacksCommandButton ;
    private readonly RibbonButton _exportRacksCommandButton ;
    private readonly RibbonButton _eraseAllRacksCommandButton ;
    private readonly RibbonButton _rackGuidCommanddButton;

    private readonly RibbonButton _monitorSelectionCommandButton ;

    private readonly RegisterFromToTreeCommand _registerFromToTreeCommand;

    
    
    private RoutingAppUI( UIControlledApplication application )
    {
      var tab = application.CreateRibbonTabEx( ToDisplayName( RibbonTabNameKey ) ) ;
      {
        var initPanel = tab.CreateRibbonPanel( InitPanel.Key, ToDisplayName( InitPanel.TitleKey ) ) ;
        _initializeCommandButton = initPanel.AddButton<InitializeCommand>() ;
        _showRoutingViewsCommandButton = initPanel.AddButton<ShowRoutingViewsCommand>() ;
      }
      {
        var routingPanel = tab.CreateRibbonPanel( RoutingPanel.Key, ToDisplayName( RoutingPanel.TitleKey ) ) ;
        _pickRoutingCommandButton = routingPanel.AddButton<PickRoutingCommand>() ;
        _pickAndReRouteCommandButton = routingPanel.AddButton<PickAndReRouteCommand>() ;
        _allReRouteCommandButton = routingPanel.AddButton<AllReRouteCommand>() ;

        _insertPassPointCommandButton = routingPanel.AddButton<InsertPassPointCommand>() ;
        _insertBranchPointCommandButton = routingPanel.AddButton<InsertBranchPointCommand>() ;

        _eraseSelectedRoutesCommandButton = routingPanel.AddButton<EraseSelectedRoutesCommand>() ;
        _eraseAllRoutesCommandButton = routingPanel.AddButton<EraseAllRoutesCommand>() ;

        _replaceFromToCommandButton = routingPanel.AddButton<ReplaceFromToCommand>() ;
        _showFromToWindowCommandButton = routingPanel.AddButton<ShowFrom_ToWindowCommand>() ;
        _showFromToTreeCommandButton = routingPanel.AddButton<ShowFromToTreeCommand>() ;
        
      }
      {
        var rackPanel = tab.CreateRibbonPanel( RackPanel.Key, ToDisplayName( RackPanel.TitleKey ) ) ;
        _importRacksCommandButton = rackPanel.AddButton<ImportRacksCommand>() ;
        _exportRacksCommandButton = rackPanel.AddButton<ExportRacksCommand>() ;
        _eraseAllRacksCommandButton = rackPanel.AddButton<EraseAllRacksCommand>() ;
        _rackGuidCommanddButton = rackPanel.AddButton<RackGuidCommand>();
      }
      {
        var monitorPanel = tab.CreateRibbonPanel( MonitorPanel.Key, ToDisplayName( MonitorPanel.TitleKey ) ) ;
        _monitorSelectionCommandButton = monitorPanel.AddButton<MonitorSelectionCommand>( "Arent3d.Architecture.Routing.Mechanical.App.Commands.Enabler.MonitorSelectionCommandEnabler" ) ;
      }

      _registerFromToTreeCommand = new RegisterFromToTreeCommand(application) ;

      application.ControlledApplication.ApplicationInitialized += DockablePaneRegisters;
      application.ControlledApplication.ApplicationInitialized += new EventHandler<ApplicationInitializedEventArgs>( MonitorSelectionApplicationEvent.MonitorSelectionApplicationInitialized ) ;

      InitializeRibbon() ;
    }

    private static string ToDisplayName( string key )
    {
      return key.GetAppStringByKeyOrDefault( null ) ;
    }

    private void InitializeRibbon()
    {
      _initializeCommandButton.Enabled = true ;
      _showRoutingViewsCommandButton.Enabled = false ;

      _pickRoutingCommandButton.Enabled = false ;
      //_fileRoutingCommandButton.Enabled = false ;
      _pickAndReRouteCommandButton.Enabled = false ;
      _allReRouteCommandButton.Enabled = false ;
      _eraseSelectedRoutesCommandButton.Enabled = false ;
      _eraseAllRoutesCommandButton.Enabled = false ;
      //_exportRoutingCommandButton.Enabled = false ;

      _insertPassPointCommandButton.Enabled = false ;
      _insertBranchPointCommandButton.Enabled = false ;

      _replaceFromToCommandButton.Enabled = false ;
      _showFromToWindowCommandButton.Enabled = false ;
      _showFromToTreeCommandButton.Enabled = false ;

      _importRacksCommandButton.Enabled = false ;
      _exportRacksCommandButton.Enabled = false ;
      _eraseAllRacksCommandButton.Enabled = false ;
      _rackGuidCommanddButton.Enabled = false;
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
      //_fileRoutingCommandButton.Enabled = setupIsDone ;
      _pickAndReRouteCommandButton.Enabled = setupIsDone ;
      _allReRouteCommandButton.Enabled = setupIsDone ;
      _eraseSelectedRoutesCommandButton.Enabled = setupIsDone ;
      _eraseAllRoutesCommandButton.Enabled = setupIsDone ;
      //_exportRoutingCommandButton.Enabled = setupIsDone ;

      _insertPassPointCommandButton.Enabled = setupIsDone ;
      _insertBranchPointCommandButton.Enabled = setupIsDone ;

      _replaceFromToCommandButton.Enabled = setupIsDone ;
      _showFromToWindowCommandButton.Enabled = setupIsDone ;
      _showFromToTreeCommandButton.Enabled = setupIsDone ;

      _importRacksCommandButton.Enabled = setupIsDone ;
      _exportRacksCommandButton.Enabled = setupIsDone ;
      _eraseAllRacksCommandButton.Enabled = setupIsDone ;
      _rackGuidCommanddButton.Enabled = setupIsDone;
    }

    private void DockablePaneRegisters( object sender, ApplicationInitializedEventArgs e )
    {
      _registerFromToTreeCommand.Execute( new UIApplication( sender as Autodesk.Revit.ApplicationServices.Application ) ) ;
    }
  }
}