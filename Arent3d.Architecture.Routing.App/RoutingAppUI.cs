using System ;
using System.ComponentModel ;
using System.IO ;
using System.Reflection ;
using System.Windows.Media ;
using System.Windows.Media.Imaging ;
using Arent3d.Architecture.Routing.App.Commands ;
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

    public static void SetupRibbon( UIControlledApplication app )
    {
      app.CreateRibbonTab( RibbonTabName ) ;
      var ribbonPanel = app.CreateRibbonPanel( RibbonTabName, SamplePanelName ) ;
      ribbonPanel.Title = "Test" ;

      var rackCommandItem = ribbonPanel.AddItem( CreateButton<RackCommand>() ) ;

      var routeCommandItem = ribbonPanel.AddItem( CreateButton<RouteCommand>() ) ;
      

      // TODO
    }

    private static PushButtonData CreateButton<TButtonCommand>() where TButtonCommand : IExternalCommand
    {
      var commandClass = typeof( TButtonCommand ) ;

      var name = commandClass.FullName!.ToSnakeCase() ;
      var text = commandClass.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? commandClass.Name.SeparateByWords() ;

      var buttonData = new PushButtonData( name, text, commandClass.Assembly.Location, commandClass.FullName ) ;
      
      foreach ( var attr in commandClass.GetCustomAttributes<ImageAttribute>() ) {
        switch ( attr.ImageType ) {
          case ImageType.Normal : buttonData.Image = ToImageSource( attr ) ; break ;
          case ImageType.Large : buttonData.LargeImage = ToImageSource( attr ) ; break ;
          case ImageType.Tooltip : buttonData.ToolTipImage = ToImageSource( attr ) ; break ;
          default : break ;
        }
      }

      return buttonData ;
    }

    private static ImageSource? ToImageSource( ImageAttribute attr )
    {
      try {
        return new BitmapImage( new Uri( attr.ResourceName, UriKind.Relative ) ) ;
      }
      catch ( FileNotFoundException ) {
        return null ;
      }
    }
  }
}