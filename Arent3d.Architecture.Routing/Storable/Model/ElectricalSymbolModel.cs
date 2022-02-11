using System.ComponentModel ;
using System.Drawing ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class ElectricalSymbolModel
  {
    public string UniqueId { get ; set ; }
    public string FloorPlanSymbol { get ; set ; }
    public string GeneralDisplayDeviceSymbol { get ; set ; }
    public string WireType { get ; set ; }
    public string WireSize { get ; set ; }
    public string WireStrip { get ; set ; }
    public string PipingType { get ; set ; }
    public string PipingSize { get ; set ; }

    public ElectricalSymbolModel( 
      string uniqueId,
      string floorPlanSymbol,
      string generalDisplayDeviceSymbol,
      string wireType, 
      string wireSize, 
      string wireStrip,
      string pipingType,
      string pipingSize)
    {
      UniqueId = uniqueId ;
      FloorPlanSymbol = floorPlanSymbol ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      WireType = wireType ;
      WireSize = wireSize ;
      WireStrip = wireStrip ;
      PipingType = pipingType ;
      PipingSize = pipingSize ;
    }
  }
}
