using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Autodesk.Revit.DB ;
using System.Collections.ObjectModel ;
using System.Collections.Specialized;
using System.ComponentModel ;
using System.Diagnostics;
using System.Runtime.CompilerServices ;
using System.Windows.Controls;
using System.Windows.Input;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB.Mechanical;
using Arent3d.Architecture.Routing.App.Commands.Selecting;
using Arent3d.Architecture.Routing.App.ViewModel;

namespace Arent3d.Architecture.Routing.App.Forms
{
    public partial class SelectedFromTo : Window
    {
        //Diameter Info
        public ObservableCollection<DiameterInfo> Diameters { get; set ; }
        public int? DiameterIndex { get; set; }
        
        //SystemType Info
        public ObservableCollection<SystemTypeInfo> SystemTypes { get; }
        public int? SystemTypeIndex { get; set; }
        
        //CurveType Info
        public ObservableCollection<CurveTypeInfo> CurveTypes { get; }
        public  int? CurveTypeIndex { get; set; }
        public  string CurveTypeLabel { get; set; }

        //Direct Info
        public bool CurrentDirect { get; set; }
        
        public SelectedFromTo(Document doc, IList<double> diameters, int diameterIndex,
             IList<MEPSystemType> systemTypes , int systemTypeIndex, 
             IList<MEPCurveType> curveTypes, int curveTypeIndex, Type type ,bool direct)
        {
            InitializeComponent();
            DiameterIndex = diameterIndex;
            SystemTypeIndex = systemTypeIndex;
            CurveTypeIndex = curveTypeIndex;
            CurveTypeLabel = type.Name;
            CurrentDirect = direct;
            Diameters= new ObservableCollection<DiameterInfo>(diameters.Select(ToDiameterInfo));
            SystemTypes = new ObservableCollection<SystemTypeInfo>(systemTypes.Select(ToSystemTypeInfo));
            CurveTypes = new ObservableCollection<CurveTypeInfo>(curveTypes.Select(ToCurveTypeInfo));
        }


        private static DiameterInfo ToDiameterInfo(double value)
        {
            return new DiameterInfo {Diameter = UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.Millimeters) + " mm"};
            //return new DiameterInfo {Diameter = value + " inch"};
        }

        private static SystemTypeInfo ToSystemTypeInfo(MEPSystemType systemType)
        {
            return new SystemTypeInfo {SystemTypeText = systemType.Name};
        }

        private static CurveTypeInfo ToCurveTypeInfo(MEPCurveType curveType)
        {
            return new CurveTypeInfo {CurveTypeText = curveType.Name};
        }


        //Diameter 
        public class DiameterInfo
        {
            public string Diameter { get; init; } = string.Empty;
        }

        private void DiameterComboBox_Changed(object sender, SelectionChangedEventArgs e)
        {
 
        }
        
        //SystemType
        public class SystemTypeInfo
        {
            public string SystemTypeText { get; init; } = string.Empty;
        }
        
        private void SystemTypeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        
        //CurveType
        public class CurveTypeInfo
        {
            public string CurveTypeText { get; init; } = string.Empty;
        }
        
        private void CurveTypeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ( CurveTypeComboBox.IsDropDownOpen ) //avoid chnages in construction
            {
                CurveTypeInfo item = (CurveTypeInfo) CurveTypeComboBox.SelectedItem;
                int selectedIndex = CurveTypeComboBox.SelectedIndex;
            
                Diameters = new ObservableCollection<DiameterInfo>(SelectedFromToViewModel.ResetNominalDiameters( selectedIndex ).Select(ToDiameterInfo));
                DiameterComboBox.ItemsSource = Diameters ;
                DiameterComboBox.SelectedIndex = 0 ;
            }
            
        }
        
        //Direct
        private void Direct_OnChecked(object sender, RoutedEventArgs e)
        {
            //SelectedFromToViewModel.IsDirect = CurrentDirect;
        }

        private void Direct_OnUnchecked(object sender, RoutedEventArgs e)
        {
            //SelectedFromToViewModel.IsDirect = CurrentDirect;
        }

        //OK Button
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            SelectedFromToViewModel.ApplySelectedDiameter(DiameterComboBox.SelectedIndex, SystemTypeComboBox.SelectedIndex,
                CurveTypeComboBox.SelectedIndex, CurrentDirect);
            this.Close();
        }
        //Apply Button
        private void Apply_OnClick(object sender, RoutedEventArgs e)
        {
            SelectedFromToViewModel.ApplySelectedDiameter(DiameterComboBox.SelectedIndex, SystemTypeComboBox.SelectedIndex, 
                CurveTypeComboBox.SelectedIndex , CurrentDirect);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Dialog3Buttons_OnOKClick(object sender, RoutedEventArgs e)
        {
            SelectedFromToViewModel.ApplySelectedDiameter(DiameterComboBox.SelectedIndex, SystemTypeComboBox.SelectedIndex,
                CurveTypeComboBox.SelectedIndex, CurrentDirect);
            this.Close();
        }

        private void Dialog3Buttons_OnApplyClick(object sender, RoutedEventArgs e)
        {
            SelectedFromToViewModel.ApplySelectedDiameter(DiameterComboBox.SelectedIndex, SystemTypeComboBox.SelectedIndex,
                CurveTypeComboBox.SelectedIndex, CurrentDirect);
        }

        private void Dialog3Buttons_OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }


}