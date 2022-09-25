using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storages.Attributes ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
  [Schema( "B7730C86-7D47-41AA-832E-45BBCB3B0AA8", nameof( RackFromToModel ) )]
  public class RackFromToModel : IDataModel
  {
    [Field( Documentation = "Route From-To Item List" )]
    public List<RackFromToItem> RackFromToItems { get ; set ; } = new List<RackFromToItem>() ;
  }

  [Schema( "B93ABC4C-1F30-4920-B389-8EF4C07B2F5E", nameof( RackFromToItem ) )]
  public class RackFromToItem : IDataModel
  {
    [Field( Documentation = "Rack Ids List" )]
    public List<string> UniqueIds { get ; set ; } = new List<string>() ;
  }
}