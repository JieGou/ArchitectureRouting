using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

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
    }
  }
  
  public abstract class DesignSymbolInformationViewModel : SymbolInformationViewModel
  { 
  }
}