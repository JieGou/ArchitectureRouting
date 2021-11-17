using System.Collections.Generic ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class CeedModel
  {
    public string CeeDModelNumber { get ; set ; }
    public string CeeDSetCode { get ; set ; }
    public string GeneralDisplayDeviceSymbol { get ; set ; }
    public string ModelNumber { get ; set ; }
    public string FloorPlanSymbol { get ; set ; }
    
    public CeedModel( string ceeDModelNumber, string ceeDSetCode, string generalDisplayDeviceSymbol, string modelNumber, string floorPlanSymbol )
    {
      CeeDModelNumber = ceeDModelNumber ;
      CeeDSetCode = ceeDSetCode ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      ModelNumber = modelNumber ;
      FloorPlanSymbol = floorPlanSymbol ;
    }
  }
}