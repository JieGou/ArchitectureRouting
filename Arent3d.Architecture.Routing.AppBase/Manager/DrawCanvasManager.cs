using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Reflection ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Media ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Point = System.Windows.Point ;
using Polyline = System.Windows.Shapes.Polyline ;

namespace Arent3d.Architecture.Routing.AppBase.Manager
{
  public static class DrawCanvasManager
  {
    private const string MallCondition = "モール" ;
    private const string MoSymbol = " (モ)" ;

    public static Canvas CreateCanvas( ICollection<Line> lines, ICollection<Arc> arcs, ICollection<PolyLine> polyLines, string deviceSymbol, string floorPlanSymbol, string condition )
    {
      const double scale = 15 ;
      const double defaultOffset = 20 ;
      var rotateTransform = new RotateTransform( 90, 0, 0 ) ;
      Canvas canvasPanel = new() { Background = new SolidColorBrush( Colors.Black ), Width = 245, Height = 100 } ;
      var offsetX= defaultOffset * 5 ;
      var offsetY= defaultOffset * 2 ;
      var minX = defaultOffset ;
      var minY = defaultOffset ;
      var scaleOfLine = scale ;
      if ( lines.Any() || arcs.Any() || polyLines.Any() ) {
        ( minX, minY ) = GetMinPoint( polyLines, lines, arcs ) ;
      }
      try {
        if ( polyLines.Any() ) {
          foreach ( var polyline in polyLines ) {
            var pointsOfPolyLine = new PointCollection() ;
            var points = polyline.GetCoordinates() ;
            if ( polyline == polyLines.First() ) {
              scaleOfLine = lines.Any() ? GetScale( polyLines, lines, scale ) : GetScale( points, scale ) ;
              offsetX -= minX * scaleOfLine ; 
              offsetY = minY == 0 ? offsetY + scaleOfLine * 2 : offsetY - minY * scaleOfLine ;
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
              RenderTransform = rotateTransform,
            } ;

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
            RenderTransform = rotateTransform,
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
            var arcRotateTransform = new RotateTransform( 90, centerX, centerY ) ;
            var newPath = new System.Windows.Shapes.Path()
            {
              Stroke = new SolidColorBrush( Colors.Green ), 
              StrokeThickness = 2, 
              Data = pathGeometry, 
              RenderTransform = arcRotateTransform,
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
      
      var leftOffset = polyLines.Any() && minY == 0 ? offsetX - txt.Width * 0.5 - scaleOfLine : offsetX - txt.Width * 0.5 ;
      Canvas.SetTop( txt, 5 ) ;
      Canvas.SetLeft( txt, leftOffset ) ;
      canvasPanel.Children.Add( txt ) ;

      return canvasPanel ;
    }
    
    private static ( double, double ) GetMinPoint( IEnumerable<PolyLine> polyLines, IEnumerable<Line> lines, IEnumerable<Arc> arcs )
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
      }

      var minX = points.Min( p => p.X ).RevitUnitsToMillimeters() ;
      var minY = points.Min( p => p.Y ).RevitUnitsToMillimeters() ;

      return ( minX, minY ) ;
    }

    private static double GetScale( IEnumerable<PolyLine> polyLines, IEnumerable<Line> lines, double scale )
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

      var minX = points.Min( p => p.X ) ;
      var maxX = points.Max( p => p.X ) ;
      var minY = points.Min( p => p.Y ) ;
      var maxY = points.Max( p => p.Y ) ;
      var lengthX = Math.Abs( maxX - minX ).RevitUnitsToMillimeters() ;
      var lengthY = Math.Abs( maxY - minY ).RevitUnitsToMillimeters() ;
      var maxLength = Math.Max( lengthX, lengthY ) ;

      return maxLength switch
      {
        >= 15 => 30 / maxLength,
        >= 6 => scale * 0.25,
        >= 4 => scale * 0.5,
        _ => scale
      } ;
    }

    private static double GetScale( ICollection<XYZ> points, double scale )
    {
      double maxLength = 0 ;
      for ( var i = 0 ; i < points.Count - 1 ; i++ ) {
        var length = points.ElementAt( i ).DistanceTo( points.ElementAt( i + 1 ) ).RevitUnitsToMillimeters() ;
        if ( length > maxLength ) maxLength = length ;
      }

      return maxLength switch
      {
        >= 15 => 30 / maxLength,
        >= 5 => scale * 0.25,
        >= 3 => scale * 0.5,
        _ => scale
      } ;
    }

    private static double GetScale( ICollection<Line> lines, double scale )
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
      switch ( diameter ) {
        case > 15 :
          return 30 / diameter ;
        case > 5 :
          return scale * 0.25 ;
        case > 3 :
          return scale * 0.5 ;
        default :
          return scale ;
      }
    }

    public static void LoadGeometryFromGeometryObject( GeometryElement geometryElement, ICollection<Line> lines, ICollection<Arc> arcs, ICollection<PolyLine> polyLines, ICollection<Autodesk.Revit.DB.Point> points )
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

    public static string Get2DSymbolDwgPath( string dwgNumber )
    {
      const string folderName = "2D Symbol DWG" ;
      string directory = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) ! ;
      var resourcesPath = Path.Combine( directory, "resources" ) ;
      var fileName = dwgNumber + ".dwg" ;
      return Path.Combine( resourcesPath, folderName, fileName ) ;
    }
  }
}