using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Reflection ;
using Arent3d.Revit ;
using Arent3d.Routing ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing.CollisionTree
{
  public class CollisionTree : ICollisionCheck
  {
    private readonly Document _document ;
    private readonly ICollisionCheckTargetCollector _collector ;
    private readonly IReadOnlyCollection<ElementFilter> _filters ;
    private readonly BuiltInCategory[] _categoriesOnRack ;
    private readonly ElementParameterFilter _hasRouteNameFilter ;
    private readonly IReadOnlyDictionary<(string RouteName, int SubRouteIndex),MEPSystemRouteCondition> _routeConditions ;

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
      return new ElementParameterFilter( new FilterStringRule( parameterValueProvider, new FilterStringEquals(), "", false ), true ) ;
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
      var boxFilter = new BoundingBoxIntersectsFilter( new Outline( box.Min.ToXYZRaw(), box.Max.ToXYZRaw() ) ) ;
      var elements = _document.GetAllElements<Element>().Where( boxFilter ) ;
      return _filters.Aggregate( elements, ( current, filter ) => current.Where( filter ) ).Where( _collector.IsCollisionCheckElement ).Select( GetBoundingBox ) ;
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
  }
}