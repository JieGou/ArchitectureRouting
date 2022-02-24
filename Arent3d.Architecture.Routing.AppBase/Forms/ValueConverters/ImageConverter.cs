using System.Collections.Generic ;
using System.Drawing ;
using System.IO ;
using System.Linq ;
using Autodesk.Revit.DB ;
using Rectangle = System.Drawing.Rectangle ;
using Size = System.Drawing.Size ;
using View = Autodesk.Revit.DB.View ;

namespace Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters
{
  public static class ImageConverter
  {
    public static string GetFloorPlanImageFile( string path, string familyName )
    {
      return Directory.GetFiles( path ).FirstOrDefault( f => Path.GetFileName( f ).Contains( familyName ) && Path.GetExtension( Path.GetFileName( f ) ).Contains( "png" ) ) ?? string.Empty ;
    }

    public static void ExportConnectorFamilyImage( Document document, Family connectorFamily, string path, string familyName )
    {
      var familyDoc = document.EditFamily( connectorFamily ) ;
      if ( new FilteredElementCollector( familyDoc ).OfClass( typeof( View ) ).OfCategory( BuiltInCategory.OST_Views ).FirstOrDefault( v => v is ViewPlan ) is not View floorPlanView ) return ;
      if ( floorPlanView.IsTemplate ) return ;
      IList<ElementId> imageExportList = new List<ElementId>() ;
      imageExportList.Clear() ;
      imageExportList.Add( floorPlanView.Id ) ;
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
        using Image image = Image.FromFile( imageFileName ) ;
        var (x, y, width, height) = GetOriginPointAndSizeImage( image ) ;
        Bitmap cropped = new Bitmap( width, height ) ;
        // Create a Graphics object to do the drawing, *with the new bitmap as the target*
        using Graphics g = Graphics.FromImage( cropped ) ;
        // Draw the desired area of the original into the graphics object
        g.DrawImage( image, new Rectangle( 0, 0, width, height ), new Rectangle( x, y, width, height ), GraphicsUnit.Pixel ) ;
        var newImageFileName = Path.Combine( path, familyName + ".png" ) ;
        // Save the result
        cropped.Save( newImageFileName ) ;
        return newImageFileName ;
      }
      catch {
        return imageFileName ;
      }
    }

    private static (int, int, int, int) GetOriginPointAndSizeImage( Image image )
    {
      Bitmap bitmapImage = new Bitmap( image ) ;
      var maxX = 0 ;
      var maxY = 0 ;
      var minX = bitmapImage.Width ;
      var minY = bitmapImage.Height ;
      for ( var x = 0 ; x < bitmapImage.Width ; x++ ) {
        for ( var y = 0 ; y < bitmapImage.Height ; y++ ) {
          var color = bitmapImage.GetPixel( x, y ) ;
          var rgb = (byte) ( .299 * color.R + .587 * color.G + .114 * color.B ) ;
          if ( rgb == 255 ) continue ;
          if ( x < minX ) minX = x ;
          if ( y < minY ) minY = y ;
          if ( x > maxX ) maxX = x ;
          if ( y > maxY ) maxY = y ;
        }
      }

      return ( minX, minY, maxX - minX, maxY - minY ) ;
    }
  }
}