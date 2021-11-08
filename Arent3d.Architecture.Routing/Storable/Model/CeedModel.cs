using System.Collections.Generic ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class CeedModel
  {
    public string CeeDModelNumber { get ; set ; }
    public string CeeDSetCode { get ; set ; }
    public List<string> GeneralDisplayDeviceSymbol { get ; set ; }
    public string Name { get ; set ; }
    public List<string> ModelNumber { get ; set ; }
    public string FloorPlanSymbol { get ; set ; }
    
    public CeedModel( string ceeDModelNumber, string ceeDSetCode, List<string> generalDisplayDeviceSymbol, string name, List<string> modelNumber, string floorPlanSymbol )
    {
      CeeDModelNumber = ceeDModelNumber ;
      CeeDSetCode = ceeDSetCode ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      Name = name ;
      ModelNumber = modelNumber ;
      FloorPlanSymbol = floorPlanSymbol ;
    }
  }
}