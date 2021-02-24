using System.Linq ;
using Arent3d.Architecture.Routing.App.Forms ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using Arent3d.Revit.UI ;

namespace Arent3d.Architecture.Routing.App
{
  public static class ConnectorPicker
  {
    public static (Connector? Connector, Instance PickedElement) GetConnector( UIDocument uiDocument, string message, Connector? firstConnector = null, string? firstRouteId = null )
    {
      var document = uiDocument.Document ;

      var filter = ( null == firstConnector ) ? FamilyInstanceWithConnectorFilter.Instance : new FamilyInstanceCompatibleToTargetConnectorFilter( firstConnector, firstRouteId ) ;

      while ( true ) {
        var pickedObject = uiDocument.Selection.PickObject( ObjectType.Element, filter, message ) ;

        var element = document.GetElementById<Instance>( pickedObject.ElementId ) ;
        if ( null == element ) continue ;

        var (result, connector) = FindConnector( uiDocument, element, message, firstConnector ) ;
        if ( false == result ) continue ;

        return ( Connector: connector, PickedElement: element ) ;
      }
    }

    private static (bool Result, Connector? Connector) FindConnector( UIDocument uiDocument, Instance element, string message, Connector? firstConnector )
    {
      if ( element.IsAutoRoutingGeneratedElement() ) {
        return GetEndOfRouting( element, ( null == firstConnector ) ) ;
      }
      else {
        return SelectFromDialog( uiDocument, (FamilyInstance) element, message, firstConnector ) ;
      }
    }

    private static (bool Result, Connector? Connector) GetEndOfRouting( Instance element, bool fromConnector )
    {
      var routeName = element.GetRouteName() ;
      if ( null == routeName ) return ( false, null ) ;

      var connector = element.Document.CollectRoutingEndPointConnectors( routeName, fromConnector ).FirstOrDefault() ;
      return ( ( null != connector ), connector ) ;
    }

    private static (bool Result, Connector? Connector) SelectFromDialog( UIDocument uiDocument, FamilyInstance familyInstance, string message, Connector? firstConnector )
    {
      uiDocument.SetSelection( familyInstance ) ;

      var sv = new SelectConnector( familyInstance, firstConnector ) { Title = message } ;
      sv.ShowDialog() ;

      uiDocument.ClearSelection() ;
      uiDocument.GetActiveUIView()?.ZoomToFit() ;

      if ( true != sv.DialogResult ) return ( false, null ) ;

      return ( true, sv.GetSelectedConnector() ) ;
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

      private static bool IsRoutableForCenter( Element elem )
      {
        return ( elem is Instance ) ;
      }

      protected virtual bool IsRoutableElement( Element elem )
      {
        return elem switch
        {
          MEPCurve => elem.IsAutoRoutingGeneratedElement(),
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