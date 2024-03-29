﻿using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Commands.Shaft ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.Utils ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using ImportDwgMappingModel = Arent3d.Architecture.Routing.AppBase.Model.ImportDwgMappingModel ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class DefaultSettingCommandBase : IExternalCommand
  {
    private const string SetDefaultEcoModeTransactionName = "Electrical.App.Commands.Initialization.SetDefaultModeCommand" ;
    public const string Grade3FieldName = "グレード3" ;
    private const string ArentDummyViewName = "Arent Dummy" ;
    public const string SingleTextNoteTypeName = "ARENT_2.7MM_SINGLE-BORDER" ;
    public const string DoubleTextNoteTypeName = "ARENT_2.7MM_DOUBLE-BORDER" ;

    private string _activeViewName = string.Empty ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      _activeViewName = document.ActiveView.Name ;
      try {
        var result = document.TransactionGroup( "Default Settings", _ =>
        {
          // Get data of default setting from snoop DB
          var defaultSettingStorable = document.GetDefaultSettingStorable() ;
          var setupPrintStorable = document.GetSetupPrintStorable() ;
          var scale = setupPrintStorable.Scale ;

          var listFloorsDefault = new ObservableCollection<ImportDwgMappingModel>( GetFloorsDefault( document ) ) ;

          if ( defaultSettingStorable.ImportDwgMappingData.Any() )
            try {
              using Transaction transaction = new(document, "Remove") ;
              transaction.Start() ;
              foreach ( var floorPlan in defaultSettingStorable.ImportDwgMappingData.ToList() ) {
                var existFloorPlan = listFloorsDefault.FirstOrDefault( x => x.FloorName == floorPlan.FloorName ) ;
                if ( existFloorPlan == null ) defaultSettingStorable.ImportDwgMappingData.Remove( floorPlan ) ;
              }

              defaultSettingStorable.Save() ;
              transaction.Commit() ;
            }
            catch ( Exception exception ) {
              CommandUtils.DebugAlertException( exception ) ;
            }
          else
            UpdateImportDwgMappingModels( defaultSettingStorable, listFloorsDefault, new List<string>() ) ;

          var viewModel = new DefaultSettingViewModel( uiDocument, defaultSettingStorable, scale ) ;
          var dialog = new DefaultSettingDialog( viewModel ) ;
          dialog.ShowDialog() ;
          {
            if ( dialog.DialogResult == false )
              return Result.Cancelled ;

            viewModel = dialog.ViewModel ;

            // Save default db
            viewModel.SaveData() ;

            if ( viewModel.IsSetupGrade ) {
              var dataStorage = document.FindOrCreateDataStorage<DisplaySettingModel>( false ) ;
              var storageService = new StorageService<DataStorage, DisplaySettingModel>( dataStorage ) ;
              DisplaySettingViewModel.ApplyChanges(uiDocument.Document, storageService.Data);
            }

            var isEcoMode = viewModel.SelectedEcoNormalMode == DefaultSettingViewModel.EcoNormalMode.EcoMode ;
            var importDwgMappingModels = viewModel.ImportDwgMappingModels ;
            var deletedFloorName = viewModel.DeletedFloorName ;
            SetEcoModeAndGradeModeDefaultValue( document, defaultSettingStorable, isEcoMode, importDwgMappingModels, deletedFloorName ) ;

            if ( deletedFloorName.Any() ) RemoveViews( document, deletedFloorName, uiDocument ) ;

            var transactionStatus = UpdateScaleAndHeightPlanView( document, importDwgMappingModels ) ;
            if ( transactionStatus == TransactionStatus.RolledBack )
              return Result.Failed ;
            LoadDwgAndSetScale( commandData, importDwgMappingModels, viewModel.FileItems ) ;
            UpdateCeedDockPaneDataContext( uiDocument ) ;

            return Result.Succeeded ;
          }
        } ) ;

        return result ;
      }
      catch ( OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    private static List<ImportDwgMappingModel> GetFloorsDefault( Document doc )
    {
      var views = new List<ViewPlan>( new FilteredElementCollector( doc ).OfClass( typeof( ViewPlan ) ).Cast<ViewPlan>().Where( v => v.CanBePrinted && ViewType.FloorPlan == v.ViewType ) ) ;
      var importDwgMappingModels = new List<ImportDwgMappingModel>() ;
      foreach ( var view in views ) {
        var fileName = string.Empty ;
        var floorName = view.Name ;
        var heightSettingStorable = doc.GetHeightSettingStorable() ;
        var height = heightSettingStorable.HeightSettingsData.Values.FirstOrDefault( x => x.LevelId.ToString() == view.GenLevel.Id.ToString() )?.Elevation ?? 0 ;

        var scale = view.Scale ;

        importDwgMappingModels.Add( new ImportDwgMappingModel( fileName, floorName, height, scale ) ) ;
      }

      var importDwgMappingModelsGroups = importDwgMappingModels.OrderBy( x => x.FloorHeight ).GroupBy( x => x.FloorHeight ).Select( x => x.ToList() ).ToList() ;
      var result = new List<ImportDwgMappingModel>() ;

      for ( var i = 0 ; i < importDwgMappingModelsGroups.Count - 1 ; i++ ) {
        var heightCurrentLevel = importDwgMappingModelsGroups[ i ].First().FloorHeight ;
        var heightNextLevel = importDwgMappingModelsGroups[ i + 1 ].First().FloorHeight ;
        var height = heightNextLevel - heightCurrentLevel ;

        foreach ( var importDwgMappingModelsGroup in importDwgMappingModelsGroups[ i ] ) {
          var importDwgModel = new ImportDwgMappingModel( importDwgMappingModelsGroup.FileName, importDwgMappingModelsGroup.FloorName, importDwgMappingModelsGroup.FloorHeight, importDwgMappingModelsGroup.Scale, height ) ;
          result.Add( importDwgModel ) ;
        }
      }

      // Add last item
      foreach ( var importDwgMappingModelsGroup in importDwgMappingModelsGroups.Last() ) {
        var importDwgModel = new ImportDwgMappingModel( importDwgMappingModelsGroup.FileName, importDwgMappingModelsGroup.FloorName, importDwgMappingModelsGroup.FloorHeight, importDwgMappingModelsGroup.Scale, null ) ;
        result.Add( importDwgModel ) ;
      }

      return result ;
    }

    private void SetEcoModeAndGradeModeDefaultValue( Document document, DefaultSettingStorable defaultSettingStorable, bool isEcoModel, ObservableCollection<ImportDwgMappingModel> importDwgMappingModels, List<string> deletedFloorName )
    {
      try {
        Transaction transaction = new(document, SetDefaultEcoModeTransactionName) ;
        transaction.Start() ;
        var instances = new FilteredElementCollector( document ).OfClass( typeof( FamilyInstance ) ).Cast<FamilyInstance>().Where( a => a.HasParameter( Grade3FieldName ) ).ToList() ;
        var dataStorage = document.FindOrCreateDataStorage<DisplaySettingModel>( false ) ;
        var displaySettingStorageService = new StorageService<DataStorage, DisplaySettingModel>( dataStorage ) ;
        var isGrade3 = displaySettingStorageService.Data.IsGrade3 ;
        foreach ( var instance in instances ) {
          instance.SetProperty( Grade3FieldName, isGrade3 ) ;
        }

        defaultSettingStorable.EcoSettingData.IsEcoMode = isEcoModel ;

        if ( importDwgMappingModels.Any() )
          UpdateImportDwgMappingModels( defaultSettingStorable, importDwgMappingModels, deletedFloorName ) ;

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
        double.TryParse( item.FloorHeightDisplay, out var floorHeightDisplay ) ;
        if ( oldImportDwgMappingModel == null ) {
          defaultSettingStorable.ImportDwgMappingData.Add( new Storable.Model.ImportDwgMappingModel( item.Id, item.FullFilePath, item.FileName, item.FloorName, item.FloorHeight, item.Scale, floorHeightDisplay ) ) ;
        }
        else {
          oldImportDwgMappingModel.FloorHeightDisplay = floorHeightDisplay ;
          oldImportDwgMappingModel.FloorHeight = item.FloorHeight ;
          oldImportDwgMappingModel.Scale = item.Scale ;
        }
      }

      defaultSettingStorable.ImportDwgMappingData = defaultSettingStorable.ImportDwgMappingData.OrderBy( x => x.FloorHeight ).ToList() ;
    }

    private void LoadDwgAndSetScale( ExternalCommandData commandData, ObservableCollection<ImportDwgMappingModel> importDwgMappingModels, List<FileComboboxItemType> fileItems )
    {
      try {
        var nameLevelList = new List<string>() ;
        if ( ! importDwgMappingModels.Any() ) return ;
        var completeImportDwgMappingModels = importDwgMappingModels.Where( x => ! string.IsNullOrEmpty( x.FloorName ) && ! string.IsNullOrEmpty( x.FileName ) ).ToList() ;
        if ( ! completeImportDwgMappingModels.Any() ) return ;
        foreach ( var importDwgMappingModel in completeImportDwgMappingModels ) {
          var fileItem = fileItems.FirstOrDefault( x => x.FileName.Equals( importDwgMappingModel.FileName ) ) ;
          importDwgMappingModel.FullFilePath = fileItem != null ? fileItem.FullFilePath : "" ;
        }

        var uiDocument = commandData.Application.ActiveUIDocument ;
        Document doc = uiDocument.Document ;
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

        #region Import

        using var importTrans = new Transaction( doc ) ;
        importTrans.SetName( "Import" ) ;
        importTrans.Start() ;
        for ( var i = 0 ; i < completeImportDwgMappingModels.Count() ; i++ ) {
          var importDwgMappingModel = completeImportDwgMappingModels[ i ] ;
          if ( string.IsNullOrEmpty( importDwgMappingModel.FullFilePath ) ) continue ;
          var levelName = importDwgMappingModel.FloorName ;
          var importDwgLevel = allCurrentLevels.FirstOrDefault( x => x.Name.Equals( levelName ) ) ;
          if ( importDwgLevel == null ) {
            importDwgLevel = Level.Create( doc, importDwgMappingModel.FloorHeight ) ;
            importDwgLevel.Name = levelName ;
          }

          // Add name level
          nameLevelList.Add( importDwgLevel.Name ) ;

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
          if ( isNewView ) doc.Import( importDwgMappingModel.FullFilePath, dwgImportOptions, viewPlan, out _ ) ;
        }

        importTrans.Commit() ;

        #endregion

        #region Create 3D view

        using var create3D = new Transaction( doc ) ;
        create3D.SetName( "Create 3D View" ) ;
        create3D.Start() ;
        var levelListOrderByElavation = doc.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ).Select( ToLevelInfo ).Where( x => nameLevelList.Contains( x.LevelName ) ).OrderBy( l => l.Elevation ) ;

        var levelListToCreate3d = levelListOrderByElavation.Select( level => ( Id: level.LevelId, Name: level.LevelName ) ).ToList() ;

        List<View3D> all3DViews = new FilteredElementCollector( doc ).OfClass( typeof( View3D ) ).Cast<View3D>().ToList() ;

        var levels = levelListToCreate3d.Where( x => all3DViews.FirstOrDefault( y => y.Name == "3D" + x.Name ) == null ).EnumerateAll() ;

        if ( levels.Any() )
          doc.Create3DView( levels ) ;

        create3D.Commit() ;

        #endregion

        #region Create 3D ALL view

        View? view3dAll = null ;
        using var create3DTrans = new Transaction( doc ) ;
        create3DTrans.SetName( "Create 3D ALL view" ) ;
        create3DTrans.Start() ;
        var threeDimensionalViewFamilyType = new FilteredElementCollector( doc ).OfClass( typeof( ViewFamilyType ) ).ToElements().Cast<ViewFamilyType>().FirstOrDefault( vft => vft.ViewFamily == ViewFamily.ThreeDimensional ) ;
        if ( threeDimensionalViewFamilyType != null ) {
          var allCurrent3DView = new FilteredElementCollector( doc ).OfClass( typeof( View3D ) ).ToList() ;
          const string view3DName = "3D ALL" ;
          var current3DView = allCurrent3DView.FirstOrDefault( x => x.Name.Equals( view3DName ) ) ;
          if ( current3DView == null ) {
            current3DView = View3D.CreateIsometric( doc, threeDimensionalViewFamilyType.Id ) ;
            current3DView.Name = view3DName ;
          }

          view3dAll = (View) current3DView ;
        }

        create3DTrans.Commit() ;

        #endregion

        var viewPlans = new List<ViewPlan>( new FilteredElementCollector( doc ).OfClass( typeof( ViewPlan ) ).Cast<ViewPlan>().Where( v => v.CanBePrinted && ViewType.FloorPlan == v.ViewType ) ) ;

        var viewPlansWithoutDummy = viewPlans.Where( x => x.Name != ArentDummyViewName ).ToList() ;

        #region Remove view Arent dummy

        if ( viewPlansWithoutDummy.Count >= 1 ) {
          var viewIdArentDummyView = viewPlans.Where( x => x.Name == ArentDummyViewName ).Select( x => x.Id ).ToList() ;
          if ( viewIdArentDummyView.Any() ) {
            var pCurrView = uiDocument.ActiveView ;
            uiDocument.RequestViewChange( pCurrView ) ;
            uiDocument.ActiveView = viewPlansWithoutDummy[ 0 ] ;

            var levelIdDummies = doc.GetAllElements<Level>().Where( x => x.Name == ArentDummyViewName ).Select( x => x.Id ).ToList() ;
            using var removeArentDummyView = new Transaction( doc ) ;
            removeArentDummyView.SetName( "Remove view Arent dummy and level" ) ;
            removeArentDummyView.Start() ;
            doc.Delete( viewIdArentDummyView ) ;
            doc.Delete( levelIdDummies ) ;
            removeArentDummyView.Commit() ;
          }
        }

        #endregion

        #region Set view range

        using var setViewRangeTransaction = new Transaction( doc, "Set View Range" ) ;
        setViewRangeTransaction.Start() ;

        foreach ( var view in viewPlansWithoutDummy ) {
          var pvr = view.GetViewRange() ;
          pvr.SetOffset( PlanViewPlane.TopClipPlane, 4000.0 / 304.8 ) ;
          pvr.SetOffset( PlanViewPlane.CutPlane, 3950.0 / 304.8 ) ;
          pvr.SetOffset( PlanViewPlane.BottomClipPlane, 0.0 ) ;
          view.SetViewRange( pvr ) ;
          
          view.ViewTemplateId = new ElementId( -1 );
        }

        setViewRangeTransaction.Commit() ;

        #endregion

        if ( view3dAll == null ) 
          return ;
        
        var activeView = doc.GetAllInstances<View>( x => x.Name == _activeViewName ).SingleOrDefault() ;
        uiDocument.ActiveView = activeView ?? view3dAll ;
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException( exception ) ;
      }
    }

    private class LevelInfo
    {
      public string LevelName { get ; init ; } = string.Empty ;
      public ElementId LevelId { get ; init ; } = ElementId.InvalidElementId ;
      public double Elevation { get ; init ; }
    }

    private static LevelInfo ToLevelInfo( Level level )
    {
      return new LevelInfo { Elevation = level.Elevation, LevelId = level.Id, LevelName = level.Name } ;
    }

    private void RemoveViews( Document document, List<string> deletedFloorName, UIDocument uiDocument )
    {
      try {
        var deletedViews = document.GetAllElements<View>().Where( e => deletedFloorName.Any( x => x == e.Name ) && ViewType.FloorPlan == e.ViewType ).ToList() ;
        var deletedViewIds = deletedViews.Select( x => x.Id ).ToList() ;
        var deletedLevels = deletedViews.Select( x => x.GenLevel.Id ).ToList() ;
        var views = new List<ViewPlan>( new FilteredElementCollector( uiDocument.Document ).OfClass( typeof( ViewPlan ) ).Cast<ViewPlan>().Where( v => v.CanBePrinted && ViewType.FloorPlan == v.ViewType ) ) ;
        IList<ElementId> categoryIds = new List<ElementId>() ;
        var importInstances = new FilteredElementCollector( document ).OfClass( typeof( ImportInstance ) ).Cast<ImportInstance>().Where( x => deletedLevels.Any( l => l == x.LevelId ) ) ;
        foreach ( var importInstance in importInstances ) {
          var catId = importInstance.Category.Id ;
          if ( ! categoryIds.Contains( catId ) ) categoryIds.Add( catId ) ;
        }

        if ( views.Count() == deletedViewIds.Count() ) {
          ArentViewDummy( uiDocument ) ;
        }
        else {
          List<ViewPlan> viewsTemp = new List<ViewPlan>() ;
          foreach ( var view in views ) {
            bool isExist = false ;
            foreach ( var deletedViewId in deletedViewIds ) {
              if ( view.Id == deletedViewId ) {
                isExist = true ;
                break ;
              }
            }

            if ( ! isExist ) {
              viewsTemp.Add( view ) ;
            }
          }

          if ( ! deletedViewIds.Any() ) return ;
          if ( viewsTemp.Any() ) {
            var activeView = document.GetAllInstances<View>( x => x.Name == _activeViewName && viewsTemp.Any( v => v.Id == x.Id ) ).SingleOrDefault() ;
            uiDocument.ActiveView = activeView ?? viewsTemp[ 0 ] ;
          }
        }

        var removeViewsTrans = new Transaction( document, "Remove Views, Level, Dwg linked " ) ;
        removeViewsTrans.Start() ;
        document.Delete( deletedViewIds ) ;
        document.Delete( deletedLevels ) ;
        document.Delete( categoryIds ) ;
        removeViewsTrans.Commit() ;
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException( exception ) ;
      }
    }

    private void ArentViewDummy( UIDocument uiDocument )
    {
      var doc = uiDocument.Document ;

      var importTrans = new Transaction( doc ) ;
      importTrans.SetName( "Import" ) ;
      importTrans.Start() ;
      const double floorHeight = 0 ;
      const string floorName = ArentDummyViewName ;
      const int scale = 100 ;

      var viewFamily = new FilteredElementCollector( doc ).OfClass( typeof( ViewFamilyType ) ).Cast<ViewFamilyType>().First( x => x.ViewFamily == ViewFamily.FloorPlan ) ;

      var level = Level.Create( doc, floorHeight ) ;
      level.Name = floorName ;

      var viewPlan = ViewPlan.Create( doc, viewFamily.Id, level.Id ) ;
      viewPlan.Name = floorName ;

      viewPlan.Scale = scale ;
      if ( null != viewPlan.ViewTemplateId && doc.GetElement( viewPlan.ViewTemplateId ) is View viewTemplate && viewTemplate.Scale != scale ) {
        viewTemplate.Scale = scale ;
      }

      level.SetProperty( BuiltInParameter.LEVEL_ELEV, floorHeight.MillimetersToRevitUnits() ) ;
      importTrans.Commit() ;


      var pCurrView = uiDocument.ActiveView ;
      uiDocument.RequestViewChange( pCurrView ) ;
      uiDocument.ActiveView = viewPlan ;
    }

    private TransactionStatus UpdateScaleAndHeightPlanView( Document document, ObservableCollection<ImportDwgMappingModel> importDwgMappingModels )
    {
      List<ViewPlan> views = new List<ViewPlan>( new FilteredElementCollector( document ).OfClass( typeof( ViewPlan ) ).Cast<ViewPlan>().Where( v => v.CanBePrinted && ViewType.FloorPlan == v.ViewType ) ) ;

      var dataStorage = document.FindOrCreateDataStorage<BorderTextNoteModel>( true ) ;
      var storageService = new StorageService<DataStorage, BorderTextNoteModel>( dataStorage ) ;

      var allCurrentLevels = new FilteredElementCollector( document ).OfClass( typeof( Level ) ).ToList() ;
      using var updateScaleTrans = new Transaction( document ) ;
      updateScaleTrans.SetName( "Update Scale" ) ;
      updateScaleTrans.Start() ;
      foreach ( var importDwgMappingModel in importDwgMappingModels ) {
        var viewPlan = views.FirstOrDefault( x => x.Name == importDwgMappingModel.FloorName ) ;
        if ( viewPlan != null && viewPlan.Scale != importDwgMappingModel.Scale ) {
          viewPlan.Scale = importDwgMappingModel.Scale ;
          if ( null != viewPlan.ViewTemplateId && document.GetElement( viewPlan.ViewTemplateId ) is View viewTemplate && viewTemplate.Scale != importDwgMappingModel.Scale )
            viewTemplate.Scale = importDwgMappingModel.Scale ;

          document.Regenerate() ;
          FilteredElementCollector allElementsInView = new FilteredElementCollector( document, viewPlan.Id ) ;
          var elementsInView = allElementsInView.ToElements() ;
          UpdateSizeElement( document, elementsInView, storageService, viewPlan ) ;
        }

        var levelName = importDwgMappingModel.FloorName ;
        var importDwgLevel = allCurrentLevels.FirstOrDefault( x => x.Name.Equals( levelName ) ) ;
        importDwgLevel?.SetProperty( BuiltInParameter.LEVEL_ELEV, importDwgMappingModel.FloorHeight.MillimetersToRevitUnits() ) ;
      }

      return updateScaleTrans.Commit() ;
    }

    private void UpdateSizeElement( Document document, IList<Element> elementsInView, StorageService<DataStorage, BorderTextNoteModel> storageService, ViewPlan viewPlan )
    {
      var scale = ImportDwgMappingModel.GetMagnificationOfView( viewPlan.Scale ) ;
      
      // Update size text note border
      var textNoteSingleBorders = elementsInView.Where( x => x is TextNote textNote && SingleTextNoteTypeName == textNote.Name ) ;
      var textNoteDoubleBorders = elementsInView.Where( x => x is TextNote textNote && DoubleTextNoteTypeName == textNote.Name ) ;
      foreach ( var textNoteBorder in textNoteSingleBorders ) {
        var textNote = textNoteBorder as TextNote ;
        if ( textNote == null ) continue ;

        var curves = GetSingleBorderTextNote( textNote, viewPlan.Scale ) ;
        var borderIds = CreateDetailCurve( document, textNote, curves ) ;
        SetDataForTextNote( storageService, textNote, borderIds ) ;
      }
      
      foreach ( var textNoteBorder in textNoteDoubleBorders ) {
        var textNote = textNoteBorder as TextNote ;
        if ( textNote == null ) continue ;

        var curves = GetDoubleBorderTextNote( textNote, viewPlan ) ;
        var borderIds = CreateDetailCurve( document, textNote, curves ) ;
        SetDataForTextNote( storageService, textNote, borderIds ) ;
      }

      // Update Y of ceedcode
      var independentTags = elementsInView.Where( e => e is IndependentTag ).ToList() ;
      foreach ( var element in independentTags ) {
        var independentTag = (IndependentTag) element ;
        var independentTagPoint = independentTag.TagHeadPosition ;
        if ( independentTagPoint == null ) continue ;
        #if REVIT2022
        var taggedElementId = independentTag.GetTaggedLocalElementIds().FirstOrDefault() ;
        #else
        var taggedElementId = independentTag.GetTaggedLocalElement()?.Id ;
        #endif
        if ( taggedElementId == null ) continue ;
        var taggedElement = document.GetElement( taggedElementId ) ;
        var taggedElementLocation = ( taggedElement.Location as LocationPoint )!.Point ;
        independentTag.TagHeadPosition = new XYZ( taggedElementLocation.X, taggedElementLocation.Y + 2 * TextNoteHelper.TextSize.MillimetersToRevitUnits() * document.ActiveView.Scale, taggedElementLocation.Z ) ;

        // Set シンボル倍率 parameter of ceedcode if exist
        taggedElement.LookupParameter( "シンボル倍率" )?.Set( scale ) ;
      }

      // Update Symbol Information
      UpdateSymbolInformation(document, elementsInView, viewPlan.Scale) ;
      
      //Update Shaft Opening
      var shaftOpeningStore = document.GetShaftOpeningStorable() ;
      UpdateShaftOpeningInViewPlan( shaftOpeningStore, elementsInView, viewPlan ) ;
      
      // Update rack
      document.ReDrawAllRacksAndElbows( viewPlan ) ;
      
      // Change pull box dimension and related conduits if scale changes
      var level = viewPlan.GenLevel ;
      var baseLengthOfLine = scale / 100d ;
      var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var allConduits = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).OfType<Conduit>().EnumerateAll() ;
      var pullBoxElements = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_ElectricalFixtures ).Where( e => ( e.GetConnectorFamilyType() == ConnectorFamilyType.PullBox || e.GetConnectorFamilyType() == ConnectorFamilyType.Handhole ) && e.LevelId == level.Id ).ToList() ;

      foreach ( var pullBoxElement in pullBoxElements ) {
        var routesRelatedPullBox = PullBoxRouteManager.GetRoutesRelatedPullBoxByNearestEndPoints( routes, allConduits, pullBoxElement ) ;
        var pullBoxLocation = ( pullBoxElement.Location as LocationPoint )?.Point! ;
        var conduitsRelatedPullBox = PullBoxRouteManager.GetConduitsRelatedPullBox( allConduits, routesRelatedPullBox, pullBoxLocation ) ;
        PullBoxRouteManager.ResizePullBoxAndRelatedConduits( conduitsRelatedPullBox, pullBoxElement, baseLengthOfLine ) ;
      }
    }

    private static void UpdateShaftOpeningInViewPlan( ShaftOpeningStorable storable, IEnumerable<Element> elementsInView, ViewPlan viewPlan )
    {
      var openings = elementsInView.OfType<Opening>().EnumerateAll() ;
      if ( ! openings.Any() )
        return ;

      var (styleForBodyDirection, styleForOuterShape, styleForSymbol) = CreateCylindricalShaftCommandBase.GetLineStyles( storable.Document ) ;

      foreach ( var opening in openings ) {
        var openingStore = storable.ShaftOpeningModels.SingleOrDefault( x => x.ShaftOpeningUniqueId == opening.UniqueId ) ;
        if ( openingStore is null )
          continue ;

        var oldDetailCurveUniqueIds = openingStore.DetailUniqueIds.Where( x => opening.Document.GetElement( x ) is { } element && element.OwnerViewId == viewPlan.Id ) ;
        var detailCurveIds = oldDetailCurveUniqueIds.Select( x => opening.Document.GetElement( x ) ).Select( x => x.Id ).ToList() ;
        opening.Document.Delete( detailCurveIds ) ;

        var oldCableTrays = CreateCylindricalShaftCommandBase.GetOldCableTrays( opening.Document, new List<ShaftOpeningModel>{ openingStore } ) ;
        openingStore.DetailUniqueIds.RemoveAll( x => opening.Document.GetElement( x ) is { } element && element.OwnerViewId == viewPlan.Id ) ;
        var cableTrayUniqueId = openingStore.CableTrayUniqueIds.FirstOrDefault( x => opening.Document.GetElement( x ) is { } element && element.LevelId == viewPlan.GenLevel.Id ) ?? string.Empty ;
        var newDetailCurves = CreateCylindricalShaftCommandBase.CreateSymbolForShaftOpeningOnViewPlan( opening, viewPlan, styleForSymbol, styleForBodyDirection, styleForOuterShape, openingStore.Size, cableTrayUniqueId, oldCableTrays ) ;
        openingStore.DetailUniqueIds.AddRange( newDetailCurves.Select( x => x ) ) ;
      }

      storable.Save() ;
    }

    private static void UpdateSymbolInformation( Document document, IEnumerable<Element> elementsInView, int viewScale )
    {
      var symbolInstances = elementsInView.OfType<FamilyInstance>().Where( x => x.Symbol.FamilyName == ElectricalRoutingFamilyType.SymbolStar.GetFamilyName() || x.Symbol.FamilyName == ElectricalRoutingFamilyType.SymbolCircle.GetFamilyName() )
        .EnumerateAll() ;
      if ( ! symbolInstances.Any() )
        return ;

      var symbolInformationModels = document.GetSymbolInformationStorable().AllSymbolInformationModelData ;
      var ratio = ImportDwgMappingModel.GetMagnificationOfView( viewScale ) / 100d ;

      foreach ( var symbolInstance in symbolInstances ) {
        if ( symbolInformationModels.SingleOrDefault( x => x.SymbolUniqueId == symbolInstance.UniqueId ) is not { } symbolInformationModel )
          continue ;

        if ( document.GetElement( symbolInformationModel.TagUniqueId ) is not IndependentTag independentTag )
          continue ;
        
        var height = symbolInformationModel.Height * ratio ;

        if ( symbolInstance.LookupParameter( SymbolInformationCommandBase.ParameterName ) is { } parameter ) {
          parameter.Set( height.MillimetersToRevitUnits() / 2 ) ;
          document.Regenerate() ;
        }

        SymbolInformationCommandBase.MoveTag( symbolInstance, independentTag, (SymbolCoordinate) Enum.Parse( typeof( SymbolCoordinate ), symbolInformationModel.SymbolCoordinate ) ) ;
      }
    }

    private IEnumerable<Curve> GetSingleBorderTextNote( TextNote textNote, double scale )
    {
      var curveLoop = GeometryHelper.GetOutlineTextNote( textNote, scale ) ;
      return curveLoop.OfType<Curve>().ToList() ;
    }

    private IEnumerable<Curve> GetDoubleBorderTextNote( TextNote textNote, ViewPlan viewPlan )
    {
      var curveLoop = GeometryHelper.GetOutlineTextNote( textNote, viewPlan.Scale ) ;
      var curves = curveLoop.OfType<Curve>().ToList() ;
      var curveLoopOffset = CurveLoop.CreateViaOffset( curveLoop, -0.5.MillimetersToRevitUnits() * viewPlan.Scale, viewPlan.ViewDirection ) ;
      curves.AddRange( curveLoopOffset.OfType<Curve>() ) ;
      return curves ;
    }
    
    private static List<ElementId> CreateDetailCurve( Document document, TextNote textNote, IEnumerable<Curve> curves )
    {
      var curveIds = new List<ElementId>() ;
      var graphicStyle = document.Settings.Categories.get_Item( BuiltInCategory.OST_CurvesMediumLines ).GetGraphicsStyle( GraphicsStyleType.Projection ) ;
      foreach ( var curve in curves ) {
        var dl = document.Create.NewDetailCurve( (View) document.GetElement( textNote.OwnerViewId ), curve ) ;
        dl.LineStyle = graphicStyle;
        curveIds.Add(dl.Id);
      }
      return curveIds ;
    }
    
    private void SetDataForTextNote(StorageService<DataStorage, BorderTextNoteModel> storageService, TextNote textNote, List<ElementId> newDetailLineIds )
    {
      if ( storageService.Data.BorderTextNotes.ContainsKey(textNote.Id.IntegerValue) ) {
        var oldDetailCurveIds = storageService.Data.BorderTextNotes[textNote.Id.IntegerValue].BorderIds.Where( x => x != ElementId.InvalidElementId ).ToList() ;
        if( oldDetailCurveIds.Any() ) 
           textNote.Document.Delete( oldDetailCurveIds ) ;

        storageService.Data.BorderTextNotes[ textNote.Id.IntegerValue ] = new BorderModel { BorderIds = newDetailLineIds } ;
        storageService.SaveChange();
      }
      else {
        storageService.Data.BorderTextNotes.Add( textNote.Id.IntegerValue, new BorderModel { BorderIds = newDetailLineIds } );
        storageService.SaveChange();
      }
    }
    
    protected virtual void UpdateCeedDockPaneDataContext( UIDocument uiDocument ) {}
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