using System ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Mechanical.App
{
  /// <summary>
  /// Registers UI components of auto routing application.
  /// </summary>
  public partial class RoutingAppUI : AppUIBase
  {
    public static RoutingAppUI Create( UIControlledApplication application )
    {
      return new RoutingAppUI( application ) ;
    }

    protected override partial void UpdateUIForFamilyDocument( Document document, AppUIUpdateType updateType ) ;
    protected override partial void UpdateUIForNormalDocument( Document document, AppUIUpdateType updateType ) ;
  }
}