namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class ElectricalSymbolModel
  {
    public string UniqueId { get ; }
    public string FloorPlanSymbol { get ; }
    public string GeneralDisplayDeviceSymbol { get ; }
    public string WireType { get ; }
    public string WireSize { get ; }
    public string WireStrip { get ; }
    public string PipingType { get ; }
    public string PipingSize { get ; }
    public bool IsExposure { get ; }
    public bool IsInDoor { get ; }

    public ElectricalSymbolModel( 
      string uniqueId,
      string floorPlanSymbol,
      string generalDisplayDeviceSymbol,
      string wireType, 
      string wireSize, 
      string wireStrip,
      string pipingType,
      string pipingSize,
      bool isExposure,
      bool isInDoor)
    {
      UniqueId = uniqueId ;
      FloorPlanSymbol = floorPlanSymbol ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      WireType = wireType ;
      WireSize = wireSize ;
      WireStrip = wireStrip ;
      PipingType = pipingType ;
      PipingSize = pipingSize ;
      IsExposure = isExposure ;
      IsInDoor = isInDoor ;
    }
  }
}
