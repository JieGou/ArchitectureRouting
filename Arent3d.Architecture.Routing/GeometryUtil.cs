using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using MathLib ;
using Line = Autodesk.Revit.DB.Line ;

namespace Arent3d.Architecture.Routing
{
  public static class GeometryUtil
  {
    public static bool IsPerpendicularTo( this XYZ dir1, XYZ dir2, double angleTolerance )
    {
      return Math.Abs( Math.PI / 2 - dir1.AngleTo( dir2 ) ) < angleTolerance ;
    }
    
    public static bool FilterBeamWithXyDirection( this FamilyInstance beam )
    {
      return beam.Location switch
      {
        LocationPoint => true,
        LocationCurve { Curve: Line line } => line.Direction.IsAlmostEqualTo( XYZ.BasisX ) || line.Direction.IsAlmostEqualTo( -XYZ.BasisX ) || line.Direction.IsAlmostEqualTo( XYZ.BasisY ) || line.Direction.IsAlmostEqualTo( -XYZ.BasisY ),
        _ => false
      } ;
    }

    public static bool FilterBeamUniqueSolid( this FamilyInstance beam, Options option )
    {
      var geometryElement = beam.get_Geometry( option ) ;
      return geometryElement.Select( x => x as Solid ).Count( x => x?.Volume > 0 ) == 1 ;
    }

    public static IEnumerable<Line> GetAllXLine( IEnumerable<Line> lines )
    {
      return lines.Where( l => l.Direction.IsAlmostEqualTo( XYZ.BasisX ) || l.Direction.IsAlmostEqualTo( -XYZ.BasisX ) ) ;
    }

    public static IEnumerable<Line> GetAllYLine( IEnumerable<Line> lines )
    {
      return lines.Where( l => l.Direction.IsAlmostEqualTo( XYZ.BasisY ) || l.Direction.IsAlmostEqualTo( -XYZ.BasisY ) ) ;
    }

    public static Line JoinListStraitLines( List<Line> lines )
    {
      var listX = new List<double>() ;
      var listY = new List<double>() ;
      lines.ForEach( x =>
      {
        listX.Add( x.GetEndPoint( 0 ).X ) ;
        listX.Add( x.GetEndPoint( 1 ).X ) ;
        listY.Add( x.GetEndPoint( 0 ).Y ) ;
        listY.Add( x.GetEndPoint( 1 ).Y ) ;
      } ) ;
      var start = new XYZ( listX.Min(), listY.Min(), lines.First().Origin.Z ) ;
      var end = new XYZ( listX.Max(), listY.Max(), lines.First().Origin.Z ) ;
      return Line.CreateBound( start, end ) ;
    }

    private static Line ExtendLineStartY( this Line line, double value )
    {
      if ( line.Direction.IsAlmostEqualTo( XYZ.BasisY ) ) {
        var start = line.GetEndPoint( 0 ) ;
        var end = line.GetEndPoint( 1 ) ;
        return Line.CreateBound( new XYZ( start.X, start.Y - value, start.Z ), end ) ;
      }
      else if ( line.Direction.IsAlmostEqualTo( -XYZ.BasisY ) ) {
        var start = line.GetEndPoint( 0 ) ;
        var end = line.GetEndPoint( 1 ) ;
        return Line.CreateBound( new XYZ( start.X, start.Y + value, start.Z ), end ) ;
      }
      else {
        return line ;
      }
    }

    private static Line ExtendLineEndY( this Line line, double value )
    {
      if ( line.Direction.IsAlmostEqualTo( XYZ.BasisY ) ) {
        var start = line.GetEndPoint( 0 ) ;
        var end = line.GetEndPoint( 1 ) ;
        return Line.CreateBound( start, new XYZ( end.X, end.Y + value, end.Z ) ) ;
      }
      else if ( line.Direction.IsAlmostEqualTo( -XYZ.BasisY ) ) {
        var start = line.GetEndPoint( 0 ) ;
        var end = line.GetEndPoint( 1 ) ;
        return Line.CreateBound( start, new XYZ( end.X, end.Y - value, end.Z ) ) ;
      }
      else {
        return line ;
      }
    }

    public static Line ExtendBothY( this Line line, double value )
    {
      return line.ExtendLineStartY( value ).ExtendLineEndY( value ) ;
    }

    private static XYZ LowerPoint( this Line line )
    {
      var list = new List<XYZ>() { line.GetEndPoint( 0 ), line.GetEndPoint( 1 ) } ;
      return list.OrderBy( x => x.Y ).First() ;
    }

    private static XYZ UpperPoint( this Curve line )
    {
      var list = new List<XYZ>() { line.GetEndPoint( 0 ), line.GetEndPoint( 1 ) } ;
      return list.OrderBy( x => x.Y ).Last() ;
    }

    private static XYZ AddHeight( this XYZ point, double value )
    {
      var (x, y, z) = point ;
      return new XYZ( x, y, z + value ) ;
    }

    public static List<Line> FixDiagonalLines( List<Line> lines, double lengthEx )
    {
      var allLineX = GetAllXLine( lines ).ToList() ;
      var allLineY = GetAllYLine( lines ).ToList() ;
      var diagLines = lines.Except( allLineX ).Except( allLineY ).ToList() ;
      if ( ! diagLines.Any() ) return lines ;

      var listOut = new List<Line>() ;
      listOut.AddRange( allLineX ) ;
      listOut.AddRange( allLineY ) ;

      foreach ( var line in diagLines ) {
        var lowerPoint = line.LowerPoint() ;
        var upperPoint = line.UpperPoint() ;

        var lineDown = Line.CreateBound( lowerPoint, new XYZ( lowerPoint.X, lowerPoint.Y - lengthEx, lowerPoint.Z ) ) ;
        var allXCheck = allLineX.Where( x => Math.Abs( x.Origin.Y - lowerPoint.Y ) > 0.0001 ).ToList() ;
        var intersected = lineDown.TryIntersectPoint( allXCheck, out _) ;

        if ( intersected ) {
          var lineNew = Line.CreateBound( upperPoint, new XYZ( upperPoint.X, lowerPoint.Y, upperPoint.Z ) ) ;
          listOut.Add( lineNew ) ;
        }
        else {
          var lineNew = Line.CreateBound( lowerPoint, new XYZ( lowerPoint.X, upperPoint.Y, lowerPoint.Z ) ) ;
          listOut.Add( lineNew ) ;
        }
      }

      return listOut ;
    }
    
    public static bool TryIntersectPoint( this Line line, IEnumerable<Line> lines, out List<XYZ> resultPoints )
    {
      resultPoints = new List<XYZ>() ;
      foreach ( var l in lines ) {
        if ( line.Intersect( l, out var result ) == SetComparisonResult.Overlap ) {
          resultPoints.Add( result.get_Item( 0 ).XYZPoint ) ;
        }
      }
      
      return resultPoints.Any() ;
    }
    
    public static IEnumerable<Box3d> FindRectangularBox( IEnumerable<XYZ> list, IEnumerable<Line> supportLine, double lengthExt, double addHeight )
    {
      var listOut = new List<Box3d>() ;
      try {
        var listSort = list.GroupBy( x => Math.Round( x.Y, 6 ) ).Where( d => d.Count() > 1 ).OrderBy( x => x.Key ).ToList() ;
        if ( listSort.Count == 2 ) {
          var (lower, upper) = ( listSort.First().OrderBy( x => x.X ).ToList(), listSort.Last().OrderBy( x => x.X ).ToList() ) ;
          var box3d = Box3d.ConstructClosure( new[] { lower.First().To3dRaw(), lower.Last().To3dRaw(), upper.First().AddHeight( addHeight ).To3dRaw(), upper.Last().AddHeight( addHeight ).To3dRaw() } ) ;
          listOut.Add( box3d ) ;
        }
        else {
          //Create line in middle
          var lower = listSort.First().OrderBy( x => x.X ).ToList() ;
          var (pointX, pointY, pointZ) = ( ( lower.First().X + lower.Last().X ) / 2, ( lower.First().Y + lower.Last().Y ) / 2, ( lower.First().Z + lower.Last().Z ) / 2 ) ;
          var lineCheck = Line.CreateBound( new XYZ( pointX, pointY - lengthExt, pointZ ), new XYZ( pointX, pointY + lengthExt, pointZ ) ) ;

          //Check all intersect
          var intersected = lineCheck.TryIntersectPoint( supportLine, out var points) ;
          if ( intersected ) {
            var listYIntersected = points.Select( x => Math.Round( x.Y, 6 ) );
            var listByY = listSort.Where( x => listYIntersected.Contains( Math.Round( x.Key, 6 ) ) ).OrderBy( x => x.Key ).ToList() ;
            if ( listByY.Count < 2 ) return listOut ;

            var splitList = listByY.Select( ( x, i ) => new { Index = i, Value = x } ).GroupBy( x => x.Index / 2 ).Select( x => x.Select( v => v.Value ).ToList() ) ;
            splitList.ForEach( listItem =>
            {
              if ( listItem.Count() == 2 ) {
                var (low, up) = ( listItem.First().OrderBy( x => x.X ).ToList(), listItem.Last().OrderBy( x => x.X ).ToList() ) ;
                var box3d = Box3d.ConstructClosure( new[] { low.First().To3dRaw(), low.Last().To3dRaw(), up.First().AddHeight( addHeight ).To3dRaw(), up.Last().AddHeight( addHeight ).To3dRaw() } ) ;
                listOut.Add( box3d ) ;
              }
            } ) ;
          }
        }
      }
      catch ( Exception ) {
        //ignore
      }

      return listOut ;
    }
  }
}