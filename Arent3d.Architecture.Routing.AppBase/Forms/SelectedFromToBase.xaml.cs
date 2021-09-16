using System ;
using System.Collections.Generic ;
using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Revit.I18n ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class SelectedFromToBase : UserControl
  {
    public FromToTree? ParentFromToTree { get ; set ; }

    private PropertySource.RoutePropertySource? _editingSource ;
    public PropertySource.RoutePropertySource? EditingSource
    {
      get => _editingSource ;
      set
      {
        _editingSource = value ;
        if ( value is { } source ) {
          FromToEdit.SetPropertySourceValues( source ) ;
          ResetDialog() ;
        }
        else {
          FromToEdit.ClearDialog() ;
        }
      }
    }

    public SelectedFromToBase()
    {
      InitializeComponent() ;
    }

    private void ResetDialog()
    {
      FromToEdit.ResetDialog() ;
      UpdateDateContext() ;
    }

    private void UpdateDateContext()
    {
      if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTag == "Route" ) {
        FromToEdit.SystemTypeEditable = SelectedFromToViewModel.FromToItem.IsRootRoute ;
        FromToEdit.CurveTypeEditable = SelectedFromToViewModel.FromToItem.IsRootRoute ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTypeName == "Section" ) {
        FromToEdit.SystemTypeEditable = false ;
        FromToEdit.CurveTypeEditable = true ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is ConnectorPropertySource ) {
        FromToEdit.SystemTypeEditable = false ;
        FromToEdit.CurveTypeEditable = false ;
      }
    }

    private void Dialog2Buttons_OnLeftOnClick( object sender, RoutedEventArgs e )
    {
      if ( SelectedFromToViewModel.FromToItem?.ItemTag == "Route" ) {
        var result = MessageBox.Show( "Dialog.Forms.SelectedFromToBase.ChangeFromTo".GetAppStringByKeyOrDefault( "Do you want to change the From-To information?&#xA;If you change it, it will be automatically re-routed." ), "", MessageBoxButton.YesNo ) ;
        if ( result != MessageBoxResult.Yes ) return ;
      }

      var systemType = GetSelected( FromToEdit.SystemTypes, FromToEdit.SystemTypeComboBox.SelectedIndex ) ;
      if ( GetSelected( FromToEdit.Diameters, FromToEdit.DiameterComboBox.SelectedIndex ) is not { } diameter ) return ;
      if ( GetSelected( FromToEdit.CurveTypes, FromToEdit.CurveTypeComboBox.SelectedIndex ) is not { } curveType ) return ;

      SelectedFromToViewModel.ApplySelectedChanges( diameter, systemType, curveType, FromToEdit.UseDirectRouting, FromToEdit.UseFixedHeight, FromToEdit.FixedHeight, FromToEdit.AvoidType, ParentFromToTree?.PostCommandExecutor ) ;
    }

    private static T? GetSelected<T>( IReadOnlyList<T> systemTypes, int selectedIndex ) where T : class
    {
      if ( selectedIndex < 0 || systemTypes.Count <= selectedIndex ) return null ;

      return systemTypes[ selectedIndex ] ;
    }
    private static T? GetSelected<T>( IList<T> systemTypes, int selectedIndex ) where T : struct
    {
      if ( selectedIndex < 0 || systemTypes.Count <= selectedIndex ) return null ;

      return systemTypes[ selectedIndex ] ;
    }

    private void Dialog2Buttons_OnRightOnClick( object sender, RoutedEventArgs e )
    {
      ResetDialog() ;
    }

    private void FromToEdit_OnValueChanged( object sender, EventArgs e )
    {
      this.DataContext = new
      {
        IsEnableLeftBtn = true,
        IsRouterVisibility = true,
        IsConnectorVisibility = false,
      } ;
    }
  }
}