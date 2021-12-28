using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class ConfirmNotConnectingCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      Document document = commandData.Application.ActiveUIDocument.Document ;
      try {
        return document.Transaction( "TransactionName.Commands.Routing.ConfirmUnset".GetAppStringByKeyOrDefault( "Confirm Not Connect" ), _ =>
        {
          var elementNotConnect = GetElementsNotConnect( document ) ;
          var color = new Color( 255, 0, 0 ) ;
          ConfirmUnsetCommandBase.ChangeElementColor( document, elementNotConnect, color ) ;

          return Result.Succeeded ;
        } ) ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }
    
    private IEnumerable<Element> GetElementsNotConnect( Document document )
    {
      var conduitsNotConnected = new List<Element>() ;
      var connectorIdsNotConnected = new List<ElementId>() ;
      List<Element> elementsNotConnected = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Connectors ).ToList() ;
      var allConduits = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).ToList() ;
      var conduits = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).ToList() ;
      var allConnectors = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
      foreach ( var conduit in conduits ) {
        GetFromConnectorIdAndToConnectorIdOfCable( allConduits, allConnectors, connectorIdsNotConnected, conduitsNotConnected, conduit ) ;
      }
      
      if ( connectorIdsNotConnected.Any() ) {
        connectorIdsNotConnected = connectorIdsNotConnected.Distinct().ToList() ;
        elementsNotConnected = elementsNotConnected.Where( c => ! connectorIdsNotConnected.Contains( c.Id ) ).ToList() ;
      }

      if ( ! conduitsNotConnected.Any() ) return elementsNotConnected ;
      conduitsNotConnected = conduitsNotConnected.Distinct().ToList() ;
      elementsNotConnected.AddRange( conduitsNotConnected ) ;

      return elementsNotConnected ;
    }

    private void GetFromConnectorIdAndToConnectorIdOfCable( IEnumerable<Element> allConduits, IReadOnlyCollection<Element> allConnectors, ICollection<ElementId> connectorIdsNotConnected, List<Element> conduitsNotConnected, Element conduit )
    {
      var cableIsFromConnected = false ;
      var cableIsToConnected = false ;
      var fromEndPoint = conduit.GetNearestEndPoints( true ) ;
      var fromEndPointKey = fromEndPoint.FirstOrDefault()?.Key ;
      if ( fromEndPointKey != null ) {
        var fromElementId = fromEndPointKey!.GetElementId() ;
        if ( ! string.IsNullOrEmpty( fromElementId ) ) {
          var fromConnector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == fromElementId ) ;
          if ( fromConnector != null ) {
            cableIsFromConnected = true ;
            if ( ! connectorIdsNotConnected.Contains( fromConnector.Id ) ) 
              connectorIdsNotConnected.Add( fromConnector.Id ) ;
          }
        }
      }
      
      var toEndPoint = conduit.GetNearestEndPoints( false ) ;
      var toEndPointKey = toEndPoint.FirstOrDefault()?.Key ;
      if ( toEndPointKey != null ) {
        var toElementId = toEndPointKey!.GetElementId() ;
        if ( ! string.IsNullOrEmpty( toElementId ) ) {
          var toConnector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == toElementId ) ;
          if ( toConnector != null ) {
            cableIsToConnected = true ;
            if ( ! connectorIdsNotConnected.Contains( toConnector.Id ) )
              connectorIdsNotConnected.Add( toConnector.Id ) ;
          }
        }
      }

      if ( cableIsFromConnected && cableIsToConnected ) return ;
      {
        var allConduitOfRoute = allConduits.Where( c => c.GetRouteName() == conduit.GetRouteName() ).ToList() ;
        conduitsNotConnected.AddRange( allConduitOfRoute );
      }
    }
  }
}