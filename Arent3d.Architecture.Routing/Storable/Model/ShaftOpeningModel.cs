using System.Collections.Generic ;
using Arent3d.Revit ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
    public class ShaftOpeningModel
    {
        public int ShaftIndex { get ; set ; }
        public string ShaftOpeningUniqueId { get ; set ; }
        public List<string> CableTrayUniqueIds { get ; set ; }
        public List<string> DetailUniqueIds { get ; set ; }
        public double Size { get ; set ; } 

        public ShaftOpeningModel( int? shaftIndex, string? shaftOpeningUniqueId, List<string>? cableTrayUniqueIds, List<string>? detailUniqueIds, double? size )
        {
            ShaftIndex = shaftIndex ?? 0 ;
            ShaftOpeningUniqueId = shaftOpeningUniqueId ?? string.Empty ;
            CableTrayUniqueIds = cableTrayUniqueIds ?? new List<string>() ;
            DetailUniqueIds = detailUniqueIds ?? new List<string>() ;
            Size = size ?? 60d.MillimetersToRevitUnits() ;
        }
    }
}