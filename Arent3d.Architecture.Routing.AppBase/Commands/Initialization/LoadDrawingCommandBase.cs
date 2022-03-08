using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class LoadDrawingCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "DWG files (*.dwg )|*.dwg", Multiselect = true } ;
      if ( openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
        var importDwgMappingModels = new List<ImportDwgMappingModel>()
        {
          new ImportDwgMappingModel( string.Empty, "B1FL", 3000 ),
          new ImportDwgMappingModel( string.Empty, "PH1F", 3000  ),
          new ImportDwgMappingModel( string.Empty, "PH1RFL", 3000  )
        } ;
        var fileItems = new List<FileComboboxItemType>() ;
        for ( int i = 1 ; i <= openFileDialog.FileNames.Length ; i++ ) {
          importDwgMappingModels.Add( new ImportDwgMappingModel( string.Empty, $"{i}F", 3000  ) );
          fileItems.Add( new FileComboboxItemType(openFileDialog.FileNames[i - 1]) );
        }
        var dialog = new ImportDwgMappingDialog( new ImportDwgMappingViewModel( importDwgMappingModels, fileItems ) ) ;
        dialog.ShowDialog() ;
        if ( dialog.DialogResult ?? false ) {
          var importDwgMappingViewModel = dialog.DataContext as ImportDwgMappingViewModel ;
          if(importDwgMappingViewModel == null || !importDwgMappingViewModel.ImportDwgMappingModels.Any()) return Result.Succeeded ;
          var completeImportDwgMappingModels = importDwgMappingViewModel.ImportDwgMappingModels.Where( x =>
            ! string.IsNullOrEmpty( x.FloorName ) && ! string.IsNullOrEmpty( x.FileName ) ).ToList() ;
          if(!completeImportDwgMappingModels.Any()) return Result.Succeeded ;
          foreach ( var importDwgMappingModel in completeImportDwgMappingModels ) {
            var fileItem = fileItems.FirstOrDefault( x => x.FileName.Equals( importDwgMappingModel.FileName ) ) ;
            importDwgMappingModel.FullFilePath = fileItem != null ? fileItem.FullFilePath : "" ;
          }
          SaveFileDialog saveFileDialog = new SaveFileDialog() { Filter = "Revit files (*.rvt )|*.rvt", Title = "Save an Revit File" };
          if ( saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
            var doc = commandData.Application.Application.NewProjectDocument( UnitSystem.Metric ) ;
            File.Delete( saveFileDialog.FileName ) ;
            doc.SaveAs( saveFileDialog.FileName ) ;
            commandData.Application.OpenAndActivateDocument( doc.PathName ) ;
            
            var actView = doc.ActiveView;
            var dwgImportOptions = new DWGImportOptions
            {
              ColorMode = ImportColorMode.Preserved,
              CustomScale = 0.0,
              Unit = ImportUnit.Default,
              OrientToView = true,
              Placement = ImportPlacement.Origin,
              ThisViewOnly = false,
              VisibleLayersOnly = false
            } ;
            var firstMappingModel = completeImportDwgMappingModels.First() ;
            var processFirstFloorTrans = new Transaction( doc ) ;
            processFirstFloorTrans.SetName("Import");    
            processFirstFloorTrans.Start();
            Level firstFloorLevel = actView.GenLevel;
            firstFloorLevel.Elevation = firstMappingModel.FloorHeight ;
            firstFloorLevel.Name = "Level " + firstMappingModel.FloorName ;
            actView.Name = firstMappingModel.FloorName ;
            if(!string.IsNullOrEmpty(firstMappingModel.FullFilePath)) doc.Import( firstMappingModel.FullFilePath, dwgImportOptions, actView, out ElementId firstElementId ) ;
            processFirstFloorTrans.Commit();
            
            double levelElevation = actView.GenLevel.Elevation ;
            var importTrans = new Transaction( doc ) ;
            importTrans.SetName("Import");
            importTrans.Start();
            for ( int i = 1 ; i < completeImportDwgMappingModels.Count() ; i++ ) {
              var importDwgMappingModel = completeImportDwgMappingModels[ i ] ;
              if ( ! string.IsNullOrEmpty( importDwgMappingModel.FullFilePath ) ) {
                var viewFamily = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().First(x => x.ViewFamily == ViewFamily.FloorPlan);
                levelElevation += importDwgMappingModel.FloorHeight ;
                var importDwgLevel = Level.Create(doc, levelElevation) ;
                importDwgLevel.Name = "Level " + importDwgMappingModel.FloorName ;
                var viewPlan = ViewPlan.Create(doc, viewFamily.Id , importDwgLevel.Id);
                doc.Import( importDwgMappingModel.FullFilePath, dwgImportOptions, viewPlan, out ElementId importElementId ) ;
                viewPlan.Name = importDwgMappingModel.FloorName;
              }
            }
            importTrans.Commit();
            doc.Save() ;
          }
        }
      }
      
      return Result.Succeeded ;
    }
  }
  
  public class FileComboboxItemType
  {
    public string FullFilePath { get ; set ; }
    public string FileName { get ; set ; }

    public FileComboboxItemType( string fullFilePath )
    {
      FullFilePath = fullFilePath ;
      FileName = Path.GetFileName(fullFilePath)  ;
    }
  }
}