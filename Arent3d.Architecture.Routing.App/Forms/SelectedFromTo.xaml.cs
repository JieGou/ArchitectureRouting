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

namespace Arent3d.Architecture.Routing.App.Forms
{
    public partial class SelectedFromTo : Window
    {
        public ObservableCollection<DiameterInfo> DiameterList { get; }

        
        
        public int? CurrentIndex { get; set; }
        public SelectedFromTo(Document doc, IList<double> values, int currentIndex)
        {
            InitializeComponent();
            CurrentIndex = currentIndex;
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
                MessageBox.Show(item.Diameter.ToString());
            }
            
        }
    }


}