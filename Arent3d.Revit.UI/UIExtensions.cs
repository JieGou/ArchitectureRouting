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

    /// <summary>
    /// Add PushButton. When class <see cref="TCommand"/> implements <see cref="IExternalCommandAvailability"/>, it will set into <see cref="PushButtonData"/>'s <see cref="PushButtonData.AvailabilityClassName"/> property.
    /// </summary>
    /// <param name="ribbonPanel"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <returns></returns>
    public static RibbonButton AddButton<TCommand>( this RibbonPanel ribbonPanel ) where TCommand : IExternalCommand
    {
      if ( typeof( TCommand ).HasInterface<IExternalCommandAvailability>() ) {
        return ribbonPanel.AddButton<TCommand>( typeof( TCommand ).FullName ) ;
      }

      var assemblyName = Assembly.GetCallingAssembly().GetName().Name ;
      return (RibbonButton) ribbonPanel.AddItem( CreateButton<TCommand>( assemblyName ) ) ;
    }

    /// <summary>
    /// Add PushButton with AvailabilityClassName.
    /// </summary>
    /// <param name="ribbonPanel"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <typeparam name="TCommandAvailability"></typeparam>
    /// <returns></returns>
    public static RibbonButton AddButton<TCommand, TCommandAvailability>( this RibbonPanel ribbonPanel ) where TCommand : IExternalCommand where TCommandAvailability : IExternalCommandAvailability
    {
      return ribbonPanel.AddButton<TCommand>( typeof( TCommandAvailability ).FullName ) ;
    }

    /// <summary>
    /// Add PushButton with <see cref="PushButtonData"/>'s <see cref="PushButtonData.AvailabilityClassName"/> property.
    /// </summary>
    /// <param name="ribbonPanel"></param>
    /// <param name="availabilityClassName"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <returns></returns>
    public static RibbonButton AddButton<TCommand>( this RibbonPanel ribbonPanel, string availabilityClassName ) where TCommand : IExternalCommand
    {
      var assemblyName = Assembly.GetCallingAssembly().GetName().Name ;
      var pushButtonData = CreateButton<TCommand>( assemblyName ) ;
      pushButtonData.AvailabilityClassName = availabilityClassName ;

      return (RibbonButton) ribbonPanel.AddItem( pushButtonData ) ;
    }
    
    public static ImageSource? GetImageFromName( string imageName )
    {
      var assemblyName = Assembly.GetCallingAssembly().GetName().Name ;
      try {
        var uri = new Uri( "pack://application:,,,/" + assemblyName + ";component/resources/" + imageName ) ;
        return new BitmapImage( uri ) ;
      }
      catch ( Exception ) {
        return null ;
      }
    }

    private static PushButtonData CreateButton<TButtonCommand>( string assemblyName ) where TButtonCommand : IExternalCommand
    {
      var commandClass = typeof( TButtonCommand ) ;

      var name = commandClass.FullName!.ToSnakeCase() ;
      var text = GetDisplayName( commandClass ) ;
      var description = commandClass.GetCustomAttribute<DescriptionAttribute>()?.Description ;

      var buttonData = new PushButtonData( name, text, commandClass.Assembly.Location, commandClass.FullName ) ;

      foreach ( var attr in commandClass.GetCustomAttributes<ImageAttribute>() ) {
        switch ( attr.ImageType ) {
          case ImageType.Normal :
            buttonData.Image = ToImageSource( assemblyName, attr ) ;
            break ;
          case ImageType.Large :
            buttonData.LargeImage = ToImageSource( assemblyName, attr ) ;
            break ;
          case ImageType.Tooltip :
            buttonData.ToolTipImage = ToImageSource( assemblyName, attr ) ;
            break ;
          default : break ;
        }
      }

      if ( null != description ) {
        buttonData.LongDescription = description ;
      }

      return buttonData ;
    }

    private static string GetDisplayName( Type commandClass )
    {
      return commandClass.GetCustomAttribute<DisplayNameKeyAttribute>()?.GetApplicationString() ?? commandClass.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? commandClass.Name.SeparateByWords() ;
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

    public static bool CanPostCommand<TCommand>( this UIApplication app ) where TCommand : IExternalCommand
    {
      if ( typeof( TCommand ).GetCustomAttribute<RevitAddinAttribute>() is not { } attr ) return false ;

      var commandId = RevitCommandId.LookupCommandId( attr.Guid.ToString() ) ;
      if ( null == commandId ) return false ;

      return app.CanPostCommand( commandId ) ;
    }

    public static void PostCommand<TCommand>( this UIApplication app ) where TCommand : IExternalCommand
    {
      if ( typeof( TCommand ).GetCustomAttribute<RevitAddinAttribute>() is not { } attr ) throw new InvalidOperationException() ;

      var commandId = RevitCommandId.LookupCommandId( attr.Guid.ToString() ) ;
      if ( null == commandId ) return ;

      app.PostCommand( commandId ) ;
    }
  }
}