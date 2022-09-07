using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class ShowPickUpInformationCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      ResetSymbolInformationData( document ) ;

      var level = GetLevel( document ) ;
      var vm = new EquipmentCategoryViewModel( document, level ) ;
      var view = new EquipmentCategoryDialog() { DataContext = vm } ;
      view.ShowDialog() ;
      if ( !(view.DialogResult ?? false) ) {
        return Result.Cancelled ;
      }

      var version = vm.SelectedPickUpVersion == EquipmentCategoryViewModel.LatestVersion ? null : vm.SelectedPickUpVersion ;
      var pickUpViewModel = new PickUpViewModel( document, level, version, vm.SelectedEquipmentCategory ) ;
      var pickUpDialog = new PickupDialog( pickUpViewModel ) ;
      if(!pickUpViewModel.OriginPickUpModels.Any())
        return Result.Cancelled ;
      
      pickUpDialog.ShowDialog() ;
      if ( pickUpDialog.DialogResult ?? false ) {
        return Result.Succeeded ;
      }
      else {
        return Result.Cancelled ;
      }
    }
    
    /// <summary>
    /// Delete all symbol info that has been deleted.
    /// </summary>
    /// <param name="document"></param>
    private static void ResetSymbolInformationData( Document document )
    {
      var symbolInformationStorable = document.GetSymbolInformationStorable() ;
      var ceedDetailStorable = document.GetCeedDetailStorable() ;
      var symbolInformations = symbolInformationStorable.AllSymbolInformationModelData ;
      var ceedDetails = ceedDetailStorable.AllCeedDetailModelData ;

      var deleteSymbolInformations = symbolInformations.Where( x => document.GetElement( x.SymbolUniqueId ) is not FamilyInstance ).Select( x => x.SymbolUniqueId ).EnumerateAll() ;
      if ( ! deleteSymbolInformations.Any() ) 
        return ;

      using Transaction t = new(document, "Update Storage") ;
      t.Start() ;
      
      symbolInformations.RemoveAll( x => deleteSymbolInformations.Contains( x.SymbolUniqueId ) ) ;
      ceedDetails.RemoveAll( x => deleteSymbolInformations.Contains( x.ParentId ) ) ;
      symbolInformationStorable.AllSymbolInformationModelData = symbolInformations ;
      ceedDetailStorable.AllCeedDetailModelData = ceedDetails ;
      symbolInformationStorable.Save() ;
      ceedDetailStorable.Save() ;
      
      t.Commit() ;
    }
    
    protected virtual Level? GetLevel( Document document ) => null ;
  }
}