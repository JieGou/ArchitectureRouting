using System.Windows.Controls ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  public class FromToVirtualizingStackPanel: VirtualizingStackPanel
  {
    /// <summary>
    /// Publically expose BringIndexIntoView.
    /// </summary>
    public void BringIntoView(int index)
    {
      this.BringIndexIntoView(index);
    }
  }
}