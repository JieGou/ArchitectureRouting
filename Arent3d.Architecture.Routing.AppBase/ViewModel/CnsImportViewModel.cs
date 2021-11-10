using System;
using Arent3d.Architecture.Routing.Storable;
using Arent3d.Architecture.Routing.Storable.Model;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
    public class CnsImportViewModel : ViewModelBase
    {
        public ObservableCollection<CnsImportModel> CnsImportModels { get; set; }

        public CnsImportStorable CnsImportStorable { get; }

        public CnsImportViewModel(CnsImportStorable cnsStorables)
        {
            CnsImportStorable = cnsStorables;
            CnsImportModels = new ObservableCollection<CnsImportModel>(cnsStorables.CnsImportData);
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
                (p) => { cnsStorables.CnsImportData = CnsImportModels; } // Execute()
            );
        }

        public ICommand ReadFileCommand { get; set; }
        public ICommand AddRowCommand { get; set; }
        public ICommand DeleteRowCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand WriteFileCommand { get; set; }

        private void ReadFile()
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".cns"; // Default file extension
            dlg.Filter = "CNS files (.cns)|*.cns"; // Filter files by extension

            // Show open file dialog box
            bool? result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
                CnsImportModels.Clear();
                var index = 1;
                var inputData = System.IO.File.ReadLines(filename);
                foreach (string line in inputData)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        CnsImportModels.Add(new CnsImportModel(index, line.Trim()));
                        index++;
                    }
                }
            }
        }

        private void AddRow()
        {
            CnsImportModels.Add(new CnsImportModel(CnsImportModels.Count + 1, ""));
        }

        private void DeleteRow(int index)
        {
            if (index == -1) return;
            CnsImportModels.RemoveAt(index);
            UpdateSequence();
        }

        private void WriteFile()
        {
            // Configure open file dialog box
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".cns"; // Default file extension
            dlg.Filter = "CNS files (.cns)|*.cns"; // Filter files by extension

            // Show open file dialog box
            bool? result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                string createText = "";
                foreach (var item in CnsImportModels)
                {
                    if (!string.IsNullOrEmpty(item.CategoryName))
                    {
                        createText += item.CategoryName.Trim() + Environment.NewLine + Environment.NewLine;
                    }
                }

                File.WriteAllText(dlg.FileName, createText.Trim());
            }
        }

        private void UpdateSequence()
        {
            for (int i = 0; i < CnsImportModels.Count; i++)
            {
                CnsImportModels[i].Sequence = i + 1;
            }
        }
        
    }
}
