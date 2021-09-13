using System ;
using System.Collections.Generic ;
using System.Windows.Media.Imaging ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit.I18n ;

namespace Arent3d.Architecture.Routing.Electrical.App.Forms
{
  public class FromToItemsUi : FromToItemsUiBase
  {
    private static readonly IReadOnlyDictionary<string, BitmapImage> FromToTreeIconDictionary = new Dictionary<string, BitmapImage>
    {
      { "RouteItem", new BitmapImage( new Uri( "../resources/DeleteAllFrom-To.png", UriKind.Relative ) ) },
      { "ConnectorItem", new BitmapImage( new Uri( "../resources/DeleteAllPS.png", UriKind.Relative ) ) },
      { "SubRouteItem", new BitmapImage( new Uri( "../resources/DeleteFrom-To.png", UriKind.Relative ) ) },
      { "PassPointItem", new BitmapImage( new Uri( "../resources/ExportFromTo.png", UriKind.Relative ) ) },
      { "TerminatePointItem", new BitmapImage( new Uri( "../resources/ExportPS.png", UriKind.Relative ) ) },
      { "PassPointBranchItem", new BitmapImage( new Uri( "../resources/From-ToWindow.png", UriKind.Relative ) ) },
    } ;

    public override bool UseHierarchies => false ;
    public override bool ShowSubRoutes => false ;

    public FromToItemsUi() : base( "Dialog.Forms.FromToTree.Title".GetAppStringByKeyOrDefault( "Electrical From-To" ), FromToTreeIconDictionary )
    {
    }
  }
}