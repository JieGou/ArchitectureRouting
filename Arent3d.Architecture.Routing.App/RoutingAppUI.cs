using System ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App
{
  /// <summary>
  /// Registers UI components of auto routing application.
  /// </summary>
  public partial class RoutingAppUI : IDisposable
  {
    public static RoutingAppUI Create( UIControlledApplication application )
    {
      return new RoutingAppUI( application ) ;
    }
    
    public enum UpdateType
    {
      Start,
      Finish,
      Change,
    }

    public partial void UpdateRibbon( Document document, UpdateType updateType ) ;



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