using System ;
using System.Collections.Generic ;
using System.Windows ;
using Arent3d.Architecture.Routing.App.Commands ;
using Arent3d.Architecture.Routing.App.Commands.Initialization ;
using Arent3d.Architecture.Routing.App.Commands.PassPoint ;
using Arent3d.Architecture.Routing.App.Commands.BranchPoint ;
using Arent3d.Architecture.Routing.App.Commands.Routing ;
using Arent3d.Architecture.Routing.App.Commands.Rack ;
using Arent3d.Architecture.Routing.App.Commands.Selecting ;
using Arent3d.Architecture.Routing.App.Forms ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.ApplicationServices ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Events ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App
{
  partial class RoutingAppUI
  {
    private const string RibbonTabNameKey = "App.Routing.TabName" ;

    private static readonly (string Key, string TitleKey) InitPanel = ( Key: "arent3d.architecture.routing.init", TitleKey: "App.Panels.Routing.Initialize" ) ;
    private static readonly (string Key, string TitleKey) RoutingPanel = ( Key: "arent3d.architecture.routing.routing", TitleKey: "App.Panels.Routing.Routing" ) ;
    private static readonly (string Key, string TitleKey) RackPanel = ( Key: "arent3d.architecture.routing.rack", TitleKey: "App.Panels.Routing.Racks" ) ;

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
    private static RibbonButton? _showFromToTreeCommandButton ;

    private readonly RibbonButton _fileRoutingCommandButton ;
    private readonly RibbonButton _exportRoutingCommandButton ;

    private readonly RibbonButton _importRacksCommandButton ;
    private readonly RibbonButton _exportRacksCommandButton ;
    private readonly RibbonButton _eraseAllRacksCommandButton ;

    private readonly RibbonButton _modifySelectedFromToCommandButton ;

    private readonly RibbonButton _monitorSelectionCommandButton ;


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
        _modifySelectedFromToCommandButton = routingPanel.AddButton<GetSelectedFromToInfoCommand>() ;
        _showFromToWindowCommandButton = routingPanel.AddButton<ShowFrom_ToWindowCommand>() ;
        _showFromToTreeCommandButton = routingPanel.AddButton<ShowFromToTreeCommand>() ;

        _fileRoutingCommandButton = routingPanel.AddButton<FileRoutingCommand>() ;
        _exportRoutingCommandButton = routingPanel.AddButton<ExportRoutingCommand>() ;

        _monitorSelectionCommandButton = routingPanel.AddButton<MonitorSelectionCommand>( "Arent3d.Architecture.Routing.App.Commands.Enabler.MonitorSelectionCommandEnabler" ) ;
      }
      {
        var rackPanel = tab.CreateRibbonPanel( RackPanel.Key, ToDisplayName( RackPanel.TitleKey ) ) ;
        _importRacksCommandButton = rackPanel.AddButton<ImportRacksCommand>() ;
        _exportRacksCommandButton = rackPanel.AddButton<ExportRacksCommand>() ;
        _eraseAllRacksCommandButton = rackPanel.AddButton<EraseAllRacksCommand>() ;
      }

      application.ControlledApplication.ApplicationInitialized += DockablePaneRegisters ;
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
      _fileRoutingCommandButton.Enabled = false ;
      _pickAndReRouteCommandButton.Enabled = false ;
      _allReRouteCommandButton.Enabled = false ;
      _eraseSelectedRoutesCommandButton.Enabled = false ;
      _eraseAllRoutesCommandButton.Enabled = false ;
      _exportRoutingCommandButton.Enabled = false ;

      _insertPassPointCommandButton.Enabled = false ;
      _insertBranchPointCommandButton.Enabled = false ;

      _replaceFromToCommandButton.Enabled = false ;
      _showFromToWindowCommandButton.Enabled = false ;
      if ( _showFromToTreeCommandButton != null ) {
        _showFromToTreeCommandButton.Enabled = false ;
      }

      _importRacksCommandButton.Enabled = false ;
      _exportRacksCommandButton.Enabled = false ;
      _eraseAllRacksCommandButton.Enabled = false ;

      _modifySelectedFromToCommandButton.Enabled = false ;
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
      _insertBranchPointCommandButton.Enabled = setupIsDone ;

      _replaceFromToCommandButton.Enabled = setupIsDone ;
      _showFromToWindowCommandButton.Enabled = setupIsDone ;
      if ( _showFromToTreeCommandButton != null ) {
        _showFromToTreeCommandButton.Enabled = setupIsDone ;

      }
      _importRacksCommandButton.Enabled = setupIsDone ;
      _exportRacksCommandButton.Enabled = setupIsDone ;
      _eraseAllRacksCommandButton.Enabled = setupIsDone ;

      _modifySelectedFromToCommandButton.Enabled = setupIsDone ;
    }

    private void DockablePaneRegisters( object sender, ApplicationInitializedEventArgs e )
    {
      var fromToTreeRegisterCommand = new RegisterFromToTreeCommand() ;
      fromToTreeRegisterCommand.Execute( new UIApplication( sender as Autodesk.Revit.ApplicationServices.Application ) ) ;
    }

    /// <summary>
    /// Toggle ShowFromToButton State
    /// </summary>
    /// <param name="isShown"></param>
    public static void ToggleShowFromToTreeCommandButton(bool isShown)
    {
      if ( _showFromToTreeCommandButton != null ) {
        if ( isShown ) {
          _showFromToTreeCommandButton.ItemText = "Shown" ;
        }
        else {
          _showFromToTreeCommandButton.ItemText = "Hide" ;
        }
      }
    }
  }
}