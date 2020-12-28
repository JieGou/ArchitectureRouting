using System ;
using Arent3d.Revit ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App
{
  /// <summary>
  /// Registers UI components of auto routing application.
  /// </summary>
  public class RoutingAppUI
  {
    private const string RibbonTabName = "Route Assist" ;

    private const string SamplePanelName = "arent3d.architecture.test" ;

    private const string ButtonName = "arent3d.architecture.test.button" ;
    private const string ButtonText = "Test" ;

    public static void SetupRibbon( UIControlledApplication app )
    {
      app.CreateRibbonTab( RibbonTabName ) ;
      var ribbonPanel = app.CreateRibbonPanel( RibbonTabName, SamplePanelName ) ;
      ribbonPanel.Title = "Test" ;

      var item = ribbonPanel.AddItem( CreateButton( ButtonName, ButtonText, typeof( Commands.TestCommand ) ) ) ;

      // TODO
    }

    private static PushButtonData CreateButton( string name, string text, Type commandClass )
    {
      if ( ! commandClass.IsExternalCommand() ) throw new ArgumentException( $"{commandClass.FullName} is not a command type." ) ;

      return new PushButtonData( name, text, commandClass.Assembly.Location, commandClass.FullName ) ;
    }
  }
}