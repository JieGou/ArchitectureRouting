using System ;
using System.Collections.Generic ;
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
    public static Connector GetConnector( UIDocument uiDocument, string message, Connector? firstConnector = null )
    {
      var document = uiDocument.Document ;

      var filter = ( null == firstConnector ) ? FamilyInstanceWithConnectorFilter.Instance : new FamilyInstanceCompatibleToTargetConnectorFilter( firstConnector ) ;

      while ( true ) {
        var pickedObject = uiDocument.Selection.PickObject( ObjectType.Element, filter, message ) ;

        var familyInstance = document.GetElementById<FamilyInstance>( pickedObject.ElementId ) ;
        if ( null == familyInstance ) continue ;

        uiDocument.SetSelection( familyInstance ) ;

        var sv = new SelectConnector( familyInstance, firstConnector ) ;
        sv.ShowDialog() ;

        uiDocument.ClearSelection() ;
        uiDocument.GetActiveUIView()?.ZoomToFit() ;

        if ( true != sv.DialogResult ) continue ;

        var connector = sv.GetSelectedConnector() ;
        if ( null == connector ) continue ;

        return connector ;
      }
    }

    private class FamilyInstanceWithConnectorFilter : ISelectionFilter
    {
      public static ISelectionFilter Instance { get ; } = new FamilyInstanceWithConnectorFilter() ;

      public bool AllowElement( Element elem )
      {
        if ( ! ( elem is FamilyInstance familyInstance ) ) return false ;
        if ( ! ( familyInstance.MEPModel?.ConnectorManager?.Connectors is { } connectorSet ) ) return false ;

        return connectorSet.OfType<Connector>().Any( IsTargetConnector ) ;
      }

      protected virtual bool IsTargetConnector( Connector connector )
      {
        return connector.ConnectorType == ConnectorType.End && connector.Domain switch
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

      public FamilyInstanceCompatibleToTargetConnectorFilter( Connector connector )
      {
        _connector = connector ;
      }

      protected override bool IsTargetConnector( Connector connector )
      {
        return base.IsTargetConnector( connector ) && _connector.IsCompatibleTo( connector ) ;
      }
    }
  }
}