using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
    /// <summary>
    /// SetProperty.xaml の相互作用ロジック
    /// </summary>
    public partial class SetRouteProperty : Window
    {
        public SetRouteProperty()
        {
            InitializeComponent();
        }

        public void UpdateFromToParameters( IList<double>? diameters, IList<MEPSystemType>? systemTypes, IList<MEPCurveType>? curveTypes, MEPSystemType? systemType, MEPCurveType? curveType, double? diameter )
        {
            SelectedFromToBaseDialog.UpdateFromToParameters( diameters, systemTypes, curveTypes );

            SelectedFromToBaseDialog.SystemTypeOrg = SelectedFromToBaseDialog.SystemType = SelectedFromToBaseDialog.SystemType = systemType;
            SelectedFromToBaseDialog.Diameter = diameter;

            SelectedFromToBaseDialog.CurveTypeOrg = SelectedFromToBaseDialog.CurveType = curveType;
            SelectedFromToBaseDialog.SystemType = systemType;
            SelectedFromToBaseDialog.CurveType = curveType;
            SelectedFromToBaseDialog.ResetDialog();

        }
        

        private void Dialog2Buttons_OnLeftOnClick( object sender, RoutedEventArgs e )
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Dialog2Buttons_OnRightOnClick( object sender, RoutedEventArgs e )
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Dialog2Buttons_Loaded( object sender, RoutedEventArgs e )
        {
        }

        public MEPSystemType? GetSelectSystemType(  )
        {
            if ( SelectedFromToBaseDialog.SystemTypeComboBox.SelectedIndex == -1 ) return null;
            return SelectedFromToBaseDialog.SystemTypes[ SelectedFromToBaseDialog.SystemTypeComboBox.SelectedIndex ];
        }

        public MEPCurveType GetSelectCurveType()
        {
            return SelectedFromToBaseDialog.CurveTypes[ SelectedFromToBaseDialog.CurveTypeComboBox.SelectedIndex ];
        }

        public double GetSelectDiameter()
        {
            return SelectedFromToBaseDialog.Diameters[ SelectedFromToBaseDialog.DiameterComboBox.SelectedIndex ];
        }

        public bool? GetCurrentDirect()
        {
            return  SelectedFromToBaseDialog.Direct.IsChecked;
        }

        public bool? GetCurrentHeightSetting()
        {
            return SelectedFromToBaseDialog.HeightSetting.IsChecked;
        }

        public double? GetFixedHeight()
        {
            double? fixedBopHeight = SelectedFromToBaseDialog.HeightNud.Value ;
            if ( fixedBopHeight == 0 ) {
                fixedBopHeight = null ;
            }
            return fixedBopHeight;
        }

       public AvoidType GetAvoidTypeKey()
        { 
            return SelectedFromToBaseDialog.AvoidTypeKey;
        }

    }
}
