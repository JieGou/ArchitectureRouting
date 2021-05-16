using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Autodesk.Revit.DB ;
using System.Collections.ObjectModel ;
using System.Text.RegularExpressions ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Autodesk.Revit.UI ;
using Visibility = System.Windows.Visibility ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  public partial class SelectedFromToBase : UserControl
  {
    //Diameter Info
    public ObservableCollection<string> Diameters { get ; set ; }
    public int? DiameterIndex { get ; set ; }
    public int? DiameterOrgIndex { get; set; }

    //SystemType Info
    public ObservableCollection<MEPSystemType> SystemTypes { get ; set ; }
    public int? SystemTypeIndex { get ; set ; }
    public int? SystemTypeOrgIndex { get; set; }

    //CurveType Info
    public ObservableCollection<MEPCurveType> CurveTypes { get ; set ; }
    public int? CurveTypeIndex { get ; set ; }
    public int? CurveTypeOrgIndex { get; set; }
    public string CurveTypeLabel { get ; set ; }

    //Direct Info
    public bool? CurrentDirect { get ; set ; }

    public bool? CurrentOrgDirect { get; set; }
    
    //HeightSetting
    public bool? CurrentHeightSetting { get ; set ; }
    public bool? CurrentOrgHeightSetting { get ; set ; }
    
    public string FixedHeight { get ; set ; }
    public string? FixedOrgHeight { get ; set ; }


    public bool IsEnableSystemType
    {
        get { return (bool) GetValue( IsEnableSystemTypeProperty ); }
        set { SetValue( IsEnableSystemTypeProperty, value ); }
    }

    public bool IsEnableCurveType
    {
        get { return (bool) GetValue( IsEnableCurveTypeProperty ); }
        set { SetValue( IsEnableCurveTypeProperty, value ); }
    }
    public static readonly DependencyProperty IsEnableSystemTypeProperty = DependencyProperty.Register( "IsEnableSystemType",
                                typeof( bool ),
                                typeof( SelectedFromToBase ),
                                new PropertyMetadata( true ) );
    public static readonly DependencyProperty IsEnableCurveTypeProperty = DependencyProperty.Register( "IsEnableCurveType",
                        typeof( bool ),
                        typeof( SelectedFromToBase ),
                        new PropertyMetadata( true ) );
    public SelectedFromToBase()
    {
      InitializeComponent() ;

      DiameterIndex = 0 ;
      SystemTypeIndex = 0 ;
      CurveTypeIndex = 0 ;
      CurveTypeLabel = "Type" ;
      CurrentDirect = false ;
      CurrentHeightSetting = false ;
      FixedHeight = "" ;
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
    }

    private void CurveTypeComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( CurveTypeComboBox.IsDropDownOpen ) //avoid chnages in construction
      {
        int selectedIndex = CurveTypeComboBox.SelectedIndex ;

        Diameters = new ObservableCollection<string>( SelectedFromToViewModel.ResetNominalDiameters( selectedIndex ).Select( d => UnitUtils.ConvertFromInternalUnits( d, UnitTypeId.Millimeters ) + " mm" ) ) ;
        DiameterComboBox.ItemsSource = Diameters ;

        DiameterComboBox.SelectedIndex = Diameters.Count - 1 ;
      }
    }


    private void DiameterComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
    {
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
          Diameters.Add( UnitUtils.ConvertFromInternalUnits( d, UnitTypeId.Millimeters ) + " mm" ) ;
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

      HeightSetting.IsChecked = CurrentHeightSetting ;
      HeightTextBox.Text = FixedHeight ;
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
      HeightTextBox.Text = "" ;
    }

    private void Direct_OnChecked( object sender, RoutedEventArgs e )
    {
      SelectedFromToViewModel.IsDirect = true ;
    }

    private void Direct_OnUnchecked( object sender, RoutedEventArgs e )
    {
      SelectedFromToViewModel.IsDirect = false ;
    }

    private void Dialog2Buttons_OnLeftOnClick( object sender, RoutedEventArgs e )
    {
        MessageBoxResult result = MessageBox.Show( "Route情報を変更してもよろしいでしょうか。",
                "FromToTree",
                MessageBoxButton.YesNo );
        if(result == MessageBoxResult.Yes ) {
          SelectedFromToViewModel.ApplySelectedChanges( DiameterComboBox.SelectedIndex, SystemTypeComboBox.SelectedIndex, CurveTypeComboBox.SelectedIndex, CurrentDirect, 
            HeightSetting.IsChecked, Convert.ToDouble(HeightTextBox.Text) ) ;
        }
    }

    private void Dialog2Buttons_OnRightOnClick( object sender, RoutedEventArgs e )
    {
            if ( SystemTypeOrgIndex != null ) {
                SystemTypeComboBox.SelectedIndex = (int) SystemTypeOrgIndex;
            }

            if ( CurveTypeOrgIndex != null ) {
                CurveTypeComboBox.SelectedIndex = (int) CurveTypeOrgIndex;
            }

            if ( DiameterOrgIndex != null ) {
                DiameterComboBox.SelectedIndex = (int) DiameterOrgIndex;
            }

            Direct.IsChecked = CurrentOrgDirect;
            HeightSetting.IsChecked = CurrentOrgHeightSetting ;
            HeightTextBox.Text = FixedOrgHeight ;
    }

    private void Dialog2Buttons_Loaded( object sender, RoutedEventArgs e )
    {
    }

    private void Height_OnChecked( object sender, RoutedEventArgs e )
    {
      FL.Visibility = Visibility.Visible ;
      HeightTextBox.Visibility = Visibility.Visible ;
      mm.Visibility = Visibility.Visible ;

    }

    private void Height_OnUnchecked( object sender, RoutedEventArgs e )
    {
      FL.Visibility = Visibility.Hidden ;
      HeightTextBox.Visibility = Visibility.Hidden ;
      mm.Visibility = Visibility.Hidden ;
    }
  }
}