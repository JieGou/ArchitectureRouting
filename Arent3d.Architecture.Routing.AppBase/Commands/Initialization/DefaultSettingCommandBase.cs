using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Collections.Specialized ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using View = Autodesk.Revit.DB.View ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class DefaultSettingCommandBase : IExternalCommand
  {
    private const string SetDefaultEcoModeTransactionName = "Electrical.App.Commands.Initialization.SetDefaultModeCommand" ;
    private const string Grade3 = "グレード3" ;
    private const string ArentDummyViewName = "Arent Dummy" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      try {
        var uiDocument = commandData.Application.ActiveUIDocument ;
        var document = uiDocument.Document ;
        var activeViewName = document.ActiveView.Name ;
        // Get data of default setting from snoop DB
        DefaultSettingStorable defaultSettingStorable = document.GetDefaultSettingStorable() ;
        SetupPrintStorable setupPrintStorable = document.GetSetupPrintStorable() ;
        var scale = setupPrintStorable.Scale ;

        var listFloorsDefault = new ObservableCollection<ImportDwgMappingModel>(GetFloorsDefault(document)) ;
        
        if( defaultSettingStorable.ImportDwgMappingData.Any())
          try {
            Transaction transaction = new( document, "Remove" ) ;
            transaction.Start() ;
            foreach ( var floorPlan in defaultSettingStorable.ImportDwgMappingData.ToList() ) {
              var existFloorPlan = listFloorsDefault.FirstOrDefault( x => x.FloorName == floorPlan.FloorName ) ;
              if ( existFloorPlan == null ) {
                defaultSettingStorable.ImportDwgMappingData.Remove( floorPlan ) ;
              }
            }
            defaultSettingStorable.Save() ;
            transaction.Commit() ;
          }
          catch ( Exception exception ) {
            CommandUtils.DebugAlertException( exception ) ;
          }
        else 
          UpdateImportDwgMappingModels(defaultSettingStorable, listFloorsDefault, new List<string>()) ;

        var viewModel = new DefaultSettingViewModel(uiDocument, defaultSettingStorable, scale, activeViewName ) ;
        var dialog = new DefaultSettingDialog( viewModel ) ;
        dialog.ShowDialog() ;
        {
          if ( dialog.DialogResult == false )
            return Result.Cancelled ;

          viewModel = dialog.ViewModel ;
          
          // Save default db
          viewModel.SaveData() ;
          
          var isEcoMode = viewModel.SelectedEcoNormalMode == DefaultSettingViewModel.EcoNormalMode.EcoMode ;
          var gradeMode = viewModel.SelectedGradeMode ;
          var importDwgMappingModels = viewModel.ImportDwgMappingModels ;
          var deletedFloorName = viewModel.DeletedFloorName ;
          SetEcoModeAndGradeModeDefaultValue( document, defaultSettingStorable, isEcoMode, gradeMode, importDwgMappingModels, deletedFloorName ) ;

          if ( deletedFloorName.Any() ) {
            RemoveViews( document, deletedFloorName, uiDocument ) ;
          }

          UpdateScaleAndHeightPlanView( document, importDwgMappingModels ) ;
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
    
    private List<ImportDwgMappingModel> GetFloorsDefault(Document doc)
    {
      List<ViewPlan> views = new List<ViewPlan>( new FilteredElementCollector( doc )
        .OfClass( typeof( ViewPlan ) ).Cast<ViewPlan>()
        .Where<ViewPlan>( v => v.CanBePrinted && ViewType.FloorPlan == v.ViewType ) ) ;
      var importDwgMappingModels = new List<ImportDwgMappingModel>() ;
      foreach ( var view in views ) {
        var fileName = string.Empty ;
        var floorName = view.Name ;
        HeightSettingStorable settingStorables = doc.GetHeightSettingStorable() ;
        var height = settingStorables.HeightSettingsData.Values.FirstOrDefault( x => x.LevelId.ToString() == view.GenLevel.Id.ToString() )?.Elevation ?? 0 ;
        
        var scale = view.Scale ;

        importDwgMappingModels.Add( new ImportDwgMappingModel( fileName, floorName, height, scale ) ) ;
      }
      
      var importDwgMappingModelsOrder = importDwgMappingModels.OrderBy( x => x.FloorHeight ).ToList() ;
      var result = new List<ImportDwgMappingModel>() ;
      result.Add( importDwgMappingModelsOrder[0] );
      for ( int i = 1 ; i < importDwgMappingModelsOrder.Count ; i++ ) {
        var height = importDwgMappingModelsOrder[ i ].FloorHeight - importDwgMappingModelsOrder[ i - 1 ].FloorHeight ;
        var importDwgModel = new ImportDwgMappingModel( importDwgMappingModelsOrder[ i ].FileName, importDwgMappingModelsOrder[ i ].FloorName, importDwgMappingModelsOrder[ i ].FloorHeight,
          importDwgMappingModelsOrder[ i ].Scale, height ) ;
        result.Add( importDwgModel );
      }

      return result ;
    }
    
    private void SetEcoModeAndGradeModeDefaultValue( Document document, DefaultSettingStorable defaultSettingStorable, bool isEcoModel, int gradeMode, ObservableCollection<ImportDwgMappingModel> importDwgMappingModels, List<string> deletedFloorName )
    {
      try {
        Transaction transaction = new( document, SetDefaultEcoModeTransactionName ) ;
        transaction.Start() ;
        var instances = new FilteredElementCollector( document ).OfClass( typeof( FamilyInstance ) ).Cast<FamilyInstance>().Where( a => a.HasParameter( Grade3 ) ).ToList() ;
        foreach ( var instance in instances ) {
          instance.SetProperty( Grade3, gradeMode == 3 ) ;
        }

        defaultSettingStorable.EcoSettingData.IsEcoMode = isEcoModel ;
        defaultSettingStorable.GradeSettingData.GradeMode = (int)gradeMode ;
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
          defaultSettingStorable.ImportDwgMappingData.Add( new Storable.Model.ImportDwgMappingModel( item.Id, item.FullFilePath, item.FileName, item.FloorName, item.FloorHeight, item.Scale, item.FloorHeightDisplay ) ) ;
        }
        else {
          oldImportDwgMappingModel.FloorHeightDisplay = item.FloorHeightDisplay ;
          oldImportDwgMappingModel.Scale = item.Scale ;
        }
      }

      defaultSettingStorable.ImportDwgMappingData = defaultSettingStorable.ImportDwgMappingData.OrderBy( x => x.FloorHeight ).ToList() ;
    }

    private void LoadDwgAndSetScale( ExternalCommandData commandData, ObservableCollection<ImportDwgMappingModel> importDwgMappingModels, List<FileComboboxItemType> fileItems )
    {
      try {
        ListDictionary levelList = new ListDictionary();
        List<string> nameLevelList = new List<string>() ;
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
        ViewPlan? firstViewPlan = null ;
        
        #region Import

        using var importTrans = new Transaction( doc ) ;
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
          if ( isNewView ) doc.Import( importDwgMappingModel.FullFilePath, dwgImportOptions, viewPlan, out ElementId importElementId ) ;
          if ( i == 0 ) firstViewPlan = viewPlan ;
        }

        importTrans.Commit() ;

        #endregion

        #region Create 3D view
        using var create3D = new Transaction( doc ) ;
        create3D.SetName( "Create 3D View" );
        create3D.Start() ;
        var levelListOrderByElavation = doc.GetAllElements<Level>()
          .OfCategory( BuiltInCategory.OST_Levels ).Select( ToLevelInfo )
          .Where( x=> nameLevelList.Contains(x.LevelName) )
          .OrderBy( l => l.Elevation ) ;
        
        var levelListToCreate3d = levelListOrderByElavation.Select( level => ( Id: level.LevelId, Name: level.LevelName ) ).ToList();

        List<View3D> all3DViews = new FilteredElementCollector(doc).OfClass(typeof(View3D)).Cast<View3D>().ToList();

        var levels = levelListToCreate3d.Where( x => all3DViews.FirstOrDefault( y => y.Name == "3D" + x.Name ) == null ).EnumerateAll() ;
        
        if(levels.Any())
          doc.Create3DView( levels ) ;
        
        create3D.Commit() ;
        #endregion

        #region Create 3D ALL view

        if ( firstViewPlan != null ) commandData.Application.ActiveUIDocument.ActiveView = firstViewPlan ;
        View? view3dAll = null ;
        using var create3DTrans = new Transaction( doc ) ;
        create3DTrans.SetName( "Create 3D ALL view" ) ;
        create3DTrans.Start() ;
        var threeDimensionalViewFamilyType = new FilteredElementCollector( doc ).OfClass( typeof( ViewFamilyType ) ).ToElements().Cast<ViewFamilyType>().FirstOrDefault( vft => vft.ViewFamily == ViewFamily.ThreeDimensional ) ;
        if ( threeDimensionalViewFamilyType != null ) {
          var allCurrent3DView = new FilteredElementCollector( doc ).OfClass( typeof( View3D ) ).ToList() ;
          const string view3DName = "3D ALL" ;
          var current3DView = allCurrent3DView.FirstOrDefault( x => x.Name.Equals( view3DName ) ) ;
          if ( current3DView != null ) doc.Delete( current3DView.Id ) ;
          current3DView = View3D.CreateIsometric( doc, threeDimensionalViewFamilyType.Id ) ;
          current3DView.Name = view3DName ;
          view3dAll = (View) current3DView  ;
        }

        create3DTrans.Commit() ;

        #endregion
        
        var viewPlans = new List<ViewPlan>( new FilteredElementCollector( doc )
          .OfClass( typeof( ViewPlan ) ).Cast<ViewPlan>()
          .Where( v => v.CanBePrinted && ViewType.FloorPlan == v.ViewType ) ) ;

        var viewPlansWithoutDummy = viewPlans.Where( x => x.Name != ArentDummyViewName ).ToList() ;
        
        #region Remove view Arent dummy
        if ( viewPlansWithoutDummy.Count > 1 ) {
          var viewIdArentDummyView = viewPlans.Where( x => x.Name == ArentDummyViewName ).Select( p=>p.Id ).ToList() ;
          if ( viewIdArentDummyView.Any() ) {
            var pCurrView = uiDocument.ActiveView ;
            uiDocument.RequestViewChange( pCurrView ) ;
            uiDocument.ActiveView = viewPlansWithoutDummy[ 0 ] ;
            
            using var removeArentDummyView = new Transaction( doc ) ;
            removeArentDummyView.SetName( "Remove view Arent dummy" ) ;
            removeArentDummyView.Start() ;
            doc.Delete( viewIdArentDummyView ) ;
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
        }

        setViewRangeTransaction.Commit() ;
        #endregion

        if ( view3dAll != null ) 
        {
          uiDocument.RequestViewChange(view3dAll);
        }
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
        var deletedViewIds = document.GetAllElements<View>()
          .Where( e => deletedFloorName.Contains( e.Name ) ).Select( e => e.Id ).ToList() ;
        List<ViewPlan> views = new List<ViewPlan>(
          new FilteredElementCollector( uiDocument.Document ).OfClass( typeof( ViewPlan ) )
            .Cast<ViewPlan>()
            .Where<ViewPlan>( v => v.CanBePrinted && ViewType.FloorPlan == v.ViewType ) ) ;
        if (views.Count() == deletedViewIds.Count()) {
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
            var pCurrView = uiDocument.ActiveView ;
            uiDocument.RequestViewChange( pCurrView ) ;
            uiDocument.ActiveView = viewsTemp[ 0 ] ;
          }
        }
        var removeViewsTrans = new Transaction( document, "Remove Views " ) ;
        removeViewsTrans.Start() ;
        document.Delete( deletedViewIds ) ;
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

      var level = Level.Create( doc, floorHeight) ;

      var viewPlan = ViewPlan.Create( doc, viewFamily.Id, level.Id ) ;
      viewPlan.Name = floorName ;

      viewPlan.Scale = scale ;
      if ( null != viewPlan.ViewTemplateId && doc.GetElement( viewPlan.ViewTemplateId ) is View viewTemplate && viewTemplate.Scale != scale) {
        viewTemplate.Scale = scale;
      }
      level.SetProperty( BuiltInParameter.LEVEL_ELEV, floorHeight.MillimetersToRevitUnits() ) ;
      importTrans.Commit() ;
      
      var pCurrView = uiDocument.ActiveView ;
      uiDocument.RequestViewChange( pCurrView ) ;
      uiDocument.ActiveView = viewPlan ;
    }

    private void UpdateScaleAndHeightPlanView(  Document document, ObservableCollection<ImportDwgMappingModel> importDwgMappingModels )
    {
      List<ViewPlan> views = new List<ViewPlan>( new FilteredElementCollector( document )
          .OfClass( typeof( ViewPlan ) )
          .Cast<ViewPlan>()
          .Where<ViewPlan>( v => v.CanBePrinted && ViewType.FloorPlan == v.ViewType ) ) ;
      
      var allCurrentLevels = new FilteredElementCollector( document ).OfClass( typeof( Level ) ).ToList() ;
      var updateScaleTrans = new Transaction( document ) ;
      updateScaleTrans.SetName( "Update Scale" ) ;
      updateScaleTrans.Start() ;
      foreach ( var importDwgMappingModel in importDwgMappingModels ) {
        var viewPlan = views.FirstOrDefault( x => x.Name == importDwgMappingModel.FloorName ) ;
        if ( viewPlan != null && viewPlan.Scale != importDwgMappingModel.Scale) {
          viewPlan.Scale = importDwgMappingModel.Scale ;
          if ( null != viewPlan.ViewTemplateId && document.GetElement( viewPlan.ViewTemplateId ) is View viewTemplate && viewTemplate.Scale != importDwgMappingModel.Scale ) {
            viewTemplate.Scale = importDwgMappingModel.Scale ;
          }
        }
        
        var levelName = importDwgMappingModel.FloorName ;
        var importDwgLevel = allCurrentLevels.FirstOrDefault( x => x.Name.Equals( levelName ) ) ;
        importDwgLevel?.SetProperty( BuiltInParameter.LEVEL_ELEV, importDwgMappingModel.FloorHeight.MillimetersToRevitUnits() ) ;
      }
      updateScaleTrans.Commit() ;
    }
  }
}