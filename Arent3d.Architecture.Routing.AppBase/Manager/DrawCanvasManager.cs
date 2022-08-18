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
      const double defaultOffset = 40 ;
      var rotateTransform = new RotateTransform( 90, 0, 0 ) ;
      Canvas canvasPanel = new() { Background = new SolidColorBrush( Colors.Black ), Width = 245, Height = 80 } ;
      try {
        var scaleOfLine = scale ;
        if ( polyLines.Any() ) {
          foreach ( var polyline in polyLines ) {
            var pointsOfPolyLine = new PointCollection() ;
            var points = polyline.GetCoordinates() ;
            if ( polyline == polyLines.First() ) {
              scaleOfLine = lines.Any() ? GetScale( polyLines, lines, scale ) : GetScale( points, scale ) ;
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

            Canvas.SetTop( newPolyline, defaultOffset ) ;
            Canvas.SetLeft( newPolyline, defaultOffset ) ;
            canvasPanel.Children.Add( newPolyline ) ;
          }
        }

        foreach ( var line in lines ) {
          if ( line == lines.First() && ! polyLines.Any() ) {
            scaleOfLine = GetScale( lines, scale ) ;
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

          Canvas.SetTop( newLine, defaultOffset ) ;
          Canvas.SetLeft( newLine, defaultOffset ) ;
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
            }
          }

          var newDiameter = diameter * scaleOfArc ;
          if ( arc.IsClosed ) {
            var newEllipse = new System.Windows.Shapes.Ellipse() { Stroke = new SolidColorBrush( Colors.Green ), StrokeThickness = 2, Width = newDiameter, Height = newDiameter } ;

            Canvas.SetTop( newEllipse, defaultOffset + centerY * scaleOfArc - newDiameter / 2 ) ;
            Canvas.SetLeft( newEllipse, defaultOffset + centerX * scaleOfArc - newDiameter / 2 ) ;
            canvasPanel.Children.Add( newEllipse ) ;
          }
          else {
            var firstPoint = arc.GetEndPoint( 0 ) ;
            var secondPoint = arc.GetEndPoint( 1 ) ;
            var startPoint = new Point( firstPoint.X.RevitUnitsToMillimeters() * scaleOfArc, firstPoint.Y.RevitUnitsToMillimeters() * scaleOfArc ) ;
            var endPoint = new Point( secondPoint.X.RevitUnitsToMillimeters() * scaleOfArc, secondPoint.Y.RevitUnitsToMillimeters() * scaleOfArc ) ;
            var arcSegment = new ArcSegment( endPoint, new Size( newDiameter / 2, newDiameter / 2 ), 0, false, SweepDirection.Clockwise, true ) ;
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
            Canvas.SetTop( newPath, defaultOffset ) ;
            Canvas.SetLeft( newPath, defaultOffset ) ;
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
        Canvas.SetTop( txtFloorPlanSymbol, 20 ) ;
        Canvas.SetLeft( txtFloorPlanSymbol, defaultOffset ) ;
        canvasPanel.Children.Add( txtFloorPlanSymbol ) ;
      }

      var text = condition == MallCondition ? deviceSymbol + MoSymbol : deviceSymbol ;
      TextBlock txt = new()
      {
        FontSize = 19, 
        Text = text, 
        Foreground = Brushes.White
      } ;
      Canvas.SetTop( txt, 20 ) ;
      Canvas.SetLeft( txt, 70 ) ;
      canvasPanel.Children.Add( txt ) ;

      return canvasPanel ;
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

      if ( lengthX >= 6 || lengthY >= 6 )
        return scale / 4 ;
      if ( lengthX >= 4 || lengthY >= 4 )
        return scale / 2 ;

      return scale ;
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
        >= 5 => scale / 4,
        >= 3 => scale / 2,
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
        >= 6 => scale / 4,
        >= 4 => scale / 2,
        _ => scale
      } ;
    }

    private static double GetScale( double diameter, double scale )
    {
      switch ( diameter ) {
        case > 5 :
          return scale / 4 ;
        case > 3 :
          return scale / 2 ;
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
      var resourcesPath = Path.Combine( directory.Substring( 0, directory.IndexOf( "bin", StringComparison.Ordinal ) ), "resources" ) ;
      var fileName = dwgNumber + ".dwg" ;
      return Path.Combine( resourcesPath, folderName, fileName ) ;
    }
  }
}