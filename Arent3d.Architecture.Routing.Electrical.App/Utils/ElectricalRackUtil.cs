using System ;
using System.Collections ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Architecture.Routing.EndPoints ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using Autodesk.Revit.ApplicationServices ;
using MoreLinq ;


namespace Arent3d.Architecture.Routing.Electrical.App.Utils
{
  public static class RackUtils
  {
    public static CableTray? CreateCableTrayForStraightConduit( this Conduit conduit,  UIDocument uiDocument, double cableRackWidth = 75, double startParam = 0.0, double endParam = 1.0 )
    {
      var document = uiDocument.Document ;
      var firstConnector = conduit.GetConnectorManager()!.Connectors.Flatten().OfType<Connector>()
        .FirstOrDefault(con => con!.Id == 0 )!;

      var location = ( conduit.Location as LocationCurve )! ;
      var line = ( location.Curve as Line )! ;
      var length = line.Length ;
      var startPoint = firstConnector.Origin + line.Direction * length * startParam ;
      var endPoint = firstConnector.Origin + line.Direction * length * endParam ;
      var cableRackType = document.GetAllElements<CableTrayType>( ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      
      // Create cable tray
      var ct = CableTray.Create( document, cableRackType.Id, startPoint, endPoint, conduit.LevelId ) ;
      ct.SetProperty(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM, cableRackWidth.MillimetersToRevitUnits());
      return ct ;
    }

    public static void CreateCableTrayForConduit( UIDocument uiDocument, IEnumerable<Element> allElementsInRoute, List<Element> racks,
      List<(Element Conduit, double StartParam, double EndParam)>? specialLengthList = null )
    {
      
    }
  }
}