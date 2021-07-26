using System ;
using System.Collections.Generic ;
using System.Reflection ;
using Arent3d.Revit.UI.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
// ReSharper disable VirtualMemberCallInConstructor

namespace Arent3d.Revit.UI
{

  public abstract class AppUIBase : IAppUIBase
  {
    private readonly Dictionary<Type, (RibbonTabEx, TabAttribute)> _tabs = new() ;
    private readonly Dictionary<Type, (RibbonPanel, PanelAttribute)> _panels = new() ;
    private readonly Dictionary<Type, (RibbonButton, ButtonAttribute)> _buttons = new() ;

    protected abstract string KeyToDisplayText( string key ) ;
    protected abstract bool IsInitialized( Document document ) ;
    
    protected AppUIBase( UIControlledApplication application )
    {
      foreach ( var tabType in GetType().GetNestedTypes( BindingFlags.Public | BindingFlags.NonPublic ) ) {
        if ( tabType.GetCustomAttribute<TabAttribute>() is not { } tabAttribute ) continue ;

        AddTab( application, tabType, tabAttribute ) ;
      }
    }

    private void AddTab( UIControlledApplication application, Type tabType, TabAttribute tabAttribute )
    {
      var tab = application.CreateRibbonTabEx( KeyToDisplayText( tabAttribute.TabNameKey ) ) ;
      _tabs.Add( tabType, ( tab, tabAttribute ) ) ;

      foreach ( var panelType in tabType.GetNestedTypes( BindingFlags.Public | BindingFlags.NonPublic ) ) {
        if ( panelType.GetCustomAttribute<PanelAttribute>() is not { } panelAttribute ) continue ;

        AddPanel( tab, panelType, panelAttribute ) ;
      }
    }

    private void AddPanel( RibbonTabEx tab, Type panelType, PanelAttribute panelAttribute )
    {
      var panel = tab.CreateRibbonPanel( panelAttribute.KeyString, KeyToDisplayText( panelAttribute.TitleKey ) ) ;
      _panels.Add( panelType, ( panel, panelAttribute ) ) ;

      foreach ( var buttonType in panelType.GetNestedTypes( BindingFlags.Public | BindingFlags.NonPublic ) ) {
        if ( buttonType.GetCustomAttribute<ButtonAttribute>() is not { } buttonAttribute ) continue ;
        if ( false == buttonAttribute.CommandType.HasInterface<IExternalCommand>() ) continue ;

        AddButton( panel, buttonType, buttonAttribute ) ;
      }
    }

    private void AddButton( RibbonPanel panel, Type buttonType, ButtonAttribute buttonAttribute )
    {
      var button = panel.AddButtonImpl( buttonAttribute.CommandType, buttonAttribute.AvailabilityType?.FullName, buttonAttribute.TypeInResourceAssembly?.Assembly ?? buttonType.Assembly ) ;
      _buttons.Add( buttonType, ( button, buttonAttribute ) ) ;
    }

    protected virtual void UpdateUIForFamilyDocument( Document document, AppUIUpdateType updateType )
    {
      foreach ( var (tab, tabAttribute) in _tabs.Values ) {
        tab.Visible = ( TabVisibilityMode.None != ( tabAttribute.VisibilityMode & TabVisibilityMode.FamilyDocument ) ) ;
      }
    }

    protected virtual void UpdateUIForNormalDocument( Document document, AppUIUpdateType updateType )
    {
      foreach ( var (tab, tabAttribute) in _tabs.Values ) {
        tab.Visible = ( TabVisibilityMode.None != ( tabAttribute.VisibilityMode & TabVisibilityMode.NormalDocument ) ) ;
      }

      if ( updateType == AppUIUpdateType.Finish ) {
        InitializeRibbon() ;
      }
      else {
        SetInitialized( IsInitialized( document ) ) ;
      }
    }

    private void InitializeRibbon()
    {
      SetInitialized( false ) ;
    }

    private void SetInitialized( bool isInitialized )
    {
      foreach ( var (button, buttonAttribute) in _buttons.Values ) {
        if ( buttonAttribute.InitializeButton ) {
          button.Enabled = ! isInitialized ;
        }
        else if ( buttonAttribute.OnlyInitialized ) {
          button.Enabled = isInitialized ;
        }
      }
    }


    public bool IsDisposed { get ; private set ; }

    ~AppUIBase()
    {
      if ( IsDisposed ) return ;
      IsDisposed = true ;

      ReleaseUnmanagedResources() ;
    }

    void IDisposable.Dispose()
    {
      if ( IsDisposed ) return ;
      IsDisposed = true ;

      GC.SuppressFinalize( this ) ;

      DisposeManagedResources() ;

      ReleaseUnmanagedResources() ;
    }

    public void UpdateUI( Document document, AppUIUpdateType updateType )
    {
      if ( document.IsFamilyDocument ) {
        UpdateUIForFamilyDocument( document, updateType ) ;
      }
      else {
        UpdateUIForNormalDocument( document, updateType ) ;
      }
    }

    protected virtual void DisposeManagedResources()
    {
    }

    protected virtual void ReleaseUnmanagedResources()
    {
    }
  }
}