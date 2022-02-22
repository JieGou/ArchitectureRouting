using System ;
using System.Collections.Generic ;
using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.Forms;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Forms
{
  public partial class SelectedFromToHaseko : UserControl
  {
    public static readonly DependencyProperty DisplayUnitSystemProperty = DependencyProperty.Register( "DisplayUnitSystem", typeof( DisplayUnit ), typeof( SelectedFromToHaseko ), new PropertyMetadata( DisplayUnit.IMPERIAL ) ) ;

    public DisplayUnit DisplayUnitSystem
    {
      get { return (DisplayUnit)GetValue( DisplayUnitSystemProperty ) ; }
      set { SetValue( DisplayUnitSystemProperty, value ) ; }
    }

    public FromToTreeHaseko? ParentFromToTree { get ; set ; }

    private RoutePropertySource? _editingSource ;
    public RoutePropertySource? EditingSource
    {
      get => _editingSource ;
      set
      {
        _editingSource = value ;
        if ( value is { } source ) {
          FromToEdit.SetRouteProperties( new RoutePropertyTypeList( source.TargetSubRoutes ), source.Properties ) ;
          ResetDialog() ;
        }
        else {
          FromToEdit.ClearDialog() ;
        }
      }
    }

    public FromToItem? TargetFromToItem { get ; set ; }

    public SelectedFromToHaseko()
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
      if ( TargetFromToItem?.PropertySourceType is RoutePropertySource && TargetFromToItem.ItemTag == "Route" ) {
        FromToEdit.SystemTypeEditable = TargetFromToItem.IsRootRoute ;
        FromToEdit.CurveTypeEditable = TargetFromToItem.IsRootRoute ;
        FromToEdit.ShaftEditable = true ;
      }
      else if ( TargetFromToItem?.PropertySourceType is RoutePropertySource && TargetFromToItem.ItemTypeName == "Section" ) {
        FromToEdit.SystemTypeEditable = false ;
        FromToEdit.CurveTypeEditable = true ;
        FromToEdit.ShaftEditable = false ;
      }
      else if ( TargetFromToItem?.PropertySourceType is ConnectorPropertySource ) {
        FromToEdit.SystemTypeEditable = false ;
        FromToEdit.CurveTypeEditable = false ;
        FromToEdit.ShaftEditable = false ;
      }
      else {
        FromToEdit.SystemTypeEditable = false ;
        FromToEdit.CurveTypeEditable = false ;
        FromToEdit.ShaftEditable = false ;
      }
    }

    private void Dialog2Buttons_OnLeftOnClick( object sender, RoutedEventArgs e )
    {
      if ( TargetFromToItem?.PropertySourceType is not RoutePropertySource routePropertySource ) return ;

      if ( TargetFromToItem?.ItemTag == "Route" ) {
        var result = MessageBox.Show( "Dialog.Forms.SelectedFromToBase.ChangeFromTo".GetAppStringByKeyOrDefault( "Do you want to change the From-To information?&#xA;If you change it, it will be automatically re-routed." ), "", MessageBoxButton.YesNo ) ;
        if ( result != MessageBoxResult.Yes ) return ;
      }

      var route = routePropertySource.TargetRoute ;
      var fromFixedHeight = FixedHeight.CreateOrNull( FromToEdit.FromLocationType, FromToEdit.FromFixedHeight + FromToEdit.FromMaximumHeightAsCeilingLevel  ) ;
      var toFixedHeight = FixedHeight.CreateOrNull( FromToEdit.ToLocationType, FromToEdit.ToFixedHeight ) ;
      var routeProperties = new RouteProperties( route, FromToEdit.SystemType, FromToEdit.CurveType, FromToEdit.Diameter, FromToEdit.IsRouteOnPipeSpace, FromToEdit.UseFromFixedHeight, fromFixedHeight, FromToEdit.UseToFixedHeight, toFixedHeight, FromToEdit.AvoidType, FromToEdit.Shaft ) ;
      ParentFromToTree?.PostCommandExecutor.ApplySelectedFromToChangesCommand( route, routePropertySource.TargetSubRoutes, routeProperties ) ;
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