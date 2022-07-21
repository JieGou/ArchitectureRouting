using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class EraseAllLimitRackCommandBase : EraseLimitRackCommandBase
  {
    protected override IEnumerable<string> GetLimitRackIds( UIDocument ui, Document doc )
    {
      var allLimitRack = GetAllLimitRackInstance( doc ) ;
      var allLimitRackIds = allLimitRack.Select( x => x.UniqueId ) ;
      return allLimitRackIds ;
    }

    protected override IEnumerable<string> GetBoundaryCableTraysFromLimitRacks( Document doc, IEnumerable<string> limitRackIds )
    {
      var boundaryCableTraysIds = new FilteredElementCollector( doc )
        .OfClass( typeof( CurveElement ) )
        .OfType<CurveElement>()
        .Where( x => null != x.LineStyle && ( x.LineStyle as GraphicsStyle )!.GraphicsStyleCategory.Name == BoundaryCableTrayLineStyleName )
        .Select( x => x.UniqueId )
        .ToList() ;
      return boundaryCableTraysIds ;
    }
  }
}