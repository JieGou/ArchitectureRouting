using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.DirectContext3D ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class HiroiMasterDialog
  { 
    public HiroiMasterDialog( HiroiMasterViewModel viewModel )
    {
      InitializeComponent() ;
      DataContext = viewModel ;
    }
   
    private void BtnCancel_Click( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
      Close();
    }
  }
  
  
  public abstract class DesignHiroiMasterViewModel : HiroiMasterViewModel
  {
    protected DesignHiroiMasterViewModel( Document? document, List<HiroiMasterModel> hiroiMasterList, List<HiroiSetMasterModel>? hiroiSetMasterEcoModels, List<HiroiSetMasterModel>? hiroiSetMasterNormalModels, bool isEcoModel ) : base( document, hiroiMasterList, hiroiSetMasterEcoModels, hiroiSetMasterNormalModels, isEcoModel )
    {
    }
  }
}