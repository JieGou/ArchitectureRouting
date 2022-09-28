using System ;
using System.Collections.Generic ;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.IO ;
using System.Linq ;
using System.Reflection ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Media ;
using System.Windows.Media.Imaging ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Brushes = System.Windows.Media.Brushes ;
using Point = System.Windows.Point ;
using Polyline = System.Windows.Shapes.Polyline ;
using Size = System.Windows.Size ;

namespace Arent3d.Architecture.Routing.AppBase.Manager
{
  public static class DrawCanvasManager
  {
    private const string MallCondition = "モール" ;
    private const string MoSymbol = " (モ)" ;
    private static readonly List<string> IsRotatedDwgNumbers = new() { "19", "28", "37", "43", "47", "50", "55", "59", "75" } ;

    public static void SetBase64FloorPlanImages ( Document document, IEnumerable<CeedModel> ceedModels )
    {
      List<CanvasChildInfo> canvasChildInfos = new() ;
      List<ElementId> dwgImportIds = new() ;
      var view = document.ActiveView ;
      DWGImportOptions dwgImportOptions = new()
      {
        ColorMode = ImportColorMode.BlackAndWhite,
        Unit = ImportUnit.Millimeter,
        OrientToView = true,
        Placement = ImportPlacement.Origin,
        ThisViewOnly = false
      } ;

      foreach ( var ceedModel in ceedModels ) {
        var lines = new List<Line>() ;
        var arcs = new List<Arc>() ;
        var polyLines = new List<PolyLine>() ;
        var points = new List<Autodesk.Revit.DB.Point>() ;
        var dwgNumber = ceedModel.DwgNumber ;
        try {
          if ( string.IsNullOrEmpty( ceedModel.DwgNumber ) ) {
            if ( string.IsNullOrEmpty( ceedModel.GeneralDisplayDeviceSymbol ) ) continue ;
            var canvas = CreateCanvas( lines, arcs, polyLines, ceedModel.GeneralDisplayDeviceSymbol, ceedModel.FloorPlanSymbol, ceedModel.Condition, false ) ;
            ceedModel.Base64FloorPlanImages = GetImageDataFromCanvas( canvas ) ;
          }
          else {
            var canvasChildInfo = canvasChildInfos.SingleOrDefault( c => c.DwgNumber == ceedModel.DwgNumber ) ;
            if ( canvasChildInfo == null ) {
              var filePath = Get2DSymbolDwgPath( dwgNumber ) ;
              using Transaction t = new( document, "Import dwg file" ) ;
              t.Start() ;
              document.Import( filePath, dwgImportOptions, view, out var elementId ) ;
              t.Commit() ;

              if ( elementId == null ) continue ;
              dwgImportIds.Add( elementId ) ;
              if ( document.GetElement( elementId ) is ImportInstance dwg ) {
                Options opt = new() ;
                foreach ( GeometryObject geoObj in dwg.get_Geometry( opt ) ) {
                  if ( geoObj is not GeometryInstance inst ) continue ;
                  LoadGeometryFromGeometryObject( inst.SymbolGeometry, lines, arcs, polyLines, points ) ;
                }
              }
              
              canvasChildInfos.Add( new CanvasChildInfo( ceedModel.DwgNumber, polyLines, lines, arcs ) ) ;
            }
            else {
              polyLines = canvasChildInfo.PolyLines ;
              lines = canvasChildInfo.Lines ;
              arcs = canvasChildInfo.Arcs ;
            }
            
            var canvas = CreateCanvas( lines, arcs, polyLines, ceedModel.GeneralDisplayDeviceSymbol, string.Empty, ceedModel.Condition, IsRotatedDwgNumbers.Contains( dwgNumber ) ) ;
            ceedModel.Base64FloorPlanImages = GetImageDataFromCanvas( canvas ) ;
          }
        }
        catch {
          // ignored
        }
      }
      
      using Transaction tRemove = new( document, "Remove dwg file" ) ;
      tRemove.Start() ;
      document.Delete( dwgImportIds ) ;
      tRemove.Commit() ;
    }

    private static Canvas CreateCanvas( ICollection<Line> lines, ICollection<Arc> arcs, ICollection<PolyLine> polyLines, string deviceSymbol, string floorPlanSymbol, string condition, bool isRotated )
    {
      const double scale = 15 ;
      const double defaultOffset = 20 ;
      const double defaultOffsetY = 25 ;
      var rotateTransform = new RotateTransform( 90, 0, 0 ) ;
      Canvas canvasPanel = new() { Background = new SolidColorBrush( Colors.Black ), Width = 245, Height = 100 } ;
      var offsetX= defaultOffset * 5 ;
      var offsetY= defaultOffset * 2 ;
      var minX = defaultOffset ;
      var minY = defaultOffset ;
      var maxX = defaultOffset ;
      var maxY = defaultOffset ;
      var scaleOfLine = scale ;
      if ( lines.Any() || arcs.Any() || polyLines.Any() ) {
        ( minX, minY, maxX, maxY ) = GetMinAndMaxPoint( polyLines, lines, arcs ) ;
      }
      try {
        if ( polyLines.Any() ) {
          foreach ( var polyline in polyLines ) {
            var pointsOfPolyLine = new PointCollection() ;
            var points = polyline.GetCoordinates() ;
            if ( polyline == polyLines.First() ) {
              scaleOfLine = GetScale( minX, minY, maxX, maxY, scale ) ;
              offsetX -= minX * scaleOfLine ; 
              offsetY = minY == 0 ? offsetY + scaleOfLine * 0.5 : offsetY - minY * scaleOfLine ;
              if ( isRotated ) offsetY += defaultOffsetY ;
            }

            foreach ( var point in points ) {
              var x = point.X.RevitUnitsToMillimeters() * scaleOfLine ;
              var y = point.Y.RevitUnitsToMillimeters() * scaleOfLine ;
              pointsOfPolyLine.Add( new Point( x, y ) ) ;
            }

            var newPolyline = new Polyline
            {
              Stroke = new SolidColorBrush( Colors.Green ),
              StrokeThickness = 2,
              FillRule = FillRule.Nonzero,
              Points = pointsOfPolyLine,
            } ;

            if ( isRotated ) newPolyline.RenderTransform = rotateTransform ;
            Canvas.SetTop( newPolyline, offsetY ) ;
            Canvas.SetLeft( newPolyline, offsetX ) ;
            canvasPanel.Children.Add( newPolyline ) ;
          }
        }

        foreach ( var line in lines ) {
          if ( line == lines.First() && ! polyLines.Any() ) {
            scaleOfLine = GetScale( lines, scale ) ;
            offsetX -= minX * scaleOfLine ; 
            offsetY -= minY * scaleOfLine ;
          }

          var (x1, y1, _) = line.GetEndPoint( 0 ) ;
          var (x2, y2, _) = line.GetEndPoint( 1 ) ;

          x1 = x1.RevitUnitsToMillimeters() * scaleOfLine ;
          y1 = y1.RevitUnitsToMillimeters() * scaleOfLine ;
          x2 = x2.RevitUnitsToMillimeters() * scaleOfLine ;
          y2 = y2.RevitUnitsToMillimeters() * scaleOfLine ;

          var newLine = new System.Windows.Shapes.Line
          {
            Stroke = new SolidColorBrush( Colors.Green ),
            StrokeThickness = 2,
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
          } ;

          Canvas.SetTop( newLine, offsetY ) ;
          Canvas.SetLeft( newLine, offsetX ) ;
          canvasPanel.Children.Add( newLine ) ;
        }

        var scaleOfArc = scale ;
        foreach ( var arc in arcs ) {
          var centerX = arc.Center.X.RevitUnitsToMillimeters() ;
          var centerY = arc.Center.Y.RevitUnitsToMillimeters() ;
          var diameter = ( arc.Radius * 2 ).RevitUnitsToMillimeters() ;
          if ( arc == arcs.First() ) {
            if ( polyLines.Any() || lines.Any() ) {
              scaleOfArc = scaleOfLine ;
            }
            else {
              scaleOfArc = GetScale( diameter, scale ) ;
              offsetX -= minX * scaleOfArc ; 
              offsetY -= minY * scaleOfArc ;
            }
          }

          var newDiameter = diameter * scaleOfArc ;
          if ( arc.IsClosed ) {
            var newEllipse = new System.Windows.Shapes.Ellipse() { Stroke = new SolidColorBrush( Colors.Green ), StrokeThickness = 2, Width = newDiameter, Height = newDiameter } ;

            Canvas.SetTop( newEllipse, offsetY + centerY * scaleOfArc - newDiameter * 0.5 ) ;
            Canvas.SetLeft( newEllipse, offsetX + centerX * scaleOfArc - newDiameter * 0.5 ) ;
            canvasPanel.Children.Add( newEllipse ) ;
          }
          else {
            var firstPoint = arc.GetEndPoint( 0 ) ;
            var secondPoint = arc.GetEndPoint( 1 ) ;
            var startPoint = new Point( firstPoint.X.RevitUnitsToMillimeters() * scaleOfArc, firstPoint.Y.RevitUnitsToMillimeters() * scaleOfArc ) ;
            var endPoint = new Point( secondPoint.X.RevitUnitsToMillimeters() * scaleOfArc, secondPoint.Y.RevitUnitsToMillimeters() * scaleOfArc ) ;
            var arcSegment = new ArcSegment( endPoint, new Size( newDiameter * 0.5, newDiameter * 0.5 ), 0, false, SweepDirection.Clockwise, true ) ;
            var segments = new PathSegmentCollection { arcSegment } ;
            var figure = new PathFigure( startPoint, segments, false ) ;
            var figures = new PathFigureCollection { figure } ;
            var pathGeometry = new PathGeometry( figures ) ;
            var newPath = new System.Windows.Shapes.Path()
            {
              Stroke = new SolidColorBrush( Colors.Green ), 
              StrokeThickness = 2, 
              Data = pathGeometry,
            } ;
            Canvas.SetTop( newPath, offsetY ) ;
            Canvas.SetLeft( newPath, offsetX ) ;
            canvasPanel.Children.Add( newPath ) ;
          }
        }
      }
      catch {
        //
      }

      if ( ! string.IsNullOrEmpty( floorPlanSymbol ) ) {
        TextBlock txtFloorPlanSymbol = new()
        {
          FontSize = 19, 
          Text = floorPlanSymbol, 
          Foreground = Brushes.Green
        } ;
        Canvas.SetTop( txtFloorPlanSymbol, offsetY ) ;
        Canvas.SetLeft( txtFloorPlanSymbol, offsetX ) ;
        canvasPanel.Children.Add( txtFloorPlanSymbol ) ;
      }

      var text = condition == MallCondition ? deviceSymbol + MoSymbol : deviceSymbol ;
      TextBlock txt = new()
      {
        FontSize = 20, 
        Text = text, 
        Foreground = Brushes.White,
        Width = 20 * text.Length,
        TextAlignment = TextAlignment.Center
      } ;
      
      var leftOffset = GetLeftOffset( polyLines, minX, minY, scaleOfLine, offsetX, txt.Width ) ;
      Canvas.SetTop( txt, 5 ) ;
      Canvas.SetLeft( txt, leftOffset ) ;
      canvasPanel.Children.Add( txt ) ;

      return canvasPanel ;
    }
    
    private static double GetLeftOffset( ICollection<PolyLine> polyLines, double minX, double minY, double scale, double offsetX, double txtWidth )
    {
      double leftOffset ;
      if ( polyLines.Any() && ( ( minY == 0 && minX == 0 ) || ( minY <= -4 && minX <= -2 ) )  ) {
        leftOffset = offsetX - txtWidth * 0.5 + scale ;
      }
      else if ( polyLines.Any() && minY == 0 && minX < -1 ) {
        leftOffset = offsetX - txtWidth * 0.5 - scale ;
      }
      else if ( polyLines.Any() && minX == 0 && minY < -6 ) {
        leftOffset = offsetX - txtWidth * 0.5 + scale * 3 ;
      }
      else {
        leftOffset = offsetX - txtWidth * 0.5 ;
      }

      return leftOffset ;
    }
    
    private static ( double, double, double, double ) GetMinAndMaxPoint( IEnumerable<PolyLine> polyLines, IEnumerable<Line> lines, IEnumerable<Arc> arcs )
    {
      var points = new List<XYZ>() ;
      foreach ( var polyLine in polyLines ) {
        var pointsOfPolyLine = polyLine.GetCoordinates() ;
        points.AddRange( pointsOfPolyLine ) ;
      }

      foreach ( var line in lines ) {
        var startPoint = line.GetEndPoint( 0 ) ;
        var endPoint = line.GetEndPoint( 1 ) ;
        points.Add( startPoint ) ;
        points.Add( endPoint ) ;
      }

      foreach ( var arc in arcs ) {
        points.Add( new XYZ( arc.Center.X - arc.Radius, arc.Center.Y - arc.Radius, 0 ) ) ;
        points.Add( new XYZ( arc.Center.X + arc.Radius, arc.Center.Y + arc.Radius, 0 ) ) ;
      }

      var minX = points.Min( p => p.X ).RevitUnitsToMillimeters() ;
      var minY = points.Min( p => p.Y ).RevitUnitsToMillimeters() ;
      var maxX = points.Max( p => p.X ).RevitUnitsToMillimeters() ;
      var maxY = points.Max( p => p.Y ).RevitUnitsToMillimeters() ; 

      return ( minX, minY, maxX, maxY ) ;
    }

    private static double GetScale( double minX, double minY, double maxX, double maxY, double scale )
    {
      var lengthX = maxX - minX ;
      var lengthY = maxY - minY ;
      var maxLength = lengthX > lengthY ? lengthX : lengthY ;

      return maxLength switch
      {
        >= 15 => 30 / maxLength,
        >= 5 => scale * 0.4,
        >= 3 => scale * 0.5,
        _ => scale
      } ;
    }

    private static double GetScale( IEnumerable<Line> lines, double scale )
    {
      double maxLength = 0 ;
      foreach ( var line in lines ) {
        var startPoint = line.GetEndPoint( 0 ) ;
        var endPoint = line.GetEndPoint( 1 ) ;
        var length = startPoint.DistanceTo( endPoint ).RevitUnitsToMillimeters() ;
        if ( length > maxLength ) maxLength = length ;
      }

      return maxLength switch
      {
        >= 15 => 30 / maxLength,
        >= 6 => scale * 0.25,
        >= 4 => scale * 0.5,
        _ => scale
      } ;
    }

    private static double GetScale( double diameter, double scale )
    {
      return diameter switch
      {
        > 15 => 30 / diameter,
        > 5 => scale * 0.25,
        > 3 => scale * 0.5,
        _ => scale
      } ;
    }

    private static void LoadGeometryFromGeometryObject( GeometryElement geometryElement, ICollection<Line> lines, ICollection<Arc> arcs, ICollection<PolyLine> polyLines, ICollection<Autodesk.Revit.DB.Point> points )
    {
      try {
        foreach ( GeometryObject geoObj in geometryElement ) {
          switch ( geoObj ) {
            case Line line :
            {
              lines.Add( line ) ;
              break ;
            }
            case Arc arc :
            {
              arcs.Add( arc ) ;
              break ;
            }
            case Autodesk.Revit.DB.Point point :
            {
              points.Add( point ) ;
              break ;
            }
            case PolyLine polyline :
            {
              polyLines.Add( polyline ) ;

              break ;
            }
            case GeometryInstance geometryInstance :
              LoadGeometryFromGeometryObject( geometryInstance.SymbolGeometry, lines, arcs, polyLines, points ) ;
              break ;
          }
        }
      }
      catch {
        //
      }
    }

    private static string Get2DSymbolDwgPath( string dwgNumber )
    {
      const string folderName = "2D Symbol DWG" ;
      string directory = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) ! ;
      var resourcesPath = Path.Combine( directory, "resources" ) ;
      var fileName = dwgNumber + ".dwg" ;
      return Path.Combine( resourcesPath, folderName, fileName ) ;
    }
    
    private static string GetImageDataFromCanvas( Canvas canvas )
    {
      var size = new Size( canvas.Width, canvas.Height ) ;
      canvas.Measure( size ) ;
      canvas.Arrange( new Rect( size ) ) ;

      var renderBitmap = new RenderTargetBitmap( (int) size.Width, (int) size.Height, 96d, 96d, PixelFormats.Pbgra32 ) ;
      renderBitmap.Render( canvas ) ;
      var bitmapFrame = BitmapFrame.Create( renderBitmap ) ;
      using MemoryStream outStream = new() ;
      BitmapEncoder enc = new BmpBitmapEncoder() ;

      enc.Frames.Add( bitmapFrame ) ;
      enc.Save( outStream ) ;
      var bitmap = new Bitmap( outStream ) ;
      var base64FloorPlanImages = ConvertBitmapToBase64( bitmap ) ;
      return base64FloorPlanImages ;
    }

    private static string ConvertBitmapToBase64( Bitmap bmp )
    {
      var ms = new MemoryStream() ;
      bmp.Save( ms, ImageFormat.Bmp ) ;
      var byteImage = ms.ToArray() ;
      var result = Convert.ToBase64String( byteImage ) ;
      return result ;
    }

    private class CanvasChildInfo
    {
      public string DwgNumber { get ; }
      public List<PolyLine> PolyLines { get ; }
      public List<Line> Lines { get ; }
      public List<Arc> Arcs { get ; }

      public CanvasChildInfo( string dwgNumber, List<PolyLine> polyLines, List<Line> lines, List<Arc> arcs )
      {
        DwgNumber = dwgNumber ;
        PolyLines = polyLines ;
        Lines = lines ;
        Arcs = arcs ;
      }
    }
  }
}