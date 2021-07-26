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
      return ribbonPanel.AddButton<TCommand>( Assembly.GetCallingAssembly() ) ;
    }

    /// <summary>
    /// Add PushButton. When class <see cref="TCommand"/> implements <see cref="IExternalCommandAvailability"/>, it will set into <see cref="PushButtonData"/>'s <see cref="PushButtonData.AvailabilityClassName"/> property.
    /// </summary>
    /// <param name="ribbonPanel"></param>
    /// <param name="resourceAssembly">An assembly which contains image resources.</param>
    /// <typeparam name="TCommand"></typeparam>
    /// <returns></returns>
    public static RibbonButton AddButton<TCommand>( this RibbonPanel ribbonPanel, Assembly resourceAssembly ) where TCommand : IExternalCommand
    {
      var commandClass = typeof( TCommand ) ;

      if ( commandClass.HasInterface<IExternalCommandAvailability>() ) {
        return ribbonPanel.AddButtonImpl( commandClass, commandClass.FullName, resourceAssembly ) ;
      }
      else {
        return ribbonPanel.AddButtonImpl( commandClass, null, resourceAssembly ) ;
      }
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
      return ribbonPanel.AddButton<TCommand, TCommandAvailability>( Assembly.GetCallingAssembly() ) ;
    }

    /// <summary>
    /// Add PushButton with AvailabilityClassName.
    /// </summary>
    /// <param name="ribbonPanel"></param>
    /// <param name="resourceAssembly">An assembly which contains image resources.</param>
    /// <typeparam name="TCommand"></typeparam>
    /// <typeparam name="TCommandAvailability"></typeparam>
    /// <returns></returns>
    public static RibbonButton AddButton<TCommand, TCommandAvailability>( this RibbonPanel ribbonPanel, Assembly resourceAssembly ) where TCommand : IExternalCommand where TCommandAvailability : IExternalCommandAvailability
    {
      return ribbonPanel.AddButtonImpl( typeof( TCommand ), typeof( TCommandAvailability ).FullName, resourceAssembly ) ;
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
      return ribbonPanel.AddButtonImpl( typeof( TCommand ), availabilityClassName, Assembly.GetCallingAssembly() ) ;
    }

    /// <summary>
    /// Add PushButton with <see cref="PushButtonData"/>'s <see cref="PushButtonData.AvailabilityClassName"/> property.
    /// </summary>
    /// <param name="ribbonPanel"></param>
    /// <param name="resourceAssembly">An assembly which contains image resources.</param>
    /// <param name="availabilityClassName"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <returns></returns>
    public static RibbonButton AddButton<TCommand>( this RibbonPanel ribbonPanel, string availabilityClassName, Assembly resourceAssembly ) where TCommand : IExternalCommand
    {
      return ribbonPanel.AddButtonImpl( typeof( TCommand ), availabilityClassName, resourceAssembly ) ;
    }

    internal static RibbonButton AddButtonImpl( this RibbonPanel ribbonPanel, Type commandClass, string? availabilityClassName, Assembly resourceAssembly )
    {
      var pushButtonData = CreateButton( commandClass, resourceAssembly ) ;
      if ( null != availabilityClassName ) {
        pushButtonData.AvailabilityClassName = availabilityClassName ;
      }

      return (RibbonButton) ribbonPanel.AddItem( pushButtonData ) ;
    }

    public static ImageSource? GetImageFromName( string imageName ) => GetImageFromName( Assembly.GetCallingAssembly(), "resources/" + imageName ) ;
    public static ImageSource? GetImageFromName( Assembly assembly, string imageName ) => ToImageSource( assembly, "resources/" + imageName ) ;


    private static PushButtonData CreateButton( Type commandClass, Assembly assembly )
    {
      var name = commandClass.FullName!.ToSnakeCase() ;
      var text = GetDisplayName( commandClass ) ;
      var description = commandClass.GetCustomAttribute<DescriptionAttribute>()?.Description ;

      var buttonData = new PushButtonData( name, text, commandClass.Assembly.Location, commandClass.FullName ) ;

      foreach ( var attr in commandClass.GetCustomAttributes<ImageAttribute>() ) {
        switch ( attr.ImageType ) {
          case ImageType.Normal :
            buttonData.Image = ToImageSource( assembly, attr ) ;
            break ;
          case ImageType.Large :
            buttonData.LargeImage = ToImageSource( assembly, attr ) ;
            break ;
          case ImageType.Tooltip :
            buttonData.ToolTipImage = ToImageSource( assembly, attr ) ;
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

    private static ImageSource? ToImageSource( Assembly assembly, ImageAttribute attr ) => ToImageSource( assembly, attr.ResourceName ) ;

    private static ImageSource? ToImageSource( Assembly assembly, string resourcePath )
    {
      try {
        var uri = new Uri( "pack://application:,,,/" + assembly.GetName().Name + ";component/" + resourcePath ) ;
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