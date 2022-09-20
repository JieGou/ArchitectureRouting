using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.PostCommands ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
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
    
    public void CreateSymbolContentTagCommand( CeedViewModel ceedViewModel )
    {
      if ( UiApp is not { } uiApp ) return ;

      uiApp.PostCommand<CreateSymbolContentTagCommand, SymbolContentTagCommandParameter>( new SymbolContentTagCommandParameter( ceedViewModel ) ) ;
    }
    
    public void LoadFamilyCommand( List<LoadFamilyCommandParameter> familyParameters )
    {
      if ( UiApp is not { } uiApp ) return ;

      uiApp.PostCommand<LoadFamilyCommand, List<LoadFamilyCommandParameter>>( familyParameters ) ;
    }
    
    public void SaveCeedStorableAndStorageServiceCommand( CeedStorable ceedStorable, StorageService<Level, CeedUserModel> storageService )
    {
      if ( UiApp is not { } uiApp ) return ;

      uiApp.PostCommand<SaveCeedStorableAndStorageServiceCommand, SaveCeedStorableAndStorageServiceCommandParameter>( new SaveCeedStorableAndStorageServiceCommandParameter( ceedStorable, storageService ) ) ;
    }
  }
}