using System ;
using System.Collections.Generic;
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
      var text = GetDisplayName( commandClass ) ;
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
    
    /// <summary>
    /// Add Pulldown Button
    /// </summary>
    /// <param name="ribbonPanel"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <returns></returns>
    public static RibbonButton AddPulldownButton<TCommand>( this RibbonPanel ribbonPanel ) where TCommand : IExternalCommand
    {
      var assemblyName = Assembly.GetCallingAssembly().GetName().Name ;

      PulldownButton pulldownButton = (PulldownButton) ribbonPanel.AddItem(CreatePulldownButton<TCommand>(assemblyName));
      
      pulldownButton.AddPushButton(CreateButton<TCommand>( assemblyName , "1 inch"));
      pulldownButton.AddPushButton(CreateButton<TCommand>( assemblyName , "2 inch"));

      return pulldownButton ;
    }

    /// <summary>
    /// Create PulldownButtonData by TButtonCommand
    /// </summary>
    /// <param name="assemblyName"></param>
    /// <typeparam name="TButtonCommand"></typeparam>
    /// <returns></returns>
    private static PulldownButtonData CreatePulldownButton<TButtonCommand>(string assemblyName) where TButtonCommand : IExternalCommand
    {
      var commandClass = typeof( TButtonCommand ) ;

      var name = commandClass.FullName!.ToSnakeCase() ;
      var text = GetDisplayName( commandClass ) ;
      var description = commandClass.GetCustomAttribute<DescriptionAttribute>()?.Description ;

      var pullButtonData = new PulldownButtonData(name, text);

      return pullButtonData;
    }
    
    /// <summary>
    /// Create Button with content string
    /// </summary>
    /// <param name="assemblyName"></param>
    /// <param name="content"></param>
    /// <typeparam name="TButtonCommand"></typeparam>
    /// <returns></returns>
    private static PushButtonData CreateButton<TButtonCommand>(string assemblyName, string content) where TButtonCommand : IExternalCommand
    {
      var commandClass = typeof(TButtonCommand);

      var name = content;
      var text = content;
      var description = commandClass.GetCustomAttribute<DescriptionAttribute>()?.Description;
      
      var buttonData = new PushButtonData(name, text, commandClass.Assembly.Location, commandClass.FullName);

      foreach (var attr in commandClass.GetCustomAttributes<ImageAttribute>())
      {
        switch (attr.ImageType)
        {
          case ImageType.Normal:
            buttonData.Image = ToImageSource(assemblyName, attr);
            break;
          case ImageType.Large:
            buttonData.LargeImage = ToImageSource(assemblyName, attr);
            break;
          case ImageType.Tooltip:
            buttonData.ToolTipImage = ToImageSource(assemblyName, attr);
            break;
          default: break;
        }
      }
      if ( null != description ) {
        buttonData.LongDescription = description ;
      }

      return buttonData ;
    }

    /// <summary>
    /// Add ComboBox
    /// </summary>
    /// <param name="ribbonPanel"></param>
    /// <param name="???"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <returns></returns>
    public static RibbonItem AddComboBox<TCommand>( this RibbonPanel ribbonPanel  ) where TCommand : IExternalCommand
    {
      var assemblyName = Assembly.GetCallingAssembly().GetName().Name ;

      ComboBox comboBox = (ComboBox) ribbonPanel.AddItem(CreateComboBox<TCommand>(assemblyName));
      
      comboBox.AddItem(CreateComboBoxMember<TCommand>( assemblyName , "101_SA給気"));
      comboBox.AddItem(CreateComboBoxMember<TCommand>( assemblyName , "101_SA給気(高圧)"));
      
      comboBox.CurrentChanged += new EventHandler<Autodesk.Revit.UI.Events.ComboBoxCurrentChangedEventArgs>(comboBx_CurrentChanged);

      return comboBox ;
    }
    
    /// <summary>
    /// Event handler for the above combo box 
    /// </summary>    
    static void comboBx_CurrentChanged(object sender, Autodesk.Revit.UI.Events.ComboBoxCurrentChangedEventArgs e)
    {
      // Cast sender as TextBox to retrieve text value
      ComboBox combodata = (ComboBox)sender;
      ComboBoxMember member = combodata.Current;
      TaskDialog.Show("Combobox Selection", "Your new selection: " + member.ItemText);
    }
    
    private static ComboBoxData CreateComboBox<TButtonCommand>(string assemblyName) where TButtonCommand : IExternalCommand
    {
      var commandClass = typeof( TButtonCommand ) ;

      var name = commandClass.FullName!.ToSnakeCase() ;

      var comboBoxData = new ComboBoxData(name);
      comboBoxData.ToolTip = "Select a Size";
      comboBoxData.LongDescription = "select a size you want to modify";

      return comboBoxData;
    }

    private static ComboBoxMemberData CreateComboBoxMember<TButtonCommand>(string assemblyName, string content) where TButtonCommand : IExternalCommand
    {
      var commandClass = typeof(TButtonCommand);

      var name = content;
      var text = content;
      var description = commandClass.GetCustomAttribute<DescriptionAttribute>()?.Description;

      var comboBoxMember = new ComboBoxMemberData(name, text);

      foreach (var attr in commandClass.GetCustomAttributes<ImageAttribute>())
      {
        switch (attr.ImageType)
        {
          case ImageType.Normal:
            comboBoxMember.Image = ToImageSource(assemblyName, attr);
            break;
          case ImageType.Tooltip:
            comboBoxMember.ToolTipImage = ToImageSource(assemblyName, attr);
            break;
          default: break;
        }
      }
      if ( null != description ) {
        comboBoxMember.LongDescription = description ;
      }

      return comboBoxMember ;
    }
    

    private static string GetDisplayName( Type commandClass )
    {
      return commandClass.GetCustomAttribute<DisplayNameKeyAttribute>()?.GetApplicationString()
             ?? commandClass.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
             ?? commandClass.Name.SeparateByWords() ;
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