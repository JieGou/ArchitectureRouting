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
        var mappings = new List<ImportDwgMappingModel>()
        {
          new ImportDwgMappingModel( string.Empty, "B1FL" ),
          new ImportDwgMappingModel( string.Empty, "PH1F" ),
          new ImportDwgMappingModel( string.Empty, "PH1RFL" )
        } ;
        var fileItems = new List<FileComboboxItemType>() ;
        for ( int i = 1 ; i <= openFileDialog.FileNames.Length ; i++ ) {
          mappings.Add( new ImportDwgMappingModel( string.Empty, $"{i}F" ) );
          fileItems.Add( new FileComboboxItemType(openFileDialog.FileNames[i - 1]) );
        }
        var dialog = new ImportDwgMappingDialog( new ImportDwgMappingViewModel( mappings, fileItems ) ) ;
        dialog.ShowDialog() ;
        if ( dialog.DialogResult ?? false ) {
          var data = dialog.DataContext as ImportDwgMappingViewModel ;
          if(data == null || !data.ImportDwgMappingModels.Any()) return Result.Succeeded ;
          var importDwgMappingModels = data.ImportDwgMappingModels.Where( x =>
            ! string.IsNullOrEmpty( x.FloorName ) && ! string.IsNullOrEmpty( x.FileName ) ).ToList() ;
          if(!importDwgMappingModels.Any()) return Result.Succeeded ;
          foreach ( var importDwgMappingModel in importDwgMappingModels ) {
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
            DWGImportOptions dwgImportOptions = new DWGImportOptions
            {
              ColorMode = ImportColorMode.Preserved,
              CustomScale = 0.0,
              Unit = ImportUnit.Default,
              OrientToView = true,
              Placement = ImportPlacement.Origin,
              ThisViewOnly = true,
              VisibleLayersOnly = false
            } ;
            var firstMappingModel = importDwgMappingModels.First() ;
            var processFirstFloorTrans = new Transaction( doc ) ;
            processFirstFloorTrans.SetName("Import");    
            processFirstFloorTrans.Start();
            actView.Name = firstMappingModel.FloorName ;
            if(!string.IsNullOrEmpty(firstMappingModel.FullFilePath)) doc.Import( firstMappingModel.FullFilePath, dwgImportOptions, actView, out ElementId firstElementId ) ;
            processFirstFloorTrans.Commit();
            
            Level level = actView.GenLevel;
            var importTrans = new Transaction( doc ) ;
            importTrans.SetName("Import");
            importTrans.Start();
            for ( int i = 1 ; i < importDwgMappingModels.Count() ; i++ ) {
              var importDwgMappingModel = importDwgMappingModels[ i ] ;
              if ( ! string.IsNullOrEmpty( importDwgMappingModel.FullFilePath ) ) {
                var viewFamily = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().First(x => x.ViewFamily == ViewFamily.FloorPlan);
                var viewPlan = ViewPlan.Create(doc, viewFamily.Id , level.Id);
                doc.Import( importDwgMappingModel.FullFilePath, dwgImportOptions, viewPlan, out ElementId importElementId ) ;
                viewPlan.Name = importDwgMappingModel.FloorName;
              }
            }
            importTrans.Commit();
            doc.Save() ;
          }
        }
        /*var actView = doc.ActiveView;
        var renameTrans = new Transaction( doc ) ;
        renameTrans.SetName("Import");    
        renameTrans.Start();
        actView.Name = "TungLe" ;
        renameTrans.Commit();
        Level level = actView.GenLevel;
        var importTrans = new Transaction( doc ) ;
        importTrans.SetName("Import");
        importTrans.Start();
        int count = 0 ;
        foreach (string file in openFileDialog.FileNames) {
          count++ ;
          DWGImportOptions dwgImportOptions = new DWGImportOptions
          {
            ColorMode = ImportColorMode.Preserved,
            CustomScale = 0.0,
            Unit = ImportUnit.Default,
            OrientToView = true,
            Placement = ImportPlacement.Origin,
            ThisViewOnly = true,
            VisibleLayersOnly = false
          } ;
          var viewFamily = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().First(x => x.ViewFamily == ViewFamily.FloorPlan);
          var viewPlan = ViewPlan.Create(doc, viewFamily.Id , level.Id);
          doc.Import( file, dwgImportOptions, viewPlan, out ElementId importElementId ) ;
          viewPlan.Name = "Floor" + count;
        }
        importTrans.Commit();*/
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