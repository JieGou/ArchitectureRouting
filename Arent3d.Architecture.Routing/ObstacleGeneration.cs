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
  public static class ObstacleGeneration
  {
    private static List<List<Box3d>> _listRoomBox3dInCurrentProject = new() ;

    public static List<List<Box3d>> GetAllObstacleRoomBox( Document doc )
    {
      var allRooms = GetAllRoomsInCurrentAndLinkDocument( doc ) ;
      var livingRooms = allRooms.Where( r => r.Name.Contains( "LDR" ) || r.Name.Contains( "LDK" ) ).ToList() ;
      var otherRooms = allRooms.Except( livingRooms ) ;
      var livingListBox3d = GetObstacleRoomBoxes( livingRooms ) ;
      var otherListBox3d = GetObstacleRoomBoxes( otherRooms.ToList() ) ;
      _listRoomBox3dInCurrentProject = new List<List<Box3d>> { livingListBox3d, otherListBox3d } ;
      return _listRoomBox3dInCurrentProject ;
    }

    private static List<Box3d> GetObstacleRoomBoxes(List<Room> rooms)
    {
      var box3dList = new List<Box3d>() ;
      foreach ( var room in rooms ) {
        box3dList.AddRange( GetRoomWallBoxes(room) );
      }

      return box3dList ;
    }
    private static Box3d GetBox3d( ElementId id, Document document )
    {
      var boundingBox = document.GetElement( id ).get_BoundingBox( null ) ;
      return new Box3d( new Vector3d( boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Min.Z ), new Vector3d( boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Max.Z ) ) ;
    }
    private static List<Box3d> GetRoomWallBoxes( Room room )
    {
      var box3dList = new List<Box3d>() ;
      var option = new SpatialElementBoundaryOptions() ;
      option.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish ;
      var boundarySegments = room.GetBoundarySegments(option) ;
      foreach ( var segments in boundarySegments ) {
        var box3ds = ( from boundarySegment in segments select GetBox3d( boundarySegment.ElementId, room.Document ) ).ToList() ;
        var min = new Vector3d( box3ds.Min( b => b.Min.x ), box3ds.Min( b => b.Min.y ), box3ds.Min( b => b.Min.z ) ) ;
        var max = new Vector3d( box3ds.Max( b => b.Max.x ), box3ds.Max( b => b.Max.y ), box3ds.Max( b => b.Max.z ) ) ;
        box3dList.Add( new Box3d( min,max ) );
      }

      return box3dList ;
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
      var firstLinkDoc = new FilteredElementCollector( doc ).OfCategory( BuiltInCategory.OST_RvtLinks ).ToElements().FirstOrDefault() ;
      if ( firstLinkDoc == null ) return null ;
      foreach ( Document linkedDoc in app.Documents ) {
        if ( linkedDoc.Title.Equals( firstLinkDoc.Name.Replace( ".rvt", "" ) ) )
          return new FilteredElementCollector( linkedDoc ) ;
      }
      
      return null ;
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