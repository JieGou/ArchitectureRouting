using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storages.Attributes ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
  [Schema( "AC41D6AC-53DD-4968-BE99-67847A86F062", nameof( RackForRouteModel ) )]
  public class RackForRouteModel : IDataModel
  {
    [Field( Documentation = "Route Item List" )]
    public List<RackForRouteItem> RackForRoutes { get ; set ; } = new List<RackForRouteItem>() ;
  }
  
  [Schema( "7513A69F-2B49-454A-9FEC-294F99811DB1", nameof( RackForRouteItem ) )]
  public class RackForRouteItem : IDataModel
  {
    [Field( Documentation = "Route Name" )]
    public string RouteName { get ; set ; } = string.Empty ;

    [Field( Documentation = "Rack Id List" )]
    public List<ElementId> RackIds { get ; set ; } = new List<ElementId>() ;
  }
}