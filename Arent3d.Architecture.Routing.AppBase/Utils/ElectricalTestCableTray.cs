using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using MoreLinq ;


namespace Arent3d.Architecture.Routing.AppBase.Utils
{
  public static class CableTrayUtils
  {
    public static CableTray? CreateCableTrayForStraightConduit( this Conduit conduit, double cableRackWidth = 75, double startParam = 0.0, double endParam = 1.0 )
    {
      var document = conduit.Document ;
      var firstConnector = conduit.GetConnectorManager()!.Connectors.Flatten().OfType<Connector>()
        .FirstOrDefault(con => con!.Id == 0 )!;
      var width = cableRackWidth > 0 ? cableRackWidth : 75 ;
      var location = ( conduit.Location as LocationCurve )! ;
      var line = ( location.Curve as Line )! ;
      var length = line.Length ;
      var isVertical = line.Direction.IsAlmostEqualTo( XYZ.BasisZ ) || line.Direction.IsAlmostEqualTo( -XYZ.BasisZ ) ;
      var moveZ = isVertical ? 0 : -30d.MillimetersToRevitUnits() ;
      var startPoint = firstConnector.Origin + line.Direction * length * startParam + moveZ * XYZ.BasisZ;
      var endPoint = firstConnector.Origin + line.Direction * length * endParam + moveZ * XYZ.BasisZ;;
      var cableRackType = document.GetAllElements<CableTrayType>( ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      
      var diameter = conduit.ParametersMap.get_Item( "Revit.Property.Builtin.OutsideDiameter".GetDocumentStringByKeyOrDefault( document, "Outside Diameter" ) ).AsDouble() ;
      
      // Create cable tray, set width, height
      var ct = CableTray.Create( document, cableRackType.Id, startPoint, endPoint, conduit.LevelId ) ;
      ct.SetProperty(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM, width.MillimetersToRevitUnits());
      ct.SetProperty(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM, 32d.MillimetersToRevitUnits());
      
      // set cable rack comments
      ct.SetProperty( "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( document, "Rack Type" ), cableRackWidth == 0 ? NewRackCommandBase.RackTypes[ 0 ] : NewRackCommandBase.RackTypes[ 1 ] ) ;

      // set To-Side Connector Id
      var (fromConnectorId, toConnectorId) = NewRackCommandBase.GetFromAndToConnectorUniqueId( conduit ) ;
      if ( ! string.IsNullOrEmpty( toConnectorId ) && ct.HasParameter(  ElectricalRoutingElementParameter.ToSideConnectorId ) )
        ct.TrySetProperty( ElectricalRoutingElementParameter.ToSideConnectorId, toConnectorId ) ;
      if ( ! string.IsNullOrEmpty( fromConnectorId ) && ct.HasParameter(  ElectricalRoutingElementParameter.FromSideConnectorId ) )
        ct.TrySetProperty( ElectricalRoutingElementParameter.FromSideConnectorId, fromConnectorId ) ;
      
      return ct ;
    }
  }
}