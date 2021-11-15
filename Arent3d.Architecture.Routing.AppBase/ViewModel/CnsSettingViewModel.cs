using System;
using Arent3d.Architecture.Routing.Storable;
using Arent3d.Architecture.Routing.Storable.Model;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
    public class CnsSettingViewModel : ViewModelBase
    {
        public ObservableCollection<CnsSettingModel> CnsSettingModels { get; set; }

        public CnsSettingStorable CnsSettingStorable { get; }

        public CnsSettingViewModel(CnsSettingStorable cnsStorables)
        {
            CnsSettingStorable = cnsStorables;
            CnsSettingModels = new ObservableCollection<CnsSettingModel>(cnsStorables.CnsSettingData);
            AddDefaultValue();
            ReadFileCommand = new RelayCommand<object>(
                (p) => true, // CanExecute()
                (p) => { ReadFile(); } // Execute()
            );

            WriteFileCommand = new RelayCommand<object>(
                (p) => true, // CanExecute()
                (p) => { WriteFile(); } // Execute()
            );

            AddRowCommand = new RelayCommand<object>(
                (p) => true, // CanExecute()
                (p) => { AddRow(); } // Execute()
            );

            DeleteRowCommand = new RelayCommand<int>(
                (p) => true, // CanExecute()
                DeleteRow // Execute()
            );

            SaveCommand = new RelayCommand<object>(
                (p) => true, // CanExecute()
                (p) => { cnsStorables.CnsSettingData = CnsSettingModels; } // Execute()
            );
        }

        public ICommand ReadFileCommand { get; set; }
        public ICommand WriteFileCommand { get; set; }
        public ICommand AddRowCommand { get; set; }
        public ICommand DeleteRowCommand { get; set; }
        public ICommand SaveCommand { get; set; }

        private void ReadFile()
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".cns"; // Default file extension
            dlg.Filter = "CNS files|*.cns"; // Filter files by extension

            // Show open file dialog box
            bool? result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
                CnsSettingModels.Clear();
                var index = 1;
                var inputData = System.IO.File.ReadLines(filename);
                foreach (string line in inputData)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        CnsSettingModels.Add(new CnsSettingModel(index, line.Trim()));
                        index++;
                    }
                }
            }
        }

        private void WriteFile()
        {
            // Configure open file dialog box
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".cns"; // Default file extension
            dlg.Filter = "CNS files|*.cns"; // Filter files by extension

            // Show open file dialog box
            bool? result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                string createText = "";
                foreach (var item in CnsSettingModels)
                {
                    if (!string.IsNullOrEmpty(item.CategoryName))
                    {
                        createText += item.CategoryName.Trim() + Environment.NewLine + Environment.NewLine;
                    }
                }

                if (createText.Trim() == "未設定")
                {
                    createText = "";
                }

                File.WriteAllText(dlg.FileName, createText.Trim());
            }
        }

        private void AddRow()
        {
            CnsSettingModels.Add(new CnsSettingModel(CnsSettingModels.Count + 1, ""));
        }

        private void DeleteRow(int index)
        {
            if (index == -1) return;
            CnsSettingModels.RemoveAt(index);
            AddDefaultValue();
            UpdateSequence();
        }

        private void UpdateSequence()
        {
            for (int i = 0; i < CnsSettingModels.Count; i++)
            {
                CnsSettingModels[i].Sequence = i + 1;
            }
        }

        private void AddDefaultValue()
        {
            if (CnsSettingModels.Count == 0)
            {
                CnsSettingModels.Add(new CnsSettingModel(sequence: 1, categoryName: "未設定"));
            }
        }
    }
}