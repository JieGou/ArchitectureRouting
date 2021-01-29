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


    private readonly RibbonButton _initializeCommandButton ;
    private readonly RibbonButton _showRoutingViewsCommandButton ;

    private readonly RibbonButton _pickRoutingCommandButton ;
    private readonly RibbonButton _fileRoutingCommandButton ;


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
      }

      InitializeRibbon() ;
    }

    private void InitializeRibbon()
    {
      _showRoutingViewsCommandButton.Enabled = false ;

      _pickRoutingCommandButton.Enabled = false ;
      _fileRoutingCommandButton.Enabled = false ;
    }

    public partial void UpdateRibbon( Document document, UpdateType updateType )
    {
      if ( updateType == UpdateType.Finish ) {
        InitializeRibbon() ;
        return ;
      }

      var setupIsDone = document.RoutingSettingsAreInitialized() ;
      _initializeCommandButton.Enabled = ! setupIsDone ;
      _showRoutingViewsCommandButton.Enabled = setupIsDone ;
      _pickRoutingCommandButton.Enabled = setupIsDone ;
      _fileRoutingCommandButton.Enabled = setupIsDone ;
    }
  }
}