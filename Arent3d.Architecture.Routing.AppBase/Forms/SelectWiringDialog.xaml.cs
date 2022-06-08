using System.Collections.Generic ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class SelectWiringDialog : Window
  {
    public SelectWiringDialog(SelectWiringViewModel viewModel)
    {
      InitializeComponent() ;
      DataContext = viewModel ;
    }
  }
  
  public abstract class DesignSelectWiringViewModel : SelectWiringViewModel
  {
    protected DesignSelectWiringViewModel( List<WiringModel> selectWiringList ) : base( selectWiringList )
    {
    }
  }
}