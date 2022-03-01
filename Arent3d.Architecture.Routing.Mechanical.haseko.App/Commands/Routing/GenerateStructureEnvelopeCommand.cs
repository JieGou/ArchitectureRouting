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
using MathLib ;
using Application = Autodesk.Revit.ApplicationServices.Application ;
using Line = Autodesk.Revit.DB.Line ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.haseko.App.Commands.Routing.GenerateStructureEnvelopeCommand", DefaultString = "Generate\nStructure Envelope" )]
  [Image( "resources/room_boxes.png" )]
  public class GenerateStructureEnvelopeCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      try {
        using var transaction = new Transaction( document ) ;
        transaction.Start( "Create structure envelope" ) ;
        //Delete all
        var elementId = document.GetFamilySymbols( RoutingFamilyType.Envelope ).FirstOrDefault()?.Id ?? throw new InvalidOperationException() ;
        FamilyInstanceFilter filter = new(document, elementId) ;
        var collector = new FilteredElementCollector( document ) ;
        var allBoxEnvelope = collector.WherePasses( filter ).WhereElementIsNotElementType().ToElementIds() ;
        document.Delete( allBoxEnvelope ) ;

        //1. Wall Envelope Feature
        ExecuteWallEnvelope( commandData ) ;
        //2. Floor Envelope Feature
        ExecuteFloorEnvelope( commandData ) ;
        //3. Column Envelope
        ExecuteColumnEnvelope( commandData ) ;
        //4. Beam Envelope
        ExecuteBeamEnvelope( commandData ) ;
        transaction.Commit() ;

        return Result.Succeeded ;
      }
      catch ( Exception ex ) {
        MessageBox.Show( ex.Message ) ;
        return Result.Cancelled ;
      }
    }

    private void ExecuteFloorEnvelope( ExternalCommandData commandData )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      ElementCategoryFilter filter = new(BuiltInCategory.OST_Floors) ;
      var linkedDocumentFilter = ObstacleGeneration.GetLinkedDocFilter( document, commandData.Application.Application ) ;
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
              var listRecBox = new List<Box3d>() ;
              for ( var i = 0 ; i < dic.Count - 1 ; i++ ) {
                var l1 = dic[ dic.Keys.ToList()[ i ] ] ;
                var l2 = dic[ dic.Keys.ToList()[ i + 1 ] ] ;
                l1.AddRange( l2 ) ;
                var recBox = GeoExtension.FindRectangularBox( l1, GeoExtension.GetAllXLine( curvesFix ), lenghtMaxY * 2, height ) ;
                listRecBox.AddRange( recBox ) ;
              }

              //Create the room box
              foreach ( var recBox in listRecBox ) {
                var envelopeOrigin = recBox.Center.ToXYZRaw() ;
                var envelopeSymbol = document.GetFamilySymbols( RoutingFamilyType.Envelope ).FirstOrDefault() ?? throw new InvalidOperationException() ;
                var envelopeInstance = envelopeSymbol.Instantiate( envelopeOrigin, StructuralType.NonStructural ) ;
                envelopeInstance.LookupParameter( "Arent-Offset" ).Set( 0.0 ) ;
                envelopeInstance.LookupParameter( "奥行き" ).Set( recBox.YWidth ) ;
                envelopeInstance.LookupParameter( "幅" ).Set( recBox.XWidth ) ;
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

    private void ExecuteWallEnvelope( ExternalCommandData commandData )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      // Get all of the wall instances
      ElementCategoryFilter filter = new(BuiltInCategory.OST_Walls) ;
      var collectorLink = ObstacleGeneration.GetLinkedDocFilter( document, commandData.Application.Application ) ;
      var collectorCurrent = new FilteredElementCollector( document ) ;
      var allWalls = new List<Wall>() ;
      if ( collectorLink is not null ) {
        var wallsLink = collectorLink.WherePasses( filter ).WhereElementIsNotElementType().ToElements().OfType<Wall>().ToList() ;
        allWalls.AddRange( wallsLink ) ;
      }

      var wallsCurrent = collectorCurrent.WherePasses( filter ).WhereElementIsNotElementType().ToElements().OfType<Wall>().ToList() ;
      allWalls.AddRange( wallsCurrent ) ;

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
        var envelopeInstance = envelopeSymbol.Instantiate( envelopeOrigin, StructuralType.NonStructural ) ;
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

    private void ExecuteColumnEnvelope( ExternalCommandData commandData )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      // Get all of the column instances
      ElementCategoryFilter filter = new(BuiltInCategory.OST_Columns) ;
      var collectorLink = ObstacleGeneration.GetLinkedDocFilter( document, commandData.Application.Application ) ;
      var collectorCurrent = new FilteredElementCollector( document ) ;
      var allColumns = new List<FamilyInstance>() ;
      if ( collectorLink is not null ) {
        var colLink = collectorLink.WherePasses( filter ).WhereElementIsNotElementType().ToElements().OfType<FamilyInstance>().ToList() ;
        allColumns.AddRange( colLink ) ;
      }

      var colCurrent = collectorCurrent.WherePasses( filter ).WhereElementIsNotElementType().ToElements().OfType<FamilyInstance>().ToList() ;
      allColumns.AddRange( colCurrent ) ;

      var option = new Options() ;
      option.DetailLevel = ViewDetailLevel.Fine ;
      foreach ( var colInstance in allColumns ) {
        var geometryElement = colInstance.get_Geometry( option ) ;
        foreach ( var geoObject in geometryElement ) {
          if ( geoObject is not Solid { Volume: > 0 } solid ) continue ;
          var bb = solid.GetBoundingBox() ;
          var (ox, oy, oz) = bb.Transform.Origin ;
          var (x, y, z) = bb.Min ;
          var (x1, y1, z1) = bb.Max ;

          var (width, length, height) = ( x1 - x, y1 - y, z1 - z ) ;
          var location = new XYZ( ox, oy, oz - z1 ) ;

          var envelopeSymbol = document.GetFamilySymbols( RoutingFamilyType.Envelope ).FirstOrDefault() ?? throw new InvalidOperationException() ;
          var envelopeInstance = envelopeSymbol.Instantiate( location, StructuralType.NonStructural ) ;
          envelopeInstance.LookupParameter( "Arent-Offset" ).Set( 0.0 ) ;
          envelopeInstance.LookupParameter( "奥行き" ).Set( length ) ;
          envelopeInstance.LookupParameter( "幅" ).Set( width ) ;
          envelopeInstance.LookupParameter( "高さ" ).Set( height ) ;
          envelopeInstance.get_Parameter( BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS ).Set( "COLUMN_ENVELOPE" ) ;
        }
      }
    }

    private void ExecuteBeamEnvelope( ExternalCommandData commandData )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      // Get all of the beam instances
      ElementCategoryFilter filter = new(BuiltInCategory.OST_StructuralFraming) ;
      var collectorLink = ObstacleGeneration.GetLinkedDocFilter( document, commandData.Application.Application ) ;
      var collectorCurrent = new FilteredElementCollector( document ) ;
      var allBeams = new List<FamilyInstance>() ;
      if ( collectorLink is not null ) {
        var beamLink = collectorLink.WherePasses( filter ).WhereElementIsNotElementType().ToElements().OfType<FamilyInstance>().ToList() ;
        allBeams.AddRange( beamLink ) ;
      }

      var beamCurrent = collectorCurrent.WherePasses( filter ).WhereElementIsNotElementType().ToElements().OfType<FamilyInstance>().ToList() ;
      allBeams.AddRange( beamCurrent ) ;

      //Filter beam not XY direction and Other Framing with multi layers
      var option = new Options() ;
      option.DetailLevel = ViewDetailLevel.Fine ;
      var filterBeams = allBeams.Where( b => b.FilterBeamWithXyDirection() ).Where( b => b.FilterBeamUniqueSolid( option ) ).ToList() ;

      foreach ( var bmInstance in filterBeams ) {
        var geometryElement = bmInstance.get_Geometry( option ) ;
        foreach ( var geoObject in geometryElement ) {
          if ( geoObject is not Solid { Volume: > 0 } solid ) continue ;
          var bb = solid.GetBoundingBox() ;
          var (ox, oy, oz) = bb.Transform.Origin ;
          var (x, y, z) = bb.Min ;
          var (x1, y1, z1) = bb.Max ;

          var (width, length, height) = ( x1 - x, y1 - y, z1 - z ) ;
          var location = new XYZ( ox, oy, oz - z1 ) ;

          var envelopeSymbol = document.GetFamilySymbols( RoutingFamilyType.Envelope ).FirstOrDefault() ?? throw new InvalidOperationException() ;
          var envelopeInstance = envelopeSymbol.Instantiate( location, StructuralType.NonStructural ) ;
          envelopeInstance.LookupParameter( "Arent-Offset" ).Set( 0.0 ) ;
          envelopeInstance.LookupParameter( "奥行き" ).Set( length ) ;
          envelopeInstance.LookupParameter( "幅" ).Set( width ) ;
          envelopeInstance.LookupParameter( "高さ" ).Set( height ) ;
          envelopeInstance.get_Parameter( BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS ).Set( "BEAM_ENVELOPE" ) ;
        }
      }

      uiDocument.Selection.SetElementIds( filterBeams.Select( x => x.Id ).ToList() ) ;
    }
  }
}