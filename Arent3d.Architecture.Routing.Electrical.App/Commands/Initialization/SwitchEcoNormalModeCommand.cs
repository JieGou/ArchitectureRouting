using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Initialization.SwitchEcoNormalModeCommand", DefaultString = "Switch EcoNormal Mode" )]
  [Image( "resources/Initialize-32.bmp", ImageType = Revit.UI.ImageType.Large )]
  public class SwitchEcoNormalModeCommand: ChangeConduitModeCommandBase
  {
    private static bool? IsEco = null;
    public override Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      UiDocument = commandData.Application.ActiveUIDocument ;
      Document document = UiDocument.Document ;
      FilteredElementCollector collector = new FilteredElementCollector(document);
      collector = collector.OfClass(typeof(FamilyInstance)) ;
      var conduitList = collector.ToElements().ToList();
      collector = new FilteredElementCollector(document);
      conduitList.AddRange( collector.OfClass(typeof( Conduit )).ToElements()) ;
      conduitList = conduitList.Where( elem => BuiltInCategorySets.ConnectorsAndConduits.Contains( elem.GetBuiltInCategory() )).ToList();
      collector = new FilteredElementCollector(document);
      collector = collector.OfClass(typeof(FamilyInstance)) ;
      var connectorList = collector.ToElements().ToList();
      collector = new FilteredElementCollector(document);
      connectorList.AddRange( collector.OfClass(typeof( TextNote )).ToElements()) ;
      connectorList = connectorList.Where( elem => ( elem.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalFixtures || elem.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalEquipment )).ToList() ;
      IsEco = false ;
      var listApplyConduit = ConduitUtil.GetConduitRelated(document, conduitList) ;
      SetModeForConduit( listApplyConduit, (bool) IsEco, document ) ;
      SetModeForConnector( connectorList, (bool) IsEco, document ) ;

      return Result.Succeeded ;
    }
  }
}