using System.Collections.Generic ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class SelectWiringDialog
  {
    public SelectWiringDialog(SelectWiringViewModel viewModel)
    {
      InitializeComponent() ;
      DataContext = viewModel ;
    }
  }
  
  public abstract class DesignSelectWiringViewModel : SelectWiringViewModel
  {
    protected DesignSelectWiringViewModel( Document document, List<WiringModel> wiringList ) : base( document, wiringList )
    {
    }
  }
}