using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using ComboBox = System.Windows.Controls.ComboBox ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class DefaultSettingDialog
  {
    public DefaultSettingViewModel ViewModel => (DefaultSettingViewModel) DataContext ;

    public DefaultSettingDialog( DefaultSettingViewModel defaultSettingViewModel )
    {
      InitializeComponent() ;
      DataContext = defaultSettingViewModel ;
    }

    private void Button_Apply_Click( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
    }

    private void BtnAdd_Click( object sender, RoutedEventArgs e )
    {
      ViewModel.AddImportDwgMappingModel() ;
      DataGridDwg.ItemsSource = ViewModel.ImportDwgMappingModels ;
    }

    private void DeleteImportDwgMappingItem( object sender, RoutedEventArgs e )
    {
      if ( sender is not Button { DataContext: ImportDwgMappingModel item } ) return ;
      var importDwgMappingModels = ViewModel.ImportDwgMappingModels.Where( x => ! x.Id.Equals( item.Id ) ).ToList() ;
      ViewModel.ImportDwgMappingModels = new ObservableCollection<ImportDwgMappingModel>( importDwgMappingModels ) ;
      DataGridDwg.ItemsSource = ViewModel.ImportDwgMappingModels ;
    }

    private void SelectDwgFile( object sender, RoutedEventArgs e )
    {
      if ( sender is not Button { DataContext: ImportDwgMappingModel item } ) return ;
      ViewModel.LoadDwgFile( item ) ;
    }
  }
}