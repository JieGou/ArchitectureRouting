using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.Utils ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Revit.UI.Forms ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Autodesk.Revit.UI ;
using Autodesk.Windows ;
using RibbonButton = Autodesk.Windows.RibbonButton ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class DisplaySettingViewModel : NotifyPropertyChanged
  {
    private readonly StorageService<DataStorage, DisplaySettingModel> _displaySettingByGradeStorageService ;

    private readonly Document _document ;
    private DisplaySettingModel _dataDisplaySettingModel ;
    private readonly bool _isCallFromDefaultSetting ;

    public DisplaySettingViewModel( Document document, bool isCallFromDefaultSetting = false )
    {
      _document = document ;
      var dataStorage = document.FindOrCreateDataStorage<DisplaySettingModel>( false ) ;
      _displaySettingByGradeStorageService = new StorageService<DataStorage, DisplaySettingModel>( dataStorage ) ;
      _dataDisplaySettingModel = _displaySettingByGradeStorageService.Data.Clone() ;
      _isCallFromDefaultSetting = isCallFromDefaultSetting ;
    }

    public DisplaySettingModel DataDisplaySettingModel
    {
      get => _dataDisplaySettingModel ;
      set
      {
        _dataDisplaySettingModel = value ;
        OnPropertyChanged() ;
      }
    }

    public RelayCommand<Window> CancelCommand => new(Cancel) ;
    public RelayCommand<Window> ExecuteCommand => new(Execute) ;

    private void Cancel( Window window )
    {
      window.DialogResult = false ;
      window.Close() ;
    }

    private void Execute( Window window )
    {
      try {
        var result = _document.TransactionGroup( "TransactionName.Commands.Initialization.DisplaySetting".GetAppStringByKeyOrDefault( "Display Setting" ), _ =>
        {
          if ( ! _isCallFromDefaultSetting ) {
            ApplyChanges( _document, _dataDisplaySettingModel ) ;
          }
          
          SaveDisplaySettingByGradeStorageService() ;
          UpdateIsEnableButton( _document, _dataDisplaySettingModel.IsDetailSymbolVisible ) ;

          return Result.Succeeded ;
        } ) ;

        window.DialogResult = result == Result.Succeeded ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        window.DialogResult = false ;
      }

      window.Close() ;
    }

    public static void ApplyChanges( Document document, DisplaySettingModel displaySettingModel )
    {
      using var progress = ProgressBar.ShowWithNewThread( new UIApplication( document.Application ) ) ;
      progress.Message = "Processing..." ;
      var views = document.GetAllElements<View>().Where( v => v is View3D or ViewSheet or ViewPlan { CanBePrinted: true, ViewType: ViewType.FloorPlan } ).ToList() ;

      using var setupTransaction = new Transaction( document, "Setup Display Setting" ) ;
      setupTransaction.Start() ;

      using ( var progressData = progress.Reserve( 0.2 ) ) {
        SetupDisplayWiring( document, views, displaySettingModel.IsWiringVisible ) ;
        progressData.ThrowIfCanceled() ;
      }

      using ( var progressData = progress.Reserve( 0.1 ) ) {
        SetupDisplayDetailSymbol( document, views, displaySettingModel.IsDetailSymbolVisible ) ;
        progressData.ThrowIfCanceled() ;
      }

      using ( var progressData = progress.Reserve( 0.1 ) ) {
        SetupDisplayPullBox( views, displaySettingModel.IsPullBoxVisible ) ;
        progressData.ThrowIfCanceled() ;
      }

      using ( var progressData = progress.Reserve( 0.1 ) ) {
        SetupDisplaySchedule( document, views, displaySettingModel.IsScheduleVisible ) ;
        progressData.ThrowIfCanceled() ;
      }

      using ( var progressData = progress.Reserve( 0.1 ) ) {
        UpdateSetCodeFollowGrade( document, displaySettingModel ) ;
        progressData.ThrowIfCanceled() ;
      }

      using ( var progressData = progress.Reserve( 0.2 ) ) {
        SetupDisplayLegend( document, views, displaySettingModel.IsLegendVisible ) ;
        progressData.ThrowIfCanceled() ;
      }

      setupTransaction.Commit() ;

      using ( var progressData = progress.Reserve( 0.2 ) ) {
        document.RefreshActiveView() ;

        // Refresh Viewports
        var viewportsOfActiveView = document.GetAllElements<Viewport>().Where( vp => vp.OwnerViewId == document.ActiveView.Id ).Select( vp => document.GetElement( vp.ViewId ) as View ) ;
        document.RefreshViews( viewportsOfActiveView ) ;
        progressData.ThrowIfCanceled() ;
      }

      progress.Finish() ;
    }

    private static void UpdateSetCodeFollowGrade( Document document, DisplaySettingModel displaySettingModel )
    {
      var setCodes = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.OtherElectricalElements ).OfNotElementType() ;
      if ( ! setCodes.Any() )
        return ;

      foreach ( var setCode in setCodes ) {
        if ( ! setCode.HasParameter( DefaultSettingCommandBase.Grade3FieldName ) )
          continue ;

        setCode.SetProperty( DefaultSettingCommandBase.Grade3FieldName, displaySettingModel.GradeOption == displaySettingModel.GradeOptions[ 0 ] ) ;
      }
    }
    
    private static void SetupDisplayWiring( Document document, List<View> views, bool isVisible )
    {
      var notationFilter = FilterUtil.FindOrCreateSelectionFilter(document, RackCommandBase.NotationSelectionName) ;
      var leakFilter = FilterUtil.FindOrCreateSelectionFilter(document, ChangeWireTypeCommand.LeakSelectionName) ;
      foreach ( var view in views ) {
        var conduitCategoryId = new ElementId( BuiltInCategory.OST_Conduit ) ;
        if(view.CanCategoryBeHidden(conduitCategoryId))
          view.SetCategoryHidden(conduitCategoryId, !isVisible);
        
        var conduitFittingCategoryId = new ElementId( BuiltInCategory.OST_ConduitFitting ) ;
        if(view.CanCategoryBeHidden(conduitFittingCategoryId))
          view.SetCategoryHidden(conduitFittingCategoryId, !isVisible);
        
        var cableTrayCategoryId = new ElementId( BuiltInCategory.OST_CableTray ) ;
        if(view.CanCategoryBeHidden(cableTrayCategoryId))
          view.SetCategoryHidden(cableTrayCategoryId, !isVisible);
        
        var cableTrayFittingCategoryId = new ElementId( BuiltInCategory.OST_CableTrayFitting ) ;
        if(view.CanCategoryBeHidden(cableTrayFittingCategoryId))
          view.SetCategoryHidden(cableTrayFittingCategoryId, !isVisible);
        
        var mechanicalEquipmentCategoryId = new ElementId( BuiltInCategory.OST_MechanicalEquipment ) ;
        if(view.CanCategoryBeHidden(mechanicalEquipmentCategoryId))
          view.SetCategoryHidden(mechanicalEquipmentCategoryId, !isVisible);
        
        if(!view.IsFilterApplied(notationFilter.Id))
          view.AddFilter(notationFilter.Id);
        view.SetFilterVisibility(notationFilter.Id, isVisible);
        
        if(!view.IsFilterApplied(leakFilter.Id))
          view.AddFilter(leakFilter.Id);
        view.SetFilterVisibility(leakFilter.Id, isVisible);
      }
    }

    private static void SetupDisplayDetailSymbol( Document document, List<View> views, bool isVisible )
    {
      var detailSymbolFilter = FilterUtil.FindOrCreateSelectionFilter(document, CreateDetailSymbolCommandBase.DetailSymbolSelectionName) ;
      foreach ( var view in views ) {
        if(!view.IsFilterApplied(detailSymbolFilter.Id))
          view.AddFilter(detailSymbolFilter.Id);
        view.SetFilterVisibility(detailSymbolFilter.Id, isVisible);
      }
    }

    private static void SetupDisplayPullBox( List<View> views, bool isVisible )
    {
      foreach ( var view in views ) {
        PullBoxRouteManager.SetHiddenPullBoxByFilter(view, isVisible);
      }
    }

    private static void SetupDisplaySchedule( Document document, List<View> views, bool isVisible )
    {
      foreach ( var view in views ) {
        if ( isVisible ) {
          var scheduleSheetInstances = document.GetAllInstances<ScheduleSheetInstance>().Where( x => x.OwnerViewId == view.Id && x.IsHidden( view ) ).EnumerateAll() ;
          if(!scheduleSheetInstances.Any())
            continue;
          
          view.UnhideElements(scheduleSheetInstances.Select(x => x.Id).ToList());
        }
        else {
          var scheduleSheetInstances = document.GetAllInstances<ScheduleSheetInstance>().Where( x => x.OwnerViewId == view.Id && !x.IsHidden( view ) ).EnumerateAll() ;
          if(!scheduleSheetInstances.Any())
            continue;
          
          view.HideElements(scheduleSheetInstances.Select(x => x.Id).ToList());
        }
      }
    }
    
    private static void SetupDisplayLegend( Document document, List<View> views, bool isVisible )
    {
      var activeView = document.ActiveView ;
      foreach ( var view in views ) {
        if ( isVisible ) {
          var viewPorts = document.GetAllInstances<Viewport>().Where( x => x.SheetId == view.Id && x.IsHidden( view ) ).EnumerateAll() ;
          if(!viewPorts.Any())
            continue;

          foreach ( var viewPort in viewPorts ) {
            var viewInViewport = (View) document.GetElement( viewPort.ViewId ) ;
            if(viewInViewport.ViewType != ViewType.Legend)
              continue;
            
            var data = viewPort.GetData<CategoryShowModel>() ;
            if(null == data)
              continue;

            foreach ( var categoryId in data.CategoryIds ) {
              viewInViewport.SetCategoryHidden(categoryId, false);
            }
            
            view.UnhideElements(new List<ElementId>{ viewPort.Id });
          }
          
          var uiDocument = new UIDocument( document ) ;
          uiDocument.RequestViewChange(activeView);
        }
        else {
          var viewPorts = document.GetAllInstances<Viewport>().Where( x => x.SheetId == view.Id && !x.IsHidden( view ) ).EnumerateAll() ;
          if(!viewPorts.Any())
            continue;
          
          foreach ( var viewPort in viewPorts ) {
            var viewInViewport = (View) document.GetElement( viewPort.ViewId ) ;
            if(viewInViewport.ViewType != ViewType.Legend)
              continue;
            
            var categoryShowModel = new CategoryShowModel() ;
            
            var enumerator = document.Settings.Categories.GetEnumerator();
            while(enumerator.MoveNext())
            {
              if(enumerator.Current is not Category category || viewInViewport.GetCategoryHidden(category.Id) || ! viewInViewport.CanCategoryBeHidden( category.Id ))
                continue;

              categoryShowModel.CategoryIds.Add(category.Id);
              viewInViewport.SetCategoryHidden(category.Id, true);
            }
            
            viewPort.SetData(categoryShowModel);
            view.HideElements(new List<ElementId>{ viewPort.Id });
          }
        }
      }
    }

    private static void UpdateIsEnableButton( Document document, bool isEnable )
    {
      var targetTabName = "Electrical.App.Routing.TabName".GetAppStringByKey() ;
      var selectionTab = UIHelper.GetRibbonTabFromName( targetTabName ) ;
      if ( selectionTab == null ) return ;

      var targetRibbonPanel = UIHelper.GetRibbonPanelFromName( "Electrical.App.Panels.Routing.Table".GetAppStringByKeyOrDefault( "Table" ), selectionTab ) ;
      if ( targetRibbonPanel == null ) return ;

      foreach ( var ribbonItem in targetRibbonPanel.Source.Items ) {
        if ( ribbonItem is not RibbonSplitButton ribbonSplitButton ) continue ;

        foreach ( var rItem in ribbonSplitButton.Items ) {
          if ( rItem is not RibbonButton ribbonButton || ribbonButton.Text != "Electrical.App.Commands.Initialization.CreateDetailTableCommand".GetDocumentStringByKeyOrDefault( document, "Create Detail Table" ) ) continue ;

          using var transaction = new Transaction( document, "Update IsEnabled Button" ) ;
          transaction.Start() ;
          rItem.IsEnabled = isEnable ;
          transaction.Commit() ;

          return ;
        }
      }
    }

    private void SaveDisplaySettingByGradeStorageService()
    {
      using Transaction transaction = new(_document, "Save Display Setting By Grade") ;
      transaction.Start() ;
      _displaySettingByGradeStorageService.Data = _dataDisplaySettingModel ;
      _displaySettingByGradeStorageService.SaveChange() ;
      transaction.Commit() ;
    }
  }
}