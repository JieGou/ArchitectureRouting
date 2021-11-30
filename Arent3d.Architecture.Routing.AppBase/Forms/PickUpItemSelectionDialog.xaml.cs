using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Windows ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class PickUpItemSelectionDialog : Window
  {
    private readonly List<PickUpReportDialog.ListBoxItem> _itemTypes ;
    public List<string> ItemsTypesSelected;
    
    public PickUpItemSelectionDialog( List<PickUpReportDialog.ListBoxItem> itemTypes )
    {
      InitializeComponent() ;
      _itemTypes = itemTypes ;
      ItemsTypesSelected = GetItemTypeSelected() ;
      LbItemType.ItemsSource = _itemTypes ;
    }

    private void Button_OK( object sender, RoutedEventArgs e )
    {
      ItemsTypesSelected = GetItemTypeSelected() ;
      DialogResult = true ;
      Close() ;
    }
    
    private void Button_Cancel( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
      Close() ;
    }

    private List<string> GetItemTypeSelected()
    {
      var itemsTypesSelected = new List<string>() ;
      foreach ( var item in _itemTypes ) {
        if ( item.TheValue == true )
          itemsTypesSelected.Add( item.TheText! ) ;
      }

      return itemsTypesSelected ;
    }
  }
}