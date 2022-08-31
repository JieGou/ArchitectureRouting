using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
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

      // get data of Cns Category from snoop DB
      CnsSettingStorable cnsStorables = UiDocument.Document.GetCnsSettingStorable() ;
      cnsStorables.ElementType = CnsSettingStorable.UpdateItemType.None ;
      var currentCnsSettingData = CnsSettingDialog.CopyCnsSetting( cnsStorables.CnsSettingData ) ;
      CnsSettingViewModel viewModel = new CnsSettingViewModel( cnsStorables ) ;
      var dialog = new CnsSettingDialog( viewModel, UiDocument.Document ) ;
      dialog.ShowDialog() ;
      if ( dialog.DialogResult ?? false ) {
        var isConnectorsHaveConstructionItem = dialog.IsConnectorsHaveConstructionItemProperty() ;
        var isConduitsHaveConstructionItem = dialog.IsConduitsHaveConstructionItemProperty() ;
        if ( cnsStorables.ElementType != CnsSettingStorable.UpdateItemType.None ) {
          try {
            if ( ( cnsStorables.ElementType == CnsSettingStorable.UpdateItemType.Connector && ! isConnectorsHaveConstructionItem ) || ( cnsStorables.ElementType == CnsSettingStorable.UpdateItemType.Conduit && ! isConduitsHaveConstructionItem ) ) {
              message = "Dialog.Electrical.SetElementProperty.NoConstructionItem".GetAppStringByKeyOrDefault( "The property Construction Item does not exist." ) ;
            }
            else {
              MessageBox.Show( "Dialog.Electrical.SelectElement.Message".GetAppStringByKeyOrDefault( "Please select a range." ), "Dialog.Electrical.SelectElement.Title".GetAppStringByKeyOrDefault( "Message" ), MessageBoxButtons.OK ) ;
            }

            switch ( cnsStorables.ElementType ) {
              case CnsSettingStorable.UpdateItemType.Conduit :
              {
                if ( ! isConduitsHaveConstructionItem ) break ;
                // pick conduits
                var selectedElements = UiDocument.Selection.PickElementsByRectangle( ConduitWithStartEndSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).Where( p => p is FamilyInstance or Conduit ) ;
                var conduitList = selectedElements.ToList() ;
                if ( ! conduitList.Any() ) {
                  message = "No Conduits are selected." ;
                  break ;
                }

                // set value to "Construction Item" property
                var categoryName = cnsStorables.CnsSettingData[ cnsStorables.SelectedIndex ].CategoryName ;
                var listApplyConduit = ConduitUtil.GetConduitRelated(UiDocument.Document, conduitList) ;
                using var transaction = new Transaction( UiDocument.Document ) ;
                transaction.Start( "Set conduits property" ) ;
                SetConstructionItemForElements( listApplyConduit, categoryName ) ;
                ConfirmUnsetCommandBase.ResetElementColor( conduitList ) ;
                transaction.Commit() ;

                break ;
              }
              case CnsSettingStorable.UpdateItemType.Connector :
              {
                if ( ! isConnectorsHaveConstructionItem ) break ;
                // pick connectors
                var connectorList = UiDocument.Selection.PickElementsByRectangle( ConnectorFamilySelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" )
                  .OfType<FamilyInstance>().EnumerateAll() ;
                var categoryName = viewModel.ApplyToSymbolsText ;
                if ( ! connectorList.Any() ) {
                  message = "No Connectors are selected." ;
                  break ;
                }
               
                using Transaction transaction = new Transaction( UiDocument.Document ) ;
                transaction.Start( "Change Parameter Value" ) ;
                
                SetConstructionItemForElements( connectorList, categoryName ) ;
                ConfirmUnsetCommandBase.ResetElementColor( connectorList ) ;
                
                transaction.Commit() ;

                break ;
              }
              case CnsSettingStorable.UpdateItemType.Rack :
              {
                var rackList = UiDocument.Selection
                  .PickElementsByRectangle( CableTraySelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" )
                  .Where( x => x is FamilyInstance  or CableTray).EnumerateAll() ;
                
                if ( ! rackList.Any() ) {
                  message = "No Racks are selected." ;
                  break ;
                }

                // set value to "Construction Item" property
                var categoryName = viewModel.ApplyToSymbolsText ;
                using Transaction transaction = new Transaction( UiDocument.Document ) ;
                transaction.Start( "Set rack property" ) ;

                SetConstructionItemForElements( rackList, categoryName ) ;
                ConfirmUnsetCommandBase.ResetElementColor( rackList ) ;

                transaction.Commit() ;

                break;
              }
              case CnsSettingStorable.UpdateItemType.All :
              {
                var elementList = UiDocument.Selection
                  .PickElementsByRectangle( ConstructionItemSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" )
                  .Where( x => x is FamilyInstance or CableTray or Conduit).ToList() ;
                
                if ( ! elementList.Any() ) {
                  message = "No Elements are selected." ;
                  break ;
                }

                // set value to "Construction Item" property
                var categoryName = viewModel.ApplyToSymbolsText ;
                using Transaction transaction = new Transaction( UiDocument.Document ) ;
                transaction.Start( "Set element property" ) ;
                
                var listApplyElement = new List<Element>() ;
                listApplyElement.AddRange( elementList.Where( x=>x is not Conduit) );
                listApplyElement.AddRange(  ConduitUtil.GetConduitRelated(UiDocument.Document, elementList) );
                SetConstructionItemForElements( listApplyElement, categoryName ) ;
                ConfirmUnsetCommandBase.ResetElementColor( listApplyElement ) ;
                
                transaction.Commit() ;
 
                break;
              }
              case CnsSettingStorable.UpdateItemType.Range :
              {
                var elementList = UiDocument.Selection
                  .PickElementsByRectangle( ConstructionItemSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" )
                  .Where( x => x is FamilyInstance or CableTray or Conduit).ToList() ;
                
                if ( ! elementList.Any() ) {
                  message = "No Elements are selected." ;
                  break ;
                }

                var constructionItemList = new ObservableCollection<string>(cnsStorables.CnsSettingData.Select( x=>x.CategoryName ).ToList()) ;
                var oldConstructionItemSelectedList = new List<CnsSettingApplyConstructionItem>() ;
                var itemIndex = 0 ;
                foreach ( var element in elementList ) {
                  itemIndex++ ;
                  element.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? oldConstructionItem ) ; 
                  oldConstructionItemSelectedList.Add( new CnsSettingApplyConstructionItem( element.UniqueId, itemIndex, oldConstructionItem ?? string.Empty, string.Empty, constructionItemList ) );
                }

                var cnsSettingApplyForRangeViewModel = new CnsSettingApplyForRangeViewModel( oldConstructionItemSelectedList, constructionItemList ) ;
                var cnsSettingApplyForRangeDialog = new CnsSettingApplyForRangeDialog( cnsSettingApplyForRangeViewModel ) ;
                var result = cnsSettingApplyForRangeDialog.ShowDialog() ;
                if ( result is null or false )
                  return Result.Cancelled;
                
                //Create dict mapping between old & new construction item 
                var dictMapping = cnsSettingApplyForRangeViewModel.MappingConstructionItems.Where( x => ! string.IsNullOrEmpty( x.NewConstructionItem ) ).ToDictionary( x => x.OldConstructionItem, x => x.NewConstructionItem ) ;
                if ( ! dictMapping.Any() ) break ;
  
                ApplyConstructionItem( UiDocument.Document, elementList, dictMapping ) ;
 
                //Add to CnsSetting Store if have new
                foreach ( var newValue in dictMapping.Values.Where( newValue => ! constructionItemList.Contains( newValue ) ) ) {
                  cnsStorables.CnsSettingData.Add( new CnsSettingModel( cnsStorables.CnsSettingData.Count + 1, newValue) );
                  constructionItemList.Add( newValue );
                } 
                break; 
              }
            }

            if ( string.IsNullOrEmpty( message ) ) {
              MessageBox.Show( "Dialog.Electrical.SetElementProperty.Success".GetAppStringByKeyOrDefault( "Success" ), "Dialog.Electrical.SetElementProperty.Title".GetAppStringByKeyOrDefault( "Construction item addition result" ), MessageBoxButtons.OK ) ;
            }
            else {
              MessageBox.Show( message, "Dialog.Electrical.SetElementProperty.Title".GetAppStringByKeyOrDefault( "Construction item addition result" ), MessageBoxButtons.OK ) ;
            }
          }
          catch {
            MessageBox.Show( "Dialog.Electrical.SetElementProperty.Failure".GetAppStringByKeyOrDefault( "Failed" ),
              "Dialog.Electrical.SetElementProperty.Title".GetAppStringByKeyOrDefault(
                "Construction item addition result" ), MessageBoxButtons.OK ) ;
          }
        }

        return UiDocument.Document.Transaction( "TransactionName.Commands.Routing.CnsSetting", _ =>
        {
          DataProcessBeforeSave( cnsStorables ) ;
          if ( ShouldSaveCnsList( UiDocument.Document, cnsStorables ) ) {
            // save CNS setting list
            using var progress = ProgressBar.ShowWithNewThread( commandData.Application ) ;
            progress.Message = "Saving CNS Setting..." ;
            using ( progress.Reserve( 0.5 ) ) {
              SaveCnsList( cnsStorables ) ;
              dialog.UpdateConstructionsItem() ;
            }
          }

          cnsStorables.ElementType = CnsSettingStorable.UpdateItemType.None ;
          return Result.Succeeded ;
        } ) ;
      }

      cnsStorables.CnsSettingData = currentCnsSettingData ;
      return Result.Succeeded ;
    }

    private void ApplyConstructionItem(Document document, List<Element> elementList, Dictionary<string, string> dictMapping)
    {
      // set value to "Construction Item" property
      using Transaction transaction = new Transaction( document ) ;
      transaction.Start( "Set Parameter Value" ) ;

      var listApplyElement = new List<Element>() ;
      listApplyElement.AddRange( elementList.Where( x=>x is not Conduit) );
      listApplyElement.AddRange(  ConduitUtil.GetConduitRelated(document, elementList) );
      foreach ( var element in listApplyElement ) {
        element.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? oldConstructionItem ) ;
        oldConstructionItem ??= string.Empty ;
        if(dictMapping.ContainsKey(oldConstructionItem))
          element.TrySetProperty( ElectricalRoutingElementParameter.ConstructionItem, dictMapping[oldConstructionItem] ) ; 
      } 

      transaction.Commit() ;
    }
    
    private static void SaveCnsList( CnsSettingStorable list )
    {
      list.Save() ;
    }

    private static bool ShouldSaveCnsList( Document document, CnsSettingStorable newSettings )
    {
      var old = document.GetAllStorables<CnsSettingStorable>()
        .FirstOrDefault() ; // generates new instance from document
      return old == null || newSettings != old ;
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

    private static void SetConstructionItemForElements( IEnumerable<Element> elements, string categoryName )
    {
      foreach ( var element in elements ) {
        if(element.HasParameter(ElectricalRoutingElementParameter.ConstructionItem))
          element.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, categoryName ) ;
      }
    }
  }
}
