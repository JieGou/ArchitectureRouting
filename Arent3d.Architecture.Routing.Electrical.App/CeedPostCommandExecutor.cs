using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.PostCommands ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.PostCommands ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App
{
  public class CeedPostCommandExecutor : IElectricalPostCommandExecutorBase
  {
    private static UIApplication? UiApp => RoutingApp.FromToTreeManager.UiApp ;
    
    public void CreateSymbolContentTagCommand( Element element, XYZ point, string deviceSymbol )
    {
      if ( UiApp is not { } uiApp ) return ;

      uiApp.PostCommand<CreateSymbolContentTagCommand, SymbolContentTagCommandParameter>( new SymbolContentTagCommandParameter( element, point, deviceSymbol ) ) ;
    }
    
    public bool LoadFamilyCommand( List<LoadFamilyCommandParameter> familyParameters )
    {
      return UiApp is { } uiApp && uiApp.PostCommand<LoadFamilyCommand, List<LoadFamilyCommandParameter>>( familyParameters ) ;
    }
    
    public void SaveCeedStorableAndStorageServiceCommand( CeedStorable ceedStorable, StorageService<Level, CeedUserModel> storageService )
    {
      if ( UiApp is not { } uiApp ) return ;

      uiApp.PostCommand<SaveCeedStorableAndStorageServiceCommand, SaveCeedStorableAndStorageServiceCommandParameter>( new SaveCeedStorableAndStorageServiceCommandParameter( ceedStorable, storageService ) ) ;
    }
  }
}