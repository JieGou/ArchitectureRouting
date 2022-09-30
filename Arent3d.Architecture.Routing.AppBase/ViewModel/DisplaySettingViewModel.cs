using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
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

    public DisplaySettingViewModel( Document document )
    {
      _document = document ;
      var dataStorage = document.FindOrCreateDataStorage<DisplaySettingModel>( false ) ;
      _displaySettingByGradeStorageService = new StorageService<DataStorage, DisplaySettingModel>( dataStorage ) ;
      _dataDisplaySettingModel = _displaySettingByGradeStorageService.Data.Clone() ;
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
          using var progress = ProgressBar.ShowWithNewThread( new UIApplication( _document.Application ) ) ;
          progress.Message = "Processing..." ;
          var views = _document.GetAllElements<View>().Where( v => v is View3D or ViewSheet or ViewPlan { CanBePrinted: true, ViewType: ViewType.FloorPlan } ).ToList() ;

          using var setupTransaction = new Transaction( _document, "Setup Display Setting" ) ;
          setupTransaction.Start() ;

          using ( var progressData = progress.Reserve( 0.2 ) ) {
            SetupDisplayWiring( views, _dataDisplaySettingModel.IsWiringVisible ) ;
            progressData.ThrowIfCanceled() ;
          }

          using ( var progressData = progress.Reserve( 0.1 ) ) {
            SetupDisplayDetailSymbol( views, _dataDisplaySettingModel.IsDetailSymbolVisible ) ;
            progressData.ThrowIfCanceled() ;
          }

          using ( var progressData = progress.Reserve( 0.1 ) ) {
            SetupDisplayPullBox( views, _dataDisplaySettingModel.IsPullBoxVisible ) ;
            progressData.ThrowIfCanceled() ;
          }

          using ( var progressData = progress.Reserve( 0.1 ) ) {
            SetupDisplaySchedule( views, _dataDisplaySettingModel.IsScheduleVisible ) ;
            progressData.ThrowIfCanceled() ;
          }

          using ( var progressData = progress.Reserve( 0.2 ) ) {
            SetupDisplayLegend( views, _dataDisplaySettingModel ) ;
            progressData.ThrowIfCanceled() ;
          }

          setupTransaction.Commit() ;

          using ( var progressData = progress.Reserve( 0.2 ) ) {
            _document.RefreshActiveView() ;

            // Refresh viewports
            var viewportsOfActiveView = _document.GetAllElements<Viewport>().Where( vp => vp.OwnerViewId == _document.ActiveView.Id ).Select( vp => _document.GetElement( vp.ViewId ) as View ) ;
            _document.RefreshViews( viewportsOfActiveView ) ;
            progressData.ThrowIfCanceled() ;
          }

          using ( var progressData = progress.Reserve( 0.1 ) ) {
            SaveDisplaySettingByGradeStorageService() ;

            UpdateIsEnableButton( _document, _dataDisplaySettingModel.IsDetailSymbolVisible ) ;
            progressData.ThrowIfCanceled() ;
          }

          progress.Finish() ;

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

    private void SetupDisplayWiring( List<View> views, bool isVisible )
    {
      // Electrical routing elements (conduits and cable trays)
      var hiddenOrUnhiddenElements = _document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.ElectricalRoutingElements ).ToList() ;

      // Pass points
      hiddenOrUnhiddenElements.AddRange( _document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PassPoints ) ) ;

      // Notation and rack
      var rackNotationStorable = _document.GetAllStorables<RackNotationStorable>().SingleOrDefault() ?? _document.GetRackNotationStorable() ;
      foreach ( var rackNotationModel in rackNotationStorable.RackNotationModelData ) {
        if ( _document.GetElement( rackNotationModel.RackId ) is { } rack )
          hiddenOrUnhiddenElements.Add( rack ) ;

        if ( _document.GetElement( rackNotationModel.NotationId ) is { } textNote )
          hiddenOrUnhiddenElements.Add( textNote ) ;

        if ( _document.GetElement( rackNotationModel.EndLineLeaderId ) is { } endLine )
          hiddenOrUnhiddenElements.Add( endLine ) ;

        foreach ( var otherLineId in rackNotationModel.OtherLineIds )
          if ( _document.GetElement( otherLineId ) is { } otherLine )
            hiddenOrUnhiddenElements.Add( otherLine ) ;
      }

      // Boundary rack
      hiddenOrUnhiddenElements.AddRange( _document.GetAllInstances<CurveElement>().Where( x => x.LineStyle.Name == EraseRackCommandBase.BoundaryCableTrayLineStyleName ) ) ;

      // Leak
      hiddenOrUnhiddenElements.AddRange( _document.GetAllInstances<FamilyInstance>().Where( x => ChangeWireTypeCommand.WireSymbolOptions.Values.Contains( x.Symbol.Family.Name ) ) ) ;
      hiddenOrUnhiddenElements.AddRange( _document.GetAllInstances<CurveElement>().Where( x => x.LineStyle.Name == ChangeWireTypeCommand.SubcategoryName ) ) ;

      HideOrUnHideElements( views, isVisible, hiddenOrUnhiddenElements ) ;
    }

    private void SetupDisplayDetailSymbol( List<View> views, bool isVisible )
    {
      // Detail symbols
      var hiddenOrUnhiddenElements = new List<Element>() ;

      foreach ( var view in views ) {
        if ( view is not ViewPlan ) continue ;

        var storageService = new StorageService<Level, DetailSymbolModel>( view.GenLevel ) ;
        foreach ( var detailSymbolItemModel in storageService.Data.DetailSymbolData.Where( detailSymbolItemModel => detailSymbolItemModel != null ) ) {
          hiddenOrUnhiddenElements.AddRange( _document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Lines ).Where( t => detailSymbolItemModel.LineUniqueIds.Split( ',' ).Contains( t.UniqueId ) ) ) ;
          hiddenOrUnhiddenElements.AddRange( _document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).Where( t => detailSymbolItemModel.DetailSymbolUniqueId == t.UniqueId ) ) ;
        }
      }

      HideOrUnHideElements( views, isVisible, hiddenOrUnhiddenElements ) ;
    }

    private void SetupDisplayPullBox( List<View> views, bool isVisible )
    {
      var hiddenOrUnhiddenElements = new List<Element>() ;

      // Pull boxes
      hiddenOrUnhiddenElements.AddRange( _document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_ElectricalFixtures ).Where( e => ( e.Name == ElectricalRoutingFamilyType.PullBox.GetFamilyName() || e.Name == ElectricalRoutingFamilyType.Handhole.GetFamilyName() ) ) ) ;

      // Text notes
      var labelOfPullBoxIds = _document.GetAllDatas<Level, PullBoxInfoModel>().SelectMany( p => p.Data.PullBoxInfoData ).Select( p => p.TextNoteUniqueId ) ;
      hiddenOrUnhiddenElements.AddRange( _document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).Where( t => labelOfPullBoxIds.Contains( t.UniqueId ) ) ) ;

      HideOrUnHideElements( views, isVisible, hiddenOrUnhiddenElements ) ;
    }

    private static void SetupDisplaySchedule( List<View> views, bool isVisible )
    {
      foreach ( var view in views ) {
        var category = Category.GetCategory( view.Document, BuiltInCategory.OST_ScheduleGraphics ) ;
        if ( ! view.CanCategoryBeHidden( category.Id ) )
          continue ;

        view.SetCategoryHidden( category.Id, !isVisible ) ;
      }
    }

    private void SetupDisplayLegend( List<View> views, DisplaySettingModel displaySettingModel )
    {
      // Legends
      var legendViews = _document.GetAllElements<View>().Where( vp => vp.ViewType == ViewType.Legend ).ToList() ;
      if ( ! legendViews.Any() ) return ;

      if ( displaySettingModel.IsLegendVisible ) {
        if ( displaySettingModel.HiddenLegendElementIds.Any() ) {
          foreach ( var legendView in legendViews ) {
            var hiddenElementIds = displaySettingModel.HiddenLegendElementIds.Where( e => _document.GetElement( e ) is { } element && element.IsHidden( legendView ) ).Select( e => _document.GetElement( e ).Id ).ToList() ;
            if ( hiddenElementIds.Any() )
              legendView.UnhideElements( hiddenElementIds ) ;
          }

          displaySettingModel.HiddenLegendElementIds = new List<string>() ;
        }
      }
      else {
        foreach ( var legendView in legendViews ) {
          var allElementsInLegendView = new FilteredElementCollector( _document, legendView.Id ) ;
          displaySettingModel.HiddenLegendElementIds.AddRange( allElementsInLegendView.Select( e => e.UniqueId ) ) ;
          if ( allElementsInLegendView.Any() )
            legendView.HideElements( allElementsInLegendView.ToElementIds() ) ;
        }
      }

      var hiddenOrUnhiddenElements = _document.GetAllElements<Viewport>().Where( vp => legendViews.Any( lv => lv.Id.IntegerValue == vp.ViewId.IntegerValue ) ).EnumerateAll() ;
      HideOrUnHideElements( views, displaySettingModel.IsLegendVisible, hiddenOrUnhiddenElements ) ;
    }

    private static void HideOrUnHideElements( List<View> views, bool isVisible, IReadOnlyCollection<Element> hiddenOrUnhiddenElements )
    {
      views.ForEach( v => HideOrUnHideElements( v, isVisible, hiddenOrUnhiddenElements ) ) ;
    }

    private static void HideOrUnHideElements( View view, bool isVisible, IReadOnlyCollection<Element> hiddenOrUnhiddenElements )
    {
      if ( isVisible ) {
        var hiddenElementIds = hiddenOrUnhiddenElements.Where( e => e.IsHidden( view ) ).Select( h => h.Id ).ToList() ;
        if ( hiddenElementIds.Any() )
          view.UnhideElements( hiddenElementIds ) ;
      }
      else {
        var unhiddenElementIds = hiddenOrUnhiddenElements.Where( e => ! e.IsHidden( view ) ).Select( h => h.Id ).ToList() ;
        if ( unhiddenElementIds.Any() )
          view.HideElements( unhiddenElementIds ) ;
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