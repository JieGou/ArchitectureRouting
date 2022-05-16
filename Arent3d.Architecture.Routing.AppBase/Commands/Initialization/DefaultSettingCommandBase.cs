using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using View = Autodesk.Revit.DB.View ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class DefaultSettingCommandBase : IExternalCommand
  {
    private const string SetDefaultEcoModeTransactionName = "Electrical.App.Commands.Initialization.SetDefaultModeCommand" ;
    private const string DialogResultSuccessKey = "Dialog.Electrical.ChangeMode.Success" ;
    private const string DialogResultTitleKey = "Dialog.Electrical.ChangeMode.Title" ;
    private const string UpdateDataSuccessMessage = "Update data success." ;
    private const string ElectricalChangeModeTitle = "Change mode result" ;
    private const string Grade3 = "グレード3" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      try {
        var document = commandData.Application.ActiveUIDocument.Document ;
        // Get data of default setting from snoop DB
        DefaultSettingStorable defaultSettingStorable = document.GetDefaultSettingStorable() ;
        SetupPrintStorable setupPrintStorable = document.GetSetupPrintStorable() ;
        var isEcoMode = defaultSettingStorable.EcoSettingData.IsEcoMode ;
        var isInGrade3Mode = defaultSettingStorable.GradeSettingData.IsInGrade3Mode ;
        var scale = setupPrintStorable.Scale ;
        var viewModel = new DefaultSettingViewModel( isEcoMode, isInGrade3Mode, scale ) ;
        var dialog = new DefaultSettingDialog( viewModel ) ;
        dialog.ShowDialog() ;
        {
          if ( dialog.DialogResult == false )
            return Result.Cancelled ;

          viewModel = dialog.ViewModel ;
          isEcoMode = viewModel.SelectedEcoNormalMode == DefaultSettingViewModel.EcoNormalMode.EcoMode ;
          isInGrade3Mode = viewModel.SelectedGradeMode == DefaultSettingViewModel.GradeModes.Grade3 ;
          var setDefaultModeResult = SetEcoModeAndGradeModeDefaultValue( document, defaultSettingStorable, isEcoMode, isInGrade3Mode ) ;
          if ( setDefaultModeResult == Result.Cancelled ) return Result.Cancelled ;

          var loadDwgAndSetScaleResult = LoadDwgAndSetScale( commandData, viewModel.ImportDwgMappingModels, viewModel.FileItems ) ;
          return loadDwgAndSetScaleResult ;
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

    /// <summary>
    /// Set default value for isEcoMode and grade mode
    /// </summary>
    /// <param name="document"></param>
    /// <param name="defaultSettingStorable"></param>
    /// <param name="isEcoModel"></param>
    /// <param name="isInGrade3Mode"></param>
    /// <returns></returns>
    private Result SetEcoModeAndGradeModeDefaultValue( Document document, DefaultSettingStorable defaultSettingStorable, bool isEcoModel, bool isInGrade3Mode )
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
        defaultSettingStorable.Save() ;
        transaction.Commit() ;

        return Result.Succeeded ;
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException( exception ) ;
        return Result.Cancelled ;
      }
    }

    private Result LoadDwgAndSetScale( ExternalCommandData commandData, ObservableCollection<ImportDwgMappingModel> importDwgMappingModels, List<FileComboboxItemType> fileItems )
    {
      try {
        if ( ! importDwgMappingModels.Any() ) return Result.Succeeded ;
        var completeImportDwgMappingModels = importDwgMappingModels.Where( x => ! string.IsNullOrEmpty( x.FloorName ) && ! string.IsNullOrEmpty( x.FileName ) ).ToList() ;
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

          viewPlan.Scale = importDwgMappingModel.Scale ;
          if ( null != viewPlan.ViewTemplateId && doc.GetElement( viewPlan.ViewTemplateId ) is View viewTemplate && viewTemplate.Scale != importDwgMappingModel.Scale ) {
            viewTemplate.Scale = importDwgMappingModel.Scale ;
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
          const string view3DName = "3D ALL" ;
          var current3DView = allCurrent3DView.FirstOrDefault( x => x.Name.Equals( view3DName ) ) ;
          if ( current3DView != null ) doc.Delete( current3DView.Id ) ;
          current3DView = View3D.CreateIsometric( doc, threeDimensionalViewFamilyType.Id ) ;
          current3DView.Name = view3DName ;
        }

        create3DTrans.Commit() ;

        #endregion

        return Result.Succeeded ;
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException( exception ) ;
        return Result.Cancelled ;
      }
    }

    private void ShowResult( string message )
    {
      MessageBox.Show( string.IsNullOrEmpty( message ) ? DialogResultSuccessKey.GetAppStringByKeyOrDefault( UpdateDataSuccessMessage ) : message, DialogResultTitleKey.GetAppStringByKeyOrDefault( ElectricalChangeModeTitle ), MessageBoxButtons.OK ) ;
    }
  }
}