using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.ViewModel;
using System.Windows;
using System.Windows.Controls;
using Arent3d.Architecture.Routing.Storable.Model ;

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
            if( grdCategories.SelectedItem == null) return;
            var selectedItem = ( (CnsSettingModel) grdCategories.SelectedItem ) ;
            if ( selectedItem.CategoryName == "未設定" ) return ;
            grdCategories.IsReadOnly = false ;
            grdCategories.CurrentCell = new DataGridCellInfo( grdCategories.SelectedItem, grdCategories.Columns[ 1 ] ) ;
            grdCategories.BeginEdit() ;
        }
        
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
        
        private void GrdCategories_OnCellEditEnding( object sender, DataGridCellEditEndingEventArgs e )
        {
            grdCategories.IsReadOnly = true ;
        }

        private void ButtonAddNewRow_OnClick( object sender, RoutedEventArgs e )
        {
            grdCategories.IsReadOnly = false ;
        }

        private void ButtonSave_OnClick( object sender, RoutedEventArgs e )
        {
            if ( CheckDuplicateName() ) {
                MessageBox.Show( "工事項目名称がすでに存在しています。再度工事項目名称を入力してください。" ) ;
                return ;
            }
            DialogResult = true ;
            Close() ;
        }
        private void ButtonApply_OnClick( object sender, RoutedEventArgs e )
        {
            if ( CheckDuplicateName() ) {
                MessageBox.Show( "工事項目名称がすでに存在しています。再度工事項目名称を入力してください。" ) ;
                return ;
            }
            DialogResult = true ;
            Close() ;
        }

        private bool CheckDuplicateName()
        {
            return grdCategories.ItemsSource.Cast<CnsSettingModel>().ToList()
                .GroupBy(x => x.CategoryName).Any(g => g.Count() > 1);
        }
    }
}
