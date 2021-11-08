using System;
using System.IO;
using Arent3d.Architecture.Routing.AppBase.ViewModel;
using System.Windows;
using System.Windows.Controls;
using Arent3d.Architecture.Routing.Storable.Model;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
    public partial class CnsImportDialog : Window
    {
        private CnsImportViewModel model;

        public CnsImportDialog(CnsImportViewModel viewModel)
        {
            InitializeComponent();
            model = viewModel;
            DataContext = model;
        }

        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            model.CnsImportModels.Add(new CnsImportModel(model.CnsImportModels.Count + 1, ""));
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = grdCategories.SelectedIndex;
            if (selectedIndex != -1)
            {
                model.CnsImportModels.RemoveAt(selectedIndex);
                UpdateSequen();
            }
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            grdCategories.CurrentCell = new DataGridCellInfo(grdCategories.SelectedItem, grdCategories.Columns[1]);
            grdCategories.BeginEdit();
        }

        private void ReadCNS_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".txt"; // Default file extension
            dlg.Filter = "CNS files (.cns)|*.cns"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
                model.CnsImportModels.Clear();
                var index = 1;
                var inputData = System.IO.File.ReadLines(filename);
                foreach (string line in inputData)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        model.CnsImportModels.Add(new CnsImportModel(index, line.Trim()));
                        index++;
                    }
                }
            }
          
        }

        private void WriteCNS_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".txt"; // Default file extension
            dlg.Filter = "CNS files (.cns)|*.cns"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                string createText = "";
                foreach (var item in model.CnsImportModels)
                {
                    if (!string.IsNullOrEmpty(item.CategoryName))
                    {
                        createText += item.CategoryName.Trim() + Environment.NewLine + Environment.NewLine;
                    }
                }
                File.WriteAllText(dlg.FileName, createText.Trim());
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void UpdateSequen()
        {
            int index = 1;
            foreach (var item in model.CnsImportModels)
            {
                item.Sequen = index;
                index++;
            }

            grdCategories.Items.Refresh();
        }
    }
}