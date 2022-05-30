using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class SymbolInformationDialog
  { 
    public SymbolInformationDialog(SymbolInformationViewModel viewModel)
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
      Close();
    }

    private void ButtonCancel_Click( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
      Close();
    }
  }
  
  public abstract class DesignSymbolInformationViewModel : SymbolInformationViewModel
  {
    protected DesignSymbolInformationViewModel( Document? document, SymbolInformationModel? symbolInformationModel ) : base( document, symbolInformationModel )
    {
    }
  }
}