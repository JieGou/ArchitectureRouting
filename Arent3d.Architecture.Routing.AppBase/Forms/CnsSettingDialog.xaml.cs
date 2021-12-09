﻿using Arent3d.Architecture.Routing.AppBase.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
    public partial class CnsSettingDialog : Window
    {
        public CnsSettingDialog(CnsSettingViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
        
        private void Update_Click(object sender, RoutedEventArgs e)
        {
            grdCategories.CurrentCell = new DataGridCellInfo(grdCategories.SelectedItem, grdCategories.Columns[1]);
            grdCategories.BeginEdit();
        }
        
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}