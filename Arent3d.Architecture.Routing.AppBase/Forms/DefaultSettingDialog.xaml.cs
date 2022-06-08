using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;
using ImportDwgMappingModel = Arent3d.Architecture.Routing.AppBase.Model.ImportDwgMappingModel ;

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