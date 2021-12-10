using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input ;
using System.Windows.Threading ;
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
            SetDuplicateName() ;
            DialogResult = true;
            Close();
        }
        
        private void GrdCategories_OnCellEditEnding( object sender, DataGridCellEditEndingEventArgs e )
        {
           var isDuplicateName = grdCategories.ItemsSource.Cast<CnsSettingModel>().ToList()
                .GroupBy(x => x.CategoryName).Any(g => g.Count() > 1);
            if ( isDuplicateName ) {
                MessageBox.Show( "工事項目名称がすでに存在しています。再度工事項目名称を入力してください。" ) ;
                e.Cancel = true ;
                return;
            }

            grdCategories.IsReadOnly = true ;
        }

        private void ButtonAddNewRow_OnClick( object sender, RoutedEventArgs e )
        {
            grdCategories.IsReadOnly = false ;
        }

        private void SetDuplicateName()
        {
            var duplicateCategoryName = grdCategories.ItemsSource.Cast<CnsSettingModel>().GroupBy( x => x.CategoryName )
                .Where( g => g.Count() > 1 ).ToList() ;
           
            if ( !duplicateCategoryName.Any()) return ;
            var sequences = duplicateCategoryName.FirstOrDefault()!.Select(c=>c.Sequence).ToList() ;
            for ( var i = 0 ; i < sequences.Count() ; i++ ) {
              if( i!=0 )
                grdCategories.ItemsSource.Cast<CnsSettingModel>().ToList()[sequences[ i ] - 1].CategoryName = string.Empty;
            }
           
        }
    }
}
