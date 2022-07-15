using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storages.Attributes ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
    [Schema("CF4DB4C2-71AF-4C23-B382-5CD8008D159C", "Location Type Storage" )]
    public class LocationTypeModel: IDataModel
    {
        [Field( Documentation = "Location Type" )]
        public string LocationType { get ; set ; } = string.Empty ;
    }
}