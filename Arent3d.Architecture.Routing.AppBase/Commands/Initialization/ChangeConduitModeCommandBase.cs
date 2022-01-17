using System.Collections.Generic ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Commands.Base ;
using Arent3d.Architecture.Routing.AppBase.Constants ;
using Arent3d.Architecture.Routing.AppBase.Enums ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class ChangeConduitModeCommandBase: ConduitCommandBase, IExternalCommand
  {
    protected ElectricalMode Mode ;
    private UIDocument UiDocument { get ; set ; } = null! ;

    #region ReadOnly Prop

    private readonly List<BuiltInCategory> _conduitFilterCategory = new List<BuiltInCategory>()
    {
      BuiltInCategory.OST_Conduit,
      BuiltInCategory.OST_ConduitFitting,
      BuiltInCategory.OST_ConduitRun,
      BuiltInCategory.OST_MechanicalEquipment,
      BuiltInCategory.OST_ElectricalFixtures
    } ;
    
    private readonly List<BuiltInCategory> _rackFilterCategory = new List<BuiltInCategory>()
    {
      BuiltInCategory.OST_CableTray,
      BuiltInCategory.OST_CableTrayFitting
    } ;

    #endregion
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      UiDocument = commandData.Application.ActiveUIDocument ;
      Document document = UiDocument.Document ;
      MessageBox.Show( MessageKeys.DialogKeys.ELECTRICAL_SELECT_ELEMENT_MESSAGE_KEY.GetAppStringByKeyOrDefault( MessageConstants.SELECT_RANGE_MESSAGE ), MessageKeys.DialogKeys.ELECTRICAL_SELECT_ELEMENT_TITLE_KEY.GetAppStringByKeyOrDefault( MessageConstants.DIALOG_MESSAGE_TITLE ), MessageBoxButtons.OK ) ;
      var selectedElements = UiDocument.Selection.PickElementsByRectangle( ConduitWithStartEndSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).Where( p => p is FamilyInstance or Conduit or CableTray).ToList() ;
      var conduitList = selectedElements.Where( elem => _conduitFilterCategory.Contains( elem.GetBuiltInCategory() ) && elem is FamilyInstance or Conduit).ToList();
      var connectorList = selectedElements.Where( elem => elem.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalFixtures && elem is FamilyInstance or TextNote).ToList() ;
      var rackList = selectedElements.Where( elem =>  _rackFilterCategory.Contains(elem.GetBuiltInCategory()) && elem is FamilyInstance or CableTray ).ToList() ;
      if ( ! conduitList.Any() && ! connectorList.Any() && ! rackList.Any() ) {
        message = MessageConstants.NO_ITEM_SELECTED_MESSAGE ;
      }
      var listApplyConduit = GetConduitRelated(document, conduitList) ;
      SetModeForConduitOrRack( listApplyConduit, Mode, document ) ;
      SetModeForConnector( connectorList, Mode, document ) ;
      SetModeForConduitOrRack( rackList, Mode, document ) ;
      MessageBox.Show(
        string.IsNullOrEmpty( message )
          ? MessageKeys.DialogKeys.ELECTRICAL_CHANGE_MODE_SUCCESS_KEY.GetAppStringByKeyOrDefault( MessageConstants.UPDATE_DATA_SUCCESS_MESSAGE )
          : message,
        MessageKeys.DialogKeys.ELECTRICAL_CHANGE_MODE_TITLE_KEY.GetAppStringByKeyOrDefault( MessageConstants.ELECTRICAL_CHANGE_MODE_TITLE ),
        MessageBoxButtons.OK ) ;
      return Result.Succeeded ;
    }

    #region Private Method

    private static void SetModeForConduitOrRack( List<Element> elements, ElectricalMode mode, Document document )
    {
      if ( elements.Count == 0 ) return ;
      using var transaction = new Transaction( document ) ;
      transaction.Start( "Set conduits/racks mode" ) ;
      foreach ( var conduit in elements ) {
        conduit.SetProperty( RoutingFamilyLinkedParameter.Mode, mode.ToString() ) ;
      }
      transaction.Commit() ;
    }
    
    private static void SetModeForConnector( List<Element> elements, ElectricalMode mode, Document document )
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
        connector.SetProperty( ConnectorFamilyParameter.Mode, mode.ToString() ) ;
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