using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storages.Attributes ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
    [Schema("CF4DB4C2-72AF-4C23-B382-5CD9008D149C", "Conduit & Detail Curve Storage")]
    public class ConduitAndDetailCurveModel : IDataModel
    {
        [Field( Documentation = "Conduit & Detail Curve List" )]
        public List<ConduitAndDetailCurveItemModel> ConduitAndDetailCurveData { get ; set ; } = new() ;
    }
    
    [Schema("CF4DB4C2-72AF-4C23-B392-5CD8008D249C", "Item Conduit & Detail Curve Storage" )]
    public class ConduitAndDetailCurveItemModel : IDataModel
    {
        [Field( Documentation = "Conduit Id" )]
        public string ConduitId { get ; set ; } = string.Empty ;
        
        [Field( Documentation = "Detail Curve Id" )]
        public string DetailCurveId { get ; set ; } = string.Empty ;
        
        [Field( Documentation = "Wire Type" )]
        public string WireType { get ; set ; } = string.Empty ;
        
        [Field( Documentation = "Is Leak Route" )]
        public bool IsLeakRoute { get ; set ; }
    }
}