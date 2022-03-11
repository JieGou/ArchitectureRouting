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
          new ImportDwgMappingModel( string.Empty, "PH1F", 0  ),
          new ImportDwgMappingModel( string.Empty, "PH1RFL", 0  )
        } ;
        var fileItems = new List<FileComboboxItemType>() ;
        for ( int i = 1 ; i <= openFileDialog.FileNames.Length ; i++ ) {
          importDwgMappingModels.Add( new ImportDwgMappingModel( string.Empty, $"{i}F", 0  ) );
          fileItems.Add( new FileComboboxItemType(openFileDialog.FileNames[i - 1]) );
        }

        UpdateDefaultFloorHeight( ref importDwgMappingModels ) ;
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
          var viewFamily = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().First(x => x.ViewFamily == ViewFamily.FloorPlan);
          var allCurrentLevels = new FilteredElementCollector( doc ).OfClass( typeof( Level ) ).ToList() ;
          var allCurrentViewPlans = new FilteredElementCollector( doc ).OfClass( typeof( ViewPlan ) ).ToList() ;
          ViewPlan? firstViewPlan = null ;
          #region  Import
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
              importDwgLevel.SetProperty( BuiltInParameter.LEVEL_ELEV, importDwgMappingModel.FloorHeight.MillimetersToRevitUnits());
              doc.Import( importDwgMappingModel.FullFilePath, dwgImportOptions, viewPlan, out ElementId importElementId ) ;
              if ( i == 0 ) firstViewPlan = viewPlan ;
            }
          }
          importTrans.Commit();
          #endregion

          #region Create 3D view

          if(firstViewPlan != null) commandData.Application.ActiveUIDocument.ActiveView = firstViewPlan;
          var create3DTrans = new Transaction( doc ) ;
          create3DTrans.SetName("Import");
          create3DTrans.Start();
          var threeDimensionalViewFamilyType = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).ToElements()
            .Cast<ViewFamilyType>().FirstOrDefault(vft => vft.ViewFamily == ViewFamily.ThreeDimensional);
          if ( threeDimensionalViewFamilyType != null ) {
            var allCurrent3DView = new FilteredElementCollector( doc ).OfClass( typeof( View3D ) ).ToList() ;
            string view3DName = "3D ALL" ;
            var current3DView = allCurrent3DView.FirstOrDefault( x => x.Name.Equals( view3DName ) ) ;
            if(current3DView != null) doc.Delete( current3DView.Id ) ;
            current3DView = View3D.CreateIsometric(doc, threeDimensionalViewFamilyType.Id);
            current3DView.Name = view3DName ;
          }

          create3DTrans.Commit() ;

          #endregion

        }
      }
      
      return Result.Succeeded ;
    }

    private void UpdateDefaultFloorHeight( ref List<ImportDwgMappingModel> importDwgMappingModels )
    {
      Dictionary<string, double> defaultHeights = new Dictionary<string, double>()
      {
        { "1F", 4200 },
        { "2F", 9200 },
        { "3F", 13900 },
        { "4F", 18300 },
        { "5F", 22700 },
        { "6F", 27100 },
        { "7F", 31500 },
        { "8F", 35900 },
        { "9F", 40300 },
        { "10F", 44700 }
      } ;
      foreach ( var importDwgMappingModel in importDwgMappingModels ) {
        var defaultFloorHeight = defaultHeights.FirstOrDefault( x => x.Key.Equals( importDwgMappingModel.FloorName ) ) ;
        if ( defaultFloorHeight.Key != null ) importDwgMappingModel.FloorHeight = defaultFloorHeight.Value ;
      }
      var maxFloorHeight = importDwgMappingModels.Max( x => x.FloorHeight ) ;
      var pH1FFloor = importDwgMappingModels.FirstOrDefault( x => x.FloorName.Equals( "PH1F" ) ) ;
      if ( pH1FFloor != null ) {
        pH1FFloor.FloorHeight = maxFloorHeight + 6500 ;
        var pH1RFLFloor = importDwgMappingModels.FirstOrDefault( x => x.FloorName.Equals( "PH1RFL" ) ) ?? throw new ArgumentNullException( "importDwgMappingModels.FirstOrDefault( x => x.FloorName.Equals( \"PH1RFL\" ) )" ) ;
        pH1RFLFloor.FloorHeight = pH1FFloor.FloorHeight + 3400 ;
      }

      importDwgMappingModels = importDwgMappingModels.OrderBy( x => x.FloorHeight ).ToList() ;
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