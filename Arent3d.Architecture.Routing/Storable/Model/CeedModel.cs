using System ;
using System.Collections.Generic ;
using System.Diagnostics.CodeAnalysis ;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.IO ;
using System.Linq ;
using System.Windows.Media.Imaging ;
using Color = System.Drawing.Color ;

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
    public string Base64ImageString { get ; set ; }
    public string Base64FloorImages { get ; set ; }
    public BitmapImage? InstrumentationImages { get ; set ; }
    public BitmapImage? FloorImages { get ; set ; }
    public List<BitmapImage?>? ListImages { get ; set ; }

    public CeedModel( string ceeDModelNumber, string ceeDSetCode, string generalDisplayDeviceSymbol, string modelNumber,
      string floorPlanSymbol, string instrumentationSymbol, string name, string condition, string base64ImageString, string base64FloorImages )
    {
      CeeDModelNumber = ceeDModelNumber ;
      CeeDSetCode = ceeDSetCode ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      ModelNumber = modelNumber ;
      FloorPlanSymbol = floorPlanSymbol ;
      InstrumentationSymbol = instrumentationSymbol ;
      Name = name ;
      Condition = condition ;
      Base64ImageString = base64ImageString ;
      Base64FloorImages = base64FloorImages ;
      ListImages = null ;
      FloorImages = null ;
      var tempFloorImage = new BitmapImage() ;
      if ( FloorImages == null && ! string.IsNullOrEmpty( Base64FloorImages ) ) {
        tempFloorImage = BitmapToImageSource( Base64StringToBitmap( Base64FloorImages ) ) ;
      }
      FloorImages = tempFloorImage ;
      if ( ListImages != null || string.IsNullOrEmpty( Base64ImageString ) ) return ;
      var listBimapImage = ( from image in Base64ImageString.Split( new string[] { "||" }, StringSplitOptions.None ) select Base64StringToBitmap( image ) into bmpFromString select BitmapToImageSource( bmpFromString ) ).ToList() ;
      ListImages = listBimapImage ;
    }

    public CeedModel( string ceeDModelNumber, string ceeDSetCode, string generalDisplayDeviceSymbol, string modelNumber,
      List<Image>? floorPlanImages, List<Image>? instrucmentChartImages, string floorPlanSymbol, string instrumentationSymbol, string name,
      string condition, string base64ImageString )
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
      ListImages = GetImages( instrucmentChartImages ) ;
      Base64ImageString = base64ImageString ;
      string tempFloorString = string.Empty;
      if ( FloorImages != null ) {
        tempFloorString = ConvertBitmapToBase64( FloorImages ) ;
      }
      Base64FloorImages = tempFloorString ;
      if ( ListImages == null || ! ListImages.Any() ) return ;
      var tempImage = ( from item in ListImages select ConvertBitmapToBase64( item ) ).ToList() ;
      Base64ImageString = string.Join( "||",tempImage );
    }

    private static BitmapImage? BitmapToImageSource( Bitmap? bitmap )
    {
      using ( var memory = new MemoryStream() ) {
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
          var padding = 45 ;
          var imageWidth = images.Sum( item => item.Width ) + ( images.Count - 1 ) * padding ;
          var finalImage =
            new Bitmap( imageWidth, maxImageHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb ) ;
          using ( Graphics g = Graphics.FromImage( finalImage ) ) {
            g.Clear( Color.White ) ;
            var offset = 0 ;

            for ( var i = 0 ; i < images.Count ; i++ ) {
              Image image = images[ i ] ;
              g.DrawImage( image, new Rectangle( new Point( offset, 0 ), image.Size ),
                new Rectangle( new Point(), image.Size ), GraphicsUnit.Pixel ) ;
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

    private static string ConvertBitmapToBase64( BitmapImage? bmp )
    {
      Bitmap bImage = BitmapImage2Bitmap( bmp ) ;
      var ms = new MemoryStream() ;
      bImage?.Save( ms, ImageFormat.Bmp ) ;
      var byteImage = ms.ToArray() ;
      var result = Convert.ToBase64String( byteImage ) ;
      return result ;
    }

    private static Bitmap BitmapImage2Bitmap( BitmapImage? bitmapImage )
    {
      using ( MemoryStream outStream = new MemoryStream() ) {
        BitmapEncoder enc = new BmpBitmapEncoder() ;
        if ( bitmapImage != null ) enc.Frames.Add( BitmapFrame.Create( bitmapImage ) ) ;
        enc.Save( outStream ) ;
        var bitmap = new System.Drawing.Bitmap( outStream ) ;

        return new Bitmap( bitmap ) ;
      }
    }

    private static Bitmap Base64StringToBitmap(string base64String)
    {
      Byte[] bitmapData = Convert.FromBase64String(FixBase64ForImage(base64String));
      System.IO.MemoryStream streamBitmap = new System.IO.MemoryStream(bitmapData);
      Bitmap bitImage = new Bitmap((Bitmap)Image.FromStream(streamBitmap));
      return bitImage ;
    }

    private static string FixBase64ForImage(string image) { 
      System.Text.StringBuilder sbText = new System.Text.StringBuilder(image,image.Length);
      sbText.Replace("\r\n", String.Empty); sbText.Replace(" ", String.Empty); 
      return sbText.ToString(); 
    }
  }
}
