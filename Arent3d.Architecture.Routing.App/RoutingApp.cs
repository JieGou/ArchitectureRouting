using System.ComponentModel ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App
{
  /// <summary>
  /// Entry point of auto routing application. This class calls UI initializers.
  /// </summary>
  [Revit.RevitAddin( "{077B2D5D-D1EE-4511-9349-350745120633}" )]
  [DisplayName( "Routing" )]
  public class RoutingApp : IExternalApplication
  {
    public Result OnStartup( UIControlledApplication application )
    {
      RoutingAppUI.SetupRibbon( application ) ;

      return Result.Succeeded ;
    }

    public Result OnShutdown( UIControlledApplication application )
    {
      return Result.Succeeded ;
    }
  }
}