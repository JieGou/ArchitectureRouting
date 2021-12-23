using System ;
using System.Collections.Generic ;
using System.Diagnostics.CodeAnalysis ;
using System.Drawing ;
using System.IO ;
using System.Linq ;
using System.Windows.Media.Imaging ;
using Autodesk.Revit.DB.Electrical ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  [SuppressMessage( "ReSharper", "ConvertToUsingDeclaration" )]
  public class CeedModel
  {
    public string CeeDModelNumber { get ; set ; }
    public string CeeDSetCode { get ; set ; }
    public string GeneralDisplayDeviceSymbol { get ; set ; }
    public string ModelNumber { get ; set ; }
    public string FloorPlanSymbol { get ; set ; }
    
    public string InstrumentationSymbol { get ; set ; }
    public string Name { get ; set ; }
    public  BitmapImage? InstrumentationImages { get ; set ; }
    public BitmapImage? FloorImages { get ; set ; }
  
    
    public CeedModel( string ceeDModelNumber, string ceeDSetCode, string generalDisplayDeviceSymbol, string modelNumber, string floorPlanSymbol,  string name )
    {
      CeeDModelNumber = ceeDModelNumber ;
      CeeDSetCode = ceeDSetCode ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      ModelNumber = modelNumber ;
      FloorPlanSymbol = floorPlanSymbol ;
      InstrumentationSymbol = "" ;
      Name = name ;
      InstrumentationImages = null ;
      FloorImages = null ;
    }
    public CeedModel( string ceeDModelNumber, string ceeDSetCode, string generalDisplayDeviceSymbol, string modelNumber, string floorPlanSymbol,  string instrumentationSymbol, string name )
    {
      CeeDModelNumber = ceeDModelNumber ;
      CeeDSetCode = ceeDSetCode ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      ModelNumber = modelNumber ;
      FloorPlanSymbol = floorPlanSymbol ;
      InstrumentationSymbol = instrumentationSymbol ;
      Name = name ;
      InstrumentationImages = null ;
      FloorImages = null ;
    }
    public CeedModel( string ceeDModelNumber, string ceeDSetCode, string generalDisplayDeviceSymbol, string modelNumber, List<byte[]>? symbolBytesList,  List<byte[]>? instrumentationSymbol, string name )
    {
      CeeDModelNumber = ceeDModelNumber ;
      CeeDSetCode = ceeDSetCode ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      ModelNumber = modelNumber ;
      FloorPlanSymbol = "" ;
      InstrumentationSymbol = "" ;
      Name = name ;
      InstrumentationImages = null ;// BitmapToImageSource(ConvertByteToImage( instrumentationSymbol)) ;
      FloorImages = BitmapToImageSource(ConvertByteToImage( symbolBytesList)) ;
    }

    BitmapImage? BitmapToImageSource( Bitmap? bitmap )
    {
      using ( MemoryStream memory = new MemoryStream() ) {
        if ( bitmap != null ) bitmap.Save( memory, System.Drawing.Imaging.ImageFormat.Bmp ) ;
        memory.Position = 0 ;
        BitmapImage? bitmapimage = new BitmapImage() ;
        bitmapimage.BeginInit() ;
        bitmapimage.StreamSource = memory ;
        bitmapimage.CacheOption = BitmapCacheOption.OnLoad ;
        bitmapimage.EndInit() ;

        return bitmapimage ;
      }
    }

    private Bitmap? ConvertByteToImage( List<byte[]>? symbolBytesList )
    {
      List<Bitmap> images = new List<Bitmap>() ;
      try {
        if ( symbolBytesList != null )
          foreach ( var symbolByte in symbolBytesList ) {
            using ( var ms = new MemoryStream( symbolByte ) ) {
              var image = (Bitmap) Image.FromStream( ms ) ;
              image.MakeTransparent() ;
              images.Add( image ) ;
            }
          }

        if ( images.Count == 1 ) return images.First() ;
        return MergeImages( images ) ;
      }
      catch ( Exception e ) {
        Console.WriteLine( e ) ;
        return null;
        //ConvertTextToImage( floorPlanShapes ) ;
      }
    }

    private static Bitmap MergeImages( List<Bitmap> images )
    {
      int outputImageHeight = images.OrderByDescending( c => c.Height ).Select( c => c.Height ).First() ;
      int outputImageWidth = images.Sum( item => item.Width ) + ( images.Count - 1 ) * 10 ;

      //Bitmap outputImage = new Bitmap( outputImageWidth, outputImageHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb ) ;
      var finalImage = new Bitmap( outputImageWidth, outputImageHeight ) ;
      using ( Graphics g = Graphics.FromImage( finalImage ) ) {
        g.Clear( Color.White ) ;
       // finalImage.MakeTransparent(Color.Transparent) ;
        int offset = 0 ;
        foreach ( Bitmap image in images ) {
          //image.MakeTransparent(Color.Transparent) ;
          g.DrawImage( image, new System.Drawing.Rectangle( offset, 0, image.Width, image.Height ) ) ;
          offset += image.Width + 10 ;
          // g.DrawImage(finalImage, finalImage.Width, 0, finalImage.Width, finalImage.Height);
        }
      //g.DrawImage(finalImage, finalImage.Width, 0, finalImage.Width, finalImage.Height);
      }

      return finalImage ;
    }
    private Bitmap ConvertTextToImage( byte[] symbolByte )
    {
      var text = System.Text.Encoding.ASCII.GetString( symbolByte ).Trim() ;
      Bitmap bmp = new Bitmap( 16, 16 ) ;
      try {
        using ( Graphics graphics = Graphics.FromImage( bmp ) ) {
          Font font = new Font( "ＭＳ Ｐゴシック", 14 ) ;
          graphics.FillRectangle( new SolidBrush( Color.Transparent ), 0, 0, bmp.Width, bmp.Height ) ;
          graphics.DrawString( text, font, new SolidBrush( Color.Black ), 0, 0 ) ;
          graphics.Flush() ;
          //font.Dispose();
          graphics.Dispose() ;
        }
      }
      catch ( Exception e ) {
        Console.WriteLine( e ) ;
      }

      return bmp ;
    }
    }
}