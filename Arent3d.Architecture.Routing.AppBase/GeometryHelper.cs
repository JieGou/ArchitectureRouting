﻿using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;

namespace Arent3d.Architecture.Routing.AppBase
{
  public static class GeometryHelper
  {
    public const double Tolerance = 0.0001 ;

    public static IList<Curve> GetCurvesAfterIntersection( ViewPlan viewPlan, IList<Curve> bodyDirections, List<Type>? excludingTypes = null, List<Element>? excludingElements = null)
    {
      excludingElements ??= new List<Element>() ; 
      var elevation = viewPlan.GenLevel.Elevation ;
      var type2Ds = new List<Type> { typeof( Wire ), typeof( TextNote ) } ;
      if(null != excludingTypes)
        type2Ds = type2Ds.Except( excludingTypes ).ToList() ;
      var classFilter2Ds = new ElementMulticlassFilter( type2Ds ) ;
      var type3Ds = new List<Type> { typeof( CableTray ), typeof( Conduit ), typeof( FamilyInstance ) } ;
      if(null != excludingTypes)
        type3Ds = type3Ds.Except( excludingTypes ).ToList() ;
      var classFilter3Ds = new ElementMulticlassFilter( type3Ds ) ;
      var outlineCurve = GetOutlineFromCurve( viewPlan, bodyDirections.Select( x => x.Tessellate() ).SelectMany( x => x ).ToList() ) ;
      var boxFilter = new BoundingBoxIntersectsFilter( outlineCurve ) ;

      var element2Ds = new FilteredElementCollector( viewPlan.Document, viewPlan.Id ).WherePasses( classFilter2Ds ).ToElements().Where( x => ! excludingElements.Exists( excludedElement => excludedElement.Id == x.Id ) ) ;
      var element3Ds = new FilteredElementCollector( viewPlan.Document, viewPlan.Id ).WherePasses( new LogicalAndFilter( classFilter3Ds, boxFilter ) ).ToElements().Where( x => x is FamilyInstance familyInstance && ( familyInstance.MEPModel?.ConnectorManager?.Connectors?.Size ?? 0 ) > 0 || true ).Where( x => ! excludingElements.Exists( excludedElement => excludedElement.Id == x.Id ) ) ;
      
      var viewDirection = viewPlan.ViewDirection ;
      var curveIntersects = new List<Curve>( bodyDirections ) ;

      foreach ( var element2D in element2Ds ) {
        var boxElement2D = element2D.get_BoundingBox( viewPlan ) ;
        var outlineElement2D = new Outline( new XYZ( boxElement2D.Min.X, boxElement2D.Min.Y, elevation ), new XYZ( boxElement2D.Max.X, boxElement2D.Max.Y, elevation ) ) ;
        if ( ! outlineCurve.Intersects( outlineElement2D, 0d ) ) continue ;

        CurveLoop? curveLoopOrigin = null ;
        CurveLoop? curveLoopOffset = null ;
        Line? locationElement = null ;
        switch ( element2D ) {
          case TextNote textNote :
            var outline = GetOutlineTextNote( textNote ) ;
            curveLoopOffset = CurveLoop.CreateViaTransform( outline, Transform.CreateTranslation( viewDirection * ( elevation - textNote.Coord.Z ) ) ) ;
            break ;
          case Wire wire :
            if ( wire.Location is not LocationCurve { Curve: Line line } )
              continue ;

            if ( line.Length <= viewPlan.Document.Application.ShortCurveTolerance )
              continue ;

            locationElement = line.Clone() as Line ;
            curveLoopOffset = CurveLoop.CreateViaThicken( locationElement, 400d.MillimetersToRevitUnits(), viewDirection ) ;
            break ;
        }

        if ( null != curveLoopOffset && curveIntersects.Any() )
          curveIntersects = GetCurvesIntersectSolid( viewPlan, curveLoopOrigin, curveLoopOffset, locationElement, curveIntersects ) ;
      }

      foreach ( var element3D in element3Ds ) {
        CurveLoop? curveLoopOrigin = null ;
        CurveLoop? curveLoopOffset = null ;
        Line? locationElement = null ;

        if ( element3D is FamilyInstance familyInstance ) {
          curveLoopOrigin = GetBoundaryBoundingBox( familyInstance.get_BoundingBox( null ), elevation ) ;
          curveLoopOffset = CurveLoop.CreateViaOffset( curveLoopOrigin, 100d.MillimetersToRevitUnits(), viewDirection ) ;
        }
        else {
          if ( element3D.Location is not LocationCurve { Curve: Line line } )
            continue ;

          if ( line.Length <= viewPlan.Document.Application.ShortCurveTolerance )
            continue ;

          var endPointOne = new XYZ( line.GetEndPoint( 0 ).X, line.GetEndPoint( 0 ).Y, elevation ) ;
          var endPointTwo = new XYZ( line.GetEndPoint( 1 ).X, line.GetEndPoint( 1 ).Y, elevation ) ;
          if ( endPointOne.DistanceTo( endPointTwo ) <= viewPlan.Document.Application.ShortCurveTolerance )
            continue ;
          locationElement = Line.CreateBound( endPointOne, endPointTwo ) ;

          switch ( element3D ) {
            case CableTray cableTray :
              curveLoopOffset = CurveLoop.CreateViaThicken( locationElement, 100d.MillimetersToRevitUnits() + cableTray.Width, viewDirection ) ;
              break ;
            case Conduit conduit :
            {
              var outSizePara = conduit.get_Parameter( BuiltInParameter.RBS_CONDUIT_OUTER_DIAM_PARAM ) ;
              if ( null == outSizePara )
                continue ;
              curveLoopOffset = CurveLoop.CreateViaThicken( locationElement, 100d.MillimetersToRevitUnits() + outSizePara.AsDouble(), viewDirection ) ;
              break ;
            }
          }
        }

        if ( null != curveLoopOffset && curveIntersects.Any() )
          curveIntersects = GetCurvesIntersectSolid( viewPlan, curveLoopOrigin, curveLoopOffset, locationElement, curveIntersects ) ;
      }

      return curveIntersects ;
    }

    public static Solid GetSolidExecutionOfTextNotes( Document document, TextNote textNote1, TextNote textNote2, BooleanOperationsType booleanOperationsType )
    {
      var outline1 = GetOutlineTextNote( textNote1 ) ;
      var outline2 = GetOutlineTextNote( textNote2 ) ;
      var solidOrigin1 = CreateSolid( document.ActiveView, outline1 ) ;
      var solidOrigin2 = CreateSolid( document.ActiveView, outline2 ) ;
      return BooleanOperationsUtils.ExecuteBooleanOperation( solidOrigin1, solidOrigin2,
        booleanOperationsType ) ;
    }
    
    private static Outline GetOutlineFromCurve( View viewPlan, IList<XYZ> points )
    {
      var rangeExtend = 3000d.MillimetersToRevitUnits() ;
      var minZ = viewPlan.GenLevel.Elevation - rangeExtend ;
      var maxZ = viewPlan.GenLevel.Elevation + rangeExtend ;

      var levels = viewPlan.Document.GetAllElements<Level>().OrderBy( x => x.Elevation ).EnumerateAll() ;
      if ( levels.Any() ) {
        minZ = levels.First().Elevation - rangeExtend ;
        maxZ = levels.Last().Elevation + rangeExtend ;
      }

      var minPoint = new XYZ( points.MinBy( x => x.X )!.X, points.MinBy( x => x.Y )!.Y, minZ ) ;
      var maxPoint = new XYZ( points.MaxBy( x => x.X )!.X, points.MaxBy( x => x.Y )!.Y, maxZ ) ;
      return new Outline( minPoint, maxPoint ) ;
    }
    
    private static List<Curve> GetCurvesIntersectSolid( View viewPlan, CurveLoop? curveLoopOrigin, CurveLoop curveLoopOffset, Curve? locationElement, List<Curve> curves )
    {
      var curvesResult = new List<Curve>() ;
      try {
        if ( null != curveLoopOrigin ) {
          var solidOrigin = CreateSolid( viewPlan, curveLoopOrigin ) ;
          var curvesTemp = GetCurvesIntersectSolid( solidOrigin, curves, viewPlan.Document.Application.ShortCurveTolerance ) ;
          if ( Math.Abs( curves.Select( x => x.Length ).Sum() - curvesTemp.Select( x => x.Length ).Sum() ) < Tolerance )
            return curves ;
        }

        if ( null != locationElement ) {
          var pointOne = new XYZ( locationElement.GetEndPoint( 0 ).X, locationElement.GetEndPoint( 0 ).Y, viewPlan.GenLevel.Elevation ) ;
          var pointTwo = new XYZ( locationElement.GetEndPoint( 1 ).X, locationElement.GetEndPoint( 1 ).Y, viewPlan.GenLevel.Elevation ) ;
          if ( pointOne.DistanceTo( pointTwo ) < Tolerance )
            return curves ;

          var newLocationElement = Line.CreateBound( pointOne, pointTwo ) ;
          if ( ! IsCurveIntersectCurves( newLocationElement, curves ) )
            return curves ;
        }

        var solidScale = CreateSolid( viewPlan, curveLoopOffset ) ;
        curvesResult = GetCurvesIntersectSolid( solidScale, curves, viewPlan.Document.Application.ShortCurveTolerance ) ;
      }
      catch {
        // ignore
      }

      return curvesResult ;
    }
    
    private static List<Curve> GetCurvesIntersectSolid( Solid? solid, IList<Curve> curves, double shortCurveTolerance )
    {
      var curveResult = new List<Curve>() ;
      if ( null == solid || ! curves.Any() )
        return curveResult ;

      var option = new SolidCurveIntersectionOptions { ResultType = SolidCurveIntersectionMode.CurveSegmentsOutside } ;
      foreach ( var curve in curves ) {
        var result = solid.IntersectWithCurve( curve, option ) ;
        if ( null == result ) continue ;

        for ( var i = 0 ; i < result.SegmentCount ; i++ ) {
          if ( result.GetCurveSegment( i ).Length > shortCurveTolerance ) {
            curveResult.Add( result.GetCurveSegment( i ) ) ;
          }
        }
      }

      return curveResult ;
    }

    private static Solid CreateSolid( View viewPlan, CurveLoop curveLoop )
    {
      var tolerance = 100d.MillimetersToRevitUnits() ;
      curveLoop = CurveLoop.CreateViaTransform( curveLoop, Transform.CreateTranslation( viewPlan.ViewDirection.Negate() * tolerance ) ) ;
      var solid = GeometryCreationUtilities.CreateExtrusionGeometry( new List<CurveLoop> { curveLoop }, viewPlan.ViewDirection, 2 * tolerance ) ;
      return solid ;
    }

    public static (DetailCurve? DetailCurve, int? EndPoint) GetCurveClosestPoint( IList<DetailCurve>? detailCurves, XYZ point )
    {
      if ( ! detailCurves?.Any() ?? true )
        return ( null, null ) ;

      var lists = new List<(DetailCurve DetailCurve, (double Distance, int EndPoint) Point)>() ;

      foreach ( var detailCurve in detailCurves! ) {
        var distanceOne = detailCurve.GeometryCurve.GetEndPoint( 0 ).DistanceTo( point ) ;
        var distanceTwo = detailCurve.GeometryCurve.GetEndPoint( 1 ).DistanceTo( point ) ;

        lists.Add( distanceOne < distanceTwo ? ( detailCurve, ( distanceOne, 0 ) ) : ( detailCurve, ( distanceTwo, 1 ) ) ) ;
      }

      var min = lists.MinBy( x => x.Point.Distance ) ;
      return ( min.DetailCurve, min.Point.EndPoint ) ;
    }

    public static Line CreateUnderLineText( TextNote textNote, XYZ basePoint )
    {
      var height = ( textNote.Height + textNote.TextNoteType.get_Parameter( BuiltInParameter.LEADER_OFFSET_SHEET ).AsDouble() ) * textNote.Document.ActiveView.Scale ;
      var coord = Transform.CreateTranslation( textNote.UpDirection.Negate() * height ).OfPoint( textNote.Coord ) ;
      var width = ( textNote.HorizontalAlignment == HorizontalTextAlignment.Right ? -1 : 1 ) * textNote.Width * textNote.Document.ActiveView.Scale / 2 ;
      var middle = Transform.CreateTranslation( textNote.BaseDirection * width ).OfPoint( coord ) ;

      return Line.CreateBound( new XYZ( coord.X, coord.Y, basePoint.Z ), new XYZ( middle.X, middle.Y, basePoint.Z ) ) ;
    }

    public static CurveLoop GetOutlineTextNote( TextNote textNote, double? viewScale = null )
    {
      var scale = viewScale ?? textNote.Document.ActiveView.Scale ;
      var offset = textNote.TextNoteType.get_Parameter( BuiltInParameter.LEADER_OFFSET_SHEET ).AsDouble() ;
      var height = ( textNote.Height + 2 * offset ) * scale ;
      var width = ( textNote.HorizontalAlignment == HorizontalTextAlignment.Right ? -1 : 1 ) * ( textNote.Width + 2 * offset ) * scale ;

      var transformHeight = Transform.CreateTranslation( textNote.UpDirection.Negate() * height ) ;
      var transformWidth = Transform.CreateTranslation( textNote.BaseDirection * width ) ;
      var transformCoord = Transform.CreateTranslation( textNote.UpDirection.Add( textNote.HorizontalAlignment == HorizontalTextAlignment.Right ? textNote.BaseDirection : textNote.BaseDirection.Negate() ) * offset * scale ) ;

      var curveLoop = new CurveLoop() ;
      var p1 = transformCoord.OfPoint( textNote.Coord ) ;
      var p2 = transformWidth.OfPoint( p1 ) ;
      var p3 = transformHeight.OfPoint( p2 ) ;
      var p4 = transformHeight.OfPoint( p1 ) ;

      curveLoop.Append( Line.CreateBound( p1, p2 ) ) ;
      curveLoop.Append( Line.CreateBound( p2, p3 ) ) ;
      curveLoop.Append( Line.CreateBound( p3, p4 ) ) ;
      curveLoop.Append( Line.CreateBound( p4, p1 ) ) ;

      return curveLoop ;
    }

    public static CurveLoop GetBoundaryBoundingBox( BoundingBoxXYZ boundingBox, double elevation )
    {
      var curveLoop = new CurveLoop() ;

      var p1 = new XYZ( boundingBox.Min.X, boundingBox.Min.Y, elevation ) ;
      var p2 = new XYZ( boundingBox.Max.X, boundingBox.Min.Y, elevation ) ;
      var p3 = new XYZ( boundingBox.Max.X, boundingBox.Max.Y, elevation ) ;
      var p4 = new XYZ( boundingBox.Min.X, boundingBox.Max.Y, elevation ) ;

      curveLoop.Append( Line.CreateBound( p1, p2 ) ) ;
      curveLoop.Append( Line.CreateBound( p2, p3 ) ) ;
      curveLoop.Append( Line.CreateBound( p3, p4 ) ) ;
      curveLoop.Append( Line.CreateBound( p4, p1 ) ) ;

      return curveLoop ;
    }

    public static bool IsCurveIntersectCurves( Curve hostCurve, List<Curve> curves )
    {
      if ( ! curves.Any() )
        return false ;

      foreach ( var curve in curves ) {
        hostCurve.Intersect( curve, out var resultArray ) ;
        if ( null != resultArray && resultArray.Cast<IntersectionResult>().Any( result => null != result.XYZPoint ) ) {
          return true ;
        }
      }

      return false ;
    }

    public static Line? GetMaxLengthLine( Line firstLine, Line secondLine )
    {
      var lines = new List<Line> { firstLine, secondLine } ;
      if(firstLine.GetEndPoint(0).DistanceTo(secondLine.GetEndPoint(0)) > Tolerance)
        lines.Add(Line.CreateBound(firstLine.GetEndPoint(0), secondLine.GetEndPoint(0)));
      
      if(firstLine.GetEndPoint(0).DistanceTo(secondLine.GetEndPoint(1)) > Tolerance)
        lines.Add(Line.CreateBound(firstLine.GetEndPoint(0), secondLine.GetEndPoint(1)));
      
      if(firstLine.GetEndPoint(1).DistanceTo(secondLine.GetEndPoint(0)) > Tolerance)
        lines.Add(Line.CreateBound(firstLine.GetEndPoint(1), secondLine.GetEndPoint(0)));
      
      if(firstLine.GetEndPoint(1).DistanceTo(secondLine.GetEndPoint(1)) > Tolerance)
        lines.Add(Line.CreateBound(firstLine.GetEndPoint(1), secondLine.GetEndPoint(1)));

      return lines.MaxBy( x => x.Length ) ;
    }
    public static Dictionary<Curve, string> GetCurveFromElements( View view, IEnumerable<Element> elements )
    {
      var curves = new Dictionary<Curve, string>() ;

      foreach ( var element in elements ) {
        if ( element is FamilyInstance familyInstance ) {
          var options = new Options { DetailLevel = ViewDetailLevel.Coarse, IncludeNonVisibleObjects = true } ;
          if ( familyInstance.get_Geometry( options ) is { } geometryElement )
            RecursiveCurves( geometryElement, element.UniqueId, ref curves ) ;
        }
        else {
          var options = new Options { View = view } ;
          if ( element.get_Geometry( options ) is { } geometryElement )
            RecursiveCurves( geometryElement, element.UniqueId, ref curves ) ;
        }
      }

      return curves ;
    }

    private static void RecursiveCurves( GeometryElement geometryElement, string elementId, ref Dictionary<Curve, string> curves, bool includingSolidEdges = false )
    {
      foreach ( var geometry in geometryElement ) {
        switch ( geometry ) {
          case GeometryInstance geometryInstance :
          {
            if ( geometryInstance.GetInstanceGeometry() is { } subGeometryElement )
              RecursiveCurves( subGeometryElement, elementId, ref curves, includingSolidEdges ) ;
            break ;
          }
          case Curve curve :
            curves.Add( curve, elementId ) ;
            break ;
          case Solid solid :
            if ( ! includingSolidEdges )
              break ;
            var edges = GetSolidEdgesAsCurves( solid ) ;
            curves.AddRange( edges.Select( edge => new KeyValuePair<Curve, string>( edge, elementId ) ) ) ;
            break ;
        }
      }
    }

    private static IEnumerable<Curve> GetSolidEdgesAsCurves( Solid solid )
    {
      foreach ( var ed in solid.Edges ) {
        if ( ed is not Edge edge )
          continue ;
        yield return edge.AsCurve() ;
      }
    }

    public static IEnumerable<Curve> GetVisibleLinesInView( this FamilyInstance threeDObject, View viewPlan, bool includingSolidEdge )
    {
      var geometryElement = threeDObject.get_Geometry( new Options { ComputeReferences = true, IncludeNonVisibleObjects = false, View = viewPlan } ) ;

      var curveToIdDictionary = new Dictionary<Curve, string>() ;
      if ( geometryElement is { } )
        RecursiveCurves( geometryElement, "", ref curveToIdDictionary , includingSolidEdge ) ;
      return curveToIdDictionary.Select( item => item.Key ) ;
    }

    public static IEnumerable<GeometryObject> GetGeometryObjectsFromElementInstance( Element element, Options options, bool isInstance = true )
    {
      var geometryObjects = new List<GeometryObject>() ;
      if ( element is Wall wall && wall.IsStackedWall ) {
        IList<ElementId> elementIds = wall.GetStackedWallMemberIds() ;
        foreach ( var elementId in elementIds ) {
          var memberWallStack = element.Document.GetElement( elementId ) ;
          if ( memberWallStack.get_Geometry( options ) is { } geometryElement )
            RecursiveGeometryObjects( geometryElement, geometryObjects, isInstance ) ;
        }
      }
      else {
        if ( element.get_Geometry( options ) is { } geometryElement )
          RecursiveGeometryObjects( geometryElement, geometryObjects, isInstance ) ;
      }

      return geometryObjects ;
    }

    private static void RecursiveGeometryObjects( GeometryElement geometryElement, List<GeometryObject> geometryObjects, bool isInstance = true )
    {
      foreach ( var geometry in geometryElement ) {
        if ( geometry is GeometryInstance geometryInstance ) {
          if ( isInstance && geometryInstance.GetInstanceGeometry() is { } nestedGeometryInstance) {
            RecursiveGeometryObjects( nestedGeometryInstance, geometryObjects, isInstance ) ;
          }
          else if (geometryInstance.GetSymbolGeometry() is { } nestedGeometrySymbol) {
            RecursiveGeometryObjects( nestedGeometrySymbol, geometryObjects, isInstance ) ;
          }
        }
        else if ( geometry is { } geometryObject ) {
          geometryObjects.Add( geometryObject ) ;
        }
      }
    }
  }
}