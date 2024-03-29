﻿using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Autodesk.Revit.Attributes ;
using static Arent3d.Architecture.Routing.AppBase.Commands.Initialization.CreateDetailTableCommandBase ;
using static Arent3d.Architecture.Routing.AppBase.Commands.Initialization.ShowElectricSymbolsCommandBase ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Initialization.ShowDialogCreateTableByFloorCommand", DefaultString = "Create Table\nBy Floors" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class ShowDialogCreateTableByFloorCommand : IExternalCommand
  {
    private const string DetailTableType = "明細表" ;
    private const string ElectricalSymbolTableType = "電気シンボル表" ;
    private const string AllFloors = "全部の階" ;
    private static readonly string[] TableTypes = { DetailTableType, ElectricalSymbolTableType } ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var doc = uiDocument.Document ;

      var dialog = new CreateTableByFloors( uiDocument.Document, TableTypes ) ;
      dialog.ShowDialog() ;
      if ( ! dialog.DialogResult ?? true )
        return Result.Cancelled ;

      var tableType = dialog.SelectedTableType ;
      var levelIds = dialog.LevelList.Where( t => t.IsSelected ).Select( p => p.LevelId ).ToList() ;
      var allConduits = new FilteredElementCollector( doc ).OfClass( typeof( Conduit ) ).OfType<Conduit>().ToList() ;

      return tableType switch
      {
        DetailTableType => CreateDetailTable( doc, levelIds, allConduits, dialog.IsCreateTableEachFloors ),
        ElectricalSymbolTableType => CreateElectricalTable( doc, levelIds, allConduits, dialog.IsCreateTableEachFloors ),
        _ => Result.Failed
      } ;
    }

    private Result CreateDetailTable( Document doc, List<ElementId> levelIds, List<Conduit> allConduits, bool isCreateTableEachFloors )
    {
      if ( ! allConduits.Any() ) return Result.Cancelled ;
      var csvStorable = doc.GetCsvStorable() ;
      List<DetailTableItemModel> detailTableModelsAllFloors = new() ;
      try {
        return doc.Transaction( "TransactionName.Commands.Routing.ShowDialogCreateTableByFloorCommand".GetAppStringByKeyOrDefault( "Create detail table" ), _ =>
        {
          var message = string.Empty ;
          foreach ( var levelId in levelIds ) {
            var level = (Level) doc.GetElement( levelId ) ;
            var conduitsByFloor = allConduits.Where( x => x.ReferenceLevel.Id == levelId ).ToList() ;
            var elementsByFloor = conduitsByFloor.Cast<Element>().ToList() ;
            var conduitsByFloorIds = conduitsByFloor.Select( p => p.UniqueId ).ToList() ;
            var storageService = new StorageService<Level, DetailSymbolModel>( level ) ;
            var (detailTableItemModels, _, _) = CreateDetailTableItem( doc, csvStorable, storageService, elementsByFloor, conduitsByFloorIds, false ) ;
            if ( ! detailTableItemModels.Any() ) continue ;
            if ( isCreateTableEachFloors ) {
              var floorName = detailTableItemModels.FirstOrDefault( d => ! string.IsNullOrEmpty( d.Floor ) )?.Floor ?? string.Empty ;
              var scheduleName = CreateDetailTableSchedule( doc, detailTableItemModels, floorName ) ;
              message += string.Format( "Revit.Electrical.CreateSchedule.Message".GetAppStringByKeyOrDefault( CreateScheduleSuccessfullyMessage ), scheduleName ) + "\n" ;
            }
            
            SaveDetailTableData( detailTableModelsAllFloors, level ) ;
            detailTableModelsAllFloors.AddRange( detailTableItemModels ) ;
          }

          if ( ! isCreateTableEachFloors ) {
            var scheduleName = CreateDetailTableSchedule( doc, detailTableModelsAllFloors, AllFloors ) ;
            message = string.Format( "Revit.Electrical.CreateSchedule.Message".GetAppStringByKeyOrDefault( CreateScheduleSuccessfullyMessage ), scheduleName ) ;
          }
          MessageBox.Show( message, "Message" ) ;
          return Result.Succeeded ;
        } ) ;
      }
      catch {
        return Result.Cancelled ;
      }
    }

    private Result CreateElectricalTable( Document doc, List<ElementId> levelIds, List<Conduit> allConduits, bool isCreateTableEachFloors )
    {
      if ( ! allConduits.Any() ) return Result.Cancelled ;
      var ceedStorable = doc.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      List<ElectricalSymbolModel> electricalTableOfAllFloors = new() ;
      try {
        return doc.Transaction( "TransactionName.Commands.Routing.ShowDialogCreateTableByFloorCommand".GetAppStringByKeyOrDefault( "Create electrical symbol table" ), _ =>
        {
          var message = string.Empty ;
          foreach ( var levelId in levelIds ) {
            List<ElectricalSymbolModel> electricalSymbolModels = new() ;
            var conduitsByFloor = allConduits.Where( x => x.ReferenceLevel.Id == levelId ).ToList() ;
            var elementsByFloor = conduitsByFloor.Cast<Element>().ToList() ;
            CreateElectricalSymbolModels( doc, ceedStorable!, electricalSymbolModels, elementsByFloor ) ;
            if ( ! electricalSymbolModels.Any() ) continue ;
            if ( isCreateTableEachFloors ) {
              var level = doc.GetElement( levelId ).Name ;
              var scheduleName = CreateElectricalSchedule( doc, electricalSymbolModels, level ) ;
              message += string.Format( "Revit.Electrical.CreateSchedule.Message".GetAppStringByKeyOrDefault( CreateScheduleSuccessfullyMessage ), scheduleName ) + "\n" ;
            }
            else {
              electricalTableOfAllFloors.AddRange( electricalSymbolModels ) ;
            }
          }

          if ( ! isCreateTableEachFloors && electricalTableOfAllFloors.Any() ) {
            var scheduleName = CreateElectricalSchedule( doc, electricalTableOfAllFloors, AllFloors ) ;
            message = string.Format( "Revit.Electrical.CreateSchedule.Message".GetAppStringByKeyOrDefault( CreateScheduleSuccessfullyMessage ), scheduleName ) ;
          }

          MessageBox.Show( message, "Message" ) ;
          return Result.Succeeded ;
        } ) ;
      }
      catch {
        return Result.Cancelled ;
      }
    }

    private void SaveDetailTableData( IReadOnlyCollection<DetailTableItemModel> detailTableItemModels, Level level )
    {
      try {
        var storageService = new StorageService<Level, DetailTableModel>( level ) ;
        if ( ! detailTableItemModels.Any() )
          return ;
        
        var existedKeys = storageService.Data.DetailTableData.Select( GetKeyRouting ).Distinct().ToList() ;
        var itemNotInDb = detailTableItemModels.Where( d => ! existedKeys.Contains( GetKeyRouting(d) ) ).ToList() ;
        
        if ( itemNotInDb.Any() ) 
          storageService.Data.DetailTableData.AddRange( itemNotInDb ) ;
        storageService.SaveChange() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }
  }
}