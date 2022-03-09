using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Revit ;
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
          new ImportDwgMappingModel( string.Empty, "B1FL", 0 ),
          new ImportDwgMappingModel( string.Empty, "PH1F", 51200  ),
          new ImportDwgMappingModel( string.Empty, "PH1RFL", 54600  )
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
          Document doc = commandData.Application.ActiveUIDocument.Document ;
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
          /*var firstMappingModel = completeImportDwgMappingModels.First() ;
          var processFirstFloorTrans = new Transaction( doc ) ;
          processFirstFloorTrans.SetName("Import");    
          processFirstFloorTrans.Start();
          Level firstFloorLevel = actView.GenLevel;
          firstFloorLevel.Elevation = firstMappingModel.FloorHeight ;
          firstFloorLevel.Name = "Level " + firstMappingModel.FloorName ;
          actView.Name = firstMappingModel.FloorName ;
          if(!string.IsNullOrEmpty(firstMappingModel.FullFilePath)) doc.Import( firstMappingModel.FullFilePath, dwgImportOptions, actView, out ElementId firstElementId ) ;
          processFirstFloorTrans.Commit();*/

          var viewFamily = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().First(x => x.ViewFamily == ViewFamily.FloorPlan);
          var allCurrentLevels = new FilteredElementCollector( doc ).OfClass( typeof( Level ) ).ToList() ;
          var allCurrentViewPlans = new FilteredElementCollector( doc ).OfClass( typeof( ViewPlan ) ).ToList() ;
          var importTrans = new Transaction( doc ) ;
          importTrans.SetName("Import");
          importTrans.Start();
          for ( int i = 0 ; i < completeImportDwgMappingModels.Count() ; i++ ) {
            var importDwgMappingModel = completeImportDwgMappingModels[ i ] ;
            if ( ! string.IsNullOrEmpty( importDwgMappingModel.FullFilePath ) ) {
              var levelName = "Level " + importDwgMappingModel.FloorName ;
              var importDwgLevel = allCurrentLevels.FirstOrDefault( x => x.Name.Equals( levelName ) ) ;
              if ( importDwgLevel == null ) {
                importDwgLevel = Level.Create( doc, importDwgMappingModel.FloorHeight ) ;
                importDwgLevel.Name =  levelName;
              }
              var viewPlan = allCurrentViewPlans.FirstOrDefault( x => x.Name.Equals( importDwgMappingModel.FloorName ) ) as ViewPlan;
              if ( viewPlan == null ) {
                viewPlan = ViewPlan.Create(doc, viewFamily.Id , importDwgLevel.Id);
                viewPlan.Name = importDwgMappingModel.FloorName;
              }
              /*var columnSymbolicOffset = viewPlan.get_Parameter( BuiltInParameter.VIEW_SLANTED_COLUMN_SYMBOL_OFFSET ).AsValueString();
              importDwgLevel.SetProperty( BuiltInParameter.LEVEL_ELEV, importDwgMappingModel.FloorHeight / Double.Parse(columnSymbolicOffset) );*/
              doc.Import( importDwgMappingModel.FullFilePath, dwgImportOptions, viewPlan, out ElementId importElementId ) ;
            }
          }
          importTrans.Commit();
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