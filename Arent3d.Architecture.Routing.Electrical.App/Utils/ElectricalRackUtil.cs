using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Architecture.Routing.Electrical.App.Utils;
using Autodesk.Revit.ApplicationServices ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;


namespace Arent3d.Architecture.Routing.Electrical.App.Utils
{
  /// <summary>
  /// Arent's internal utilities for processing electrical rack
  /// </summary>
  public static class ElectricalRackUtil
  {
    public static void CreateRackForConduit( this UIDocument uiDocument, Application app, IEnumerable<Element> conduits, List<FamilyInstance> racks, List<(Element Conduit, double StartParam, double EndParam)>? specialLengthList = null ) => NewRackCommandBase.CreateRackForConduit( uiDocument, app, conduits, racks, specialLengthList ) ;
  }
}