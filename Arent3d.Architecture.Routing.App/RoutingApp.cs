using System.ComponentModel ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App
{
  /// <summary>
  /// Entry point of auto routing application. This class calls UI initializers.
  /// </summary>
  [Revit.RevitAddin( "{077B2D5D-D1EE-4511-9349-350745120633}" )]
  [DisplayName( "Routing" )]
  public class RoutingApp : ExternalApplicationBase
  {
    protected override IAppUIBase? CreateAppUI( UIControlledApplication application )
    {
      return RoutingAppUI.Create( application ) ;
    }

    protected override void OnDocumentListenStarted( Document document )
    {
      base.OnDocumentListenStarted( document ) ;

      DocumentMapper.Register( document ) ;
    }

    protected override void OnDocumentListenFinished( Document document )
    {
      base.OnDocumentListenFinished( document ) ;

      DocumentMapper.Unregister( document ) ;
    }
  }
}