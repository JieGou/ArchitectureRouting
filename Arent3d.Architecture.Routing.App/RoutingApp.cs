using System.ComponentModel ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App
{
  [Revit.RevitAddin( "{077B2D5D-D1EE-4511-9349-350745120633}" )]
  [DisplayName( "Routing" )]
  public class RoutingApp : IExternalApplication
  {
    public Result OnStartup( UIControlledApplication application )
    {
      SetupUI( application ) ;
      DocumentManager.Watch( application ) ;

      return Result.Succeeded ;
    }

    public Result OnShutdown( UIControlledApplication application )
    {
      DocumentManager.Unwatch( application ) ;
      return Result.Succeeded ;
    }



    private void SetupUI( UIControlledApplication app )
    {
      // TODO
    }
  }

}