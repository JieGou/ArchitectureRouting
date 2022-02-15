using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Architecture ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using Application = Autodesk.Revit.ApplicationServices.Application ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.haseko.App.Commands.Routing.CreateRoomAndEnvelopeCommand", DefaultString = "Create\nRoom And Envelope" )]
  [Image( "resources/room_boxes.png" )]
  public class CreateRoomAndEnvelopeCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      try {
        using ( var transaction = new Transaction( document ) ) {
          transaction.Start( "Create room and envelope" ) ;
          //Delete all
          ElementCategoryFilter filterGen = new(BuiltInCategory.OST_GenericModel) ;

          var pvp = new ParameterValueProvider( new ElementId( BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS ) ) ;
          var stringContains = new FilterStringContains() ;
          var value1 = "ROOM_BOX" ;
          var value2 = "_ENVELOPE" ;
          var fRule1 = new FilterStringRule( pvp, stringContains, value1, true ) ;
          var fRule2 = new FilterStringRule( pvp, stringContains, value2, true ) ;
          var filterComment1 = new ElementParameterFilter( fRule1 ) ;
          var filterComment2 = new ElementParameterFilter( fRule2 ) ;
          var filterComment = new LogicalOrFilter( filterComment1, filterComment2 ) ;

          var collector = new FilteredElementCollector( document ) ;
          var allBoxEnvelope = collector.WherePasses( filterGen ).WherePasses( filterComment ).WhereElementIsNotElementType().ToElementIds() ;

          document.Delete( allBoxEnvelope ) ;

          //1. Wall Envelope Feature
          ExcuteWallEnvelope( commandData ) ;
          //2. Room Box Feature
          ExcuteRoomBoxes( commandData ) ;
          //3. Floor Envelope Feature
          ExcuteFloorEnvelope( commandData ) ;
          transaction.Commit() ;
        }

        return Result.Succeeded ;
      }
      catch ( Exception ex ) {
        MessageBox.Show( ex.Message ) ;
        return Result.Cancelled ;
      }
    }

    private void ExcuteFloorEnvelope( ExternalCommandData commandData )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      ElementCategoryFilter filter = new(BuiltInCategory.OST_Floors) ;
      var linkedDocumentFilter = GetLinkedDocFilter( document, commandData.Application.Application ) ;
      var collectorCurrent = new FilteredElementCollector( document ) ;
      var allFloors = new List<Floor>() ;

      if ( linkedDocumentFilter is not null ) {
        var floorsLink = linkedDocumentFilter.WherePasses( filter ).WhereElementIsNotElementType().ToElements().OfType<Floor>().ToList() ;
        allFloors.AddRange( floorsLink ) ;
      }

      var floorsCurrent = collectorCurrent.WherePasses( filter ).WhereElementIsNotElementType().ToElements().OfType<Floor>().ToList() ;
      allFloors.AddRange( floorsCurrent ) ;

      var option = new Options() ;
      option.DetailLevel = ViewDetailLevel.Fine ;
      var listComplex = new List<Floor>() ;

      var level = uiDocument.ActiveView.GenLevel ;
      var levels = document.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ).OrderBy( l => l.Elevation ).ToList() ;
      level ??= levels.First() ;

      foreach ( var floorInstance in allFloors ) {
        if ( floorInstance.LookupParameter( "専用庭キー_低木" ).AsValueString() == "専用庭以外" || floorInstance.LookupParameter( "専用庭キー_低木" ).AsValueString() == "専用庭" ) {
          continue ;
        }
        //shape complex with vertices edited
        if ( floorInstance.SlabShapeEditor.SlabShapeVertices.Size > 0 ) {
          listComplex.Add( floorInstance ) ;
          continue ;
        }
        //shape complex 
        var geoElement = floorInstance.get_Geometry( option ) ;
        foreach ( GeometryObject geoObject in geoElement ) {
          if ( geoObject is not Solid solid ) continue ;
          var btnFace = GetBottomFace( solid ) ;
          if ( btnFace is null ) continue ;
          var edgeLoops = btnFace.EdgeLoops ;
          //check shape with more than 2 boundaries
          if ( edgeLoops.Size >= 2 ) {
            listComplex.Add( floorInstance ) ;
          }
          else {
            //check shape with arc boundaries
            var edgeArray = edgeLoops.get_Item( 0 ) ;
            var isComplex = false ;
            var curves = new List<Line>() ;
            foreach ( Edge edge in edgeArray ) {
              if ( edge.AsCurve() is Line line ) curves.Add( line ) ;
              if ( edge.AsCurve() is not Arc ) continue ;
              listComplex.Add( floorInstance ) ;
              isComplex = true ;
              break ;
            }

            //basic shape
            if ( isComplex ) continue ;
            try {
              //Get all boundary segments
              var height = floorInstance.get_Parameter( BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM ).AsDouble() ;
              var bb = floorInstance.get_BoundingBox( null ) ;
              var lenghtMaxY = bb.Max.Y - bb.Min.Y ;

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
              for ( var i = 0 ; i < dic.Count - 1 ; i++ ) {
                var l1 = dic[ dic.Keys.ToList()[ i ] ] ;
                var l2 = dic[ dic.Keys.ToList()[ i + 1 ] ] ;
                l1.AddRange( l2 ) ;
                var rec = GeoExtension.FindRectangular( l1, GeoExtension.GetAllXLine( curvesFix ), lenghtMaxY * 2 ) ;
                listRec.AddRange( rec ) ;
              }
              //Create the room box
              foreach ( var rectangular in listRec ) {
                var envelopeOrigin = rectangular.GetCentroid() ;
                var envelopeSymbol = document.GetFamilySymbols( RoutingFamilyType.Envelope ).FirstOrDefault() ?? throw new InvalidOperationException() ;
                var envelopeInstance = envelopeSymbol.Instantiate( envelopeOrigin, level, StructuralType.NonStructural ) ;
                envelopeInstance.LookupParameter( "Arent-Offset" ).Set( 0.0 ) ;
                envelopeInstance.LookupParameter( "奥行き" ).Set( rectangular.GetLengthY() ) ;
                envelopeInstance.LookupParameter( "幅" ).Set( rectangular.GetLengthX() ) ;
                envelopeInstance.LookupParameter( "高さ" ).Set( height ) ;
                envelopeInstance.get_Parameter( BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS ).Set( "FLOOR_ENVELOPE" ) ;
              }
            }
            catch {
              //ignore
            }
          }
        }
      }
      if ( listComplex.Count == 0 ) return ;
      MessageBox.Show( $"このドキュメントには、{listComplex.Count}つの複雑な床の形状があります。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning ) ;
    }

    private PlanarFace? GetBottomFace( Solid solid )
    {
      PlanarFace? bottomFace = null ;
      var faces = solid.Faces ;
      foreach ( Face f in faces ) {
        var pf = f as PlanarFace ;
        if ( null == pf || ! pf.FaceNormal.IsAlmostEqualTo( -XYZ.BasisZ ) ) continue ;
        if ( ( null == bottomFace ) || ( bottomFace.Origin.Z > pf.Origin.Z ) ) {
          bottomFace = pf ;
        }
      }

      return bottomFace ;
    }

    private void ExcuteWallEnvelope( ExternalCommandData commandData )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      // Get all of the wall instances
      ElementCategoryFilter filter = new(BuiltInCategory.OST_Walls) ;
      var collectorLink = GetLinkedDocFilter( document, commandData.Application.Application ) ;
      var collectorCurrent = new FilteredElementCollector( document ) ;
      var allWalls = new List<Wall>() ;
      if ( collectorLink is not null ) {
        var wallsLink = collectorLink.WherePasses( filter ).WhereElementIsNotElementType().ToElements().OfType<Wall>().ToList() ;
        allWalls.AddRange( wallsLink ) ;
      }

      var wallsCurrent = collectorCurrent.WherePasses( filter ).WhereElementIsNotElementType().ToElements().OfType<Wall>().ToList() ;
      allWalls.AddRange( wallsCurrent ) ;

      var level = uiDocument.ActiveView.GenLevel ;
      var levels = document.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ).OrderBy( l => l.Elevation ).ToList() ;
      level ??= levels.First() ;

      foreach ( var wallInstance in allWalls ) {
        var height = wallInstance.get_Parameter( BuiltInParameter.WALL_USER_HEIGHT_PARAM ).AsDouble() ;
        var width = wallInstance.WallType.Width ;
        var backSize = wallInstance.get_Parameter( BuiltInParameter.CURVE_ELEM_LENGTH ).AsDouble() ;
        var locationCurve = ( wallInstance.Location as LocationCurve ) ! ;
        var wallLine = locationCurve.Curve as Line ;
        var wallArc = locationCurve.Curve as Arc ;

        XYZ startPoint = new(), endPoint = new() ;
        if ( wallLine != null ) {
          startPoint = wallLine.GetEndPoint( 0 ) ;
          endPoint = wallLine.GetEndPoint( 1 ) ;
        }
        else if ( wallArc != null ) {
          startPoint = wallArc.GetEndPoint( 0 ) ;
          endPoint = wallArc.GetEndPoint( 1 ) ;
        }

        var envelopeOrigin = new XYZ( ( startPoint.X + endPoint.X ) / 2, ( startPoint.Y + endPoint.Y ) / 2, ( startPoint.Z + endPoint.Z ) / 2 ) ;
        var envelopeSymbol = document.GetFamilySymbols( RoutingFamilyType.Envelope ).FirstOrDefault() ?? throw new InvalidOperationException() ;
        var envelopeInstance = envelopeSymbol.Instantiate( envelopeOrigin, level, StructuralType.NonStructural ) ;
        envelopeInstance.LookupParameter( "Arent-Offset" ).Set( 0.0 ) ;
        envelopeInstance.LookupParameter( "奥行き" ).Set( backSize ) ;
        envelopeInstance.LookupParameter( "幅" ).Set( width ) ;
        envelopeInstance.LookupParameter( "高さ" ).Set( height ) ;
        envelopeInstance.get_Parameter( BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS ).Set( "WALL_ENVELOPE" ) ;

        if ( wallLine != null ) {
          var rotationAngle = wallLine.Direction.AngleTo( XYZ.BasisY ) ;
          ElementTransformUtils.RotateElement( document, envelopeInstance.Id, Line.CreateBound( envelopeOrigin, new XYZ( envelopeOrigin.X, envelopeOrigin.Y, envelopeOrigin.Z + 1 ) ), rotationAngle ) ;
        }
      }
    }

    private void ExcuteRoomBoxes( ExternalCommandData commandData )
    {
      var uiApp = commandData.Application ;
      var uiDoc = uiApp.ActiveUIDocument ;
      var doc = uiDoc.Document ;

      var linkedDocumentFilter = GetLinkedDocFilter( doc, uiApp.Application ) ;
      var currentDocumentFilter = new FilteredElementCollector( doc ) ;

      var allRooms = GetAllRoomInCurrentAndLinkedDocument( linkedDocumentFilter, currentDocumentFilter ) ;
      var livingRooms = allRooms.Where( r => r.Name.Contains( "LDR" ) || r.Name.Contains( "LDK" ) ).ToList() ;
      var otherRooms = allRooms.Except( livingRooms ).ToList() ;

      CreateRoomBoxFromLivingRoom( livingRooms, doc ) ;
      CreateRoomBoxFromOther( otherRooms, doc ) ;
    }

    private FilteredElementCollector? GetLinkedDocFilter( Document doc, Application app )
    {
      var firstLinkDoc = new FilteredElementCollector( doc ).OfCategory( BuiltInCategory.OST_RvtLinks ).ToElements().First() ;
      foreach ( Document linkedDoc in app.Documents ) {
        if ( linkedDoc.Title.Equals( firstLinkDoc.Name.Replace( ".rvt", "" ) ) )
          return new FilteredElementCollector( linkedDoc ) ;
      }

      return null ;
    }

    public IList<Room> GetAllRoomInCurrentAndLinkedDocument( FilteredElementCollector? filterLinked, FilteredElementCollector? filterCurrent )
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

    public void CreateRoomBoxFromOther( List<Room> list, Document document )
    {
      if ( list.Count == 0 ) return ;
      foreach ( var room in list ) {
        var bb = room.get_BoundingBox( document.ActiveView ) ;
        if ( bb is null ) continue ;
        var min = bb.Min ;
        var max = bb.Max ;
        var height = ( max.Z - min.Z ) ;
        CreateBoxGenericModelInPlace( min, max, height, document, room.Name ) ;
      }
    }

    public void CreateRoomBoxFromLivingRoom( List<Room> list, Document document )
    {
      if ( list.Count == 0 ) return ;
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
            var index = listRec.IndexOf( rectangular ) ;
            CreateBoxGenericModelInPlace( rectangular.GetMin(), rectangular.GetMax(), height, document, $"{room.Name}_{index}" ) ;
          }
        }
        catch {
          //ignore
        }
      }
    }

    public static ElementId CreateBoxGenericModelInPlace( XYZ min, XYZ max, double height, Document doc, string name )
    {
      try {
        var line1 = Line.CreateBound( min, new XYZ( max.X, min.Y, min.Z ) ) ;
        var line2 = Line.CreateBound( new XYZ( max.X, min.Y, min.Z ), new XYZ( max.X, max.Y, min.Z ) ) ;
        var line3 = Line.CreateBound( new XYZ( max.X, max.Y, min.Z ), new XYZ( min.X, max.Y, min.Z ) ) ;
        var line4 = Line.CreateBound( new XYZ( min.X, max.Y, min.Z ), min ) ;

        var profile = new List<Curve>() { line1, line2, line3, line4 } ;
        var curveLoop = CurveLoop.Create( profile ) ;
        var profileLoops = new List<CurveLoop>() { curveLoop } ;
        var solid = GeometryCreationUtilities.CreateExtrusionGeometry( profileLoops, XYZ.BasisZ, height ) ;

        var ds = DirectShape.CreateElement( doc, new ElementId( BuiltInCategory.OST_GenericModel ) ) ;
        ds.SetShape( new GeometryObject[] { solid } ) ;
        ds.get_Parameter( BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS ).Set( "ROOM_BOX_" + name ) ;
        return ds.Id ;
      }
      catch ( Exception ex ) {
        var message = ex.Message ;
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
      return list.OrderBy( p => p.X ).Where( ( x, i ) => i is 2 or 3 ).OrderBy( p => p.Y ).Last() ;
    }

    public XYZ GetCentroid()
    {
      var (x, y, z) = GetMin() ;
      var (x1, y1, z1) = GetMax() ;
      return new XYZ( ( x + x1 ) / 2, ( y + y1 ) / 2, ( z + z1 ) / 2 ) ;
    }

    public double GetLengthX()
    {
      var min = GetMin() ;
      var max = GetMax() ;
      return Math.Abs( max.X - min.X ) ;
    }

    public double GetLengthY()
    {
      var min = GetMin() ;
      var max = GetMax() ;
      return Math.Abs( max.Y - min.Y ) ;
    }
  }
}