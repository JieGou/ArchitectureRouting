using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class DisplaySettingByGradeDialog : Window
  {
    public DisplaySettingByGradeDialog( DisplaySettingByGradeViewModel viewModel )
    {
      DataContext = viewModel ;
      InitializeComponent() ;
    }

    private DisplaySettingByGradeViewModel ViewModel => (DisplaySettingByGradeViewModel)DataContext ;
  }

  public class DesignDisplaySettingByGradeViewModel : DisplaySettingByGradeViewModel
  {
    public DesignDisplaySettingByGradeViewModel( Document document ) : base( default ! )
    {
    }
  }
}