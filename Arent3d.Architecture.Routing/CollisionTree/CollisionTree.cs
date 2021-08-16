using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Reflection ;
using Arent3d.Revit ;
using Arent3d.Routing ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using MathLib ;
using Line = Autodesk.Revit.DB.Line ;

namespace Arent3d.Architecture.Routing.CollisionTree
{
  public class CollisionTree : ICollisionCheck
  {
    private readonly Document _document ;
    private readonly ICollisionCheckTargetCollector _collector ;
    private readonly IReadOnlyCollection<ElementFilter> _filters ;
    private readonly BuiltInCategory[] _categoriesOnRack ;
    private readonly ElementParameterFilter _hasRouteNameFilter ;
    private readonly IReadOnlyDictionary<(string RouteName, int SubRouteIndex), MEPSystemRouteCondition> _routeConditions ;

    public CollisionTree( Document document, ICollisionCheckTargetCollector collector, IReadOnlyDictionary<(string RouteName, int SubRouteIndex), MEPSystemRouteCondition> routeConditions )
    {
      _document = document ;
      _collector = collector ;
      _filters = collector.CreateElementFilters().EnumerateAll() ;
      _categoriesOnRack = collector.GetCategoriesOfRoutes() ;
      _routeConditions = routeConditions ;

      _hasRouteNameFilter = CreateHasRouteNameFilter( document ) ;
    }

    private static ElementParameterFilter CreateHasRouteNameFilter( Document document )
    {
      var parameter = GetSharedParameterElement( document, RoutingParameter.RouteName ) ?? throw new InvalidOperationException() ;
      var parameterValueProvider = new ParameterValueProvider( parameter.Id ) ;
#if REVIT2022
      return new ElementParameterFilter( new FilterStringRule( parameterValueProvider, new FilterStringEquals(), "" ), true ) ;
#else
      return new ElementParameterFilter( new FilterStringRule( parameterValueProvider, new FilterStringEquals(), "", true ), true ) ;
#endif
    }

    private static SharedParameterElement? GetSharedParameterElement<TPropertyEnum>( Document document, TPropertyEnum propertyEnum ) where TPropertyEnum : Enum
    {
      var fieldInfo = typeof( TPropertyEnum ).GetField( propertyEnum.ToString() ) ;
      var attr = fieldInfo?.GetCustomAttribute<ParameterGuidAttribute>() ;
      if ( null == attr ) return null ;

      return SharedParameterElement.Lookup( document, attr.Guid ) ;
    }

    public IEnumerable<Box3d> GetCollidedBoxes( Box3d box )
    {
      var min = box.Min.ToXYZRaw() ;
      var max = box.Max.ToXYZRaw() ;
      var boxFilter = new BoundingBoxIntersectsFilter( new Outline( min, max ) ) ;
      var geometryFilter = new ElementIntersectsSolidFilter( CreateBoundingBoxSolid( min, max ) ) ;
      var elements = _document.GetAllElements<Element>().Where( boxFilter ) ;
      return _filters.Aggregate( elements, ( current, filter ) => current.Where( filter ) ).Where( geometryFilter ).Where( _collector.IsCollisionCheckElement ).Select( GetBoundingBox ) ;
    }

    private static Solid CreateBoundingBoxSolid( XYZ min, XYZ max )
    {
      return GeometryCreationUtilities.CreateExtrusionGeometry( new[] { CreateBaseCurveLoop( min, max ) }, XYZ.BasisZ, max.Z - min.Z ) ;

      static CurveLoop CreateBaseCurveLoop( XYZ min, XYZ max )
      {
        var p1 = min ;
        var p2 = new XYZ( max.X, min.Y, min.Z ) ;
        var p3 = new XYZ( max.X, max.Y, min.Z ) ;
        var p4 = new XYZ( min.X, max.Y, min.Z ) ;
        return CurveLoop.Create( new Curve[] { Line.CreateBound( p1, p2 ), Line.CreateBound( p2, p3 ), Line.CreateBound( p3, p4 ), Line.CreateBound( p4, p1 ) } ) ;
      }
    }

    private static Box3d GetBoundingBox( Element element )
    {
      return element.get_BoundingBox( null ).To3dRaw() ;
    }

    public IEnumerable<(Box3d, IRouteCondition, bool)> GetCollidedBoxesAndConditions( Box3d box, bool bIgnoreStructure = false )
    {
      var boxFilter = new BoundingBoxIntersectsFilter( new Outline( box.Min.ToXYZRaw(), box.Max.ToXYZRaw() ) ) ;
      var elements = _document.GetAllElements<Element>().OfCategory( _categoriesOnRack ).Where( boxFilter ).Where( _hasRouteNameFilter ) ;
      foreach ( var element in elements ) {
        if ( element.GetRouteName() is not { } routeName ) continue ;
        if ( element.GetSubRouteIndex() is not { } subRouteIndex ) continue ;
        if ( false == _routeConditions.TryGetValue( ( routeName, subRouteIndex ), out var routeCondition ) ) continue ;

        yield return ( GetBoundingBox( element ), routeCondition, false ) ;
      }
    }

    public IEnumerable<(Box3d, IRouteCondition, bool)> GetCollidedBoxesInDetailToRack( Box3d box ) => Enumerable.Empty<(Box3d, IRouteCondition, bool)>() ;

    public IEnumerable<(ElementId ElementId, MeshTriangle Triangle)> GetTriangles()
    {
      var elements = _document.GetAllElements<Element>() ;
      var filteredElements = _filters.Aggregate( elements, ( current, filter ) => current.Where( filter ) ).Where( _collector.IsCollisionCheckElement ) ;
      foreach ( var element in filteredElements.Where( elm => false == elm.IsAutoRoutingGeneratedElement() ) ) {
        var elementId = element.Id ;
        foreach ( var face in element.GetFaces() ) {
          var mesh = face.Triangulate() ;
          for ( int i = 0, n = mesh.NumTriangles ; i < n ; ++i ) {
            yield return ( elementId, mesh.get_Triangle( i ) ) ;
          }
        }
      }
    }
  }

  internal static class SolidExtensions
  {
    public static IEnumerable<Face> GetFaces( this Element element ) => element.GetFineSolids().SelectMany( solid => solid.Faces.OfType<Face>() ) ;

    private static IEnumerable<Solid> GetFineSolids( this Element element ) => element.GetSolids( new Options { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false, IncludeNonVisibleObjects = false } ) ;

    private static IEnumerable<Solid> GetSolids( this Element element, Options options )
    {
      if ( element.get_Geometry( options ) is not { } geom ) return Enumerable.Empty<Solid>() ;
      return geom.GetSolids() ;
    }

    private static IEnumerable<Solid> GetSolids( this GeometryElement geometry )
    {
      var solids = geometry.OfType<Solid>().Where( solid => false == solid.Faces.IsEmpty ).ToList() ;
      if ( 0 < solids.Count ) return solids ;

      var instanceGeometryElements = geometry.OfType<GeometryInstance>().Select( geom => geom.GetInstanceGeometry() ) ;
      return instanceGeometryElements.SelectMany( GetSolids ) ;
    }
  }
}