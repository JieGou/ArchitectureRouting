using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Autodesk.Revit.DB ;
using System.Collections.ObjectModel ;
using System.Text.RegularExpressions ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Revit ;
using Autodesk.Revit.UI ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using ControlLib ;
using static ControlLib.NumericUpDown ;
using Visibility = System.Windows.Visibility ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  public partial class SelectedFromToBase : UserControl
  {
    //Diameter Info
    public ObservableCollection<string> Diameters { get ; set ; }
    public int? DiameterIndex { get ; set ; }
    public int? DiameterOrgIndex { get ; set ; }

    //SystemType Info
    public ObservableCollection<MEPSystemType> SystemTypes { get ; set ; }
    public int? SystemTypeIndex { get ; set ; }
    public int? SystemTypeOrgIndex { get ; set ; }

    //CurveType Info
    public ObservableCollection<MEPCurveType> CurveTypes { get ; set ; }
    public int? CurveTypeIndex { get ; set ; }
    public int? CurveTypeOrgIndex { get ; set ; }
    public string CurveTypeLabel { get ; set ; }

    //Direct Info
    public bool? CurrentDirect { get ; set ; }

    public bool? CurrentOrgDirect { get ; set ; }

    //HeightSetting
    public bool? CurrentHeightSetting { get ; set ; }
    public bool? CurrentOrgHeightSetting { get ; set ; }

    public double FixedHeight { get ; set ; }
    public double FixedOrgHeight { get ; set ; }
    
    public double CurrentMaxValue { get ; set ; }
    public double CurrentMinValue { get ; set ; }

    //AvoidType
    public AvoidType AvoidTypeKey { get ; set ; }
    public AvoidType AvoidTypeOrgKey { get ; set ; }

    public Dictionary<AvoidType, string> AvoidTypes { get ; } = new Dictionary<AvoidType, string>
    {
      [ AvoidType.Whichever ] = "Dialog.Forms.SelectedFromToBase.ProcessConstraints.None".GetAppStringByKeyOrDefault( "Whichever" ), [ AvoidType.NoAvoid ] = "Dialog.Forms.SelectedFromToBase.ProcessConstraints.NoPocket".GetAppStringByKeyOrDefault( "Don't avoid From-To" ), [ AvoidType.AvoidAbove ] = "Dialog.Forms.SelectedFromToBase.ProcessConstraints.NoDrainPocket".GetAppStringByKeyOrDefault( "Avoid on From-To" ), [ AvoidType.AvoidBelow ] = "Dialog.Forms.SelectedFromToBase.ProcessConstraints.NoVentPocket".GetAppStringByKeyOrDefault( "Avoid below From-To" ),
    } ;


    public bool? IsEnableLeftBtn { get ; set ; }

    public bool IsEnableSystemType
    {
      get { return (bool) GetValue( IsEnableSystemTypeProperty ) ; }
      set { SetValue( IsEnableSystemTypeProperty, value ) ; }
    }

    public bool IsEnableCurveType
    {
      get { return (bool) GetValue( IsEnableCurveTypeProperty ) ; }
      set { SetValue( IsEnableCurveTypeProperty, value ) ; }
    }

    public static readonly DependencyProperty IsEnableSystemTypeProperty = DependencyProperty.Register( "IsEnableSystemType", typeof( bool ), typeof( SelectedFromToBase ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty IsEnableCurveTypeProperty = DependencyProperty.Register( "IsEnableCurveType", typeof( bool ), typeof( SelectedFromToBase ), new PropertyMetadata( true ) ) ;

    public SelectedFromToBase()
    {
      InitializeComponent() ;

      DiameterIndex = 0 ;
      SystemTypeIndex = 0 ;
      CurveTypeIndex = 0 ;
      CurveTypeLabel = "Type" ;
      CurrentDirect = false ;
      CurrentHeightSetting = false ;
      FixedHeight = 0.0 ;
      CurrentMaxValue = 10000 ;
      CurrentMinValue = 0 ;
      Diameters = new ObservableCollection<string>() ;
      SystemTypes = new ObservableCollection<MEPSystemType>() ;
      CurveTypes = new ObservableCollection<MEPCurveType>() ;
    }

    /// <summary>
    /// Get LableName from CurveType
    /// </summary>
    /// <param name="targetStrings"></param>
    /// <returns></returns>
    public string GetTypeLabel( string targetStrings )
    {
      string[] splitStrings = Regex.Split( targetStrings, "Type" ) ;

      return splitStrings[ 0 ] + " Type" ;
    }

    private void SystemTypeComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTag == "Route" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = true,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = SelectedFromToViewModel.FromToItem.IsRootRoute,
          IsEnableCurveType = SelectedFromToViewModel.FromToItem.IsRootRoute
        } ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTypeName == "Section" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = true,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = false,
          IsEnableCurveType = true
        } ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is ConnectorPropertySource ) {
        this.DataContext = new { IsRouterVisibility = false, IsConnectorVisibility = false } ;
      }

      IsEnableLeftBtn = true ;
    }

    private void CurveTypeComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTag == "Route" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = true,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = SelectedFromToViewModel.FromToItem.IsRootRoute,
          IsEnableCurveType = SelectedFromToViewModel.FromToItem.IsRootRoute
        } ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTypeName == "Section" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = true,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = false,
          IsEnableCurveType = true
        } ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is ConnectorPropertySource ) {
        this.DataContext = new { IsRouterVisibility = false, IsConnectorVisibility = false } ;
      }

      if ( CurveTypeComboBox.IsDropDownOpen ) //avoid changes in construction
      {
        int selectedIndex = CurveTypeComboBox.SelectedIndex ;

        Diameters = new ObservableCollection<string>( SelectedFromToViewModel.ResetNominalDiameters( selectedIndex ).Select( d => d.RevitUnitsToMillimeters() + " mm" ) ) ;
        DiameterComboBox.ItemsSource = Diameters ;

        DiameterComboBox.SelectedIndex = Diameters.Count - 1 ;
      }
    }


    private void DiameterComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTag == "Route" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = true,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = SelectedFromToViewModel.FromToItem.IsRootRoute,
          IsEnableCurveType = SelectedFromToViewModel.FromToItem.IsRootRoute
        } ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTypeName == "Section" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = true,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = false,
          IsEnableCurveType = true
        } ;
      }

      
      
      //HeightNud.MinValue = DiameterComboBox.SelectedValue. ;
    }

    /// <summary>
    /// Update Diameters, SystemTypes, and CurveTypes
    /// </summary>
    /// <param name="diameters"></param>
    /// <param name="systemTypes"></param>
    /// <param name="curveTypes"></param>
    public void UpdateFromToParameters( IList<double>? diameters, IList<MEPSystemType>? systemTypes, IList<MEPCurveType>? curveTypes )
    {
      Diameters.Clear() ;
      SystemTypes.Clear() ;
      CurveTypes.Clear() ;

      if ( diameters != null ) {
        foreach ( var d in diameters ) {
          Diameters.Add( d.RevitUnitsToMillimeters() + " mm" ) ;
        }
      }

      if ( systemTypes != null ) {
        foreach ( var s in systemTypes ) {
          SystemTypes.Add( s ) ;
        }
      }

      if ( curveTypes != null ) {
        foreach ( var c in curveTypes ) {
          CurveTypes.Add( c ) ;
        }
      }
    }

    public void ResetDialog()
    {
      SystemTypeComboBox.ItemsSource = SystemTypes ;
      if ( SystemTypeIndex != null ) {
        SystemTypeComboBox.SelectedIndex = (int) SystemTypeIndex ;
      }

      CurveTypeComboBox.ItemsSource = CurveTypes ;
      if ( CurveTypeIndex != null ) {
        CurveTypeComboBox.SelectedIndex = (int) CurveTypeIndex ;
      }

      CurveTypeDomain.Content = CurveTypeLabel ;

      DiameterComboBox.ItemsSource = Diameters ;
      if ( DiameterIndex != null ) {
        DiameterComboBox.SelectedIndex = (int) DiameterIndex ;
      }

      Direct.IsChecked = CurrentDirect ;

      HeightNud.MaxValue = CurrentMaxValue ;
      HeightNud.MinValue = CurrentMinValue ;
      HeightSetting.IsChecked = CurrentHeightSetting ;
      HeightNud.ClearValue(ValueProperty);
      HeightNud.Value = FixedHeight ;

      AvoidTypeComboBox.SelectedItem = GetAvoidTypeKeyValuePair( AvoidTypeKey ) ;
      if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTag == "Route" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = false,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = SelectedFromToViewModel.FromToItem.IsRootRoute,
          IsEnableCurveType = SelectedFromToViewModel.FromToItem.IsRootRoute
        } ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTypeName == "Section" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = false,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = false,
          IsEnableCurveType = true
        } ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is ConnectorPropertySource ) {
        this.DataContext = new { IsRouterVisibility = false, IsConnectorVisibility = false } ;
      }
    }

    public void ClearDialog()
    {
      DiameterIndex = 0 ;
      SystemTypeIndex = 0 ;
      CurveTypeIndex = 0 ;
      CurveTypeLabel = "Type" ;
      Direct.IsChecked = false ;
      Diameters.Clear() ;
      SystemTypes.Clear() ;
      CurveTypes.Clear() ;
      HeightSetting.IsChecked = false ;
      HeightNud.Value = 0 ;
      AvoidTypeComboBox.SelectedItem = null ;
    }

    private void Direct_OnChecked( object sender, RoutedEventArgs e )
    {
      if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTag == "Route" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = true,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = SelectedFromToViewModel.FromToItem.IsRootRoute,
          IsEnableCurveType = SelectedFromToViewModel.FromToItem.IsRootRoute
        } ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTypeName == "Section" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = true,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = false,
          IsEnableCurveType = true
        } ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is ConnectorPropertySource ) {
        this.DataContext = new { IsRouterVisibility = false, IsConnectorVisibility = false } ;
      }

      SelectedFromToViewModel.IsDirect = true ;
    }

    private void Direct_OnUnchecked( object sender, RoutedEventArgs e )
    {
      if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTag == "Route" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = true,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = SelectedFromToViewModel.FromToItem.IsRootRoute,
          IsEnableCurveType = SelectedFromToViewModel.FromToItem.IsRootRoute
        } ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTypeName == "Section" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = true,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = false,
          IsEnableCurveType = true
        } ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is ConnectorPropertySource ) {
        this.DataContext = new { IsRouterVisibility = false, IsConnectorVisibility = false } ;
      }

      SelectedFromToViewModel.IsDirect = false ;
    }

    private void Dialog2Buttons_OnLeftOnClick( object sender, RoutedEventArgs e )
    {
      if ( SelectedFromToViewModel.FromToItem?.ItemTag == "Route" ) {
        MessageBoxResult result = MessageBox.Show( "Dialog.Forms.SelectedFromToBase.ChangeFromTo".GetAppStringByKeyOrDefault( "Do you want to change the From-To information?&#xA;If you change it, it will be automatically re-routed." ), "", MessageBoxButton.YesNo ) ;
        if ( result == MessageBoxResult.Yes ) {
          SelectedFromToViewModel.ApplySelectedChanges( DiameterComboBox.SelectedIndex, SystemTypeComboBox.SelectedIndex, CurveTypeComboBox.SelectedIndex, CurrentDirect, HeightSetting.IsChecked, HeightNud.Value, AvoidTypeKey ) ;
        }
      }
      else {
        SelectedFromToViewModel.ApplySelectedChanges( DiameterComboBox.SelectedIndex, SystemTypeComboBox.SelectedIndex, CurveTypeComboBox.SelectedIndex, CurrentDirect, HeightSetting.IsChecked, HeightNud.Value, AvoidTypeKey ) ;
      }
    }

    private void Dialog2Buttons_OnRightOnClick( object sender, RoutedEventArgs e )
    {
      if ( SystemTypeOrgIndex != null ) {
        SystemTypeComboBox.SelectedIndex = (int) SystemTypeOrgIndex ;
      }

      if ( CurveTypeOrgIndex != null ) {
        CurveTypeComboBox.SelectedIndex = (int) CurveTypeOrgIndex ;
      }

      if ( DiameterOrgIndex != null ) {
        DiameterComboBox.SelectedIndex = (int) DiameterOrgIndex ;
      }

      Direct.IsChecked = CurrentOrgDirect ;
      HeightSetting.IsChecked = CurrentOrgHeightSetting ;
      HeightNud.Value = FixedOrgHeight ;

      AvoidTypeComboBox.SelectedItem = GetAvoidTypeKeyValuePair( AvoidTypeOrgKey ) ;
      if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTag == "Route" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = false,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = SelectedFromToViewModel.FromToItem.IsRootRoute,
          IsEnableCurveType = SelectedFromToViewModel.FromToItem.IsRootRoute
        } ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTypeName == "Section" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = false,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = false,
          IsEnableCurveType = true
        } ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is ConnectorPropertySource ) {
        this.DataContext = new { IsEnableLeftBtn = false, IsRouterVisibility = false, IsConnectorVisibility = false } ;
      }
    }

    private void Dialog2Buttons_Loaded( object sender, RoutedEventArgs e )
    {
    }

    private void Height_OnChecked( object sender, RoutedEventArgs e )
    {
      if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTag == "Route" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = true,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = SelectedFromToViewModel.FromToItem.IsRootRoute,
          IsEnableCurveType = SelectedFromToViewModel.FromToItem.IsRootRoute
        } ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTypeName == "Section" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = true,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = false,
          IsEnableCurveType = true
        } ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is ConnectorPropertySource ) {
        this.DataContext = new { IsRouterVisibility = false, IsConnectorVisibility = false } ;
      }

      SetHeightTextVisibility( true ) ;
    }

    private void Height_OnUnchecked( object sender, RoutedEventArgs e )
    {
      if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTag == "Route" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = true,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = SelectedFromToViewModel.FromToItem.IsRootRoute,
          IsEnableCurveType = SelectedFromToViewModel.FromToItem.IsRootRoute
        } ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTypeName == "Section" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = true,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = false,
          IsEnableCurveType = true
        } ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is ConnectorPropertySource ) {
        this.DataContext = new { IsRouterVisibility = false, IsConnectorVisibility = false } ;
      }

      SetHeightTextVisibility( false ) ;
    }

    public void SetHeightTextVisibility( bool visibility )
    {
      if ( visibility ) {
        FL.Visibility = Visibility.Visible ;
        HeightNud.Visibility = Visibility.Visible ;
        mm.Visibility = Visibility.Visible ;
      }
      else {
        FL.Visibility = Visibility.Hidden ;
        HeightNud.Visibility = Visibility.Hidden ;
        mm.Visibility = Visibility.Hidden ;
      }
    }

    private void AvoidTypeComboBox_OnSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( AvoidTypeComboBox.SelectedItem is { } selectedItem ) {
        var selectedItemDict = (KeyValuePair<AvoidType, string>) AvoidTypeComboBox.SelectedItem ;
        AvoidTypeKey = selectedItemDict.Key ;
      }

      if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTag == "Route" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = true,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = SelectedFromToViewModel.FromToItem.IsRootRoute,
          IsEnableCurveType = SelectedFromToViewModel.FromToItem.IsRootRoute
        } ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTypeName == "Section" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = true,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = false,
          IsEnableCurveType = true
        } ;
      }
    }

    private KeyValuePair<AvoidType, string> GetAvoidTypeKeyValuePair( AvoidType avoidTypeKey )
    {
      return new KeyValuePair<AvoidType, string>( avoidTypeKey, AvoidTypes[ avoidTypeKey ] ) ;
    }

    private void HeightNud_OnValueChanged( object sender, ValueChangedEventArgs e )
    {
      if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTag == "Route" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = true,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = SelectedFromToViewModel.FromToItem.IsRootRoute,
          IsEnableCurveType = SelectedFromToViewModel.FromToItem.IsRootRoute
        } ;
      }
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is PropertySource.RoutePropertySource && SelectedFromToViewModel.FromToItem.ItemTypeName == "Section" ) {
        this.DataContext = new
        {
          IsEnableLeftBtn = true,
          IsRouterVisibility = true,
          IsConnectorVisibility = false,
          IsEnableSystemType = false,
          IsEnableCurveType = true
        } ;
      }
    }
  }
}