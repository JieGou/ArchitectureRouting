using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase.Commands.PostCommands ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase
{
  public interface IElectricalPostCommandExecutorBase
  {
    void CreateSymbolContentTagCommand( CeedViewModel ceedViewModel ) ;

    void LoadFamilyCommand( List<LoadFamilyCommandParameter> familyParameters ) ;

    void SaveCeedStorableAndStorageServiceCommand( CeedStorable ceedStorable, StorageService<Level, CeedUserModel> storageService ) ;
  }
}