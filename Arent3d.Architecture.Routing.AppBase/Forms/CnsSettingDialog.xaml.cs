using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.Storable.Model ;
using System.ComponentModel ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CnsSettingDialog : Window
  {
    private int _editingRowIndex = -1 ;
    private readonly CnsSettingViewModel _cnsSettingViewModel ;

    public CnsSettingDialog( CnsSettingViewModel viewModel )
    {
      InitializeComponent() ;
      DataContext = viewModel ;
      _cnsSettingViewModel = viewModel ;
    }

    private void Update_Click( object sender, RoutedEventArgs e )
    {
      if ( grdCategories.SelectedItem == null ) return ;
      var selectedItem = ( (CnsSettingModel) grdCategories.SelectedItem ) ;
      if ( selectedItem.CategoryName == "未設定" ) return ;
      if ( CheckDuplicateName( e ) ) return ;
      grdCategories.IsReadOnly = false ;
      grdCategories.CurrentCell = new DataGridCellInfo( grdCategories.SelectedItem, grdCategories.Columns[ 1 ] ) ;
      grdCategories.BeginEdit() ;
    }

    private void Close_Dialog()
    {
      SetEmptyDuplicateName() ;
      DialogResult = true ;
      Close() ;
    }

    private void CnsSettingDialog_Closing( object sender, CancelEventArgs e )
    {
      DialogResult ??= false ;
    }

    private void GrdCategories_OnCellEditEnding( object sender, DataGridCellEditEndingEventArgs e )
    {
      if ( DialogResult != false ) {
        var isDuplicateName = grdCategories.ItemsSource.Cast<CnsSettingModel>().Where( x => ! string.IsNullOrEmpty( x.CategoryName ) ).GroupBy( x => x.CategoryName ).Any( g => g.Count() > 1 ) ;
        if ( isDuplicateName ) {
          MessageBox.Show( "工事項目名称がすでに存在しています。再度工事項目名称を入力してください。" ) ;
          _editingRowIndex = e.Row.GetIndex() ;
          e.Cancel = true ;
          return ;
        }
      }
      _editingRowIndex = -1 ;
      grdCategories.IsReadOnly = true ;
    }

    private void AddNewRow_Click( object sender, RoutedEventArgs e )
    {
      if ( CheckDuplicateName( e ) ) return ;
      if ( _cnsSettingViewModel.AddRowCommand.CanExecute( null ) )
        _cnsSettingViewModel.AddRowCommand.Execute( null ) ;
      grdCategories.IsReadOnly = false ;
    }

    private void Delete_Click( object sender, RoutedEventArgs e )
    {
      if ( CheckDuplicateName( e ) ) return ;
      if ( _cnsSettingViewModel.DeleteRowCommand.CanExecute( grdCategories.SelectedIndex ) )
        _cnsSettingViewModel.DeleteRowCommand.Execute( grdCategories.SelectedIndex ) ;
    }

    private void Export_Click( object sender, RoutedEventArgs e )
    {
      if ( CheckDuplicateName( e ) ) return ;
      if ( _cnsSettingViewModel.WriteFileCommand.CanExecute( null ) )
        _cnsSettingViewModel.WriteFileCommand.Execute( null ) ;
    }

    private void Import_Click( object sender, RoutedEventArgs e )
    {
      if ( CheckDuplicateName( e ) ) return ;
      if ( _cnsSettingViewModel.ReadFileCommand.CanExecute( null ) )
        _cnsSettingViewModel.ReadFileCommand.Execute( null ) ;
    }

    private void SymbolApply_Click( object sender, RoutedEventArgs e )
    {
      if ( CheckDuplicateName( e ) ) return ;
      Close_Dialog() ;
      if ( _cnsSettingViewModel.SetConstructionItemForSymbolsCommand.CanExecute( grdCategories.SelectedIndex ) )
        _cnsSettingViewModel.SetConstructionItemForSymbolsCommand.Execute( grdCategories.SelectedIndex ) ;
    }

    private void ConduitsApply_Click( object sender, RoutedEventArgs e )
    {
      if ( CheckDuplicateName( e ) ) return ;
      Close_Dialog() ;
      if ( _cnsSettingViewModel.SetConstructionItemForConduitsCommand.CanExecute( grdCategories.SelectedIndex ) )
        _cnsSettingViewModel.SetConstructionItemForConduitsCommand.Execute( grdCategories.SelectedIndex ) ;
    }

    private void Save_Click( object sender, RoutedEventArgs e )
    {
      if ( CheckDuplicateName( e ) ) return ;
      Close_Dialog() ;
      if ( _cnsSettingViewModel.SaveCommand.CanExecute( null ) )
        _cnsSettingViewModel.SaveCommand.Execute( null ) ;
    }

    private bool CheckDuplicateName( RoutedEventArgs e )
    {
      if ( ! grdCategories.ItemsSource.Cast<CnsSettingModel>().Where( x => ! string.IsNullOrEmpty( x.CategoryName ) ).GroupBy( x => x.CategoryName ).Any( g => g.Count() > 1 ) ) return false ;
      MessageBox.Show( "工事項目名称がすでに存在しています。再度工事項目名称を入力してください。" ) ;
      grdCategories.SelectedIndex = _editingRowIndex ;
      e.Handled = true ;
      return true ;
    }

    private void SetEmptyDuplicateName()
    {
      var duplicateCategoryName = grdCategories.ItemsSource.Cast<CnsSettingModel>().Where( x => ! string.IsNullOrEmpty( x.CategoryName ) ).GroupBy( x => x.CategoryName ).Where( g => g.Count() > 1 ).ToList() ;

      if ( ! duplicateCategoryName.Any() ) return ;
      var sequences = duplicateCategoryName.FirstOrDefault()!.Select( c => c.Sequence ).ToList() ;
      for ( var i = 0 ; i < sequences.Count() ; i++ ) {
        if ( i != 0 )
          grdCategories.ItemsSource.Cast<CnsSettingModel>().ToList()[ sequences[ i ] - 1 ].CategoryName = string.Empty ;
      }
    }
  }
}