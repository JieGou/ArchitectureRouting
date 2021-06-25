using System.Windows ;
using System.Windows.Interop ;
using Autodesk.Revit.UI ;

namespace Arent3d.Revit.UI
{
  public abstract class RevitDialog : Window
  {
    protected RevitDialog( UIDocument uiDoc )
    {
      //Set RevitWindow To owner
      new WindowInteropHelper( this ).Owner = uiDoc.Application.MainWindowHandle ;
    }
  }
}