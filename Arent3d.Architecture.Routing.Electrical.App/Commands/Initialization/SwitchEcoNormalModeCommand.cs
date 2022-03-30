using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.Electrical.App.Forms ;
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
    private static bool? IsProjectInEcoMode = null;
    public override Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      UiDocument = commandData.Application.ActiveUIDocument ;
      Document document = UiDocument.Document ;
      var dialog = new SwitchEcoNormalModeDialog( commandData.Application, IsProjectInEcoMode ) ;
      dialog.ShowDialog() ;
      if ( dialog.DialogResult == false ) return Result.Cancelled ;
      if ( dialog.ApplyForProject == true ) {
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
        IsProjectInEcoMode = false ;
        var listApplyConduit = ConduitUtil.GetConduitRelated(document, conduitList) ;
        SetModeForConduit( listApplyConduit, (bool) IsProjectInEcoMode, document ) ;
        SetModeForConnector( connectorList, (bool) IsProjectInEcoMode, document ) ;
      }
      else {
        MessageBox.Show( "Select a range đi ông ei" ) ;
      }
      return Result.Succeeded ;
    }
  }
}