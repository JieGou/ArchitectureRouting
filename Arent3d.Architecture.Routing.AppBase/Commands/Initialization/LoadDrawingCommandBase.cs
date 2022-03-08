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
        List<ImportDwgMappingModel> mappings = new List<ImportDwgMappingModel>() ;
        int count = 0 ;
        foreach ( string file in openFileDialog.FileNames ) {
          count++ ;
          mappings.Add( new ImportDwgMappingModel( file, "Floor" + count ) );
        }
        var dialog = new ImportDwgMappingDialog( new ImportDwgMappingViewModel( mappings ) ) ;
        dialog.ShowDialog() ;
        if ( dialog.DialogResult ?? false ) {
          var data = dialog.DataContext ;
          SaveFileDialog saveFileDialog = new SaveFileDialog() { Filter = "Revit files (*.rvt )|*.rvt", Title = "Save an Revit File" };
          if ( saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
            var doc = commandData.Application.Application.NewProjectDocument( UnitSystem.Metric ) ;
            File.Delete( saveFileDialog.FileName ) ;
            doc.SaveAs( saveFileDialog.FileName ) ;
            commandData.Application.OpenAndActivateDocument( doc.PathName ) ;
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

        //doc.Save() ;
      }
      
      return Result.Succeeded ;
    }
  }
}