using System ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class EraseAllLimitRackCommandBase : EraseLimitRackCommandBase
  {
    protected override (IEnumerable<string> rackIds, IEnumerable<string>? detailCurverIds) GetLimitRackIds( UIDocument ui, Document doc )
    {
      var allLimitRack = GetAllLimitRackInstance( doc ) ;
      var allLimitRackIds = allLimitRack.Select( x => x.UniqueId ) ;

      return new ValueTuple<IEnumerable<string>, IEnumerable<string>?>( allLimitRackIds, null ) ;
    }

  }
}