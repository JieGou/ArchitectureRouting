using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  public class RoutingVAVUtil
  {
    public static int GetAHUNumberOfAHU( Connector? rootConnector )
    {
      const int limit = 30 ;
      var ahuNumberOfAHU = (int) AHUNumberType.Invalid ;

      if ( rootConnector == null ) return ahuNumberOfAHU ;

      // AHUのコネクタを選択するとき
      if ( rootConnector.Owner is FamilyInstance parentElement && parentElement.IsFamilyInstanceOf( RoutingFamilyType.AHU_2367 ) ) {
        parentElement.TryGetProperty( AHUNumberParameter.AHUNumber, out ahuNumberOfAHU ) ;
        return ahuNumberOfAHU ;
      }

      var firstCandidates = rootConnector.GetConnectedConnectors().ToArray() ;
      if ( firstCandidates.Length == 0 ) return ahuNumberOfAHU ;

      var current = firstCandidates.First() ;

      for ( var i = 0 ; i < limit ; ++i ) {
        if ( current.Owner is FamilyInstance element && element.IsFamilyInstanceOf( RoutingFamilyType.AHU_2367 ) ) {
          element.TryGetProperty( AHUNumberParameter.AHUNumber, out ahuNumberOfAHU ) ;
          return ahuNumberOfAHU ;
        }

        var oppositeConnectors = current.Owner.GetConnectors().Where( connector => connector.Id != current.Id ).ToArray() ;
        if ( oppositeConnectors.Length == 0 ) return ahuNumberOfAHU ; // 途切れているケース

        var nextConnectors = oppositeConnectors.First().GetConnectedConnectors().ToArray() ;
        if ( nextConnectors.Length == 0 ) return ahuNumberOfAHU ; // 途切れているケース

        current = nextConnectors.First() ;
      }

      return ahuNumberOfAHU ;
    }
  }
}