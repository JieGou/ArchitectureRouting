using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel;
using System.IO ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Storable.Model;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
    public class DetailTableViewModel : ViewModelBase
    {
        public ObservableCollection<DetailTableModel> ConduitInformationModels { get; set; }

        public  bool IsCreateSchedule { get; set; }

        public ICommand SaveConduitInformationCommand { get; set; }

        public ICommand SaveAndCreateConduitInformationCommand { get; set; }
        
        public List<CreateDetailTableCommandBase.ComboboxItemType> ConduitTypes { get ; set ; }

        public List<CreateDetailTableCommandBase.ComboboxItemType> ConstructionItems { get ; set ; }
        
        public DetailTableViewModel(ObservableCollection<DetailTableModel> conduitInformationModels, List<CreateDetailTableCommandBase.ComboboxItemType> conduitTypes, List<CreateDetailTableCommandBase.ComboboxItemType> constructionItems )
        {
            ConduitInformationModels = conduitInformationModels;
            IsCreateSchedule = false ;

            SaveConduitInformationCommand = new RelayCommand<object>(
                (p) => true, // CanExecute()
                (p) => { SaveConduitInformation(); } // Execute()
            );

            SaveAndCreateConduitInformationCommand = new RelayCommand<object>(
                (p) => true, // CanExecute()
                (p) => {SaveAndCreateConduitInformation(); } // Execute()
            );

            ConduitTypes = conduitTypes ;
            ConstructionItems = constructionItems ;
        }

        private void SaveConduitInformation()
        {
            // Configure open file dialog box
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".ctl"; // Default file extension
            dlg.Filter = "CTL files|*.ctl"; // Filter files by extension

            // Show open file dialog box
            bool? result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                string createText = "";
                foreach (var item in ConduitInformationModels) {
                    string line = String.Join( ",", new string?[]{item.Floor,item.Remark,item.ConstructionClassification,item.ConstructionItems,item.DetailSymbol,item.EarthSize,item.EarthType,item.PlumbingSize,item.PlumbingType,item.PlumbingItems,item.WireBook,item.WireSize,item.WireStrip,item.WireType,item.NumberOfGrounds,item.NumberOfPlumbing}) ;
                    createText += line.Trim() + Environment.NewLine ;
                }

                if (!string.IsNullOrWhiteSpace(createText.Trim()) && createText.Trim() != "未設定")
                {
                    File.WriteAllText(dlg.FileName, createText.Trim());
                }
            }
        }

        private void SaveAndCreateConduitInformation()
        {
            SaveConduitInformation() ;
            IsCreateSchedule = true ;
        }
    }
}
