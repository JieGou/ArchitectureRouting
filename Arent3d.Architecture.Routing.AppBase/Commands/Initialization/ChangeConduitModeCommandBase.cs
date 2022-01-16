using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Threading ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Commands.Base ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Enums ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using ProgressBar = Arent3d.Revit.UI.Forms.ProgressBar ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class ChangeConduitModeCommandBase: ConduitCommandBase, IExternalCommand
  {
    protected ElectricalMode Mode ;
    private UIDocument UiDocument { get ; set ; } = null! ;
    
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      UiDocument = commandData.Application.ActiveUIDocument ;
      Document document = UiDocument.Document ;
      MessageBox.Show( "Dialog.Electrical.SelectElement.Message".GetAppStringByKeyOrDefault( "Please select a range." ), "Dialog.Electrical.SelectElement.Title".GetAppStringByKeyOrDefault( "Message" ), MessageBoxButtons.OK ) ;
      var selectedElements = UiDocument.Selection.PickElementsByRectangle( ConduitWithStartEndSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).Where( p => p is FamilyInstance or Conduit).ToList() ;
      var conduitList = selectedElements.Where( elem => BuiltInCategory.OST_Conduit == elem.GetBuiltInCategory() ||
                                                        BuiltInCategory.OST_ConduitFitting ==
                                                        elem.GetBuiltInCategory() ||
                                                        BuiltInCategory.OST_ConduitRun == elem.GetBuiltInCategory() ||
                                                        BuiltInCategory.OST_MechanicalEquipment ==
                                                        elem.GetBuiltInCategory() ||
                                                        BuiltInCategory.OST_ElectricalFixtures ==
                                                        elem.GetBuiltInCategory() )
        .Where( elem => elem is FamilyInstance or Conduit ).ToList() ;
      var connectorList = selectedElements.Where( elem => BuiltInCategory.OST_ElectricalFixtures == elem.GetBuiltInCategory() )
        .Where( elem => elem is FamilyInstance or TextNote ).ToList() ;
      var rackList = selectedElements.Where( elem => BuiltInCategory.OST_CableTray == elem.GetBuiltInCategory() || 
                                                        BuiltInCategory.OST_CableTrayFitting == elem.GetBuiltInCategory() )
        .Where( elem => elem is FamilyInstance or CableTray ).ToList() ;
      if ( ! conduitList.Any() && ! connectorList.Any() && ! rackList.Any() ) {
        message = "No items are selected." ;
      }
      var listApplyConduit = GetConduitRelated(document, conduitList) ;
      SetModeForConduitOrRack( listApplyConduit, Mode, document ) ;
      SetModeForConnector( connectorList, Mode, document ) ;
      SetModeForConduitOrRack( rackList, Mode, document ) ;
      MessageBox.Show(
        string.IsNullOrEmpty( message )
          ? "Dialog.Electrical.SetElementProperty.Success".GetAppStringByKeyOrDefault( "Success" )
          : message,
        "Dialog.Electrical.SetElementProperty.Title".GetAppStringByKeyOrDefault( "Construction item addition result" ),
        MessageBoxButtons.OK ) ;
      return Result.Succeeded ;
    }
    
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
        List<ElementId> groupIds = new List<ElementId>() ;
        groupIds.Add( item.Key ) ;
        groupIds.AddRange( item.Value ) ;
        document.Create.NewGroup( groupIds ) ;
      }
      transaction.Commit() ;
    }
  }
}