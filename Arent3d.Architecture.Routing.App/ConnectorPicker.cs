using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.App.Forms ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using Arent3d.Revit.UI ;

namespace Arent3d.Architecture.Routing.App
{
  public static class ConnectorPicker
  {
    public interface IPickResult
    {
      IEnumerable<ElementId> GetAllRelatedElements() ;
      SubRoute? SubRoute { get ; }
      Element PickedElement { get ; }
      Connector? PickedConnector { get ; }
      XYZ GetOrigin() ;
      bool IsCompatibleTo( Connector connector ) ;
      bool IsCompatibleTo( Element element ) ;
    }
    
    public static IPickResult GetConnector( UIDocument uiDocument, string message, IPickResult? firstPick )
    {
      var document = uiDocument.Document ;

      var filter = ( null == firstPick ) ? FamilyInstanceWithConnectorFilter.Instance : new FamilyInstanceCompatibleToTargetConnectorFilter( firstPick ) ;

      while ( true ) {
        var pickedObject = uiDocument.Selection.PickObject( ObjectType.Element, filter, message ) ;

        var element = document.GetElement( pickedObject.ElementId ) ;
        if ( null == element ) continue ;

        if ( PassPointPickResult.Create( element ) is { } ppResult ) return ppResult ;
        if ( SubRoutePickResult.Create( element ) is { } srResult ) return srResult ;

        var (result, connector) = FindConnector( uiDocument, element, message, firstPick?.PickedConnector ) ;
        if ( false == result ) continue ;

        if ( null != connector ) {
          return new ConnectorPickResult( element, connector ) ;
        }

        return new OriginPickResult( element ) ;
      }
    }
    
    #region PickResults

    private class ConnectorPickResult : IPickResult
    {
      private readonly Element _element ;
      private readonly Connector _connector ;
      private readonly string? _routeName ;

      public SubRoute? SubRoute => null ;
      public Element PickedElement => _element ;
      public Connector? PickedConnector => _connector ;

      public XYZ GetOrigin() => _connector.Origin ;

      public ConnectorPickResult( Element element, Connector connector )
      {
        _element = element ;
        _connector = connector ;
        _routeName = element.GetRouteName() ;
      }

      public IEnumerable<ElementId> GetAllRelatedElements()
      {
        yield return _element.Id ;
      }

      public bool IsCompatibleTo( Connector connector ) => _connector.IsCompatibleTo( connector ) ;
      public bool IsCompatibleTo( Element element ) => null == _routeName || _routeName != element.GetRouteName() ;
    }

    private class SubRoutePickResult : IPickResult
    {
      private readonly Element _pickedElement ;
      private readonly SubRoute _subRoute ;

      public SubRoute? SubRoute => _subRoute ;
      public Element PickedElement => _pickedElement ;
      public Connector? PickedConnector => null ;

      public XYZ GetOrigin() => GetCenter( _pickedElement ) ;
      
      private SubRoutePickResult( Element element, SubRoute subRoute )
      {
        _pickedElement = element ;
        _subRoute = subRoute ;
      }

      public IEnumerable<ElementId> GetAllRelatedElements()
      {
        return _pickedElement.Document.GetAllElementsOfSubRoute<Element>( _subRoute.Route.RouteName, _subRoute.SubRouteIndex ).Select( e => e.Id ) ;
      }

      public static IPickResult? Create( Element element )
      {
        var routeName = element.GetRouteName() ;
        if ( null == routeName ) return null ;

        var subRouteIndex = element.GetSubRouteIndex() ;
        if ( null == subRouteIndex ) return null ;

        if ( false ==  RouteCache.Get( element.Document ) .TryGetValue( routeName, out var route ) ) return null ;
        if ( route.GetSubRoute( subRouteIndex.Value ) is not { } subRoute ) return null ;

        return new SubRoutePickResult( element, subRoute ) ;
      }

      public bool IsCompatibleTo( Connector connector ) => _subRoute.GetReferenceConnector().IsCompatibleTo( connector ) ;
      public bool IsCompatibleTo( Element element ) => _subRoute.Route.RouteName != element.GetRouteName() ;
    }

    private class PassPointPickResult : IPickResult
    {
      private readonly Element _element ;
      private readonly Route? _route ;

      public SubRoute? SubRoute => null ;
      public Element PickedElement => _element ;
      public Connector? PickedConnector => null ;

      public XYZ GetOrigin() => GetCenter( _element ) ;

      private PassPointPickResult( Element element )
      {
        _element = element ;

        if ( element.GetRouteName() is { } routeName ) {
          RouteCache.Get( element.Document ).TryGetValue( routeName, out _route ) ;
        }
      }

      public IEnumerable<ElementId> GetAllRelatedElements()
      {
        return _element.Document.GetAllElementsOfPassPoint( _element.GetPassPointId() ?? _element.Id.IntegerValue ).Select( e => e.Id ) ;
      }

      public static IPickResult? Create( Element element )
      {
        if ( false == element.IsPassPoint() ) return null ;

        if ( element.GetPassPointId() is { } i && i != element.Id.IntegerValue ) {
          element = element.Document.GetElement( new ElementId( i ) ) ;
          if ( null == element || false == element.IsPassPoint() ) return null ;
        }

        return new PassPointPickResult( element ) ;
      }

      public bool IsCompatibleTo( Connector connector ) => null == _route || _route.GetReferenceConnector().IsCompatibleTo( connector ) ;
      public bool IsCompatibleTo( Element element ) => null == _route || _route.RouteName != element.GetRouteName() ;
    }

    private class OriginPickResult : IPickResult
    {
      private readonly Element _element ;

      public SubRoute? SubRoute => null ;
      public Element PickedElement => _element ;
      public Connector? PickedConnector => null ;

      public XYZ GetOrigin() => GetCenter( _element ) ;

      public OriginPickResult( Element element )
      {
        _element = element ;
      }

      public IEnumerable<ElementId> GetAllRelatedElements()
      {
        yield return _element.Id ;
      }

      public bool IsCompatibleTo( Connector connector ) => true ;

      public bool IsCompatibleTo( Element element ) => element.GetConnectors().Any( IsPickTargetConnector ) ;
    }

    private static XYZ GetCenter( Element element )
    {
      return element switch
      {
        MEPCurve curve => GetCenter( curve ),
        Instance instance => instance.GetTotalTransform().Origin,
        _ => throw new System.InvalidOperationException(),
      } ;
    }

    private static XYZ GetCenter( MEPCurve curve )
    {
      double minX = +double.MaxValue, minY = -double.MaxValue, minZ = +double.MaxValue ;
      double maxX = -double.MaxValue, maxY = +double.MaxValue, maxZ = -double.MaxValue ;

      foreach ( var c in curve.GetConnectors().Where( c => c.IsAnyEnd() ) ) {
        var (x, y, z) = c.Origin ;

        if ( x < minX ) minX = x ;
        if ( maxX < x ) maxX = x ;
        if ( y < minY ) minY = y ;
        if ( maxY < y ) maxY = y ;
        if ( z < minZ ) minZ = z ;
        if ( maxZ < z ) maxZ = z ;
      }

      return new XYZ( ( minX + maxX ) * 0.5, ( minY + maxY ) * 0.5, ( minZ + maxZ ) * 0.5 ) ;
    }

    #endregion

    private static (bool Result, Connector? Connector) FindConnector( UIDocument uiDocument, Element element, string message, Connector? firstConnector )
    {
      if ( element.IsAutoRoutingGeneratedElement() ) {
        return GetEndOfRouting( element, ( null == firstConnector ) ) ;
      }
      else {
        return SelectFromDialog( uiDocument, element, message, firstConnector ) ;
      }
    }

    private static (bool Result, Connector? Connector) GetEndOfRouting( Element element, bool fromConnector )
    {
      var routeName = element.GetRouteName() ;
      if ( null == routeName ) return ( false, null ) ;

      var connector = element.Document.CollectRoutingEndPointConnectors( routeName, fromConnector ).FirstOrDefault() ;
      return ( ( null != connector ), connector ) ;
    }

    private static (bool Result, Connector? Connector) SelectFromDialog( UIDocument uiDocument, Element element, string message, Connector? firstConnector )
    {
      uiDocument.SetSelection( element ) ;

      var sv = new SelectConnector( element, firstConnector ) { Title = message } ;
      sv.ShowDialog() ;

      uiDocument.ClearSelection() ;
      uiDocument.GetActiveUIView()?.ZoomToFit() ;

      if ( true != sv.DialogResult ) return ( false, null ) ;

      return ( true, sv.GetSelectedConnector() ) ;
    }

    private static bool IsPickTargetConnector( Connector connector )
    {
      return connector.IsAnyEnd() && connector.Domain switch
      {
        Domain.DomainPiping => true,
        Domain.DomainHvac => true,
        Domain.DomainCableTrayConduit => true,
        _ => false
      } ;
    }

    private class FamilyInstanceWithConnectorFilter : ISelectionFilter
    {
      public static ISelectionFilter Instance { get ; } = new FamilyInstanceWithConnectorFilter() ;

      public bool AllowElement( Element elem )
      {
        return IsRoutableForConnector( elem ) || IsRoutableForCenter( elem ) ;
      }

      private bool IsRoutableForConnector( Element elem )
      {
        return elem.GetConnectors().Any( IsTargetConnector ) && IsRoutableElement( elem ) ;
      }

      private bool IsRoutableForCenter( Element elem )
      {
        return ( elem is FamilyInstance fi ) && ( false == fi.IsPassPoint() ) ;
      }

      protected virtual bool IsRoutableElement( Element elem )
      {
        return elem switch
        {
          MEPCurve => true,
          FamilyInstance fi => IsEquipment( fi ) || elem.IsAutoRoutingGeneratedElement(),
          _ => false,
        } ;
      }

      private static bool IsEquipment( FamilyInstance fi )
      {
        if ( fi.IsFittingElement() ) return false ;
        if ( fi.IsPassPoint() ) return false ;
        return true ;
      }

      protected virtual bool IsTargetConnector( Connector connector )
      {
        return IsPickTargetConnector( connector ) ;
      }

      public bool AllowReference( Reference reference, XYZ position )
      {
        return false ;
      }
    }

    private class FamilyInstanceCompatibleToTargetConnectorFilter : FamilyInstanceWithConnectorFilter
    {
      private readonly IPickResult _compatibleResult ;

      public FamilyInstanceCompatibleToTargetConnectorFilter( IPickResult compatibleResult )
      {
        _compatibleResult = compatibleResult ;
      }

      protected override bool IsTargetConnector( Connector connector )
      {
        return base.IsTargetConnector( connector ) && _compatibleResult.IsCompatibleTo( connector ) ;
      }

      protected override bool IsRoutableElement( Element elem )
      {
        return base.IsRoutableElement( elem ) && _compatibleResult.IsCompatibleTo( elem ) ;
      }
    }
  }
}