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
        public ObservableCollection<DiameterInfo> DiameterList { get; }

        public int? CurrentIndex { get; set; }
        public bool CurrentDirect { get; set; }
        
        public SelectedFromTo(Document doc, IList<double> values, int currentIndex, bool direct)
        {
            InitializeComponent();
            CurrentIndex = currentIndex;
            CurrentDirect = direct;
            DiameterList = new ObservableCollection<DiameterInfo>(values.Select(ToDiameterInfo));
        }


        private static DiameterInfo ToDiameterInfo(double value)
        {
            return new DiameterInfo {Diameter = UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.Millimeters) + " mm"};
            //return new DiameterInfo {Diameter = value + " inch"};
        }


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
                /*MessageBox.Show(item.Diameter.ToString());
                MessageBox.Show(selectedIndex.ToString());*/
                //SelectedFromToViewModel.ApplySelectedDiameter(selectedIndex);
            }
        }

        private void Direct_OnChecked(object sender, RoutedEventArgs e)
        {
            //SelectedFromToViewModel.IsDirect = CurrentDirect;
        }

        private void Direct_OnUnchecked(object sender, RoutedEventArgs e)
        {
            //SelectedFromToViewModel.IsDirect = CurrentDirect;
        }


        private void ApplyButton_OnClick(object sender, RoutedEventArgs e)
        {
            SelectedFromToViewModel.ApplySelectedDiameter(DiameterComboBox.SelectedIndex, CurrentDirect);
            this.Close();
        }
    }


}