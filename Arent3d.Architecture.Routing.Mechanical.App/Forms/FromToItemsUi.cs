using System ;
using System.Collections.Generic ;
using System.Windows.Media.Imaging ;
using Arent3d.Architecture.Routing.AppBase.Forms ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Forms
{
  public class FromToItemsUi : FromToItemsUiBase
  {
    public FromToItemsUi()
    {
      FromToTreeIcons = new Dictionary<string, BitmapImage>()
      {
        {"RouteItem", new BitmapImage(new Uri( "../resources/From-ToWindow.png", UriKind.Relative ))},
        {"ConnectorItem", new BitmapImage(new Uri( "../resources/ImportFromTo.png", UriKind.Relative ))},
        {"SubRouteItem", new BitmapImage(new Uri( "../resources/ImportPS.png", UriKind.Relative ))},
        {"PassPointItem", new BitmapImage(new Uri( "../resources/InsertBranchPoint.png", UriKind.Relative ))},
        {"TerminatePointItem", new BitmapImage(new Uri( "../resources/MEP.ico", UriKind.Relative ))}
      } ;
    }
  }
}