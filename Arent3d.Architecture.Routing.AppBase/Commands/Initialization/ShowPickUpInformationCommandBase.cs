﻿using System.Collections.Generic ;
using System.Globalization ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
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
    private void ResetSymbolInformationData( Document document )
    {
      var symbolInformationStorable = document.GetSymbolInformationStorable() ;
      var ceedDetailStorable = document.GetCeedDetailStorable() ;
      var symbolInformations = symbolInformationStorable.AllSymbolInformationModelData ;
      var ceedDetails = ceedDetailStorable.AllCeedDetailModelData ;
      var listGroup = new FilteredElementCollector( document ).OfClass( typeof( Group ) ).Cast<Group>().ToList() ;
       
      List<string> listSymbolInforDel = new() ;
      foreach ( var symbolInformation in symbolInformations ) {
        if ( listGroup.All( x => null == Enumerable.FirstOrDefault<ElementId>( x.GetMemberIds(), y => y.ToString() == symbolInformation.Id ) ) ) {
          listSymbolInforDel.Add( symbolInformation.Id ) ; 
        } 
      }

      if ( ! listSymbolInforDel.Any() ) return ;

      using Transaction t = new(document, "Delete symbol infos that have been deleted") ;
      t.Start() ;
      symbolInformations.RemoveAll( x => listSymbolInforDel.Contains( x.Id ) ) ;
      ceedDetails.RemoveAll( x => listSymbolInforDel.Contains( x.ParentId ) ) ;
      symbolInformationStorable.AllSymbolInformationModelData = symbolInformations ;
      ceedDetailStorable.AllCeedDetailModelData = ceedDetails ;
      symbolInformationStorable.Save() ;
      ceedDetailStorable.Save() ;
      t.Commit() ;
    }
    
    protected virtual Level? GetLevel( Document document ) => null ;
  }
}