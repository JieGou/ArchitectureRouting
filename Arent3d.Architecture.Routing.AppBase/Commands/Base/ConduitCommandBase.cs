using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Base
{
  public abstract class ConduitCommandBase
  {
    protected List<Element> GetConduitRelated(Document doc, List<Element> conduits)
    {
      var result = new List<Element>() ;
      var allConduits = doc.GetAllElements<Element>().OfCategory(  BuiltInCategorySets.Conduits ) ;
      foreach ( var conduit in conduits ) {
        bool hasStartElement = false ;
        bool hasEndElement = false ;
        string startTeminateId = string.Empty;
        string endTeminateId = string.Empty;
        var conduitRouteName = conduit.GetRouteName() ;
        var startPoint = conduit.GetNearestEndPoints( true ) ;
        var startPointKey = startPoint.FirstOrDefault()?.Key ;
        if(startPointKey!=null)
        {
          startTeminateId = startPointKey.GetElementId().ToString() ;
        }
        var endPoint = conduit.GetNearestEndPoints(  false ) ;
        var endPointKey = endPoint.FirstOrDefault()?.Key ;
        if ( endPointKey != null ) {
          endTeminateId = endPointKey!.GetElementId().ToString() ;
        }

        if ( ! string.IsNullOrEmpty( startTeminateId ) && !string.IsNullOrEmpty( endTeminateId ) ) {
          var (startConnectorId, endConnectorId) =
            GetFromConnectorIdAndToConnectorId( doc, startTeminateId, endTeminateId ) ;
          hasStartElement = conduits.Any( c => c.Id.IntegerValue.ToString() == startConnectorId ) ;
          hasEndElement = conduits.Any( c => c.Id.IntegerValue.ToString() == endConnectorId ) ;
        }
        if(!string.IsNullOrEmpty( conduitRouteName ) && hasStartElement && hasEndElement ){
          var relateConduits = allConduits.Where( x =>  x.GetRouteName()==conduitRouteName).ToList() ;
          bool isNotFull = relateConduits.Any( x => conduits.All( y => y.Id != x.Id ) ) ;
          if(!isNotFull){
            result.AddRange( relateConduits ); 
          }
        }
      }
      return result ;
    }
    
    private static (string, string) GetFromConnectorIdAndToConnectorId( Document document, string fromElementId, string toElementId )
    {
      var allConnectors = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;

      if ( ! string.IsNullOrEmpty( fromElementId ) ) {
        var fromConnector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == fromElementId ) ;
        if ( fromConnector!.IsTerminatePoint() || fromConnector!.IsPassPoint() ) {
          fromConnector!.TryGetProperty( PassPointParameter.RelatedFromConnectorId, out string? fromConnectorId ) ;
          if ( ! string.IsNullOrEmpty( fromConnectorId ) )
            fromElementId = fromConnectorId! ;
        }
      }

      if ( string.IsNullOrEmpty( toElementId ) ) return ( fromElementId, toElementId ) ;
      {
        var toConnector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == toElementId ) ;
        if ( ! toConnector!.IsTerminatePoint() && ! toConnector!.IsPassPoint() ) return ( fromElementId, toElementId ) ;
        toConnector!.TryGetProperty( PassPointParameter.RelatedConnectorId, out string? toConnectorId ) ;
        if ( ! string.IsNullOrEmpty( toConnectorId ) )
          toElementId = toConnectorId! ;
      }

      return ( fromElementId, toElementId ) ;
    }
  }
}