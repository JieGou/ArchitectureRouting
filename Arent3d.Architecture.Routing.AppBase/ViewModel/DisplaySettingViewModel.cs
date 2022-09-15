using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Autodesk.Windows ;

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
      using var transactionGroup = new TransactionGroup( _document, "Setup Display Setting By Grade" ) ;
      transactionGroup.Start() ;

      var views = _document.GetAllElements<View>().Where( v => v is View3D or ViewSheet or ViewPlan { CanBePrinted: true, ViewType: ViewType.FloorPlan } ).ToList() ;

      using var setupTransaction = new Transaction( _document, "Setup Display Setting" ) ;
      setupTransaction.Start() ;

      SetupDisplayWiring( views, _dataDisplaySettingModel.IsWiringVisible ) ;
      SetupDisplayDetailSymbol( views, _dataDisplaySettingModel.IsDetailSymbolVisible ) ;
      SetupDisplayPullBox( views, _dataDisplaySettingModel.IsPullBoxVisible ) ;
      SetupDisplaySchedule( views, _dataDisplaySettingModel.IsScheduleVisible ) ;

      setupTransaction.Commit() ;

      _document.RefreshActiveView() ;

      SaveDisplaySettingByGradeStorageService() ;

      UpdateIsEnableButton( _document, _dataDisplaySettingModel.IsDetailSymbolVisible ) ;

      transactionGroup.Commit() ;

      window.DialogResult = true ;
      window.Close() ;
    }

    private void SetupDisplayWiring( List<View> views, bool isVisible )
    {
      // Electrical routing elements (conduits and cable trays)
      var hiddenOrUnhiddenElements = _document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.ElectricalRoutingElements ).ToList() ;

      // Pass points
      hiddenOrUnhiddenElements.AddRange( _document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PassPoints ) ) ;

      // Notation and rack
      var rackNotationStorable = _document.GetAllStorables<RackNotationStorable>().FirstOrDefault() ?? _document.GetRackNotationStorable() ;
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
      hiddenOrUnhiddenElements.AddRange( _document.GetAllInstances<CurveElement>().Where( x => x.LineStyle.Name == EraseLimitRackCommandBase.BoundaryCableTrayLineStyleName ) ) ;

      // Leak
      var leakageSymbolNames = new HashSet<string> { "LeakageZoneCloth", "LeakageZoneColoring", "LeakageZonePvc" } ;
      hiddenOrUnhiddenElements.AddRange( _document.GetAllInstances<FamilyInstance>().Where( x => leakageSymbolNames.Contains( x.Symbol.Family.Name ) ) ) ;
      hiddenOrUnhiddenElements.AddRange( _document.GetAllInstances<CurveElement>().Where( x => x.LineStyle.Name == "LeakageZone" ) ) ;

      HideOrUnhideElements( views, isVisible, hiddenOrUnhiddenElements ) ;
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

      HideOrUnhideElements( views, isVisible, hiddenOrUnhiddenElements ) ;
    }

    private void SetupDisplayPullBox( List<View> views, bool isVisible )
    {
      var hiddenOrUnhiddenElements = new List<Element>() ;

      // Pull boxes
      hiddenOrUnhiddenElements.AddRange( _document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_ElectricalFixtures ).Where( e => e.Name == ElectricalRoutingFamilyType.PullBox.GetFamilyName() ) ) ;

      // Text notes
      var labelOfPullBoxIds = _document.GetAllDatas<Level, PullBoxInfoModel>().SelectMany( p => p.Data.PullBoxInfoData ).Select( p => p.TextNoteUniqueId ) ;
      hiddenOrUnhiddenElements.AddRange( _document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).Where( t => labelOfPullBoxIds.Contains( t.UniqueId ) ) ) ;

      HideOrUnhideElements( views, isVisible, hiddenOrUnhiddenElements ) ;
    }

    private void SetupDisplaySchedule( List<View> views, bool isVisible )
    {
      // Schedules
      var hiddenOrUnhiddenElements = _document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_ScheduleGraphics ).Where( sg => views.Any( lv => lv.Id == sg.OwnerViewId ) ).EnumerateAll() ;
      HideOrUnhideElements( views, isVisible, hiddenOrUnhiddenElements ) ;
    }


    private static void HideOrUnhideElements( List<View> views, bool isVisible, IReadOnlyCollection<Element> hiddenOrUnhiddenElements )
    {
      views.ForEach( v => HideOrUnhideElements( v, isVisible, hiddenOrUnhiddenElements ) ) ;
    }

    private static void HideOrUnhideElements( View view, bool isVisible, IReadOnlyCollection<Element> hiddenOrUnhiddenElements )
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