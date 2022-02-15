using System ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Architecture ;
using Application = Autodesk.Revit.ApplicationServices.Application ;
using MathLib ;
using Line = Autodesk.Revit.DB.Line ;

namespace Arent3d.Architecture.Routing
{
  public class ObstacleGeneration
  {
    public static List<List<Box3d>> GetAllObstacleRoomBox( Document doc )
    {
      var listOut = new List<List<Box3d>>() ;
      var linkedDocumentFilter = GetLinkedDocFilter( doc, doc.Application ) ;
      var currentDocumentFilter = new FilteredElementCollector( doc ) ;

      var allRooms = GetAllRoomInCurrentAndLinkedDocument( linkedDocumentFilter, currentDocumentFilter ) ;
      var livingRooms = allRooms.Where( r => r.Name.Contains( "LDR" ) || r.Name.Contains( "LDK" ) ).ToList() ;
      var otherRooms = allRooms.Except( livingRooms ).ToList() ;

      var otherListBox3d = CreateBox3dFromOther( otherRooms, doc ) ;
      var livingListBox3d = CreateBox3dFromLivingRoom( livingRooms, doc ) ;

      listOut.Add( livingListBox3d ) ;
      listOut.Add( otherListBox3d.ToList() ) ;
      return listOut ;
    }

    private static FilteredElementCollector? GetLinkedDocFilter( Document doc, Application app )
    {
      var firstLinkDoc = new FilteredElementCollector( doc ).OfCategory( BuiltInCategory.OST_RvtLinks ).ToElements().First() ;
      foreach ( Document linkedDoc in app.Documents ) {
        if ( linkedDoc.Title.Equals( firstLinkDoc.Name.Replace( ".rvt", "" ) ) )
          return new FilteredElementCollector( linkedDoc ) ;
      }

      return null ;
    }

    private static IList<Room> GetAllRoomInCurrentAndLinkedDocument( FilteredElementCollector? filterLinked, FilteredElementCollector? filterCurrent )
    {
      var rooms = new List<Room>() ;
      if ( filterLinked != null ) {
        var filterRooms = filterLinked.WhereElementIsNotElementType().OfClass( typeof( SpatialElement ) ).Where( e => e.GetType() == typeof( Room ) ).Cast<Room>().ToList() ;
        rooms.AddRange( filterRooms ) ;
      }

      if ( filterCurrent != null ) {
        var filterRooms = filterCurrent.WhereElementIsNotElementType().OfClass( typeof( SpatialElement ) ).Where( e => e.GetType() == typeof( Room ) ).Cast<Room>().ToList() ;
        rooms.AddRange( filterRooms ) ;
      }

      return rooms ;
    }

    private static IEnumerable<Box3d> CreateBox3dFromOther( List<Room> list, Document document )
    {
      foreach ( var room in list ) {
        var bb = room.get_BoundingBox( document.ActiveView ) ;
        if ( bb is null ) continue ;
        var min = bb.Min.To3dRaw() ;
        var max = bb.Max.To3dRaw() ;
        var box3d = new Box3d( min, max ) ;
        yield return box3d ;
      }
    }

    private static List<Box3d> CreateBox3dFromLivingRoom( List<Room> list, Document document )
    {
      var listOut = new List<Box3d>() ;
      var option = new SpatialElementBoundaryOptions() ;
      option.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.CoreCenter ;
      foreach ( var room in list ) {
        try {
          //Get all boundary segments
          var height = room.UnboundedHeight ;
          var bb = room.get_BoundingBox( null ) ;
          var lenghtMaxY = bb.Max.Y - bb.Min.Y ;

          var curves = room!.GetBoundarySegments( option ).First().Select( x => x.GetCurve() ).Cast<Line>().ToList() ;
          var curvesFix = GeoExtension.FixDiagonalLines( curves, lenghtMaxY * 2 ) ;

          //Joined unnecessary segments 
          var listJoinedX = GeoExtension.GetAllXLine( curvesFix ).GroupBy( x => Math.Round( x.Origin.Y, 4 ) ).ToDictionary( x => x.Key, g => g.ToList() ).Select( d => GeoExtension.JoinListStraitLines( d.Value ) ).ToList() ;
          var listJoinedY = GeoExtension.GetAllYLine( curvesFix ).GroupBy( x => Math.Round( x.Origin.X, 4 ) ).ToDictionary( x => x.Key, g => g.ToList() ).Select( d => GeoExtension.JoinListStraitLines( d.Value ) ).ToList() ;

          //get all points in boundary
          var allConnerPoints = new List<XYZ>() ;
          listJoinedY.ForEach( x =>
          {
            allConnerPoints.Add( x.GetEndPoint( 0 ) ) ;
            allConnerPoints.Add( x.GetEndPoint( 1 ) ) ;
          } ) ;

          //get all points of intersect with Y to X curves
          foreach ( var lineY in listJoinedY ) {
            var lineExtend = lineY.ExtendBothY( lenghtMaxY * 2 ) ;
            var (intersected, points) = lineExtend.CheckIntersectPoint( listJoinedX ) ;
            if ( intersected ) {
              allConnerPoints.AddRange( points ) ;
            }
          }

          //distinct points
          var comparer = new XyzComparer() ;
          var distinctPoints = allConnerPoints.Distinct( comparer ).ToList() ;
          var dic = distinctPoints.GroupBy( x => Math.Round( x.X, 4 ) ).OrderBy( d => d.Key ).Where( d => d.ToList().Count > 1 ).ToDictionary( x => x.Key, g => g.ToList() ) ;

          //Find out the rectangular
          var listRec = new List<RectangularBox>() ;
          for ( int i = 0 ; i < dic.Count - 1 ; i++ ) {
            var l1 = dic[ dic.Keys.ToList()[ i ] ] ;
            var l2 = dic[ dic.Keys.ToList()[ i + 1 ] ] ;
            l1.AddRange( l2 ) ;
            var rec = GeoExtension.FindRectangular( l1, GeoExtension.GetAllXLine( curvesFix ), lenghtMaxY * 2 ) ;
            listRec.AddRange( rec ) ;
          }

          //Create the room box
          foreach ( var rectangular in listRec ) {
            rectangular.Height = height ;
            var box3d = new Box3d( rectangular.GetMin().To3dRaw(), rectangular.GetMax().To3dRaw() ) ;
            listOut.Add( box3d ) ;
          }
        }
        catch {
          //ignore
        }
      }

      return listOut ;
    }
  }

  public class XyzComparer : IEqualityComparer<XYZ>
  {
    public bool Equals( XYZ x, XYZ y )
    {
      return Math.Abs( x.X - y.X ) < 0.0001 && Math.Abs( x.Y - y.Y ) < 0.0001 && Math.Abs( x.Z - y.Z ) < 0.0001 ;
    }

    public int GetHashCode( XYZ obj )
    {
      return 1 ;
    }
  }

  public static class GeoExtension
  {
    public static List<Line> GetAllXLine( IEnumerable<Line> lines )
    {
      return lines.Where( l => l.Direction.IsAlmostEqualTo( XYZ.BasisX ) || l.Direction.IsAlmostEqualTo( -XYZ.BasisX ) ).ToList() ;
    }

    public static List<Line> GetAllYLine( IEnumerable<Line> lines )
    {
      return lines.Where( l => l.Direction.IsAlmostEqualTo( XYZ.BasisY ) || l.Direction.IsAlmostEqualTo( -XYZ.BasisY ) ).ToList() ;
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

    private static XYZ UpperPoint( this Line line )
    {
      var list = new List<XYZ>() { line.GetEndPoint( 0 ), line.GetEndPoint( 1 ) } ;
      return list.OrderBy( x => x.Y ).Last() ;
    }

    public static XYZ ProjectPointToCurve( XYZ point, Curve curve )
    {
      var res = curve.Project( point ) ;
      return res.XYZPoint ;
    }

    public static List<Line> FixDiagonalLines( List<Line> lines, double lengthEx )
    {
      var allLineX = GetAllXLine( lines ) ;
      var allLineY = GetAllYLine( lines ) ;
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
        var (intersected, result) = lineDown.CheckIntersectPoint( allXCheck ) ;

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

    public static (bool Intersected, List<XYZ> points) CheckIntersectPoint( this Line line, List<Line> lines )
    {
      var points = new List<XYZ>() ;
      foreach ( var l in lines ) {
        if ( line.Intersect( l, out var result ) == SetComparisonResult.Overlap ) {
          points.Add( result.get_Item( 0 ).XYZPoint ) ;
        }
      }

      return ( points.Count != 0, points ) ;
    }

    public static List<RectangularBox> FindRectangular( IEnumerable<XYZ> list, List<Line> supportLine, double lengthExt )
    {
      var listOut = new List<RectangularBox>() ;
      try {
        var listSort = list.GroupBy( x => Math.Round( x.Y, 6 ) ).ToDictionary( x => x.Key, g => g.ToList() ).Where( d => d.Value.Count > 1 ).OrderBy( x => x.Key ).ToList() ;
        if ( listSort.Count == 2 ) {
          var (lower, upper) = ( listSort.First().Value.OrderBy( x => x.X ).ToList(), listSort.Last().Value.OrderBy( x => x.X ).ToList() ) ;
          listOut.Add( new RectangularBox( lower.First(), lower.Last(), upper.First(), upper.Last() ) ) ;
        }
        else {
          //Create line in middle
          var lower = listSort.First().Value.OrderBy( x => x.X ).ToList() ;
          var (pointX, pointY, pointZ) = ( ( lower.First().X + lower.Last().X ) / 2, ( lower.First().Y + lower.Last().Y ) / 2, ( lower.First().Z + lower.Last().Z ) / 2 ) ;
          var lineCheck = Line.CreateBound( new XYZ( pointX, pointY - lengthExt, pointZ ), new XYZ( pointX, pointY + lengthExt, pointZ ) ) ;

          //Check all intersect
          var (intersected, points) = lineCheck.CheckIntersectPoint( supportLine ) ;
          if ( intersected ) {
            var listYIntersected = points.Select( x => Math.Round( x.Y, 6 ) ).ToList() ;
            var listByY = listSort.Where( x => listYIntersected.Contains( Math.Round( x.Key, 6 ) ) ).OrderBy( x => x.Key ).ToList() ;
            if ( listByY.Count < 2 ) return listOut ;
            var splitList = listByY.Select( ( x, i ) => new { Index = i, Value = x } ).GroupBy( x => x.Index / 2 ).Select( x => x.Select( v => v.Value ).ToList() ).ToList() ;
            splitList.ForEach( listItem =>
            {
              if ( listItem.Count == 2 ) {
                var (low, up) = ( listItem.First().Value.OrderBy( x => x.X ).ToList(), listItem.Last().Value.OrderBy( x => x.X ).ToList() ) ;
                listOut.Add( new RectangularBox( low.First(), low.Last(), up.First(), up.Last() ) ) ;
              }
            } ) ;
          }
        }
      }
      catch ( Exception ex ) {
        var message = ex.Message ;
      }

      return listOut ;
    }
  }

  public class RectangularBox
  {
    private readonly XYZ _pt1 ;
    private readonly XYZ _pt2 ;
    private readonly XYZ _pt3 ;
    private readonly XYZ _pt4 ;
    public double? Height ;

    public RectangularBox( XYZ pt1, XYZ pt2, XYZ pt3, XYZ pt4 )
    {
      _pt1 = pt1 ;
      _pt2 = pt2 ;
      _pt3 = pt3 ;
      _pt4 = pt4 ;
    }

    public XYZ GetMin()
    {
      var list = new List<XYZ>() { _pt1, _pt2, _pt3, _pt4 } ;
      return list.OrderBy( p => p.X ).Where( ( x, i ) => i is 0 or 1 ).OrderBy( p => p.Y ).First() ;
    }

    public XYZ GetMax()
    {
      var list = new List<XYZ>() { _pt1, _pt2, _pt3, _pt4 } ;
      var max = list.OrderBy( p => p.X ).Where( ( x, i ) => i is 2 or 3 ).OrderBy( p => p.Y ).Last() ;
      return Height != null ? new XYZ( max.X, max.Y, (double) ( max.Z + Height ) ) : max ;
    }
  }
}