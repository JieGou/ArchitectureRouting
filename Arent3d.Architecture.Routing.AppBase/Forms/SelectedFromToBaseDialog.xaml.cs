using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Autodesk.Revit.DB ;
using System.Collections.ObjectModel ;
using System.Configuration ;
using System.Text.RegularExpressions ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Revit ;
using Autodesk.Revit.UI ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using ControlLib ;
using static ControlLib.NumericUpDown ;
using Visibility = System.Windows.Visibility ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class SelectedFromToBaseDialog : UserControl
  {
    //Diameter Info
    public ObservableCollection<double> Diameters { get ; private set ; }
    public double? Diameter { get ; set ; }
    public double? DiameterOrg { get ; set ; }

    //SystemType Info
    public ObservableCollection<MEPSystemType> SystemTypes { get ; }
    public MEPSystemType? SystemType { get ; set ; }
    public MEPSystemType? SystemTypeOrg { get ; set ; }

    //CurveType Info
    public ObservableCollection<MEPCurveType> CurveTypes { get ; }
    public MEPCurveType? CurveType { get ; set ; }
    public MEPCurveType? CurveTypeOrg { get ; set ; }
    public string CurveTypeLabel { get ; set ; }

    //Direct Info
    public bool? CurrentDirect { get ; set ; }

    public bool? CurrentOrgDirect { get ; set ; }

    //HeightSetting
    public bool? CurrentHeightSetting { get ; set ; }
    public bool? CurrentOrgHeightSetting { get ; set ; }

    public double FixedHeight { get ; set ; }
    public double FixedOrgHeight { get ; set ; }
    public double? ConnectorFixedHeight { get ; set ; }

    public double CurrentMaxValue { get ; set ; }
    public double CurrentMinValue { get ; set ; }

    //AvoidType
    public AvoidType AvoidTypeKey { get ; set ; }
    public AvoidType AvoidTypeOrgKey { get ; set ; }

    public Dictionary<AvoidType, string> AvoidTypes { get ; } = new Dictionary<AvoidType, string>
    {
      [ AvoidType.Whichever ] = "Dialog.Forms.SelectedFromToBaseDialog.ProcessConstraints.None".GetAppStringByKeyOrDefault( "Whichever" ), [ AvoidType.NoAvoid ] = "Dialog.Forms.SelectedFromToBaseDialog.ProcessConstraints.NoPocket".GetAppStringByKeyOrDefault( "Don't avoid From-To" ), [ AvoidType.AvoidAbove ] = "Dialog.Forms.SelectedFromToBaseDialog.ProcessConstraints.NoDrainPocket".GetAppStringByKeyOrDefault( "Avoid on From-To" ), [ AvoidType.AvoidBelow ] = "Dialog.Forms.SelectedFromToBaseDialog.ProcessConstraints.NoVentPocket".GetAppStringByKeyOrDefault( "Avoid below From-To" ),
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

    public static readonly DependencyProperty IsEnableSystemTypeProperty = DependencyProperty.Register( "IsEnableSystemType", typeof( bool ), typeof( SelectedFromToBaseDialog ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty IsEnableCurveTypeProperty = DependencyProperty.Register( "IsEnableCurveType", typeof( bool ), typeof( SelectedFromToBaseDialog ), new PropertyMetadata( true ) ) ;

    public SelectedFromToBaseDialog()
    {
      InitializeComponent() ;

      Diameter = null ;
      SystemType = null ;
      CurveType = null ;
      CurveTypeLabel = "Type" ;
      CurrentDirect = false ;
      CurrentHeightSetting = false ;
      FixedHeight = 0.0 ;
      CurrentMaxValue = 10000 ;
      CurrentMinValue = 0 ;
      Diameters = new ObservableCollection<double>() ;
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
      if ( SystemType is { } systemType && SystemTypeComboBox.SelectedItem == null ) {
        SystemTypeComboBox.SelectedIndex = SystemTypes.FindIndex( s => s.Name == systemType.Name ) ;
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
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is ConnectorPropertySource ) {
        this.DataContext = new { IsRouterVisibility = false, IsConnectorVisibility = false } ;
      }

      IsEnableLeftBtn = true ;
    }

    private void CurveTypeComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( CurveType is { } curveType && CurveTypeComboBox.SelectedItem == null ) {
        CurveTypeComboBox.SelectedIndex = CurveTypes.FindIndex( c => c.Name == curveType.Name ) ;
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
      else if ( SelectedFromToViewModel.FromToItem?.PropertySourceType is ConnectorPropertySource ) {
        this.DataContext = new { IsRouterVisibility = false, IsConnectorVisibility = false } ;
      }

      if ( CurveTypeComboBox.IsDropDownOpen ) //avoid changes in construction
      {
        int selectedIndex = CurveTypeComboBox.SelectedIndex ;


        var currentDiameter = 0.0 ;
        if ( DiameterComboBox.SelectedIndex != -1 ) {
          currentDiameter = Diameters[ DiameterComboBox.SelectedIndex ].MillimetersToRevitUnits() ;
        }

        var newDiameters = SelectedFromToViewModel.ResetNominalDiameters( selectedIndex ) ;
        var enumerable = newDiameters.ToList() ;

        Diameters = new ObservableCollection<double>( enumerable.Select( d => Math.Round( d.RevitUnitsToMillimeters(), 2, MidpointRounding.AwayFromZero ) ) ) ;
        DiameterComboBox.ItemsSource = Diameters ;

        if ( currentDiameter != 0.0 ) {
          DiameterComboBox.SelectedIndex = UIHelper.FindClosestIndex( enumerable.ToList(), (double) currentDiameter ) ;
        }
        else {
          DiameterComboBox.SelectedIndex = -1 ;
        }
      }
    }


    private void DiameterComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( Diameter is { } diameter && DiameterComboBox.SelectedItem == null ) {
        DiameterComboBox.SelectedIndex = Diameters.FindIndex( d => d == Math.Round( diameter.RevitUnitsToMillimeters(), 2, MidpointRounding.AwayFromZero ) ) ;
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

      if (HeightSetting.IsChecked == true) {
        HeightNud.Value = CalcFixedHeight();
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
          Diameters.Add( Math.Round( d.RevitUnitsToMillimeters(), 2, MidpointRounding.AwayFromZero ) ) ;
        }
      }

      SystemTypeComboBox.IsEnabled = false ;
      if ( systemTypes != null ) {
        foreach ( var s in systemTypes ) {
          SystemTypes.Add( s ) ;
        }

        SystemTypeComboBox.IsEnabled = true ;
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
      if ( SystemType is { } systemType ) {
        SystemTypeComboBox.SelectedIndex = SystemTypes.FindIndex( s => s.Name == systemType.Name ) ;
      }

      CurveTypeComboBox.ItemsSource = CurveTypes ;
      if ( CurveType is { } curveType ) {
        CurveTypeComboBox.SelectedIndex = CurveTypes.FindIndex( c => c.Name == curveType.Name ) ;
        CurveTypeLabel = UIHelper.GetTypeLabel( curveType.GetType().Name ) ;
      }

      CurveTypeDomain.Content = CurveTypeLabel ;

      DiameterComboBox.ItemsSource = Diameters ;
      if ( Diameter is { } diameter ) {
        DiameterComboBox.SelectedIndex = Diameters.FindIndex( d => d == Math.Round( diameter.RevitUnitsToMillimeters(), 2, MidpointRounding.AwayFromZero ) ) ;
      }

      Direct.IsChecked = CurrentDirect ;

      HeightNud.MaxValue = CurrentMaxValue ;
      HeightNud.MinValue = CurrentMinValue ;
      HeightSetting.IsChecked = CurrentHeightSetting ;
      HeightNud.ClearValue( ValueProperty ) ;
      HeightNud.Value = FixedHeight ;

      AvoidTypeComboBox.SelectedItem = GetAvoidTypeKeyValuePair( AvoidTypeKey ) ;
    }

    public void ClearDialog()
    {
      Diameter = null ;
      SystemType = null ;
      CurveType = null ;
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

      HeightNud.Value = CalcFixedHeight();
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

      HeightNud.Value = 0.0;
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

    private double CalcFixedHeight()
    {
        double diameter = DiameterComboBox.SelectedItem != null ? (double)DiameterComboBox.SelectedItem : 0;
        double fixedHeight = ConnectorFixedHeight.HasValue ? (ConnectorFixedHeight.Value - (diameter / 2)) : 0.0;
        return fixedHeight;
    }
  }
}