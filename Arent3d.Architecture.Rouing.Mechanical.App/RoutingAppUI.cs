using System ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase
{
  /// <summary>
  /// Registers UI components of auto routing application.
  /// </summary>
  public partial class RoutingAppUI : IAppUIBase
  {
    public static RoutingAppUI Create( UIControlledApplication application )
    {
      return new RoutingAppUI( application ) ;
    }

    public partial void UpdateUI( Document document, AppUIUpdateType updateType ) ;

    ~RoutingAppUI()
    {
      ReleaseUnmanagedResources() ;
    }

    public void Dispose()
    {
      GC.SuppressFinalize( this ) ;

      ReleaseUnmanagedResources() ;
    }

    private void ReleaseUnmanagedResources()
    {
      // Nothing to do.
    }
  }
}