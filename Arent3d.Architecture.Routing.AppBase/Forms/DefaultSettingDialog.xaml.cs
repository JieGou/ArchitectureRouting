using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

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

    private void DeleteImportDwgMappingItem( object sender, RoutedEventArgs e )
    {
      if ( sender is not Button { DataContext: ImportDwgMappingModel item } ) return ;
      ViewModel.DeleteImportDwgMappingItem( item ) ;
    }

    private void SelectDwgFile( object sender, RoutedEventArgs e )
    {
      if ( sender is not Button { DataContext: ImportDwgMappingModel item } ) return ;
      ViewModel.LoadDwgFile( item ) ;
    }
  }
}