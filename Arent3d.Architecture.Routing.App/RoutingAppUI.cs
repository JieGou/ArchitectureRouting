using System ;
using Arent3d.Architecture.Routing.App.Commands ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App
{
  /// <summary>
  /// Registers UI components of auto routing application.
  /// </summary>
  public class RoutingAppUI
  {
    private const string RibbonTabName = "Routing Assist" ;

    private static readonly (string Key, string Title) InitPanel = ( Key: "arent3d.architecture.routing.init", Title: "Initialization" ) ;
    private static readonly (string Key, string Title) RoutingPanel = ( Key: "arent3d.architecture.routing.routing", Title: "Routing" ) ;

    public static void SetupRibbon( UIControlledApplication app )
    {
      var tab = app.CreateRibbonTabEx( RibbonTabName ) ;
      {
        var initPanel = tab.CreateRibbonPanel( InitPanel.Key, InitPanel.Title ) ;
        initPanel.AddButton<InitializeCommand>() ;
        initPanel.AddButton<ShowRoutingViewsCommand>() ;
      }
      {
        var routingPanel = tab.CreateRibbonPanel( RoutingPanel.Key, RoutingPanel.Title ) ;
        routingPanel.AddButton<RackCommand>() ;
        routingPanel.AddButton<RouteCommand>() ;
      }

      // TODO
    }
  }
}