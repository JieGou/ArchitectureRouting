using System.Windows ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.PostCommands
{
  public class SaveCeedStorableAndStorageServiceCommandParameter
  {
    public CeedStorable CeedStorable { get ; }
    public StorageService<Level, CeedUserModel> StorageService { get ; }

    public SaveCeedStorableAndStorageServiceCommandParameter( CeedStorable ceedStorable, StorageService<Level, CeedUserModel> storageService )
    {
      CeedStorable = ceedStorable ;
      StorageService = storageService ;
    }
  }
  
  public class SaveCeedStorableAndStorageServiceCommandBase : RoutingExternalAppCommandBaseWithParam<SaveCeedStorableAndStorageServiceCommandParameter>
  {
    protected override string GetTransactionName() => "TransactionName.Commands.PostCommands.SaveCeedStorableAndStorageServiceCommandBase".GetAppStringByKeyOrDefault( "Save Ceed Storable And Storage Service" ) ;

    protected override ExecutionResult Execute( SaveCeedStorableAndStorageServiceCommandParameter param, Document document, TransactionWrapper transaction )
    {
      try {
        param.CeedStorable.Save() ;
        param.StorageService.SaveChange() ;
        return ExecutionResult.Succeeded ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        MessageBox.Show( "Save storable failed.", "Error" ) ;
        return ExecutionResult.Cancelled ;
      }
    }
  }
}