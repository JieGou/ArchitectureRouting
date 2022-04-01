using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.Electrical.App.Forms ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Initialization.SwitchEcoNormalModeCommand", DefaultString = "Switch EcoNormal Mode" )]
  [Image( "resources/Initialize-32.bmp", ImageType = Revit.UI.ImageType.Large )]
  public class SwitchEcoNormalModeCommand : IExternalCommand
  {
    private const string SchemaGuid = "DA4AAE5A-4EE1-45A8-B3E8-F790C84CC44F" ;
    private const string IsEcoFieldName = "IsEco" ;
    private const string EcoNormalModeSchema = "EcoNormalModeSchema" ;
    private const string TransactionName = "Electrical.App.Commands.Initialization.SwitchEcoNormalModeCommand" ;
    private const string DialogResultSuccessKey = "Dialog.Electrical.ChangeMode.Success" ;
    private const string DialogResultTitleKey = "Dialog.Electrical.ChangeMode.Title" ;
    private const string SelectElementDialogMessageKey = "Dialog.Electrical.SelectElement.Message" ;
    private const string SelectElementDialogTitleKey = "Dialog.Electrical.SelectElement.Title" ;
    private const string SelectRangeMessage = "Please select a range." ;
    private const string DialogMessageTitle = "Message" ;
    private const string NoItemSelectedMessage = "No items are selected." ;
    private const string UpdateDataSuccessMessage = "Update data success." ;
    private const string ElectricalChangeModeTitle = "Change mode result" ;

    private void SetEcoNormalModeForProject( Document document, bool isEco )
    {
      var schemaBuilder = new SchemaBuilder( new Guid( SchemaGuid ) ) ;
      schemaBuilder.SetSchemaName( EcoNormalModeSchema ) ;
      schemaBuilder.AddSimpleField( IsEcoFieldName, typeof( bool ) ) ;
      var schema = schemaBuilder.Finish() ;
      var entity = new Entity( schema ) ;
      entity.Set( IsEcoFieldName, isEco ) ;
      document.ProjectInformation.SetEntity( entity ) ;
    }

    private bool? IsProjectInEcoMode( Document document )
    {
      try {
        var schema = Schema.Lookup( new Guid( SchemaGuid ) ) ;
        var entity = document.ProjectInformation.GetEntity( schema ) ;
        return entity?.Get<bool>( IsEcoFieldName ) ;
      }
      catch {
        return null ;
      }
    }

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      try {
        var uiDocument = commandData.Application.ActiveUIDocument ;
        Document document = uiDocument.Document ;
        var isEcoMode = IsProjectInEcoMode( document ) ;
        var dialog = new SwitchEcoNormalModeDialog( commandData.Application, isEcoMode ) ;
        dialog.ShowDialog() ;
        if ( dialog.DialogResult == false ) return Result.Cancelled ;
        isEcoMode = dialog.SelectedMode == EcoNormalMode.EcoMode ;
        return dialog.ApplyForProject == true ? SwitchModeForProject( document, ref message, isEcoMode ?? false ) : SwitchModeForRange( commandData, ref message, isEcoMode ?? false ) ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException( exception ) ;
        return Result.Cancelled ;
      }
    }

    private (IList<Element> conduitList, IList<Element> connectorList) GetAllConduitAndConnectorInProject( Document document )
    {
      FilteredElementCollector collector = new FilteredElementCollector( document ) ;
      collector = collector.OfClass( typeof( FamilyInstance ) ) ;
      var conduitList = collector.ToElements().ToList() ;
      collector = new FilteredElementCollector( document ) ;
      conduitList.AddRange( collector.OfClass( typeof( Conduit ) ).ToElements() ) ;
      conduitList = conduitList.Where( elem => BuiltInCategorySets.ConnectorsAndConduits.Contains( elem.GetBuiltInCategory() ) ).ToList() ;
      collector = new FilteredElementCollector( document ) ;
      collector = collector.OfClass( typeof( FamilyInstance ) ) ;
      var connectorList = collector.ToElements().ToList() ;
      collector = new FilteredElementCollector( document ) ;
      connectorList.AddRange( collector.OfClass( typeof( TextNote ) ).ToElements() ) ;
      connectorList = connectorList.Where( elem => ( elem.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalFixtures || elem.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalEquipment ) ).ToList() ;

      var listApplyConduit = ConduitUtil.GetConduitRelated( document, conduitList ) ;
      return ( listApplyConduit, connectorList ) ;
    }

    private Result SwitchModeForProject( Document document, ref string message, bool isEcoMode )
    {
      var (conduitList, connectorList) = GetAllConduitAndConnectorInProject( document ) ;
      using var transaction = new Transaction( document, TransactionName ) ;
      transaction.Start() ;
      SetModeForConduit( conduitList, (bool) isEcoMode ) ;
      SetModeForConnector( connectorList, (bool) isEcoMode, document ) ;
      SetEcoNormalModeForProject( document, (bool) isEcoMode ) ;
      transaction.Commit() ;
      MessageBox.Show( string.IsNullOrEmpty( message ) ? DialogResultSuccessKey.GetAppStringByKeyOrDefault( UpdateDataSuccessMessage ) : message, DialogResultTitleKey.GetAppStringByKeyOrDefault( ElectricalChangeModeTitle ), MessageBoxButtons.OK ) ;
      return Result.Succeeded ;
    }

    private Result SwitchModeForRange( ExternalCommandData commandData, ref string message, bool isEcoMode )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      Document document = uiDocument.Document ;
      MessageBox.Show( SelectElementDialogMessageKey.GetAppStringByKeyOrDefault( SelectRangeMessage ), SelectElementDialogTitleKey.GetAppStringByKeyOrDefault( DialogMessageTitle ), MessageBoxButtons.OK ) ;
      var selectedElements = uiDocument.Selection.PickElementsByRectangle( ConduitWithStartEndSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).Where( p => p is FamilyInstance or Conduit or CableTray ).ToList() ;
      var conduitList = selectedElements.Where( elem => BuiltInCategorySets.ConnectorsAndConduits.Contains( elem.GetBuiltInCategory() ) && elem is FamilyInstance or Conduit ).ToList() ;
      var connectorList = selectedElements.Where( elem => ( elem.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalFixtures || elem.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalEquipment ) && elem is FamilyInstance or TextNote ).ToList() ;
      if ( ! conduitList.Any() && ! connectorList.Any() ) {
        message = NoItemSelectedMessage ;
      }

      var listApplyConduit = ConduitUtil.GetConduitRelated( document, conduitList ) ;
      using var transaction = new Transaction( document, TransactionName ) ;
      transaction.Start() ;
      SetModeForConduit( listApplyConduit, isEcoMode ) ;
      SetModeForConnector( connectorList, isEcoMode, document ) ;
      transaction.Commit() ;
      MessageBox.Show( string.IsNullOrEmpty( message ) ? DialogResultSuccessKey.GetAppStringByKeyOrDefault( UpdateDataSuccessMessage ) : message, DialogResultTitleKey.GetAppStringByKeyOrDefault( ElectricalChangeModeTitle ), MessageBoxButtons.OK ) ;
      return Result.Succeeded ;
    }

    private static void SetModeForConduit( IList<Element> elements, bool isEcoMode )
    {
      if ( elements.Count == 0 ) return ;
      foreach ( var conduit in elements ) {
        conduit.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, isEcoMode.ToString() ) ;
      }
    }

    private static void SetModeForConnector( IList<Element> elements, bool isEcoMode, Document document )
    {
      if ( elements.Count == 0 ) return ;
      Dictionary<ElementId, List<ElementId>> connectorGroups = new Dictionary<ElementId, List<ElementId>>() ;
      foreach ( var connector in elements ) {
        var parentGroup = document.GetElement( connector.GroupId ) as Group ;
        if ( parentGroup != null ) {
          // ungroup before set property
          var attachedGroup = document.GetAllElements<Group>().Where( x => x.AttachedParentId == parentGroup.Id ) ;
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

        connector.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, isEcoMode.ToString() ) ;
      }

      foreach ( var item in connectorGroups ) {
        // create group for updated connector (with new property) and related text note if any
        List<ElementId> groupIds = new List<ElementId> { item.Key } ;
        groupIds.AddRange( item.Value ) ;
        document.Create.NewGroup( groupIds ) ;
      }
    }
  }
}