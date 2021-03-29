using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Autodesk.Revit.DB ;
using System.Collections.ObjectModel ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.App.ViewModel ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  public partial class SelectedFromTo : Window
  {
    //Diameter Info
    public ObservableCollection<string> Diameters { get ; set ; }
    public int? DiameterIndex { get ; set ; }

    //SystemType Info
    public ObservableCollection<MEPSystemType> SystemTypes { get ; }
    public int? SystemTypeIndex { get ; set ; }

    //CurveType Info
    public ObservableCollection<MEPCurveType> CurveTypes { get ; }
    public int? CurveTypeIndex { get ; set ; }
    public string CurveTypeLabel { get ; set ; }

    //Direct Info
    public bool CurrentDirect { get ; set ; }

    public SelectedFromTo( Document doc, IList<double> diameters, int diameterIndex, IList<MEPSystemType> systemTypes, int systemTypeIndex, IList<MEPCurveType> curveTypes, int curveTypeIndex, Type type, bool direct )
    {
      InitializeComponent() ;
      this.SizeToContent = SizeToContent.WidthAndHeight;
      DiameterIndex = diameterIndex ;
      SystemTypeIndex = systemTypeIndex ;
      CurveTypeIndex = curveTypeIndex ;
      CurveTypeLabel = type.Name.Split( 'T' )[ 0 ] + " Type";
      CurrentDirect = direct ;
      Diameters = new ObservableCollection<string>( diameters.Select( d => UnitUtils.ConvertFromInternalUnits( d, UnitTypeId.Millimeters ) + " mm" ) ) ;
      SystemTypes = new ObservableCollection<MEPSystemType>( systemTypes ) ;
      CurveTypes = new ObservableCollection<MEPCurveType>( curveTypes ) ;
    }


    //Diameter 
    private void DiameterComboBox_Changed( object sender, SelectionChangedEventArgs e )
    {
    }

    //SystemType
    private void SystemTypeComboBox_OnSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
    }

    //CurveType
    private void CurveTypeComboBox_OnSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( CurveTypeComboBox.IsDropDownOpen ) //avoid chnages in construction
      {
        int selectedIndex = CurveTypeComboBox.SelectedIndex ;

        Diameters = new ObservableCollection<string>( SelectedFromToViewModel.ResetNominalDiameters( selectedIndex ).Select( d => UnitUtils.ConvertFromInternalUnits( d, UnitTypeId.Millimeters ) + " mm" ) ) ;
        DiameterComboBox.ItemsSource = Diameters ;

        DiameterComboBox.SelectedIndex = Diameters.Count - 1 ;
      }
    }

    private void Dialog3Buttons_OnOKClick( object sender, RoutedEventArgs e )
    {
      SelectedFromToViewModel.ApplySelectedDiameter( DiameterComboBox.SelectedIndex, SystemTypeComboBox.SelectedIndex, CurveTypeComboBox.SelectedIndex, CurrentDirect ) ;
      this.Close() ;
    }

    private void Dialog3Buttons_OnApplyClick( object sender, RoutedEventArgs e )
    {
      SelectedFromToViewModel.ApplySelectedDiameter( DiameterComboBox.SelectedIndex, SystemTypeComboBox.SelectedIndex, CurveTypeComboBox.SelectedIndex, CurrentDirect ) ;
    }

    private void Dialog3Buttons_OnCancelClick( object sender, RoutedEventArgs e )
    {
      this.Close() ;
    }
  }
}