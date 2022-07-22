using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class EraseAllLimitRackCommandBase : EraseLimitRackCommandBase
  {
    protected override (IReadOnlyCollection<string> limitRackIds,IReadOnlyCollection<LimitRackModel> limitRackModels) GetLimitRackIds( UIDocument ui, Document doc, LimitRackStorable limitRackStorable )
    {
      var allLimitRack = GetAllLimitRackInstance( doc ) ;
      var allLimitRackIds = allLimitRack.Select( x => x.UniqueId ).EnumerateAll() ;
      return (allLimitRackIds,limitRackStorable.LimitRackModels.EnumerateAll()) ;
    }

    protected override IEnumerable<string> GetBoundaryCableTrays( Document doc, IReadOnlyCollection<LimitRackModel> limitRackModels )
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