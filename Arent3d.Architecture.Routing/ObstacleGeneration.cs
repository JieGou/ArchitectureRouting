using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Utils ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Architecture ;
using Application = Autodesk.Revit.ApplicationServices.Application ;
using MathLib ;
using Line = Autodesk.Revit.DB.Line ;

namespace Arent3d.Architecture.Routing
{
  public static class ObstacleGeneration
  {
    private static List<List<Box3d>> _listRoomBox3dInCurrentProject = new() ;

    public static List<List<Box3d>> GetAllObstacleRoomBox( Document doc )
    {
      var allRooms = GetAllRoomsInCurrentAndLinkDocument( doc ) ;
      var livingRooms = allRooms.Where( r => r.Name.Contains( "LDR" ) || r.Name.Contains( "LDK" ) ).ToList() ;
      var otherRooms = allRooms.Except( livingRooms ) ;

      var livingListBox3d = CreateBox3dFromDividedRoom( livingRooms ) ;
      var otherListBox3d = CreateBox3dFromDividedRoom( otherRooms ) ;

      _listRoomBox3dInCurrentProject = new List<List<Box3d>> { livingListBox3d, otherListBox3d } ;
      return _listRoomBox3dInCurrentProject ;
    }


    private static IList<Room> GetAllRoomsInCurrentAndLinkDocument( Document doc )
    {
      var filterLinked = GetLinkedDocFilter( doc, doc.Application ) ;
      var filterCurrent = new FilteredElementCollector( doc ) ;

      var rooms = new List<Room>() ;
      if ( filterLinked != null ) {
        var linkedRooms = filterLinked.WhereElementIsNotElementType().OfClass( typeof( SpatialElement ) ).Where( e => e.GetType() == typeof( Room ) ).Cast<Room>() ;
        rooms.AddRange( linkedRooms ) ;
      }

      var currentRooms = filterCurrent.WhereElementIsNotElementType().OfClass( typeof( SpatialElement ) ).Where( e => e.GetType() == typeof( Room ) ).Cast<Room>() ;
      rooms.AddRange( currentRooms ) ;

      return rooms ;
    }

    public static IList<T> GetAllElementInCurrentAndLinkDocument<T>( Document doc, BuiltInCategory category )
    {
      ElementCategoryFilter filter = new(category) ;
      var collectorLink = GetLinkedDocFilter( doc, doc.Application ) ;
      var collectorCurrent = new FilteredElementCollector( doc ) ;

      var allElements = new List<T>() ;
      if ( collectorLink is not null ) {
        var elsLink = collectorLink.WherePasses( filter ).WhereElementIsNotElementType().ToElements().OfType<T>() ;
        allElements.AddRange( elsLink ) ;
      }

      var elsCurrent = collectorCurrent.WherePasses( filter ).WhereElementIsNotElementType().ToElements().OfType<T>() ;
      allElements.AddRange( elsCurrent ) ;
      return allElements ;
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

    private static List<Box3d> CreateBox3dFromDividedRoom( IEnumerable<Room> list )
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

          var curves = room.GetBoundarySegments( option ).First().Select( x => x.GetCurve() ).Cast<Line>().ToList() ;
          var curvesFix = GeometryUtil.FixDiagonalLines( curves, lenghtMaxY * 2 ) ;

          //Joined unnecessary segments 
          var listJoinedX = GeometryUtil.GetAllXLine( curvesFix ).GroupBy( x => Math.Round( x.Origin.Y, 4 ) ).Select( d => GeometryUtil.JoinListStraitLines( d.ToList() ) ).ToList() ;
          var listJoinedY = GeometryUtil.GetAllYLine( curvesFix ).GroupBy( x => Math.Round( x.Origin.X, 4 ) ).Select( d => GeometryUtil.JoinListStraitLines( d.ToList() ) ).ToList();

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
            var intersected = lineExtend.TryIntersectPoint( listJoinedX, out var points ) ;
            if ( intersected ) {
              allConnerPoints.AddRange( points ) ;
            }
          }

          //distinct points
          var comparer = new XyzComparer() ;
          var arrayGroupByX = allConnerPoints
            .Distinct( comparer )
            .GroupBy( x => Math.Round( x.X, 4 ) )
            .OrderBy( d => d.Key )
            .Where( d => d.ToList().Count > 1 )
            .Select( x=>x.ToList() )
            .ToArray() ;
          
          //Find out the rectangular
          for ( var i = 0 ; i < arrayGroupByX.Length - 1 ; i++ ) {
            var l1 = arrayGroupByX[ i ] ;
            var l2 = arrayGroupByX[ i + 1 ]  ;
            l1.AddRange( l2 ) ;
            var recBox = GeometryUtil.FindRectangularBox( l1, GeometryUtil.GetAllXLine( curvesFix ), lenghtMaxY * 2, height ) ;
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
      _listRoomBox3dInCurrentProject.ForEach( list => list.ForEach( recBox => { CreateBoxGenericModelInPlace( recBox.Min.ToXYZRaw(), recBox.Max.ToXYZRaw(), document ) ; } ) ) ;
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
        const string envelopeParameter = "Obstacle Name" ;
        const string roomValueParam = "ROOM_BOX" ;
        
        ds.get_Parameter( BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS ).Set( roomValueParam ) ;
        ds.LookupParameter( envelopeParameter ).Set( roomValueParam ) ;
        return ds.Id ;
      }
      catch ( Exception ) {
        return ElementId.InvalidElementId ;
      }
    }
  }
}