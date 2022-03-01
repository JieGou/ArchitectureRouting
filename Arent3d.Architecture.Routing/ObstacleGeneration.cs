using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Architecture ;
using Application = Autodesk.Revit.ApplicationServices.Application ;
using MathLib ;
using Line = Autodesk.Revit.DB.Line ;

namespace Arent3d.Architecture.Routing
{
  public class ObstacleGeneration
  {
    public static List<List<Box3d>> ListRoomBox3dInCurrentProject = new() ;

    public static List<List<Box3d>> GetAllObstacleRoomBox( Document doc )
    {
      var linkedDocumentFilter = GetLinkedDocFilter( doc, doc.Application ) ;
      var currentDocumentFilter = new FilteredElementCollector( doc ) ;

      var allRooms = GetAllRoomInCurrentAndLinkedDocument( linkedDocumentFilter, currentDocumentFilter ) ;
      var livingRooms = allRooms.Where( r => r.Name.Contains( "LDR" ) || r.Name.Contains( "LDK" ) ).ToList() ;
      var otherRooms = allRooms.Except( livingRooms ).ToList() ;

      var livingListBox3d = CreateBox3dFromDividedRoom( livingRooms ) ;
      var otherListBox3d = CreateBox3dFromDividedRoom( otherRooms ) ;

      ListRoomBox3dInCurrentProject = new List<List<Box3d>> { livingListBox3d, otherListBox3d } ;
      return ListRoomBox3dInCurrentProject ;
    }

    public static FilteredElementCollector? GetLinkedDocFilter( Document doc, Application app )
    {
      var firstLinkDoc = new FilteredElementCollector( doc ).OfCategory( BuiltInCategory.OST_RvtLinks ).ToElements().First() ;
      foreach ( Document linkedDoc in app.Documents ) {
        if ( linkedDoc.Title.Equals( firstLinkDoc.Name.Replace( ".rvt", "" ) ) )
          return new FilteredElementCollector( linkedDoc ) ;
      }

      return null ;
    }

    public static IList<Room> GetAllRoomInCurrentAndLinkedDocument( FilteredElementCollector? filterLinked, FilteredElementCollector? filterCurrent )
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

    public static List<Box3d> CreateBox3dFromDividedRoom( List<Room> list )
    {
      var listOut = new List<Box3d>() ;
      var option = new SpatialElementBoundaryOptions() ;
      option.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish ;

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
          for ( var i = 0 ; i < dic.Count - 1 ; i++ ) {
            var l1 = dic[ dic.Keys.ToList()[ i ] ] ;
            var l2 = dic[ dic.Keys.ToList()[ i + 1 ] ] ;
            l1.AddRange( l2 ) ;
            var recBox = GeoExtension.FindRectangularBox( l1, GeoExtension.GetAllXLine( curvesFix ), lenghtMaxY * 2, height ) ;
            listOut.AddRange( recBox ) ;
          }
        }
        catch {
          //ignore
        }
      }

      return listOut ;
    }

    public static void ShowRoomBox( Document document )
    {
      ListRoomBox3dInCurrentProject.ForEach( list => list.ForEach( recBox => { CreateBoxGenericModelInPlace( recBox.Min.ToXYZRaw(), recBox.Max.ToXYZRaw(), document ) ; } ) ) ;
    }

    private static ElementId CreateBoxGenericModelInPlace( XYZ min, XYZ max, Document doc )
    {
      try {
        var line1 = Line.CreateBound( min, new XYZ( max.X, min.Y, min.Z ) ) ;
        var line2 = Line.CreateBound( new XYZ( max.X, min.Y, min.Z ), new XYZ( max.X, max.Y, min.Z ) ) ;
        var line3 = Line.CreateBound( new XYZ( max.X, max.Y, min.Z ), new XYZ( min.X, max.Y, min.Z ) ) ;
        var line4 = Line.CreateBound( new XYZ( min.X, max.Y, min.Z ), min ) ;

        var profile = new List<Curve>() { line1, line2, line3, line4 } ;
        var curveLoop = CurveLoop.Create( profile ) ;
        var profileLoops = new List<CurveLoop>() { curveLoop } ;
        var solid = GeometryCreationUtilities.CreateExtrusionGeometry( profileLoops, XYZ.BasisZ, max.Z - min.Z ) ;

        var ds = DirectShape.CreateElement( doc, new ElementId( BuiltInCategory.OST_GenericModel ) ) ;
        ds.SetShape( new GeometryObject[] { solid } ) ;
        ds.get_Parameter( BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS ).Set( "ROOM_BOX" ) ;
        return ds.Id ;
      }
      catch ( Exception ) {
        return ElementId.InvalidElementId ;
      }
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

    public static IEnumerable<Box3d> FindRectangularBox( IEnumerable<XYZ> list, List<Line> supportLine, double lengthExt, double addHeight )
    {
      var listOut = new List<Box3d>() ;
      try {
        var listSort = list.GroupBy( x => Math.Round( x.Y, 6 ) ).ToDictionary( x => x.Key, g => g.ToList() ).Where( d => d.Value.Count > 1 ).OrderBy( x => x.Key ).ToList() ;
        if ( listSort.Count == 2 ) {
          var (lower, upper) = ( listSort.First().Value.OrderBy( x => x.X ).ToList(), listSort.Last().Value.OrderBy( x => x.X ).ToList() ) ;
          var box3d = new Box3d( new[] { lower.First().To3dRaw(), lower.Last().To3dRaw(), upper.First().AddHeight( addHeight ).To3dRaw(), upper.Last().AddHeight( addHeight ).To3dRaw() } ) ;
          listOut.Add( box3d ) ;
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
                var box3d = new Box3d( new[] { low.First().To3dRaw(), low.Last().To3dRaw(), up.First().AddHeight( addHeight ).To3dRaw(), up.Last().AddHeight( addHeight ).To3dRaw() } ) ;
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