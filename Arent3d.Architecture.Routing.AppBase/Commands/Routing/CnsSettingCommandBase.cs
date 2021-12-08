using System.Collections.Generic ;
using System.Linq ;
using System.Threading ;
using System.Windows.Forms ;
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

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class CnsSettingCommandBase : IExternalCommand
  {
    protected UIDocument UiDocument { get ; private set ; } = null! ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      UiDocument = commandData.Application.ActiveUIDocument ;
      Document document = UiDocument.Document ;

      // get data of Cns Category from snoop DB
      CnsSettingStorable cnsStorables = document.GetCnsSettingStorable() ;
      CnsSettingViewModel viewModel = new CnsSettingViewModel( cnsStorables ) ;
      var dialog = new CnsSettingDialog( viewModel ) ;

      dialog.ShowDialog() ;
      if ( dialog.DialogResult ?? false ) {
        Dictionary<ElementId, List<ElementId>> connectorGroups = new Dictionary<ElementId, List<ElementId>>() ;

        if ( cnsStorables.ElementType != CnsSettingStorable.UpdateItemType.None ) {
          try {
            MessageBox.Show(
              "Dialog.Electrical.SelectElement.Message".GetAppStringByKeyOrDefault( "Please select a range." ),
              "Dialog.Electrical.SelectElement.Title".GetAppStringByKeyOrDefault( "Message" ), MessageBoxButtons.OK ) ;
            string returnMessage = string.Empty ;

            if ( cnsStorables.ElementType == CnsSettingStorable.UpdateItemType.Conduit ) {
              // pick conduits
              var selectedElements = UiDocument.Selection
                .PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" )
                .Where( p => p is FamilyInstance or Conduit ) ;
              var listConduit = selectedElements.ToList() ;
              if ( ! listConduit.Any() ) {
                message = "No Conduits are selected." ;
                return Result.Cancelled ;
              }
              else {
                // set value to "Construction Item" property
                var categoryName = cnsStorables.CnsSettingData[ cnsStorables.SelectedIndex ].CategoryName ;
                using Transaction transaction = new Transaction( document ) ;
                transaction.Start( "Set conduits property" ) ;

                SetConstructionItemForConduit( listConduit.ToList(), categoryName ) ;

                transaction.Commit() ;
              }
            }
            else if ( cnsStorables.ElementType == CnsSettingStorable.UpdateItemType.Connector ) {
              // pick connectors
              var selectedElements = UiDocument.Selection
                .PickElementsByRectangle( ConnectorFamilySelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" )
                .Where( x => x is FamilyInstance or TextNote ) ;
              var listConnector = selectedElements.ToList() ;
              var categoryName = viewModel.ApplyToSymbolsText ;
              if ( ! listConnector.Any() ) {
                message = "No Connectors are selected." ;
                return Result.Cancelled ;
              }
              else {
                using Transaction transaction = new Transaction( document ) ;
                transaction.Start( "Ungroup members and set connectors property" ) ;

                foreach ( var connector in listConnector ) {
                  var parentGroup = document.GetElement( connector.GroupId ) as Group ;
                  if ( parentGroup != null ) {
                    // ungroup before set property
                    var attachedGroup = document.GetAllElements<Group>()
                      .Where( x => x.AttachedParentId == parentGroup.Id ) ;
                    List<ElementId> listTextNoteIds = new List<ElementId>() ;
                    // ungroup textNote before ungroup connector
                    foreach ( var group in attachedGroup ) {
                      var ids = group.GetMemberIds() ;
                      listTextNoteIds.AddRange( ids ) ;
                      group.UngroupMembers() ;
                    }

                    connectorGroups.Add( connector.Id, listTextNoteIds ) ;
                    parentGroup.UngroupMembers() ;
                  }

                  if ( connector is FamilyInstance ) {
                    // set value to "Construction Classification" property
                    SetConstructionClassificationForConnector( connector, categoryName ) ;
                  }
                }

                transaction.Commit() ;
              }
            }

            MessageBox.Show( "Dialog.Electrical.SetElementProperty.Success".GetAppStringByKeyOrDefault( "Success" ),
              "Dialog.Electrical.SetElementProperty.Title".GetAppStringByKeyOrDefault(
                "Construction item addition result" ), MessageBoxButtons.OK ) ;
          }
          catch {
            MessageBox.Show( "Dialog.Electrical.SetElementProperty.Failure".GetAppStringByKeyOrDefault( "Failed" ),
              "Dialog.Electrical.SetElementProperty.Title".GetAppStringByKeyOrDefault(
                "Construction item addition result" ), MessageBoxButtons.OK ) ;
            return Result.Cancelled ;
          }
        }

        return document.Transaction( "TransactionName.Commands.Routing.CnsSetting", _ =>
        {
          DataProcessBeforeSave( cnsStorables ) ;
          if ( ShouldSaveCnsList( document, cnsStorables ) ) {
            // save CNS setting list
            var tokenSource = new CancellationTokenSource() ;
            using var progress = ProgressBar.ShowWithNewThread( tokenSource ) ;
            progress.Message = "Saving CNS Setting..." ;
            using ( progress?.Reserve( 0.5 ) ) {
              SaveCnsList( document, cnsStorables ) ;
            }
          }

          if ( cnsStorables.ElementType != CnsSettingStorable.UpdateItemType.None &&
               cnsStorables.ElementType == CnsSettingStorable.UpdateItemType.Connector ) {
            foreach ( var item in connectorGroups ) {
              // create group for updated connector (with new property) and related text note
              List<ElementId> groupIds = new List<ElementId>() ;
              groupIds.Add( item.Key ) ;
              groupIds.AddRange( item.Value ) ;
              document.Create.NewGroup( groupIds ) ;
            }
          }

          cnsStorables.ElementType = CnsSettingStorable.UpdateItemType.None ;
          return Result.Succeeded ;
        } ) ;
      }

      return Result.Succeeded ;
    }

    private static void SaveCnsList( Document document, CnsSettingStorable list )
    {
      list.Save() ;
    }

    private static bool ShouldSaveCnsList( Document document, CnsSettingStorable newSettings )
    {
      var old = document.GetAllStorables<CnsSettingStorable>()
        .FirstOrDefault() ; // generates new instance from document
      return old == null || ! newSettings.Equals( old ) ;
    }

    private static void DataProcessBeforeSave( CnsSettingStorable cnsSettings )
    {
      bool hadUpdating = false ;
      // Remove empty row
      foreach ( var item in cnsSettings.CnsSettingData.ToList() ) {
        if ( string.IsNullOrWhiteSpace( item.CategoryName.Trim() ) ) {
          cnsSettings.CnsSettingData.Remove( item ) ;
          hadUpdating = true ;
        }
      }

      if ( cnsSettings.CnsSettingData.Count == 0 ) {
        // Add default value if list empty
        cnsSettings.CnsSettingData.Add( new CnsSettingModel( sequence: 1, categoryName: "未設定" ) ) ;
      }
      else if ( hadUpdating ) {
        // Set sequence if list was changed
        for ( int i = 0 ; i < cnsSettings.CnsSettingData.Count ; i++ ) {
          cnsSettings.CnsSettingData[ i ].Sequence = i + 1 ;
        }
      }
    }

    private static void SetConstructionClassificationForConnector( Element element, string categoryName )
    {
      element.SetProperty( RoutingFamilyLinkedParameter.ConstructionClassification, categoryName ) ;
    }

    private static void SetConstructionItemForConduit( List<Element> elements, string categoryName )
    {
      foreach ( var conduit in elements ) {
        conduit.SetProperty( RoutingFamilyLinkedParameter.ConstructionItem, categoryName ) ;
      }
    }
  }
}
