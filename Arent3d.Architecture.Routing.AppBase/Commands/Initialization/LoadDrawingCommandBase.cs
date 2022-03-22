using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Text.RegularExpressions ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class LoadDrawingCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      OpenFileDialog openFileDialog = new() { Filter = "DWG files (*.dwg )|*.dwg", Multiselect = true } ;
      if ( openFileDialog.ShowDialog() != DialogResult.OK ) return Result.Succeeded ;
      var importDwgMappingModels = new List<ImportDwgMappingModel>() ;
      var fileItems = new List<FileComboboxItemType>() ;
      foreach ( var fileName in openFileDialog.FileNames ) {
        fileItems.Add( new FileComboboxItemType( fileName ) ) ;
        if ( fileName.Contains( "B1" ) ) {
          importDwgMappingModels.Add( new ImportDwgMappingModel( fileName, $"B1F", 0 ) ) ;
        }
        else if ( fileName.Contains( "PH1" ) ) {
          importDwgMappingModels.Add( new ImportDwgMappingModel( fileName, $"PH1F", 0 ) ) ;
        }
        else {
          var floorNumber = Regex.Match( fileName, @"\d+階" ).Value.Replace( "階", "" ) ;
          if ( int.TryParse( floorNumber, out _ ) ) importDwgMappingModels.Add( new ImportDwgMappingModel( fileName, $"{floorNumber}F", 0 ) ) ;
        }
      }

      UpdateDefaultFloorHeight( ref importDwgMappingModels ) ;
      var dialog = new ImportDwgMappingDialog( new ImportDwgMappingViewModel( importDwgMappingModels, fileItems ) ) ;
      dialog.ShowDialog() ;
      if ( ! ( dialog.DialogResult ?? false ) ) return Result.Succeeded ;
      {
        var importDwgMappingViewModel = dialog.DataContext as ImportDwgMappingViewModel ;
        if ( importDwgMappingViewModel == null || ! importDwgMappingViewModel.ImportDwgMappingModels.Any() ) return Result.Succeeded ;
        var completeImportDwgMappingModels = importDwgMappingViewModel.ImportDwgMappingModels.Where( x => ! string.IsNullOrEmpty( x.FloorName ) && ! string.IsNullOrEmpty( x.FileName ) ).ToList() ;
        if ( ! completeImportDwgMappingModels.Any() ) return Result.Succeeded ;
        foreach ( var importDwgMappingModel in completeImportDwgMappingModels ) {
          var fileItem = fileItems.FirstOrDefault( x => x.FileName.Equals( importDwgMappingModel.FileName ) ) ;
          importDwgMappingModel.FullFilePath = fileItem != null ? fileItem.FullFilePath : "" ;
        }

        Document doc = commandData.Application.ActiveUIDocument.Document ;
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
        var viewFamily = new FilteredElementCollector( doc ).OfClass( typeof( ViewFamilyType ) ).Cast<ViewFamilyType>().First( x => x.ViewFamily == ViewFamily.FloorPlan ) ;
        var allCurrentLevels = new FilteredElementCollector( doc ).OfClass( typeof( Level ) ).ToList() ;
        var allCurrentViewPlans = new FilteredElementCollector( doc ).OfClass( typeof( ViewPlan ) ).ToList() ;
        ViewPlan? firstViewPlan = null ;

        #region Import

        var importTrans = new Transaction( doc ) ;
        importTrans.SetName( "Import" ) ;
        importTrans.Start() ;
        for ( var i = 0 ; i < completeImportDwgMappingModels.Count() ; i++ ) {
          var importDwgMappingModel = completeImportDwgMappingModels[ i ] ;
          if ( string.IsNullOrEmpty( importDwgMappingModel.FullFilePath ) ) continue ;
          var levelName = "Level " + importDwgMappingModel.FloorName ;
          var importDwgLevel = allCurrentLevels.FirstOrDefault( x => x.Name.Equals( levelName ) ) ;
          if ( importDwgLevel == null ) {
            importDwgLevel = Level.Create( doc, importDwgMappingModel.FloorHeight ) ;
            importDwgLevel.Name = levelName ;
          }

          var viewPlan = allCurrentViewPlans.FirstOrDefault( x => x.Name.Equals( importDwgMappingModel.FloorName ) ) as ViewPlan ;
          if ( viewPlan == null ) {
            viewPlan = ViewPlan.Create( doc, viewFamily.Id, importDwgLevel.Id ) ;
            viewPlan.Name = importDwgMappingModel.FloorName ;
          }

          importDwgLevel.SetProperty( BuiltInParameter.LEVEL_ELEV, importDwgMappingModel.FloorHeight.MillimetersToRevitUnits() ) ;
          doc.Import( importDwgMappingModel.FullFilePath, dwgImportOptions, viewPlan, out ElementId importElementId ) ;
          if ( i == 0 ) firstViewPlan = viewPlan ;
        }

        importTrans.Commit() ;

        #endregion

        #region Create 3D view

        if ( firstViewPlan != null ) commandData.Application.ActiveUIDocument.ActiveView = firstViewPlan ;
        var create3DTrans = new Transaction( doc ) ;
        create3DTrans.SetName( "Import" ) ;
        create3DTrans.Start() ;
        var threeDimensionalViewFamilyType = new FilteredElementCollector( doc ).OfClass( typeof( ViewFamilyType ) ).ToElements().Cast<ViewFamilyType>().FirstOrDefault( vft => vft.ViewFamily == ViewFamily.ThreeDimensional ) ;
        if ( threeDimensionalViewFamilyType != null ) {
          var allCurrent3DView = new FilteredElementCollector( doc ).OfClass( typeof( View3D ) ).ToList() ;
          const string view3DName = "{3D}" ;
          var current3DView = allCurrent3DView.FirstOrDefault( x => x.Name.Equals( view3DName ) ) ;
          if ( current3DView != null ) doc.Delete( current3DView.Id ) ;
          current3DView = View3D.CreateIsometric( doc, threeDimensionalViewFamilyType.Id ) ;
          current3DView.Name = view3DName ;
        }

        create3DTrans.Commit() ;

        #endregion
      }

      return Result.Succeeded ;
    }

    private static void UpdateDefaultFloorHeight( ref List<ImportDwgMappingModel> importDwgMappingModels )
    {
      const int floorHeightDistance = 3000 ;
      Dictionary<string, double> defaultHeights = new()
      {
        { "B1F", 0 },
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
        var (key, value) = defaultHeights.FirstOrDefault( x => x.Key.Equals( importDwgMappingModel.FloorName ) ) ;
        if ( key != null ) {
          importDwgMappingModel.FloorHeight = value ;
        }
        else {
          importDwgMappingModel.FloorHeight = importDwgMappingModels.Max( x => x.FloorHeight ) + floorHeightDistance ;
        }
      }

      var maxFloorHeight = importDwgMappingModels.Max( x => x.FloorHeight ) ;
      var pH1FFloor = importDwgMappingModels.FirstOrDefault( x => x.FloorName.Equals( "PH1F" ) ) ;
      if ( pH1FFloor != null ) pH1FFloor.FloorHeight = maxFloorHeight + 6500 ;

      importDwgMappingModels = importDwgMappingModels.OrderBy( x => x.FloorHeight ).ToList() ;
    }
  }

  public class FileComboboxItemType
  {
    public string FullFilePath { get ; }
    public string FileName { get ; }

    public FileComboboxItemType( string fullFilePath )
    {
      FullFilePath = fullFilePath ;
      FileName = Path.GetFileName( fullFilePath ) ;
    }
  }
}