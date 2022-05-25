using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class AddWiringInformationDialog 
  {
    public AddWiringInformationDialog(AddWiringInformationViewModel viewModel)
    {
      InitializeComponent() ;
      DataContext = viewModel ;
    }
  
    private void CbProperty_OnPreviewTextInput( object sender, TextCompositionEventArgs e )
    {
      CbProperty.IsDropDownOpen = true ;
    }
  }
  
  public abstract class DesignAddWiringInformationViewModel : AddWiringInformationViewModel
  {
    protected DesignAddWiringInformationViewModel( Document document, Route element ) : base( document, element )
    {
    }
  }
}