using System ;
using System.Collections.Generic ;
using System.Drawing ;
using System.IO ;
using System.Windows.Media.Imaging ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class CeedModel
  {
    public string CeeDModelNumber { get ; set ; }
    public string CeeDSetCode { get ; set ; }
    public string GeneralDisplayDeviceSymbol { get ; set ; }
    public string ModelNumber { get ; set ; }
    public string FloorPlanSymbol { get ; set ; }
    public List<Byte[]>? FloorPlanShapes { get ; set ; }
    public string Name { get ; set ; }
    
    public BitmapImage? Image { get ; set ; }
    
    public CeedModel( string ceeDModelNumber, string ceeDSetCode, string generalDisplayDeviceSymbol, string modelNumber, string floorPlanSymbol, string name )
    {
      CeeDModelNumber = ceeDModelNumber ;
      CeeDSetCode = ceeDSetCode ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      ModelNumber = modelNumber ;
      FloorPlanSymbol = floorPlanSymbol ;
      FloorPlanShapes = null ;
      Name = name ;
      Image = null ;
    }
    
    public CeedModel( string ceeDModelNumber, string ceeDSetCode, string generalDisplayDeviceSymbol, string modelNumber, List<Byte[]> floorPlanShapes, string name )
    {
      CeeDModelNumber = ceeDModelNumber ;
      CeeDSetCode = ceeDSetCode ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      ModelNumber = modelNumber ;
      FloorPlanSymbol = "" ;
      FloorPlanShapes = floorPlanShapes ;
      Name = name ;
      Image = ToBitmap( floorPlanShapes[ 0 ] ) ;
    }
    
    public BitmapImage ToBitmap(Byte[] value)
    {
      if (value != null && value is byte[])
      {
        byte[] ByteArray = value as byte[];
        BitmapImage bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.StreamSource = new MemoryStream(ByteArray);
        bmp.EndInit();
        return bmp;
      }

      return new BitmapImage() ;
    }
  }
}