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
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Initialization.SwitchEcoNormalModeCommand",
    DefaultString = "Switch Eco\nNormal Mode" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class SwitchEcoNormalModeCommand : IExternalCommand
  {
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

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      try {
        var uiDocument = commandData.Application.ActiveUIDocument ;
        var document = uiDocument.Document ;
        var dialog = new SwitchEcoNormalModeDialog( commandData.Application ) ;
        dialog.ShowDialog() ;
        {
          if ( dialog.DialogResult == false ) return Result.Cancelled ;
          bool? isEcoMode = dialog.SelectedMode == EcoNormalMode.EcoMode ;
          return dialog.ApplyForProject == true ? SwitchModeForProject( document, ref message, isEcoMode ?? false ) : SwitchModeForRange( commandData, ref message, isEcoMode ?? false ) ;
        }
      }
      catch ( OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException( exception ) ;
        return Result.Cancelled ;
      }
    }

    private static IList<Element> GetAllConduitInProject( Document document )
    {
      var familyInstances = new FilteredElementCollector( document ).OfClass( typeof( FamilyInstance ) ).ToElements().ToList() ;
      var conduits = new FilteredElementCollector( document ).OfClass( typeof( Conduit ) ).ToElements().ToList() ;
      var allConduits = familyInstances.Concat( conduits ).ToList() ;
      allConduits = allConduits.Where( elem => BuiltInCategorySets.ConnectorsAndConduits.Contains( elem.GetBuiltInCategory() ) ).ToList() ;
      var listApplyConduit = ConduitUtil.GetConduitRelated( document, allConduits ) ;
      return listApplyConduit ;
    }

    private static IList<Element> GetAllConnectorInProject( Document document )
    {
      var familyInstances = new FilteredElementCollector( document ).OfClass( typeof( FamilyInstance ) ).ToElements().ToList() ;
      var textNotes = new FilteredElementCollector( document ).OfClass( typeof( TextNote ) ).ToElements().ToList() ;
      var connectorList = familyInstances.Concat( textNotes ).ToList() ;
      connectorList = connectorList.Where( elem => elem.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalFixtures || elem.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalEquipment ).ToList() ;
      return connectorList ;
    }

    private static Result SwitchModeForProject( Document document, ref string message, bool isEcoMode )
    {
      var conduitList = GetAllConduitInProject( document ) ;
      var connectorList = GetAllConnectorInProject( document ) ;
      using var transaction = new Transaction( document, TransactionName ) ;
      transaction.Start() ;
      var failureOptions = transaction.GetFailureHandlingOptions() ;
      failureOptions.SetFailuresPreprocessor( new FailurePreprocessor() ) ;
      transaction.SetFailureHandlingOptions( failureOptions ) ;
      SetModeForConduit( conduitList, isEcoMode ) ;
      SetModeForConnector( connectorList, isEcoMode, document ) ;
      transaction.Commit() ;
      MessageBox.Show( string.IsNullOrEmpty( message ) ? DialogResultSuccessKey.GetAppStringByKeyOrDefault( UpdateDataSuccessMessage ) : message, DialogResultTitleKey.GetAppStringByKeyOrDefault( ElectricalChangeModeTitle ), MessageBoxButtons.OK ) ;
      return Result.Succeeded ;
    }

    private static Result SwitchModeForRange( ExternalCommandData commandData, ref string message, bool isEcoMode )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      MessageBox.Show( SelectElementDialogMessageKey.GetAppStringByKeyOrDefault( SelectRangeMessage ), SelectElementDialogTitleKey.GetAppStringByKeyOrDefault( DialogMessageTitle ), MessageBoxButtons.OK ) ;
      var selectedElements = uiDocument.Selection.PickElementsByRectangle( ConduitWithStartEndSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).Where( p => p is FamilyInstance or Conduit or CableTray ).ToList() ;
      var conduitList = selectedElements.Where( elem => BuiltInCategorySets.ConnectorsAndConduits.Contains( elem.GetBuiltInCategory() ) && elem is FamilyInstance or Conduit ).ToList() ;
      var connectorList = selectedElements.Where( elem => ( elem.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalFixtures || elem.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalEquipment ) && elem is FamilyInstance or TextNote ).ToList() ;
      if ( ! conduitList.Any() && ! connectorList.Any() ) message = NoItemSelectedMessage ;

      var listApplyConduit = ConduitUtil.GetConduitRelated( document, conduitList ) ;
      using var transaction = new Transaction( document, TransactionName ) ;
      transaction.Start() ;
      var failureOptions = transaction.GetFailureHandlingOptions() ;
      failureOptions.SetFailuresPreprocessor( new FailurePreprocessor() ) ;
      transaction.SetFailureHandlingOptions( failureOptions ) ;
      SetModeForConduit( listApplyConduit, isEcoMode ) ;
      SetModeForConnector( connectorList, isEcoMode, document ) ;
      transaction.Commit() ;

      MessageBox.Show( string.IsNullOrEmpty( message ) ? DialogResultSuccessKey.GetAppStringByKeyOrDefault( UpdateDataSuccessMessage ) : message, DialogResultTitleKey.GetAppStringByKeyOrDefault( ElectricalChangeModeTitle ), MessageBoxButtons.OK ) ;
      return Result.Succeeded ;
    }

    private static void SetModeForConduit( ICollection<Element> elements, bool isEcoMode )
    {
      if ( elements.Count == 0 ) return ;
      foreach ( var conduit in elements )
        conduit.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, isEcoMode.ToString() ) ;
    }

    private static void SetModeForConnector( ICollection<Element> elements, bool isEcoMode, Document document )
    {
      if ( elements.Count == 0 ) return ;
      var connectorGroups = new Dictionary<ElementId, List<ElementId>>() ;
      foreach ( var connector in elements ) {
        if ( document.GetElement( connector.GroupId ) is Group parentGroup ) {
          // ungroup before set property
          var attachedGroup = document.GetAllElements<Group>().Where( x => x.AttachedParentId == parentGroup.Id ) ;
          var listTextNoteIds = new List<ElementId>() ;
          // ungroup textNote before ungroup connector
          foreach ( var group in attachedGroup ) {
            var ids = group.GetMemberIds() ;
            listTextNoteIds.AddRange( ids ) ;
            group.UngroupMembers() ;
          }

          connectorGroups.Add( connector.Id, listTextNoteIds ) ;
          parentGroup.UngroupMembers() ;
        }

        connector.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, isEcoMode.ToString() ) ;
      }

      foreach ( var (key, value) in connectorGroups ) {
        // create group for updated connector (with new property) and related text note if any
        var groupIds = new List<ElementId> { key } ;
        groupIds.AddRange( value ) ;
        document.Create.NewGroup( groupIds ) ;
      }
    }
  }

  public class FailurePreprocessor : IFailuresPreprocessor
  {
    public FailureProcessingResult PreprocessFailures( FailuresAccessor failuresAccessor )
    {
      var failureMessages = failuresAccessor.GetFailureMessages() ;
      foreach ( var message in failureMessages ) {
        if ( message.GetFailureDefinitionId() == BuiltInFailures.GroupFailures.AtomViolationWhenOnePlaceInstance )
          failuresAccessor.DeleteWarning( message ) ;
      }

      return FailureProcessingResult.Continue ;
    }
  }
}