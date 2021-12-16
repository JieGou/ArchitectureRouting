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
    
    public string InstrumentationSymbol { get ; set ; }
    public List<Byte[]>? FloorPlanShapes { get ; set ; }
    public string Name { get ; set ; }
    public List<BitmapImage>? FloorImages { get ; set ; }
    public List<BitmapImage>? InstrumentationImages { get ; set ; }
    
    public CeedModel( string ceeDModelNumber, string ceeDSetCode, string generalDisplayDeviceSymbol, string modelNumber, string floorPlanSymbol, string name )
    {
      CeeDModelNumber = ceeDModelNumber ;
      CeeDSetCode = ceeDSetCode ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      ModelNumber = modelNumber ;
      FloorPlanSymbol = floorPlanSymbol ;
      FloorPlanShapes = null ;
      Name = name ;
      InstrumentationSymbol = "" ;
      FloorImages = null ;
    }
    
    public CeedModel( string ceeDModelNumber, string ceeDSetCode, string generalDisplayDeviceSymbol, string modelNumber, List<Byte[]> floorPlanShapes, string name, string instrumentationSymbol )
    {
      CeeDModelNumber = ceeDModelNumber ;
      CeeDSetCode = ceeDSetCode ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      ModelNumber = modelNumber ;
      FloorPlanSymbol = "" ;
      FloorPlanShapes = floorPlanShapes ;
      Name = name ;
      InstrumentationSymbol = instrumentationSymbol ;
      FloorImages = ConvertToBitmaps( floorPlanShapes) ;
    }

    private List<BitmapImage> ConvertToBitmaps(List<byte[]> floorPlanShapes)
    {
      var images = new List<BitmapImage>() ;
      try {
        foreach ( var imageData in floorPlanShapes ) {
          var bitmap = new BitmapImage();
          using (var stream = new MemoryStream(imageData))
          {
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze(); // optionally make it cross-thread accessible
            images.Add(bitmap);
          }
        }
      }
      catch ( Exception e ) {
        Console.WriteLine( e ) ;
        throw ;
      }

      return images ;
    }
  }
}