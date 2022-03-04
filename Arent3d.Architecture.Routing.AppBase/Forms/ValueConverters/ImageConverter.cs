using System ;
using System.Collections.Generic ;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.IO ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Autodesk.Revit.DB ;
using Rectangle = System.Drawing.Rectangle ;
using Size = System.Drawing.Size ;
using View = Autodesk.Revit.DB.View ;

namespace Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters
{
  public static class ImageConverter
  {
    private const int RedFactor = (int) ( 0.298912 * 1024 ) ;
    private const int GreenFactor = (int) ( 0.586611 * 1024 ) ;
    private const int BlueFactor = (int) ( 0.114478 * 1024 ) ;

    public static string GetFloorPlanImageFile( string path, string familyName )
    {
      return Directory.GetFiles( path ).FirstOrDefault( f => Path.GetFileName( f ).Contains( familyName ) && Path.GetExtension( Path.GetFileName( f ) ).Contains( "png" ) ) ?? string.Empty ;
    }

    public static bool ExportConnectorFamilyImage( Document document, Family connectorFamily, string path, string familyName )
    {
      var familyDoc = document.EditFamily( connectorFamily ) ;
      if ( new FilteredElementCollector( familyDoc ).OfClass( typeof( View ) ).OfCategory( BuiltInCategory.OST_Views ).First( v => v is ViewPlan ) is not View floorPlanView ) return false ;
      if ( floorPlanView.IsTemplate ) return false ;
      var imageExportList = new List<ElementId> { floorPlanView.Id } ;
      using Transaction tx = new Transaction( familyDoc ) ;
      tx.Start( "Export Image" ) ;
      var imageExportOptions = new ImageExportOptions
      {
        ZoomType = ZoomFitType.FitToPage,
        PixelSize = 512,
        FilePath = Path.Combine( path, familyName ),
        FitDirection = FitDirectionType.Horizontal,
        HLRandWFViewsFileType = ImageFileType.PNG,
        ImageResolution = ImageResolution.DPI_600,
        ExportRange = ExportRange.SetOfViews,
      } ;

      imageExportOptions.SetViewsAndSheets( imageExportList ) ;
      familyDoc.ExportImage( imageExportOptions ) ;
      tx.RollBack() ;
      return true ;
    }

    public static Image ResizeImage( Image imgToResize, Size size )
    {
      return new Bitmap( imgToResize, size ) ;
    }

    public static string CropImage( string path, string familyName )
    {
      var imageFileName = GetFloorPlanImageFile( path, familyName ) ;
      if ( string.IsNullOrEmpty( imageFileName ) ) return imageFileName ;
      try {
        //Load image from file
        using Bitmap bitmapImage = (Bitmap) Image.FromFile( imageFileName ) ;
        var (x, y, width, height) = GetOriginPointAndSizeImage( bitmapImage ) ;
        using Bitmap cropped = new( width, height ) ;
        // Create a Graphics object to do the drawing, *with the new bitmap as the target*
        using Graphics g = Graphics.FromImage( cropped ) ;
        // Draw the desired area of the original into the graphics object
        g.DrawImage( bitmapImage, new Rectangle( 0, 0, width, height ), new Rectangle( x, y, width, height ), GraphicsUnit.Pixel ) ;
        var newImageFileName = Path.Combine( path, familyName + ".png" ) ;
        // Save the result
        cropped.Save( newImageFileName ) ;
        return newImageFileName ;
      }
      catch {
        return imageFileName ;
      }
    }

    private static (int, int, int, int) GetOriginPointAndSizeImage( Bitmap bitmapImage )
    {
      var rect = new Rectangle( 0, 0, bitmapImage.Width, bitmapImage.Height ) ;
      BitmapData bitmapData = bitmapImage.LockBits( rect, ImageLockMode.ReadWrite, bitmapImage.PixelFormat ) ;
      var maxX = 0 ;
      var maxY = 0 ;
      var minX = bitmapData.Width ;
      var minY = bitmapData.Height ;
      var pointer = bitmapData.Scan0 ;
      var size = Math.Abs( bitmapData.Stride ) * bitmapImage.Height ;
      byte[] pixels = new byte[ size ] ;
      Marshal.Copy( pointer, pixels, 0, size ) ;
      for ( var x = 0 ; x < bitmapData.Width ; x++ ) {
        for ( var y = 0 ; y < bitmapData.Height ; y++ ) {
          if ( ConvertToGrayscale( pixels, x, y, bitmapData.Stride ) >= 254 ) continue ;
          if ( x < minX ) minX = x ;
          if ( y < minY ) minY = y ;
          if ( x > maxX ) maxX = x ;
          if ( y > maxY ) maxY = y ;
        }
      }

      bitmapImage.UnlockBits( bitmapData ) ;
      return ( minX, minY, maxX - minX, maxY - minY ) ;
    }

    private static int ConvertToGrayscale( IReadOnlyList<byte> srcPixels, int x, int y, int stride )
    {
      var position = x * 3 + stride * y ;
      var b = srcPixels[ position + 0 ] ;
      var g = srcPixels[ position + 1 ] ;
      var r = srcPixels[ position + 2 ] ;

      return ( r * RedFactor + g * GreenFactor + b * BlueFactor ) >> 10 ;
    }
  }
}