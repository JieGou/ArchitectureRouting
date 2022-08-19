using System.Collections.Generic ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
    public class ShaftOpeningModel
    {
        public string ShaftOpeningUniqueId { get ; set ; }
        public List<string> DetailUniqueIds { get ; set ; }

        public ShaftOpeningModel(string? shaftOpeningUniqueId, List<string>? detailUniqueIds )
        {
            ShaftOpeningUniqueId = shaftOpeningUniqueId ?? string.Empty ;
            DetailUniqueIds = detailUniqueIds ?? new List<string>() ;
        }
    }
}