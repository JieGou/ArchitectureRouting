using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class SwitchEcoNormalModeCommand: ChangeConduitModeCommandBase
  {
    private static bool? IsEco = null;
    public override Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      UiDocument = commandData.Application.ActiveUIDocument ;
      Document document = UiDocument.Document ;
      // MessageBox.Show( "Dialog.Electrical.SelectElement.Message".GetAppStringByKeyOrDefault( SELECT_RANGE_MESSAGE ), "Dialog.Electrical.SelectElement.Title".GetAppStringByKeyOrDefault( DIALOG_MESSAGE_TITLE ), MessageBoxButtons.OK ) ;
      // Apply the filter to the elements in the active document
      FilteredElementCollector collector = new FilteredElementCollector(document);
      collector = collector.OfClass(typeof(FamilyInstance)) ;
      collector = collector.UnionWith(collector.OfClass( typeof( FamilyInstance ))) ;
      foreach ( var category in BuiltInCategorySets.ConnectorsAndConduits ) {
        collector = collector.UnionWith(collector.OfCategory(category)) ;
      }
      // var selectedElements = UiDocument.Selection.PickElementsByRectangle( ConduitWithStartEndSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).Where( p => p is FamilyInstance or Conduit or CableTray).ToList() ;
      var conduitList = collector.ToElements().ToList();
      //
      // var connectorList = selectedElements.Where( elem => ( elem.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalFixtures || elem.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalEquipment ) && elem is FamilyInstance or TextNote).ToList() ;
      // if( ! conduitList.Any() && ! connectorList.Any() )  {
      //   message = NO_ITEM_SELECTED_MESSAGE ;
      // }
      IsEco = true ;
      var listApplyConduit = ConduitUtil.GetConduitRelated(document, conduitList) ;
      SetModeForConduit( listApplyConduit, IsEco??true, document ) ;
      // SetModeForConnector( connectorList, IsEcoMode, document ) ;
      MessageBox.Show(
        string.IsNullOrEmpty( message )
          ? "Dialog.Electrical.ChangeMode.Success".GetAppStringByKeyOrDefault( UPDATE_DATA_SUCCESS_MESSAGE )
          : message,
        "Dialog.Electrical.ChangeMode.Title".GetAppStringByKeyOrDefault( ELECTRICAL_CHANGE_MODE_TITLE ),
        MessageBoxButtons.OK ) ;
      return Result.Succeeded ;
    }
  }
}