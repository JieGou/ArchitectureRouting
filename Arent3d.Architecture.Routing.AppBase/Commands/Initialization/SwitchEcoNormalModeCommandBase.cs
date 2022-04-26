using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.DB.Electrical ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class SwitchEcoNormalModeCommandBase : IExternalCommand
  {
    private const string TransactionName = "Electrical.App.Commands.Initialization.SwitchEcoNormalModeCommand" ;
    private const string SetDefaultEcoModeTransactionName = "Electrical.App.Commands.Initialization.SetDefaultEcoModeCommand" ;
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
        var document = commandData.Application.ActiveUIDocument.Document ;
        var viewModel = new SwitchEcoNormalModeViewModel() ;
        var dialog = new SwitchEcoNormalModeDialog( viewModel ) ;
        dialog.ShowDialog() ;
        {
          if ( dialog.DialogResult == false )
            return Result.Cancelled ;

          bool isEcoMode = viewModel.SelectedEcoNormalModeItem == SwitchEcoNormalModeViewModel.EcoNormalMode.EcoMode ;
          switch ( viewModel.SwitchEcoDialogResult ) {
            case SwitchEcoNormalModeViewModel.SwitchEcoDialogEnumResult.ApplyRange :
              return SwitchModeForRange( commandData, ref message, isEcoMode ) ;
            case SwitchEcoNormalModeViewModel.SwitchEcoDialogEnumResult.SetDefault :
              return SetEcoModeDefaultValue( commandData, ref message, isEcoMode ) ;
            case SwitchEcoNormalModeViewModel.SwitchEcoDialogEnumResult.ApplyAllProject :
              return SwitchModeForProject( document, ref message, isEcoMode ) ;
          }

          return Result.Cancelled ;
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

    private IList<Element> GetAllConduitInProject( Document document )
    {
      var familyInstances = new FilteredElementCollector( document ).OfClass( typeof( FamilyInstance ) ).ToElements().ToList() ;
      var conduits = new FilteredElementCollector( document ).OfClass( typeof( Conduit ) ).ToElements().ToList() ;
      var allConduits = familyInstances.Concat( conduits ).ToList() ;
      allConduits = allConduits.Where( elem => BuiltInCategorySets.ConnectorsAndConduits.Contains( elem.GetBuiltInCategory() ) ).ToList() ;
      var listApplyConduit = ConduitUtil.GetConduitRelated( document, allConduits ) ;
      return listApplyConduit ;
    }

    private IList<Element> GetAllConnectorInProject( Document document )
    {
      var familyInstances = new FilteredElementCollector( document ).OfClass( typeof( FamilyInstance ) ).ToElements().ToList() ;
      var textNotes = new FilteredElementCollector( document ).OfClass( typeof( TextNote ) ).ToElements().ToList() ;
      var connectorList = familyInstances.Concat( textNotes ).ToList() ;
      connectorList = connectorList.Where( elem => elem.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalFixtures || elem.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalEquipment ).ToList() ;
      return connectorList ;
    }

    private Result SwitchModeForProject( Document document, ref string message, bool isEcoMode )
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

      ShowResult( message ) ;
      return Result.Succeeded ;
    }

    private Result SwitchModeForRange( ExternalCommandData commandData, ref string message, bool isEcoMode )
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

      ShowResult( message ) ;
      return Result.Succeeded ;
    }

    private void SetModeForConduit( ICollection<Element> elements, bool isEcoMode )
    {
      if ( elements.Count == 0 ) return ;
      foreach ( var conduit in elements )
        conduit.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, isEcoMode.ToString() ) ;
    }

    private void SetModeForConnector( ICollection<Element> elements, bool isEcoMode, Document document )
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

    /// <summary>
    /// Set default value for isEcoMode
    /// </summary>
    /// <param name="commandData"></param>
    /// <param name="message"></param>
    /// <param name="isEcoModel"></param>
    /// <returns></returns>
    private Result SetEcoModeDefaultValue( ExternalCommandData commandData, ref string message, bool isEcoModel )
    {
      try {
        var document = commandData.Application.ActiveUIDocument.Document ;

        // Get data of eco setting from snoop DB
        EcoSettingStorable ecoSettingStorable = document.GetEcoSettingStorable() ;

        Transaction transaction = new Transaction( document, SetDefaultEcoModeTransactionName ) ;
        transaction.Start() ;
        ecoSettingStorable.EcoSettingData.IsEcoMode = isEcoModel ;
        ecoSettingStorable.Save() ;
        transaction.Commit() ;

        ShowResult( message ) ;
        return Result.Succeeded ;
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException( exception ) ;
        return Result.Cancelled ;
      }
    }

    private void ShowResult( string message )
    {
      MessageBox.Show( string.IsNullOrEmpty( message ) ? DialogResultSuccessKey.GetAppStringByKeyOrDefault( UpdateDataSuccessMessage ) : message, DialogResultTitleKey.GetAppStringByKeyOrDefault( ElectricalChangeModeTitle ), MessageBoxButtons.OK ) ;
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