using System ;
using System.ComponentModel ;
using System.Reflection ;
using System.Windows.Media ;
using System.Windows.Media.Imaging ;
using Autodesk.Revit.UI ;

namespace Arent3d.Revit.UI
{
  public static class UIExtensions
  {
    public static RibbonTabEx CreateRibbonTabEx( this UIControlledApplication app, string tabName )
    {
      return new RibbonTabEx( app, tabName ) ;
    }

    public static RibbonButton AddButton<TCommand>( this RibbonPanel ribbonPanel ) where TCommand : IExternalCommand
    {
      var assemblyName = Assembly.GetCallingAssembly().GetName().Name ;

      return (RibbonButton) ribbonPanel.AddItem( CreateButton<TCommand>( assemblyName ) ) ;
    }
    
    private static PushButtonData CreateButton<TButtonCommand>( string assemblyName ) where TButtonCommand : IExternalCommand
    {
      var commandClass = typeof( TButtonCommand ) ;

      var name = commandClass.FullName!.ToSnakeCase() ;
      var text = commandClass.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? commandClass.Name.SeparateByWords() ;
      var description = commandClass.GetCustomAttribute<DescriptionAttribute>()?.Description ;

      var buttonData = new PushButtonData( name, text, commandClass.Assembly.Location, commandClass.FullName ) ;

      foreach ( var attr in commandClass.GetCustomAttributes<ImageAttribute>() ) {
        switch ( attr.ImageType ) {
          case ImageType.Normal : buttonData.Image = ToImageSource( assemblyName, attr ) ; break ;
          case ImageType.Large : buttonData.LargeImage = ToImageSource( assemblyName, attr ) ; break ;
          case ImageType.Tooltip : buttonData.ToolTipImage = ToImageSource( assemblyName, attr ) ; break ;
          default : break ;
        }
      }

      if ( null != description ) {
        buttonData.LongDescription = description ;
      }

      return buttonData ;
    }

    private static ImageSource? ToImageSource( string assemblyName, ImageAttribute attr )
    {
      try {
        var uri = new Uri( "pack://application:,,,/" + assemblyName + ";component/" + attr.ResourceName ) ;
        return new BitmapImage( uri ) ;
      }
      catch ( Exception ) {
        return null ;
      }
    }
  }
}