using System ;
using System.Collections.Generic ;
using System.Diagnostics.CodeAnalysis ;
using System.Drawing ;
using System.IO ;
using System.Linq ;
using System.Text ;
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
    public CeedModel( string ceeDModelNumber, string ceeDSetCode, string generalDisplayDeviceSymbol, string modelNumber, List<Image>? symbolBytesList, string floorPlanSymbol,  string name )
    {
      CeeDModelNumber = ceeDModelNumber ;
      CeeDSetCode = ceeDSetCode ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      ModelNumber = modelNumber ;
      FloorPlanSymbol = floorPlanSymbol ;
      InstrumentationSymbol = "" ;
      Name = name ;
      InstrumentationImages = null ;// BitmapToImageSource(ConvertByteToImage( instrumentationSymbol)) ;
      FloorImages = BitmapToImageSource(GetImage( symbolBytesList)) ;
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
    private Bitmap? GetImage( List<Image>? symbolImages )
    { try {
      if ( symbolImages != null && symbolImages.Count == 1 ) return (Bitmap) symbolImages[0] ;
      return MergeImages( symbolImages ) ;
      // List<Bitmap> images = new List<Bitmap>() ;
      //
      //   if ( symbolImages != null )
      //     foreach ( var image in symbolImages ) {
      //       using ( var ms = new MemoryStream( image ) ) {
      //         var new = (Bitmap) Image.FromStream( ms ) ;
      //         images.Add(image);
      //       }
      //    
      //     }
      //
      //   if ( images.Count == 1 ) return images.First() ;
      //   return MergeImages( images ) ;
      }
      catch ( Exception e ) {
        Console.WriteLine( e ) ;
        return null;
        //ConvertTextToImage( floorPlanShapes ) ;
      }
    }
    // private Bitmap? ConvertByteToImage( List<byte[]>? symbolBytesList )
    // {
    //   List<Bitmap> images = new List<Bitmap>() ;
    //   try {
    //     if ( symbolBytesList != null )
    //       foreach ( var symbolByte in symbolBytesList ) {
    //         using ( var ms = new MemoryStream( symbolByte ) ) {
    //           var image = (Bitmap) Image.FromStream( ms ) ;
    //           //Bitmap newImg = new Bitmap(image.Width, image.Height);
    //           // Graphics g = Graphics.FromImage(image);
    //           // g.Clear(Color.Green);
    //           // g.DrawImage( newImg, new System.Drawing.Rectangle( 0, 0, image.Width, image.Height ) ) ;
    //           images.Add(image);
    //         }
    //      
    //       }
    //
    //     if ( images.Count == 1 ) return images.First() ;
    //     return MergeImages( images ) ;
    //   }
    //   catch ( Exception e ) {
    //     Console.WriteLine( e ) ;
    //     return null;
    //     //ConvertTextToImage( floorPlanShapes ) ;
    //   }
    // }

    private static Bitmap MergeImages( List<Image>? images )
    {
      try {
        if ( images != null ) {
          var maxImageHeight = images.OrderByDescending( c => c.Height ).Select( c => c.Height ).First() ;
          //var minImageHeight = images.OrderBy( c => c.Height ).Select( c => c.Height ).First() ;
          // var centerPoint = ( maxImageHeight - minImageHeight ) / 2 ;
          var padding = 50 ;
          var imageWidth = images.Sum( item => item.Width ) + ( images.Count - 1 ) * padding ;
          var finalImage = new Bitmap( imageWidth, maxImageHeight,System.Drawing.Imaging.PixelFormat.Format32bppArgb ) ;
          var textImage = ConvertTextToImage( "又は", 25 ) ; //fix default height
          using ( Graphics g = Graphics.FromImage( finalImage ) ) {
            g.Clear(Color.White);
            var offset = 0 ;

            for ( var i = 0 ; i < images.Count ; i++ ) {
              Image image = images[ i ] ;
              //image.Save( @"D:\GIT\Arent\a" + i + ".png" ) ;
              //g.DrawImage( image, new System.Drawing.Rectangle( offset, 0, image.Width, image.Height ) ) ;
              g.DrawImage(image, new Rectangle(new Point(offset,0), image.Size), new Rectangle(new Point(), image.Size), GraphicsUnit.Pixel);  
              offset += image.Width + padding ;
             // g.DrawImage(finalImage, finalImage.Width, 0, finalImage.Width, finalImage.Height);
             
            
              // if ( i == images.Count - 1 ) continue ;
              // g.DrawImage( textImage, new System.Drawing.Rectangle( offset, 2, textImage.Width, textImage.Height ) ) ;
              // offset += textImage.Width + 4 ;
            }
            // Color backColor = finalImage.GetPixel(1, 1);
            // finalImage.MakeTransparent(backColor);
           // g.DrawImage(finalImage, finalImage.Width, 0, finalImage.Width, finalImage.Height);
          }

          return finalImage ;
        }
      }
      catch ( Exception e ) {
        Console.WriteLine( e ) ;
        throw ;
      }
      return new Bitmap(1,1) ;
    }

    private static Bitmap ConvertTextToImage(string text,int height )
    {
      Bitmap bmp = new Bitmap( 30, height ) ;
      try {
        using ( Graphics graphics = Graphics.FromImage( bmp ) ) {
          Font font = new Font( "ＭＳ Ｐゴシック", 10 ) ;
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