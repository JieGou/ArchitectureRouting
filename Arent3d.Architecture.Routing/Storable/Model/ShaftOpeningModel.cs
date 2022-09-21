using System.Collections.Generic ;
using Arent3d.Revit ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
    public class ShaftOpeningModel
    {
        public string ShaftOpeningUniqueId { get ; set ; }
        public List<string> DetailUniqueIds { get ; set ; }
        public double Size { get ; set ; } 

        public ShaftOpeningModel(string? shaftOpeningUniqueId, List<string>? detailUniqueIds, double? size )
        {
            ShaftOpeningUniqueId = shaftOpeningUniqueId ?? string.Empty ;
            DetailUniqueIds = detailUniqueIds ?? new List<string>() ;
            Size = size ?? 60d.MillimetersToRevitUnits() ;
        }
    }
}