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
    
    public byte[]? InstrumentationSymbol { get ; set ; }
    public List<Byte[]>? FloorPlanShapes { get ; set ; }
    public string Name { get ; set ; }
    public BitmapImage? FloorImages { get ; set ; }
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
      InstrumentationSymbol = null ;
      FloorImages = null ;
    }
    
    public CeedModel( string ceeDModelNumber, string ceeDSetCode, string generalDisplayDeviceSymbol, string modelNumber, byte[] floorPlanShapes, byte[]? instrumentationSymbol, string name )
    {
      CeeDModelNumber = ceeDModelNumber ;
      CeeDSetCode = ceeDSetCode ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      ModelNumber = modelNumber ;
      FloorPlanSymbol = "" ;
     // FloorPlanShapes = floorPlanShapes ;
      Name = name ;
      InstrumentationSymbol =instrumentationSymbol ;
      FloorImages = ConvertToBitmaps( floorPlanShapes) ;
    }

    private BitmapImage? ConvertToBitmaps( byte[] floorPlanShapes )
    {
      var image = new BitmapImage() ;
      try {
        if ( floorPlanShapes == null ) return image ;
        using ( var stream = new MemoryStream( floorPlanShapes ) ) {
          stream.Position = 0 ;
          image.BeginInit() ;
          image.CacheOption = BitmapCacheOption.OnLoad ;
          image.StreamSource = stream ;
          image.EndInit() ;
          image.Freeze() ; // optionally make it cross-thread accessible
        }
      }
      catch ( ArgumentException e ) {
        Console.WriteLine( e ) ;
        if ( floorPlanShapes != null ) ConvertTextToImage( floorPlanShapes ) ;
      }

      return image ;
    }

    private Bitmap ConvertTextToImage(byte[] symbolByte)
    {
     var  text =System.Text.Encoding.ASCII.GetString(symbolByte).Trim();
      Bitmap bmp = new Bitmap(16, 16);
      using (Graphics graphics = Graphics.FromImage(bmp))
      {
        Font font = new Font("ＭＳ Ｐゴシック", 14);
        graphics.FillRectangle(new SolidBrush(Color.Transparent), 0, 0, bmp.Width, bmp.Height);
        graphics.DrawString(text, font, new SolidBrush(Color.Black), 0, 0);
        graphics.Flush();
        //font.Dispose();
        graphics.Dispose();
      }
      return bmp;
    }
  }
}