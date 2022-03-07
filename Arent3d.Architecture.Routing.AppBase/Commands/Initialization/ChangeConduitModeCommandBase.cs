using System.Collections.Generic ;
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
  public abstract class ChangeConduitModeCommandBase: IExternalCommand
  {
    private static string SELECT_RANGE_MESSAGE = "Please select a range." ;
    private static string DIALOG_MESSAGE_TITLE = "Message" ;
    private static string NO_ITEM_SELECTED_MESSAGE = "No items are selected.";
    private static string UPDATE_DATA_SUCCESS_MESSAGE = "Update data success.";
    private static string ELECTRICAL_CHANGE_MODE_TITLE = "Change mode result";
    
    protected bool IsEcoMode ;
    private UIDocument UiDocument { get ; set ; } = null! ;
    
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      UiDocument = commandData.Application.ActiveUIDocument ;
      Document document = UiDocument.Document ;
      MessageBox.Show( "Dialog.Electrical.SelectElement.Message".GetAppStringByKeyOrDefault( SELECT_RANGE_MESSAGE ), "Dialog.Electrical.SelectElement.Title".GetAppStringByKeyOrDefault( DIALOG_MESSAGE_TITLE ), MessageBoxButtons.OK ) ;
      var selectedElements = UiDocument.Selection.PickElementsByRectangle( ConduitWithStartEndSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).Where( p => p is FamilyInstance or Conduit or CableTray).ToList() ;
      var conduitList = selectedElements.Where( elem => BuiltInCategorySets.ConnectorsAndConduits.Contains( elem.GetBuiltInCategory() ) && elem is FamilyInstance or Conduit).ToList();
      var connectorList = selectedElements.Where( elem => ( elem.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalFixtures || elem.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalEquipment ) && elem is FamilyInstance or TextNote).ToList() ;
      if( ! conduitList.Any() && ! connectorList.Any() )  {
        message = NO_ITEM_SELECTED_MESSAGE ;
      }
      var listApplyConduit = ConduitUtil.GetConduitRelated(document, conduitList) ;
      SetModeForConduit( listApplyConduit, IsEcoMode, document ) ;
      SetModeForConnector( connectorList, IsEcoMode, document ) ;
      MessageBox.Show(
        string.IsNullOrEmpty( message )
          ? "Dialog.Electrical.ChangeMode.Success".GetAppStringByKeyOrDefault( UPDATE_DATA_SUCCESS_MESSAGE )
          : message,
        "Dialog.Electrical.ChangeMode.Title".GetAppStringByKeyOrDefault( ELECTRICAL_CHANGE_MODE_TITLE ),
        MessageBoxButtons.OK ) ;
      return Result.Succeeded ;
    }

    #region Private Method

    private static void SetModeForConduit( List<Element> elements, bool isEcoMode, Document document )
    {
      if ( elements.Count == 0 ) return ;
      using var transaction = new Transaction( document ) ;
      transaction.Start( "Set conduits/racks mode" ) ;
      foreach ( var conduit in elements ) {
        conduit.SetProperty( RoutingFamilyLinkedParameter.IsEcoMode, isEcoMode.ToString() ) ;
      }
      transaction.Commit() ;
    }
    
    private static void SetModeForConnector( List<Element> elements, bool isEcoMode, Document document )
    {
      if ( elements.Count == 0 ) return ;
      using var transaction = new Transaction( document ) ;
      transaction.Start( "Set connector mode" ) ;
      Dictionary<ElementId, List<ElementId>> connectorGroups = new Dictionary<ElementId, List<ElementId>>() ;
      foreach ( var connector in elements ) {
        var parentGroup = document.GetElement( connector.GroupId ) as Group ;
        if ( parentGroup != null ) {
          // ungroup before set property
          var attachedGroup = document.GetAllElements<Group>()
            .Where( x => x.AttachedParentId == parentGroup.Id ) ;
          List<ElementId> listTextNoteIds = new List<ElementId>() ;
          // ungroup textNote before ungroup connector
          foreach ( var group in attachedGroup ) {
            var ids = @group.GetMemberIds() ;
            listTextNoteIds.AddRange( ids ) ;
            @group.UngroupMembers() ;
          }

          connectorGroups.Add( connector.Id, listTextNoteIds ) ;
          parentGroup.UngroupMembers() ;
        }
        connector.SetProperty( RoutingFamilyLinkedParameter.IsEcoMode, isEcoMode.ToString() ) ;
      }
      transaction.Commit() ;
      transaction.Start( "Set connector group" ) ;
      foreach ( var item in connectorGroups ) {
        // create group for updated connector (with new property) and related text note if any
        List<ElementId> groupIds = new List<ElementId> { item.Key } ;
        groupIds.AddRange( item.Value ) ;
        document.Create.NewGroup( groupIds ) ;
      }
      transaction.Commit() ;
    }

    #endregion
  }
}