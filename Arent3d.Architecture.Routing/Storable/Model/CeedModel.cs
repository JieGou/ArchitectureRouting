using System ;
using System.Collections.Generic ;
using System.Diagnostics.CodeAnalysis ;
using System.Drawing ;
using System.IO ;
using System.Linq ;
using System.Windows.Media.Imaging ;

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
    public string Condition { get ; set ; }
    public BitmapImage? InstrumentationImages { get ; set ; }
    public BitmapImage? FloorImages { get ; set ; }
    public List<BitmapImage?>? ListImages { get ; set ; }

    public CeedModel( string ceeDModelNumber, string ceeDSetCode, string generalDisplayDeviceSymbol, string modelNumber, string floorPlanSymbol, string instrumentationSymbol, string name, string condition )
    {
      CeeDModelNumber = ceeDModelNumber ;
      CeeDSetCode = ceeDSetCode ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      ModelNumber = modelNumber ;
      FloorPlanSymbol = floorPlanSymbol ;
      InstrumentationSymbol = instrumentationSymbol ;
      Name = name ;
      Condition = condition ;
      ListImages = null ;
      FloorImages = null ;
    }

    public CeedModel( string ceeDModelNumber, string ceeDSetCode, string generalDisplayDeviceSymbol, string modelNumber, List<Image>? floorPlanImages, string floorPlanSymbol, string instrumentationSymbol, string name, string condition )
    {
      CeeDModelNumber = ceeDModelNumber ;
      CeeDSetCode = ceeDSetCode ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      ModelNumber = modelNumber ;
      FloorPlanSymbol = floorPlanSymbol ;
      InstrumentationSymbol = instrumentationSymbol ;
      Name = name ;
      Condition = condition ;
      FloorImages = BitmapToImageSource( GetImage( floorPlanImages ) ) ;
      ListImages = GetImages( floorPlanImages ) ;
    }

    private static BitmapImage? BitmapToImageSource( Bitmap? bitmap )
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

    private List<BitmapImage?>? GetImages( List<Image>? images )
    {
      List<BitmapImage?>? listImages = new List<BitmapImage?>() ;
      try {
        if ( images != null )
          foreach ( var image in images ) {
            var bitmapImage = BitmapToImageSource( (Bitmap) image ) ;
            listImages.Add( bitmapImage ) ;
          }
      }
      catch ( Exception e ) {
        Console.WriteLine( e ) ;
      }

      return listImages ;
    }

    private Bitmap? GetImage( List<Image>? symbolImages )
    {
      try {
        if ( symbolImages is { Count: 1 } ) return (Bitmap) symbolImages[ 0 ] ;
        return MergeImages( symbolImages ) ;
      }
      catch ( Exception e ) {
        Console.WriteLine( e ) ;
        return null ;
      }
    }

    private static Bitmap MergeImages( List<Image>? images )
    {
      try {
        if ( images != null ) {
          var maxImageHeight = images.OrderByDescending( c => c.Height ).Select( c => c.Height ).First() ;
          //var minImageHeight = images.OrderBy( c => c.Height ).Select( c => c.Height ).First() ;
          // var centerPoint = ( maxImageHeight - minImageHeight ) / 2 ;
          var padding = 45 ;
          var imageWidth = images.Sum( item => item.Width ) + ( images.Count - 1 ) * padding ;
          var finalImage = new Bitmap( imageWidth, maxImageHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb ) ;
          using ( Graphics g = Graphics.FromImage( finalImage ) ) {
            g.Clear( Color.White ) ;
            var offset = 0 ;

            for ( var i = 0 ; i < images.Count ; i++ ) {
              Image image = images[ i ] ;
              g.DrawImage( image, new Rectangle( new Point( offset, 0 ), image.Size ), new Rectangle( new Point(), image.Size ), GraphicsUnit.Pixel ) ;
              offset += image.Width + padding ;
            }
          }

          return finalImage ;
        }
      }
      catch ( Exception e ) {
        Console.WriteLine( e ) ;
        throw ;
      }

      return new Bitmap( 1, 1 ) ;
    }
  }
}