using System.Linq ;
using Arent3d.Architecture.Routing.App.Forms ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using Arent3d.Revit.UI ;

namespace Arent3d.Architecture.Routing.App
{
  public static class ConnectorPicker
  {
    public static (Connector Connector, Element PickedElement) GetConnector( UIDocument uiDocument, string message, Connector? firstConnector = null, string? firstRouteId = null )
    {
      var document = uiDocument.Document ;

      var filter = ( null == firstConnector ) ? FamilyInstanceWithConnectorFilter.Instance : new FamilyInstanceCompatibleToTargetConnectorFilter( firstConnector, firstRouteId ) ;

      while ( true ) {
        var pickedObject = uiDocument.Selection.PickObject( ObjectType.Element, filter, message ) ;

        var element = document.GetElement( pickedObject.ElementId ) ;
        if ( null == element ) continue ;

        var connector = FindConnector( uiDocument, element, message, firstConnector ) ;
        if ( null == connector ) continue ;

        return ( Connector: connector, PickedElement: element ) ;
      }
    }

    private static Connector? FindConnector( UIDocument uiDocument, Element element, string message, Connector? firstConnector )
    {
      if ( element.IsAutoRoutingGeneratedElement() ) {
        return GetEndOfRouting( element, ( null == firstConnector ) ) ;
      }
      else {
        return SelectFromDialog( uiDocument, (FamilyInstance) element, message, firstConnector ) ;
      }
    }

    private static Connector? GetEndOfRouting( Element element, bool fromConnector )
    {
      var routeName = element.GetRouteName() ;
      if ( null == routeName ) return null ;

      return element.Document.CollectRoutingEndPointConnectors( routeName, fromConnector ).FirstOrDefault() ;
    }

    private static Connector? SelectFromDialog( UIDocument uiDocument, FamilyInstance familyInstance, string message, Connector? firstConnector )
    {
      uiDocument.SetSelection( familyInstance ) ;

      var sv = new SelectConnector( familyInstance, firstConnector ) { Title = message } ;
      sv.ShowDialog() ;

      uiDocument.ClearSelection() ;
      uiDocument.GetActiveUIView()?.ZoomToFit() ;

      if ( true != sv.DialogResult ) return null ;

      return sv.GetSelectedConnector() ;
    }

    private class FamilyInstanceWithConnectorFilter : ISelectionFilter
    {
      public static ISelectionFilter Instance { get ; } = new FamilyInstanceWithConnectorFilter() ;

      public bool AllowElement( Element elem )
      {
        if ( ! ( elem.GetConnectorManager()?.Connectors is { } connectorSet ) ) return false ;

        return connectorSet.OfType<Connector>().Any( IsTargetConnector ) && IsRoutableElement( elem ) ;
      }

      protected virtual bool IsRoutableElement( Element elem )
      {
        return elem switch
        {
          MEPCurve => elem.IsAutoRoutingGeneratedElement(),
          FamilyInstance => ! elem.IsFittingElement() || elem.IsAutoRoutingGeneratedElement(),
          _ => false,
        } ;
      }

      protected virtual bool IsTargetConnector( Connector connector )
      {
        return connector.IsAnyEnd() && connector.Domain switch
        {
          Domain.DomainPiping => true,
          Domain.DomainHvac => true,
          _ => false
        } ;
      }

      public bool AllowReference( Reference reference, XYZ position )
      {
        return false ;
      }
    }

    private class FamilyInstanceCompatibleToTargetConnectorFilter : FamilyInstanceWithConnectorFilter
    {
      private readonly Connector _connector ;
      private readonly string? _routeId ;

      public FamilyInstanceCompatibleToTargetConnectorFilter( Connector connector, string? routeId )
      {
        _connector = connector ;
        _routeId = routeId ;
      }

      protected override bool IsTargetConnector( Connector connector )
      {
        return base.IsTargetConnector( connector ) && _connector.IsCompatibleTo( connector ) ;
      }

      protected override bool IsRoutableElement( Element elem )
      {
        if ( false == base.IsRoutableElement( elem ) ) return false ;

        if ( null != _routeId ) {
          if ( elem.GetRouteName() == _routeId ) return false ;
        }

        return true ;
      }
    }
  }
}