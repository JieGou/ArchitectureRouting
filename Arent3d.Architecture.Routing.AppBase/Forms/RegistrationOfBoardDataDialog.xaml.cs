using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters;
using Arent3d.Architecture.Routing.AppBase.ViewModel;
using Arent3d.Architecture.Routing.Extensions;
using Arent3d.Architecture.Routing.Storable;
using Arent3d.Architecture.Routing.Storable.Model;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using MessageBox = System.Windows.MessageBox;
using ProgressBar = Arent3d.Revit.UI.Forms.ProgressBar;
using Visibility = System.Windows.Visibility;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
    public partial class RegistrationOfBoardDataDialog
    {
        private readonly Document _document;
        private RegistrationOfBoardDataViewModel? _allRegistrationOfBoardDataModels ;

        public RegistrationOfBoardDataDialog( UIApplication uiApplication )
        {
            InitializeComponent() ;
            _document = uiApplication.ActiveUIDocument.Document ;
        }

        private void Button_LoadData(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Please select 【CeeD】セットコード一覧表 file.", "Message");
            OpenFileDialog openFileDialog = new()
                {Filter = "Csv files (*.xlsx; *.xls)|*.xlsx;*.xls", Multiselect = false};
            string filePath = string.Empty;

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filePath = openFileDialog.FileName;
            }

            if (string.IsNullOrEmpty(filePath)) return;
            
            RegistrationOfBoardDataStorable registrationOfBoardDataStorable = _document.GetRegistrationOfBoardDataStorable();
            {
                List<RegistrationOfBoardDataModel> registrationOfBoardDataModelData =
                    ExcelToModelConverter.GetAllRegistrationOfBoardDataModel(filePath);
                if (!registrationOfBoardDataModelData.Any()) return;
                registrationOfBoardDataStorable.RegistrationOfBoardData = registrationOfBoardDataModelData;
                
                LoadData( registrationOfBoardDataStorable ) ;
            }
        }
        
        private void LoadData( RegistrationOfBoardDataStorable registrationOfBoardDataStorable )
        {
            var viewModel = new RegistrationOfBoardDataViewModel( registrationOfBoardDataStorable ) ;
            DataContext = viewModel ;
            _allRegistrationOfBoardDataModels = viewModel ;
            DtGrid.ItemsSource = viewModel.RegistrationOfBoardDataModels ;
        }
    }
}