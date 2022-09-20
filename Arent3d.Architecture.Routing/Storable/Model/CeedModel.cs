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
    public string LegendDisplay { get ; set ; }
    public string CeedModelNumber { get ; set ; }
    public string CeedSetCode { get ; set ; }
    public string GeneralDisplayDeviceSymbol { get ; set ; }
    public string ModelNumber { get ; set ; }
    public string FloorPlanSymbol { get ; set ; }
    public string InstrumentationSymbol { get ; set ; }
    public string Name { get ; set ; }
    public string DwgNumber { get ; set ; }
    public string Condition { get ; set ; }
    public string FloorPlanType { get ; set ; }
    public string Base64InstrumentationImageString { get ; set ; }
    public string Base64FloorPlanImages { get ; set ; }
    public BitmapImage? FloorPlanImages { get ; set ; }
    public List<BitmapImage?>? InstrumentationImages { get ; set ; }
    
    public bool IsAdded { get ; set ; }
    public bool IsEditFloorPlan { get ; set ; }
    public bool IsEditInstrumentation { get ; set ; }
    public bool IsEditCondition{ get ; set ; }
    public bool IsUsingCode { get ; set ; }

    public CeedModel( string legendDisplay, string ceedModelNumber, string ceedSetCode, string generalDisplayDeviceSymbol, string modelNumber, string floorPlanSymbol, string instrumentationSymbol, string name, string dwgNumber, string base64InstrumentationImageString, string base64FloorPlanImages, string floorPlanType )
    {
      const string dummySymbol = "Dummy" ;
      LegendDisplay = legendDisplay ;
      CeedModelNumber = ceedModelNumber ;
      CeedSetCode = ceedSetCode ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      ModelNumber = modelNumber ;
      FloorPlanSymbol = floorPlanSymbol ;
      InstrumentationSymbol = instrumentationSymbol ;
      Name = name ;
      DwgNumber = dwgNumber ;
      Condition = GetCondition( ceedModelNumber ) ;
      FloorPlanType = floorPlanType ;
      Base64InstrumentationImageString = base64InstrumentationImageString ;
      Base64FloorPlanImages = base64FloorPlanImages ;
      InstrumentationImages = null ;
      FloorPlanImages = null ;
      if ( floorPlanSymbol != dummySymbol ) {
        var temporaryFloorPlanImage = new BitmapImage() ;
        if ( FloorPlanImages == null && ! string.IsNullOrEmpty( Base64FloorPlanImages ) ) {
          temporaryFloorPlanImage = BitmapToImageSource( Base64StringToBitmap( Base64FloorPlanImages ) ) ;
        }

        FloorPlanImages = temporaryFloorPlanImage ;
      }
      if ( InstrumentationImages != null || string.IsNullOrEmpty( Base64InstrumentationImageString ) ) return ;
      var listBimapImage = ( from image in Base64InstrumentationImageString.Split( new string[] { "||" }, StringSplitOptions.None ) select Base64StringToBitmap( image ) into bmpFromString select BitmapToImageSource( bmpFromString ) ).ToList() ;
      InstrumentationImages = listBimapImage ;
    }

    public CeedModel( string legendDisplay, string ceedModelNumber, string ceedSetCode, string generalDisplayDeviceSymbol, string modelNumber, List<Image>? floorPlanImages, List<Image>? instrumentationImages, string floorPlanSymbol, string instrumentationSymbol, string name, string dwgNumber, string base64InstrumentationImageString, string floorPlanType )
    {
      CeedModelNumber = ceedModelNumber ;
      LegendDisplay = legendDisplay ;
      CeedSetCode = ceedSetCode ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      ModelNumber = modelNumber ;
      FloorPlanSymbol = floorPlanSymbol ;
      InstrumentationSymbol = instrumentationSymbol ;
      Name = name ;
      DwgNumber = dwgNumber ;
      Condition = GetCondition( ceedModelNumber ) ;
      FloorPlanType = floorPlanType ;
      FloorPlanImages = BitmapToImageSource( GetImage( floorPlanImages ) ) ;
      InstrumentationImages = GetImages( instrumentationImages ) ;
      Base64InstrumentationImageString = base64InstrumentationImageString ;
      string tempFloorPlanString = string.Empty ;
      if ( FloorPlanImages != null ) {
        tempFloorPlanString = ConvertBitmapToBase64( FloorPlanImages ) ;
      }

      Base64FloorPlanImages = tempFloorPlanString ;
      if ( InstrumentationImages == null || ! InstrumentationImages.Any() ) return ;
      var tempImage = ( from item in InstrumentationImages select ConvertBitmapToBase64( item ) ).ToList() ;
      Base64InstrumentationImageString = string.Join( "||", tempImage ) ;
    }
    
    public CeedModel( string legendDisplay, string ceedModelNumber, string ceedSetCode, string generalDisplayDeviceSymbol, string modelNumber, List<Image>? floorPlanImages, List<Image>? instrumentationImages, string floorPlanSymbol, string instrumentationSymbol, string name, string dwgNumber, string floorPlanType )
    {
      LegendDisplay = legendDisplay ;
      CeedModelNumber = ceedModelNumber ;
      CeedSetCode = ceedSetCode ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      ModelNumber = modelNumber ;
      FloorPlanSymbol = floorPlanSymbol ;
      InstrumentationSymbol = instrumentationSymbol ;
      Name = name ;
      DwgNumber = dwgNumber ;
      Condition = GetCondition( ceedModelNumber ) ;
      FloorPlanType = floorPlanType ;
      FloorPlanImages = BitmapToImageSource( GetImage( floorPlanImages ) ) ;
      InstrumentationImages = GetImages( instrumentationImages ) ;
      Base64InstrumentationImageString = string.Empty ;
      string tempFloorPlanString = string.Empty ;
      if ( FloorPlanImages != null ) {
        tempFloorPlanString = ConvertBitmapToBase64( FloorPlanImages ) ;
        FloorPlanImages = null ;
      }
    
      Base64FloorPlanImages = tempFloorPlanString ;
      if ( InstrumentationImages == null || ! InstrumentationImages.Any() ) return ;
      var tempImage = ( from item in InstrumentationImages select ConvertBitmapToBase64( item ) ).ToList() ;
      Base64InstrumentationImageString = string.Join( "||", tempImage ) ;
    }
    
    public CeedModel( string legendDisplay, string ceedModelNumber, string ceedSetCode, string generalDisplayDeviceSymbol, string modelNumber, string floorPlanSymbol, string instrumentationSymbol, string name, string dwgNumber, 
      string base64InstrumentationImageString, string base64FloorPlanImages, string floorPlanType, bool? isAdded, bool? isEditFloorPlan, bool? isEditInstrumentation, bool? isEditCondition, bool? isUsingCode )
    {
      LegendDisplay = legendDisplay ;
      CeedModelNumber = ceedModelNumber ;
      CeedSetCode = ceedSetCode ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      ModelNumber = modelNumber ;
      FloorPlanSymbol = floorPlanSymbol ;
      InstrumentationSymbol = instrumentationSymbol ;
      Name = name ;
      DwgNumber = dwgNumber ;
      Condition = GetCondition( ceedModelNumber ) ;
      FloorPlanType = floorPlanType ;
      Base64InstrumentationImageString = base64InstrumentationImageString ;
      Base64FloorPlanImages = base64FloorPlanImages ;
      InstrumentationImages = null ;
      FloorPlanImages = null ;
      IsAdded = isAdded ?? false ;
      IsEditFloorPlan = isEditFloorPlan ?? false ;
      IsEditInstrumentation = isEditInstrumentation ?? false ;
      IsEditCondition = isEditCondition ?? false ;
      IsUsingCode = isUsingCode ?? false ;
      if ( InstrumentationImages != null || string.IsNullOrEmpty( Base64InstrumentationImageString ) ) return ;
      var listBimapImage = ( from image in Base64InstrumentationImageString.Split( new string[] { "||" }, StringSplitOptions.None ) select Base64StringToBitmap( image ) into bmpFromString select BitmapToImageSource( bmpFromString ) ).ToList() ;
      InstrumentationImages = listBimapImage ;
    }

    public static BitmapImage? BitmapToImageSource( Bitmap? bitmap )
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

    private static string ConvertBitmapToBase64( BitmapImage? bmp )
    {
      Bitmap bImage = BitmapImageBitmap( bmp ) ;
      var ms = new MemoryStream() ;
      bImage?.Save( ms, ImageFormat.Bmp ) ;
      var byteImage = ms.ToArray() ;
      var result = Convert.ToBase64String( byteImage ) ;
      return result ;
    }

    private static Bitmap BitmapImageBitmap( BitmapImage? bitmapImage )
    {
      using ( MemoryStream outStream = new MemoryStream() ) {
        BitmapEncoder enc = new BmpBitmapEncoder() ;
        if ( bitmapImage != null ) enc.Frames.Add( BitmapFrame.Create( bitmapImage ) ) ;
        enc.Save( outStream ) ;
        var bitmap = new System.Drawing.Bitmap( outStream ) ;

        return new Bitmap( bitmap ) ;
      }
    }

    public static Bitmap Base64StringToBitmap( string base64String )
    {
      Byte[] bitmapData = Convert.FromBase64String( FixBase64ForImage( base64String ) ) ;
      System.IO.MemoryStream streamBitmap = new System.IO.MemoryStream( bitmapData ) ;
      Bitmap bitImage = new Bitmap( (Bitmap) Image.FromStream( streamBitmap ) ) ;
      return bitImage ;
    }

    private static string FixBase64ForImage( string image )
    {
      System.Text.StringBuilder sbText = new System.Text.StringBuilder( image, image.Length ) ;
      sbText.Replace( "\r\n", String.Empty ) ;
      sbText.Replace( " ", String.Empty ) ;
      return sbText.ToString() ;
    }

    private static string GetCondition( string ceedModelNumber )
    {
      string condition ;
      if ( ceedModelNumber.EndsWith( "P" ) ) {
        condition = "隠蔽 、床隠蔽" ;
      }
      else if ( ceedModelNumber.EndsWith( "K" ) ) {
        condition = "コロガシ" ;
      }
      else if ( ceedModelNumber.EndsWith( "F" ) ) {
        condition = "フリアク" ;
      }
      else if ( ceedModelNumber.EndsWith( "E" ) ) {
        condition = "露出" ;
      }
      else if ( ceedModelNumber.EndsWith( "G" ) ) {
        condition = "屋外" ;
      }
      else if ( ceedModelNumber.EndsWith( "M" ) ) {
        condition = "モール" ;
      }
      else if ( ceedModelNumber.EndsWith( "U" ) ) {
        condition = "地中埋設" ;
      }
      else {
        condition = string.Empty ;
      }

      return condition ;
    }
  }
}