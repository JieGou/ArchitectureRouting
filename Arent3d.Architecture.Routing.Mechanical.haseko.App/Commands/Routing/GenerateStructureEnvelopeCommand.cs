using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using MathLib ;
using Line = Autodesk.Revit.DB.Line ;

namespace Arent3d.Architecture.Routing.Mechanical.Haseko.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.Haseko.App.Commands.Routing.GenerateStructureEnvelopeCommand", DefaultString = "Generate\nStructure Envelope" )]
  [Image( "resources/structure_envelope.png" )]
  public class GenerateStructureEnvelopeCommand : IExternalCommand
  {
    private const string EnvelopeParameter = "Obstacle Name" ;
    private const string StructureValueParam = "STRUCTURE_ENVELOPE" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      try {
        using var transaction = new Transaction( document ) ;
        transaction.Start( "TransactionName.Commands.Routing.GenerateStructureEnvelopeCommand".GetAppStringByKeyOrDefault( "Generate Structure Envelope" ) ) ;
        
        var envelopeSymbol = document.GetFamilySymbols( RoutingFamilyType.Envelope ).Single() ;
        FamilyInstanceFilter filter = new(document, envelopeSymbol.Id) ;
        var collector = new FilteredElementCollector( document ) ;
        var allBoxEnvelope = collector.WherePasses( filter ).WhereElementIsNotElementType().Where( r => r.LookupParameter( EnvelopeParameter ).AsString() == StructureValueParam ) ;
        document.Delete( allBoxEnvelope.Select( x => x.Id ).ToList() ) ;

        ExecuteWallEnvelope( envelopeSymbol ) ;
        ExecuteFloorEnvelope( envelopeSymbol ) ;
        ExecuteColumnEnvelope( envelopeSymbol ) ;
        ExecuteBeamEnvelope( envelopeSymbol ) ;

        transaction.Commit() ;
        return Result.Succeeded ;
      }
      catch ( Exception ex ) {
        MessageBox.Show( ex.Message ) ;
        return Result.Cancelled ;
      }
    }

    private void ExecuteFloorEnvelope( FamilySymbol envelopeSymbol )
    {
      var document = envelopeSymbol.Document ;
      var allFloors = ObstacleGeneration.GetAllElementInCurrentAndLinkDocument<Floor>( document, BuiltInCategory.OST_Floors ) ;

      const string floorParamFilter = "専用庭キー_低木" ;
      const string floorValueFilter1 = "専用庭以外" ;
      const string floorValueFilter2 = "専用庭" ;
      const string floorComment = "FLOOR_ENVELOPE" ;

      var option = new Options() ;
      var listComplex = new List<Floor>() ;
      foreach ( var floorInstance in allFloors ) {
        if ( floorInstance.LookupParameter( floorParamFilter ).AsValueString() == floorValueFilter1 || floorInstance.LookupParameter( floorParamFilter ).AsValueString() == floorValueFilter2 ) {
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
              if ( edge.AsCurve() is Line line ) {
                curves.Add( line ) ;
              }
              else {
                listComplex.Add( floorInstance ) ;
                isComplex = true ;
                break ;
              }
            }

            //basic shape
            if ( isComplex ) continue ;
            try {
              //Get all boundary segments
              var height = floorInstance.get_Parameter( BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM ).AsDouble() ;
              var bb = floorInstance.get_BoundingBox( null ) ;
              var lenghtMaxY = bb.Max.Y - bb.Min.Y ;

              var curvesFix = GeometryUtil.FixDiagonalLines( curves, lenghtMaxY * 2 ) ;

              //Joined unnecessary segments 
              var listJoinedX = GeometryUtil.GetAllXLine( curvesFix ).GroupBy( x => Math.Round( x.Origin.Y, 4 ) ).Select( d => GeometryUtil.JoinListStraitLines( d.ToList() ) ).ToList() ;
              var listJoinedY = GeometryUtil.GetAllYLine( curvesFix ).GroupBy( x => Math.Round( x.Origin.X, 4 ) ).Select( d => GeometryUtil.JoinListStraitLines( d.ToList() ) ).ToList() ;

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
                .Select( x => x.ToList() )
                .ToArray() ;

              //Find out the rectangular
              var listRecBox = new List<Box3d>() ;
              for ( var i = 0 ; i < arrayGroupByX.Length - 1 ; i++ ) {
                var l1 = arrayGroupByX[ i ] ;
                var l2 = arrayGroupByX[ i + 1 ] ;
                l1.AddRange( l2 ) ;
                var recBox = GeometryUtil.FindRectangularBox( l1, GeometryUtil.GetAllXLine( curvesFix ), lenghtMaxY * 2, height ) ;
                listRecBox.AddRange( recBox ) ;
              }

              //Create the floor envelope
              foreach ( var recBox in listRecBox ) {
                var envelopeOrigin = recBox.Center.ToXYZRaw() ;
                CreateEnvelopeElement( envelopeSymbol, envelopeOrigin, recBox.YWidth, recBox.XWidth, height, floorComment ) ;
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

    private void ExecuteWallEnvelope( FamilySymbol envelopeSymbol )
    {
      var document = envelopeSymbol.Document ;
      var allWalls = ObstacleGeneration.GetAllElementInCurrentAndLinkDocument<Wall>( document, BuiltInCategory.OST_Walls ) ;
      const string wallComment = "WALL_ENVELOPE" ;

      foreach ( var wallInstance in allWalls ) {
        var height = wallInstance.get_Parameter( BuiltInParameter.WALL_USER_HEIGHT_PARAM ).AsDouble() ;
        var width = wallInstance.WallType.Width ;
        var length = wallInstance.get_Parameter( BuiltInParameter.CURVE_ELEM_LENGTH ).AsDouble() ;
        var locationCurve = ( wallInstance.Location as LocationCurve ) ! ;

        var wallCurve = locationCurve.Curve ;
        if ( wallCurve is not Line && wallCurve is not Arc ) continue ;

        var startPoint = wallCurve.GetEndPoint( 0 ) ;
        var endPoint = wallCurve.GetEndPoint( 1 ) ;
        var envelopeOrigin = new XYZ( ( startPoint.X + endPoint.X ) / 2, ( startPoint.Y + endPoint.Y ) / 2, ( startPoint.Z + endPoint.Z ) / 2 ) ;

        var envelopeInstance = CreateEnvelopeElement( envelopeSymbol, envelopeOrigin, length, width, height, wallComment ) ;
        var rotationAngle = Line.CreateBound( startPoint, endPoint ).Direction.AngleTo( XYZ.BasisY ) ;
        ElementTransformUtils.RotateElement( document, envelopeInstance.Id, Line.CreateBound( envelopeOrigin, new XYZ( envelopeOrigin.X, envelopeOrigin.Y, envelopeOrigin.Z + 1 ) ), rotationAngle ) ;
      }
    }

    private void ExecuteColumnEnvelope( FamilySymbol envelopeSymbol )
    {
      var document = envelopeSymbol.Document ;
      var allColumns = ObstacleGeneration.GetAllElementInCurrentAndLinkDocument<FamilyInstance>( document, BuiltInCategory.OST_Columns ).ToList() ;
      allColumns.AddRange( ObstacleGeneration.GetAllElementInCurrentAndLinkDocument<FamilyInstance>( document, BuiltInCategory.OST_StructuralColumns ) ) ;
      const string columnComment = "COLUMN_ENVELOPE" ;

      var option = new Options() ;
      allColumns.ForEach( col => CreateEnvelopeFromInstance( col, envelopeSymbol, option, columnComment ) ) ;
    }

    private void ExecuteBeamEnvelope( FamilySymbol envelopeSymbol )
    {
      var document = envelopeSymbol.Document ;
      var allBeams = ObstacleGeneration.GetAllElementInCurrentAndLinkDocument<FamilyInstance>( document, BuiltInCategory.OST_StructuralFraming ) ;
      const string beamComment = "BEAM_ENVELOPE" ;

      var option = new Options() ;
      var filterBeams = allBeams.Where( b => b.FilterBeamWithXyDirection() ).Where( b => b.FilterBeamUniqueSolid( option ) ) ;
      filterBeams.ForEach( beam => CreateEnvelopeFromInstance( beam, envelopeSymbol, option, beamComment ) ) ;
    }

    private void CreateEnvelopeFromInstance( Element instance, FamilySymbol envelopeSymbol, Options option, string comment )
    {
      var geometryElement = instance.get_Geometry( option ) ;
      foreach ( var geoObject in geometryElement ) {
        if ( geoObject is not Solid { Volume: > 0 } solid ) continue ;
        var bb = solid.GetBoundingBox() ;
        var (ox, oy, oz) = bb.Transform.Origin ;
        var (x, y, z) = bb.Min ;
        var (x1, y1, z1) = bb.Max ;

        var (width, length, height) = ( x1 - x, y1 - y, z1 - z ) ;
        var location = new XYZ( ox, oy, oz - z1 ) ;

        CreateEnvelopeElement( envelopeSymbol, location, length, width, height, comment ) ;
      }
    }

    private FamilyInstance CreateEnvelopeElement( FamilySymbol familySymbol, XYZ location, double length, double width, double height, string comment )
    {
      const string offsetParam = "Arent-Offset" ;
      const string lengthParam = "奥行き" ;
      const string widthParam = "幅" ;
      const string heightParam = "高さ" ;
      var envelopeInstance = familySymbol.Instantiate( location, StructuralType.NonStructural ) ;
      envelopeInstance.LookupParameter( offsetParam ).Set( 0.0 ) ;
      envelopeInstance.LookupParameter( lengthParam ).Set( length ) ;
      envelopeInstance.LookupParameter( widthParam ).Set( width ) ;
      envelopeInstance.LookupParameter( heightParam ).Set( height ) ;
      envelopeInstance.get_Parameter( BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS ).Set( comment ) ;
      envelopeInstance.LookupParameter( EnvelopeParameter ).Set( StructureValueParam ) ;
      return envelopeInstance ;
    }

    private PlanarFace? GetBottomFace( Solid solid )
    {
      PlanarFace? bottomFace = null ;
      var faces = solid.Faces ;

      foreach ( Face face in faces ) {
        var planarFace = face as PlanarFace ;

        if ( null == planarFace || ! planarFace.FaceNormal.IsAlmostEqualTo( -XYZ.BasisZ ) ) continue ;
        if ( ( null == bottomFace ) || ( bottomFace.Origin.Z > planarFace.Origin.Z ) ) {
          bottomFace = planarFace ;
        }
      }

      return bottomFace ;
    }
  }
}