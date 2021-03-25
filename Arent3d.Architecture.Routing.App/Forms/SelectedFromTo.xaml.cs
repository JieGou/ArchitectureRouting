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
        public ObservableCollection<DiameterInfo> DiameterList { get; }
        public int? DiameterIndex { get; set; }
        
        //SystemType Info
        public ObservableCollection<SystemTypeInfo> SystemTypeList { get; }
        public int? SystemTypeIndex { get; set; }
        
        //CurveType Info
        public ObservableCollection<CurveTypeInfo> CurveTypeList { get; }
        public  int? CurveTypeIndex { get; set; }

        //Direct Info
        public bool CurrentDirect { get; set; }
        
        public SelectedFromTo(Document doc, IList<double> diameterList, int diameterIndex,
             IList<MEPSystemType> systemTypeList , int systemTypeIndex,bool direct)
        {
            InitializeComponent();
            DiameterIndex = diameterIndex;
            SystemTypeIndex = systemTypeIndex;
            
            CurrentDirect = direct;
            DiameterList = new ObservableCollection<DiameterInfo>(diameterList.Select(ToDiameterInfo));
            SystemTypeList = new ObservableCollection<SystemTypeInfo>(systemTypeList.Select(ToSystemTypeInfo));
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


        //Diameter 
        public class DiameterInfo
        {
            public string Diameter { get; init; } = string.Empty;
        }

        private void DiameterComboBox_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (DiameterComboBox.IsDropDownOpen)
            {
                DiameterInfo item = (DiameterInfo)DiameterComboBox.SelectedItem;
                int selectedIndex = DiameterComboBox.SelectedIndex;
            }
        }
        
        //SystemType
        public class SystemTypeInfo
        {
            public string SystemTypeText { get; init; } = string.Empty;
        }
        
        private void SystemTypeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SystemTypeComboBox.IsDropDownOpen)
            {
                SystemTypeInfo item = (SystemTypeInfo) SystemTypeComboBox.SelectedItem;
                int selectedIndex = SystemTypeComboBox.SelectedIndex;
            }
        }
        
        //CurveType
        public class CurveTypeInfo
        {
            public string CurveTypeText { get; init; } = string.Empty;
        }
        
        private void CurveTypeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurveTypeInfo item = (CurveTypeInfo) CurveTypeComboBox.SelectedItem;
            int selectedIndex = CurveTypeComboBox.SelectedIndex;
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

        
        //Apply Button
        private void ApplyButton_OnClick(object sender, RoutedEventArgs e)
        {
            SelectedFromToViewModel.ApplySelectedDiameter(DiameterComboBox.SelectedIndex, SystemTypeComboBox.SelectedIndex, CurrentDirect);
            this.Close();
        }


        
    }


}