using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Autodesk.Revit.DB ;
using System.Collections.ObjectModel ;
using System.Text.RegularExpressions ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.App.ViewModel ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  public partial class SelectedFromToBase : UserControl
  {
    //Diameter Info
    public ObservableCollection<string> Diameters { get ; set ; }
    public int? DiameterIndex { get ; set ; }

    //SystemType Info
    public ObservableCollection<MEPSystemType> SystemTypes { get ; set ; }
    public int? SystemTypeIndex { get ; set ; }

    //CurveType Info
    public ObservableCollection<MEPCurveType> CurveTypes { get ; set ; }
    public int? CurveTypeIndex { get ; set ; }
    public string CurveTypeLabel { get ; set ; }

    //Direct Info
    public bool CurrentDirect { get ; set ; }

    public SelectedFromToBase()
    {
      InitializeComponent() ;
      
      DiameterIndex = 0 ;
      SystemTypeIndex = 0 ;
      CurveTypeIndex = 0 ;
      CurveTypeLabel = "Type" ;
      CurrentDirect = false ;
      Diameters = new ObservableCollection<string>() ;
      SystemTypes = new ObservableCollection<MEPSystemType>() ;
      CurveTypes = new ObservableCollection<MEPCurveType>() ;
    }
    
    /// <summary>
    /// Get LableName from CurveType
    /// </summary>
    /// <param name="targetStrings"></param>
    /// <returns></returns>
    public string GetTypeLabel(string targetStrings)
    {
      string[] splitStrings = Regex.Split( targetStrings, "Type" ) ;

      return splitStrings[ 0 ] + " Type";
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

    private void Dialog3Buttons_OnOKClick( object sender, System.Windows.RoutedEventArgs e )
    {
    }

    private void Dialog3Buttons_OnApplyClick( object sender, System.Windows.RoutedEventArgs e )
    {
      SelectedFromToViewModel.ApplySelectedChanges( DiameterComboBox.SelectedIndex, SystemTypeComboBox.SelectedIndex, CurveTypeComboBox.SelectedIndex, CurrentDirect ) ;
    }

    private void Dialog3Buttons_OnCancelClick( object sender, System.Windows.RoutedEventArgs e )
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
      Diameters.Clear();
      SystemTypes.Clear();
      CurveTypes.Clear();

      if ( diameters != null ) {
        foreach ( var d in diameters ) {
          Diameters.Add(UnitUtils.ConvertFromInternalUnits( d, UnitTypeId.Millimeters ) + " mm" );
        }
      }

      if ( systemTypes != null ) {
        foreach ( var s in systemTypes ) {
          SystemTypes.Add(s);
        }
      }

      if ( curveTypes != null ) {
        foreach ( var c in curveTypes ) {
          CurveTypes.Add(c);
        }
      }
    }
    
    public void ResetDialog()
    {
      SystemTypeComboBox.ItemsSource = SystemTypes ;
      if ( SystemTypeIndex != null ) {
        SystemTypeComboBox.SelectedIndex = SelectedFromToViewModel.SystemTypeIndex ;
      }

      CurveTypeComboBox.ItemsSource = CurveTypes ;
      if ( CurveTypeIndex != null ) {
        CurveTypeComboBox.SelectedIndex = SelectedFromToViewModel.CurveTypeIndex ;
      }

      CurveTypeDomain.Content = CurveTypeLabel ;

      DiameterComboBox.ItemsSource = Diameters ;
      if ( DiameterIndex != null ) {
        DiameterComboBox.SelectedIndex = (int) DiameterIndex ;
      }

      Direct.IsChecked = CurrentDirect ;
    }

    private void Direct_OnChecked( object sender, RoutedEventArgs e )
    {
      SelectedFromToViewModel.IsDirect = true ;
    }

    private void Direct_OnUnchecked( object sender, RoutedEventArgs e )
    {
      SelectedFromToViewModel.IsDirect = false ;
    }
  }
}