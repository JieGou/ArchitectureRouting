namespace Arent3d.Revit.UI
{
  public static class UiThread
  {
    public static System.Windows.Threading.Dispatcher RevitUiDispatcher => Autodesk.Windows.ComponentManager.Ribbon.Dispatcher ;
  }
}