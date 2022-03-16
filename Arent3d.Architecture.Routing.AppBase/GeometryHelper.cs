﻿using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;

namespace Arent3d.Architecture.Routing.AppBase
{
  public static class GeometryHelper
  {
    private static (IList<T>? elements, (double minHeight, double maxHeight)) IntersectElements<T>( this (XYZ, XYZ ) leader,
      Document document ) where T : Element
    {
      var (elbow, end) = leader ;

      var (minHeight, maxHeight) = GetHeightRange( document ) ;
      if ( minHeight.Equals( maxHeight ) )
        return ( null, ( minHeight, maxHeight ) ) ;

      var minOutline = new XYZ( Math.Min( elbow.X, end.X ), Math.Min( elbow.Y, end.Y ), minHeight ) ;
      var maxOutline = new XYZ( Math.Max( elbow.X, end.X ), Math.Max( elbow.Y, end.Y ), maxHeight ) ;

      var intersectFilter = new BoundingBoxIntersectsFilter( new Outline( minOutline, maxOutline ) ) ;
      var elements = new FilteredElementCollector( document, document.ActiveView.Id ).WherePasses( intersectFilter )
        .ToElements().OfType<T>() ;

      var elementFilters = new List<T>() ;
      foreach ( var element in elements ) {
        if ( element.Location is not LocationCurve { Curve: Line locationLine } ) 
          continue ;
        
        if ( Math.Abs( locationLine.Direction.DotProduct( document.ActiveView.ViewDirection ) ) < 0.0001 ) {
          elementFilters.Add( element ) ;
        }
      }

      return ( elementFilters, ( minHeight, maxHeight ) ) ;
    }

    public static List<Curve> IntersectCurveLeader( Document document, (XYZ, XYZ) leader )
    {
      var (elbow, end) = leader ;
      var curvesIntersected = new List<Curve>() { Line.CreateBound( elbow, end ) } ;
      var (conduits, (minHeight, maxHeight)) = leader.IntersectElements<Conduit>( document ) ;
      if ( (conduits?.Count ?? 0) == 0 )
        return curvesIntersected ;
      
      var solidOption = new SolidCurveIntersectionOptions()
      {
        ResultType = SolidCurveIntersectionMode.CurveSegmentsOutside
      } ;

      foreach ( var conduit in conduits! ) {
        if ( conduit.Location is LocationCurve locationCurve && locationCurve.Curve is Line locationLine ) {
          var (startPoint, endPoint) = ( locationLine.GetEndPoint( 0 ), locationLine.GetEndPoint( 1 ) ) ;
          var line = Line.CreateBound( new XYZ( startPoint.X, startPoint.Y, elbow.Z ),
            new XYZ( endPoint.X, endPoint.Y, elbow.Z ) ) ;

          var diameter = conduit.ParametersMap
            .get_Item( "Revit.Property.Builtin.OutsideDiameter".GetDocumentStringByKeyOrDefault( document,
              "Outside Diameter" ) ).AsDouble() ;
          var curveLoop = CurveLoop.CreateViaThicken( line.Clone(), 9 * diameter, document.ActiveView.ViewDirection ) ;
          var transform =
            Transform.CreateTranslation( document.ActiveView.ViewDirection.Negate() * ( elbow.Z - minHeight ) ) ;
          curveLoop = CurveLoop.CreateViaTransform( curveLoop, transform ) ;

          var solid = GeometryCreationUtilities.CreateExtrusionGeometry( new List<CurveLoop>() { curveLoop },
            document.ActiveView.ViewDirection, maxHeight - minHeight ) ;
          var curvesIntersectSolid = new List<Curve>() ;

          foreach ( var curveIntersected in curvesIntersected ) {
            var results = solid.IntersectWithCurve( curveIntersected, solidOption ) ;

            if ( null != results ) {
              results.ForEach( curve =>
              {
                if ( curve.Length > document.Application.ShortCurveTolerance ) {
                  curvesIntersectSolid.Add( curve ) ;
                }
              });
            }
          }

          curvesIntersected = curvesIntersectSolid ;
        }
      }

      return curvesIntersected ;
    }

    private static (double, double) GetHeightRange( Document document )
    {
      var levels = document.GetAllElements<Level>() ;
      if ( ! levels.Any() )
        return ( 0d, 0d ) ;

      var elevations = levels.Select( x => x.Elevation ).ToList() ;

      return ( elevations.Min(), elevations.Max() ) ;
    }

    public static (DetailCurve? detailCurve, int? endPoint) GetCurveClosestPoint( IList<DetailCurve>? detailCurves, XYZ point )
    {
      if ( ! detailCurves?.Any() ?? true )
        return (null, null) ;

      var lists = new List<(DetailCurve detailCurve, (double distance, int endPoint) point)>() ;

      foreach ( var detailCurve in detailCurves! ) {
        var dis1 = detailCurve.GeometryCurve.GetEndPoint( 0 ).DistanceTo( point ) ;
        var dis2 = detailCurve.GeometryCurve.GetEndPoint( 1 ).DistanceTo( point ) ;

        lists.Add( dis1 < dis2 ? ( detailCurve, ( dis1, 0 ) ) : ( detailCurve, ( dis2, 1 ) ) ) ;
      }

      var min = lists.MinBy( x => x.point.distance ) ;
      return (min.detailCurve, min.point.endPoint) ;
    }

    public static Line CreateUnderLineText( TextNote textNote, XYZ basePoint )
    {
      var height = (textNote.Height + 
                    textNote.TextNoteType.get_Parameter(BuiltInParameter.LEADER_OFFSET_SHEET).AsDouble()) * 
                   textNote.Document.ActiveView.Scale ;
      var coord = Transform.CreateTranslation( textNote.UpDirection.Negate() * height ).OfPoint( textNote.Coord ) ;
      var width = ( textNote.HorizontalAlignment == HorizontalTextAlignment.Right ? -1 : 1 ) * textNote.Width *
        textNote.Document.ActiveView.Scale / 2 ;
      var middle = Transform.CreateTranslation( textNote.BaseDirection * width ).OfPoint( coord ) ;

      return Line.CreateBound( new XYZ(coord.X, coord.Y, basePoint.Z), new XYZ(middle.X, middle.Y, basePoint.Z) ) ;
    }

    public static List<DetailCurve> CreateDetailCurve( View? view, List<Curve> curves )
    {
      var detailCurves = new List<DetailCurve>() ;
      if ( null == view )
        return detailCurves ;
      
      var graphicsStyle = view.Document.Settings.Categories.get_Item( BuiltInCategory.OST_CurvesMediumLines )
        .GetGraphicsStyle( GraphicsStyleType.Projection ) ;

      foreach ( var curve in curves ) {
        var detailCurve = view.Document.Create.NewDetailCurve( view, curve ) ;
        detailCurve.LineStyle = graphicsStyle ;
        detailCurves.Add( detailCurve ) ;
      }
      
      return  detailCurves;
    }
  }
}