using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Data ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;
using KeyEventArgs = System.Windows.Input.KeyEventArgs ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class SymbolInformationDialog
  {
    public SymbolInformationDialog( SymbolInformationViewModel viewModel )
    {
      InitializeComponent() ;
      DataContext = viewModel ;
      CbSymbolKind.ItemsSource = viewModel.SymbolKinds ;
      CbSymbolCoordinate.ItemsSource = viewModel.SymbolCoordinates ;
      CbSymbolColor.ItemsSource = viewModel.SymbolColors ; 
    }

    private void ButtonOK_Click( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void ButtonCancel_Click( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
      Close() ;
    }

    private void OnComboboxBuzaiCDEnter( object sender, KeyEventArgs e )
    {
      var comboBox = (System.Windows.Controls.ComboBox) sender ;   
      var viewModel = ( (SymbolInformationViewModel) DataContext ) ;
      if ( e.Key == Key.Enter ) {
        comboBox.Text = string.Empty ; 
      } else {
        comboBox.IsDropDownOpen = true ;
      }
    }
  }

  public abstract class DesignSymbolInformationViewModel : SymbolInformationViewModel
  {
    protected DesignSymbolInformationViewModel( Document? document, SymbolInformationModel? symbolInformationModel ) : base( document, symbolInformationModel )
    {
    }
  }
}