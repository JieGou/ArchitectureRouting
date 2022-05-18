using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using View = Autodesk.Revit.DB.View ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class DefaultSettingCommandBase : IExternalCommand
  {
    private const string SetDefaultEcoModeTransactionName = "Electrical.App.Commands.Initialization.SetDefaultModeCommand" ;
    private const string Grade3 = "グレード3" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      try {
        var document = commandData.Application.ActiveUIDocument.Document ;
        var activeViewName = document.ActiveView.Name ;
        // Get data of default setting from snoop DB
        DefaultSettingStorable defaultSettingStorable = document.GetDefaultSettingStorable() ;
        SetupPrintStorable setupPrintStorable = document.GetSetupPrintStorable() ;
        var scale = setupPrintStorable.Scale ;
        var viewModel = new DefaultSettingViewModel( defaultSettingStorable, scale, activeViewName ) ;
        var dialog = new DefaultSettingDialog( viewModel ) ;
        dialog.ShowDialog() ;
        {
          if ( dialog.DialogResult == false )
            return Result.Cancelled ;

          viewModel = dialog.ViewModel ;
          var isEcoMode = viewModel.SelectedEcoNormalMode == DefaultSettingViewModel.EcoNormalMode.EcoMode ;
          var isInGrade3Mode = viewModel.SelectedGradeMode == DefaultSettingViewModel.GradeModes.Grade3 ;
          var importDwgMappingModels = viewModel.ImportDwgMappingModels ;
          var deletedFloorName = viewModel.DeletedFloorName ;
          SetEcoModeAndGradeModeDefaultValue( document, defaultSettingStorable, isEcoMode, isInGrade3Mode, importDwgMappingModels, deletedFloorName ) ;

          if ( deletedFloorName.Any() ) {
            RemoveViews( document, deletedFloorName ) ;
          }
          
          LoadDwgAndSetScale( commandData, importDwgMappingModels, viewModel.FileItems ) ;
          return Result.Succeeded ;
        }
      }
      catch ( OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException( exception ) ;
        return Result.Cancelled ;
      }
    }
    
    private void SetEcoModeAndGradeModeDefaultValue( Document document, DefaultSettingStorable defaultSettingStorable, bool isEcoModel, bool isInGrade3Mode, ObservableCollection<ImportDwgMappingModel> importDwgMappingModels, List<string> deletedFloorName )
    {
      try {
        Transaction transaction = new( document, SetDefaultEcoModeTransactionName ) ;
        transaction.Start() ;
        var instances = new FilteredElementCollector( document ).OfClass( typeof( FamilyInstance ) ).Cast<FamilyInstance>().Where( a => a.HasParameter( Grade3 ) ).ToList() ;
        foreach ( var instance in instances ) {
          instance.SetProperty( Grade3, isInGrade3Mode ) ;
        }

        defaultSettingStorable.EcoSettingData.IsEcoMode = isEcoModel ;
        defaultSettingStorable.GradeSettingData.IsInGrade3Mode = isInGrade3Mode ;
        if ( importDwgMappingModels.Any() ) {
          UpdateImportDwgMappingModels( defaultSettingStorable, importDwgMappingModels, deletedFloorName ) ;
        }

        defaultSettingStorable.Save() ;
        transaction.Commit() ;
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException( exception ) ;
      }
    }

    private void UpdateImportDwgMappingModels( DefaultSettingStorable defaultSettingStorable, ObservableCollection<ImportDwgMappingModel> importDwgMappingModels, List<string> deletedFloorName )
    {
      if ( deletedFloorName.Any() ) {
        foreach ( var floorName in deletedFloorName ) {
          var deletedImportDwgMappingModel = defaultSettingStorable.ImportDwgMappingData.SingleOrDefault( i => i.FloorName == floorName ) ;
          defaultSettingStorable.ImportDwgMappingData.Remove( deletedImportDwgMappingModel ) ;
        }
      }
      foreach ( var item in importDwgMappingModels ) {
        var oldImportDwgMappingModel = defaultSettingStorable.ImportDwgMappingData.SingleOrDefault( i => i.FloorName == item.FloorName ) ;
        if ( oldImportDwgMappingModel == null ) {
          defaultSettingStorable.ImportDwgMappingData.Add( new Storable.Model.ImportDwgMappingModel( item.Id, item.FullFilePath, item.FileName, item.FloorName, item.FloorHeight, item.Scale ) ) ;
        }
        else {
          oldImportDwgMappingModel.FloorHeight = item.FloorHeight ;
          oldImportDwgMappingModel.Scale = item.Scale ;
        }
      }

      defaultSettingStorable.ImportDwgMappingData = defaultSettingStorable.ImportDwgMappingData.OrderBy( x => x.FloorHeight ).ToList() ;
    }

    private void LoadDwgAndSetScale( ExternalCommandData commandData, ObservableCollection<ImportDwgMappingModel> importDwgMappingModels, List<FileComboboxItemType> fileItems )
    {
      try {
        if ( ! importDwgMappingModels.Any() ) return ;
        var completeImportDwgMappingModels = importDwgMappingModels.Where( x => ! string.IsNullOrEmpty( x.FloorName ) && ! string.IsNullOrEmpty( x.FileName ) ).ToList() ;
        if ( ! completeImportDwgMappingModels.Any() ) return ;
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

          var isNewView = false ;
          var viewPlan = allCurrentViewPlans.FirstOrDefault( x => x.Name.Equals( importDwgMappingModel.FloorName ) ) as ViewPlan ;
          if ( viewPlan == null ) {
            viewPlan = ViewPlan.Create( doc, viewFamily.Id, importDwgLevel.Id ) ;
            viewPlan.Name = importDwgMappingModel.FloorName ;
            isNewView = true ;
          }

          viewPlan.Scale = importDwgMappingModel.Scale ;
          if ( null != viewPlan.ViewTemplateId && doc.GetElement( viewPlan.ViewTemplateId ) is View viewTemplate && viewTemplate.Scale != importDwgMappingModel.Scale ) {
            viewTemplate.Scale = importDwgMappingModel.Scale ;
          }

          importDwgLevel.SetProperty( BuiltInParameter.LEVEL_ELEV, importDwgMappingModel.FloorHeight.MillimetersToRevitUnits() ) ;
          if ( isNewView ) doc.Import( importDwgMappingModel.FullFilePath, dwgImportOptions, viewPlan, out ElementId importElementId ) ;
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
          const string view3DName = "3D ALL" ;
          var current3DView = allCurrent3DView.FirstOrDefault( x => x.Name.Equals( view3DName ) ) ;
          if ( current3DView != null ) doc.Delete( current3DView.Id ) ;
          current3DView = View3D.CreateIsometric( doc, threeDimensionalViewFamilyType.Id ) ;
          current3DView.Name = view3DName ;
        }

        create3DTrans.Commit() ;

        #endregion
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException( exception ) ;
      }
    }

    private void RemoveViews( Document document, List<string> deletedFloorName )
    {
      try {
        var deletedViewIds = document.GetAllElements<View>().Where( e => deletedFloorName.Contains( e.Name ) ).Select( e => e.Id ).ToList() ;
        if ( ! deletedViewIds.Any() ) return ;
        var removeViewsTrans = new Transaction( document, "Remove Views " ) ;
        removeViewsTrans.Start() ;
        document.Delete( deletedViewIds ) ;
        removeViewsTrans.Commit() ;
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException( exception ) ;
      }
    }
  }
}