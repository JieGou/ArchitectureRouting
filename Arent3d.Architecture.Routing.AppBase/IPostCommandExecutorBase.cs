using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase.Commands.PostCommands ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase
{
  public interface IPostCommandExecutorBase
  {
    void ChangeRouteNameCommand( Route route, string newName ) ;

    void ApplySelectedFromToChangesCommand( Route route, IReadOnlyCollection<SubRoute> subRoutes, RouteProperties properties ) ;

    void CreateSymbolContentTagCommand( Element element, XYZ point, string deviceSymbol ) ;

    bool LoadFamilyCommand( List<LoadFamilyCommandParameter> familyParameters ) ;

    void SaveCeedStorableAndStorageServiceCommand( CeedStorable ceedStorable, StorageService<Level, CeedUserModel> storageService ) ;
  }
}