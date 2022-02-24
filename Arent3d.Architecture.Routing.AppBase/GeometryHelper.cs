using System ;
using System.Collections ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase
{
  public static class GeometryHelper
  {
    private static (IEnumerable<T>?, (double, double)) IntersectElements<T>(this (XYZ, XYZ ) leader, Document document) where T : Element
    {
      var (elbow, end) = leader ;
      
      var (minHeight, maxHeight) = GetHeightRange( document ) ;
      if ( minHeight.Equals( maxHeight ) )
        return (null, (minHeight, maxHeight)) ;

      var minOutline = new XYZ( Math.Min( elbow.X, end.X ), Math.Min( elbow.Y, end.Y ), minHeight ) ;
      var maxOutline = new XYZ( Math.Max( elbow.X, end.X ), Math.Max( elbow.Y, end.Y ), maxHeight ) ;
      
      var intersectFilter = new BoundingBoxIntersectsFilter(new Outline(minOutline, maxOutline));
      var elements = new FilteredElementCollector( document, document.ActiveView.Id ).WherePasses( intersectFilter )
        .ToElements().OfType<T>() ;

      var elementFilters = new List<T>() ;
      foreach ( var element in elements ) {
        if ( element.Location is LocationCurve locationCurve && locationCurve.Curve is Line locationLine ) {
          if ( Math.Abs( locationLine.Direction.DotProduct( document.ActiveView.ViewDirection ) ) < 0.0001 ) {
            elementFilters.Add(element);
          }
        }
      }

      return (elementFilters, (minHeight, maxHeight)) ;
    }

    public static List<Curve> IntersectCurveLeader( Document document, (XYZ, XYZ) leader)
    {
      var (elbow, end) = leader ;
      var curvesIntersected = new List<Curve>() { Line.CreateBound(elbow, end) } ;
      var (conduits, (minHeight, maxHeight)) = leader.IntersectElements<Conduit>( document ) ; 
      if ( ! conduits?.Any() ?? true )
        return curvesIntersected;

      var locationIntersects = new List<XYZ>() ;
      var solidOption = new SolidCurveIntersectionOptions()
      {
        ResultType = SolidCurveIntersectionMode.CurveSegmentsOutside
      } ;

      foreach ( var conduit in conduits! ) {
        if ( conduit.Location is LocationCurve locationCurve && locationCurve.Curve is Line locationLine ) {

          var (startPoint, endPoint) = (locationLine.GetEndPoint( 0 ), locationLine.GetEndPoint( 1 ) ) ;
          var line = Line.CreateBound( new XYZ( startPoint.X, startPoint.Y, elbow.Z ),
            new XYZ( endPoint.X, endPoint.Y, elbow.Z ) ) ;
          
          var curveLoop = CurveLoop.CreateViaThicken( line.Clone(), conduit.Diameter * 5, document.ActiveView.ViewDirection ) ;
          var transform =
            Transform.CreateTranslation( document.ActiveView.ViewDirection.Negate() * ( elbow.Z - minHeight )) ;
          curveLoop = CurveLoop.CreateViaTransform(curveLoop, transform);
          
          var solid = GeometryCreationUtilities.CreateExtrusionGeometry( new List<CurveLoop>() { curveLoop },
            document.ActiveView.ViewDirection, maxHeight - minHeight ) ;
          var curvesIntersectSolid = new List<Curve>() ;

          foreach ( var curveIntersected in curvesIntersected ) {
            
            var results = solid.IntersectWithCurve( curveIntersected, solidOption ) ;
            
            if ( null != results ) {
              foreach ( var curve in results ) {
                if ( curve.Length > document.Application.ShortCurveTolerance ) {
                  curvesIntersectSolid.Add(curve);
                }
              }
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

      var elevations = levels.Select( x => x.Elevation) ;

      return ( elevations.Min(), elevations.Max() ) ;
    }
  }
}