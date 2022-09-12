﻿using System.Collections.Generic ;
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
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Autodesk.Windows ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class DisplaySettingByGradeViewModel : NotifyPropertyChanged
  {
    private const string GradeMode1Or2 = "1~2" ;

    private readonly StorageService<DataStorage, DisplaySettingByGradeModel> _displaySettingByGradeStorageService ;

    private readonly Document _document ;

    private readonly string _gradeMode ;

    private List<DisplaySettingByGradeItemModel> _dataDisplaySettingByGradeModel ;

    private DisplaySettingByGradeItemModel _selectedGradeItemModel ;

    public DisplaySettingByGradeViewModel( Document document )
    {
      _document = document ;
      _selectedGradeItemModel = new DisplaySettingByGradeItemModel() ;

      var defaultSettingStorable = _document.GetDefaultSettingStorable() ;
      _gradeMode = defaultSettingStorable.GradeSettingData.GradeMode switch
      {
        1 or 2 => "1~2",
        _ => defaultSettingStorable.GradeSettingData.GradeMode.ToString()
      } ;

      var dataStorage = document.FindOrCreateDataStorage<DisplaySettingByGradeModel>( false ) ;
      _displaySettingByGradeStorageService = new StorageService<DataStorage, DisplaySettingByGradeModel>( dataStorage ) ;
      _dataDisplaySettingByGradeModel = _displaySettingByGradeStorageService.Data.DisplaySettingByGradeData ;

      if ( ! _dataDisplaySettingByGradeModel.Any() ) {
        _dataDisplaySettingByGradeModel.Add( new DisplaySettingByGradeItemModel( GradeMode1Or2,
          new DisplaySettingByGradeItemDetailsModel( false ), new DisplaySettingByGradeItemDetailsModel( false ),
          new DisplaySettingByGradeItemDetailsModel( false ), new DisplaySettingByGradeItemDetailsModel( true ) ) ) ;

        _dataDisplaySettingByGradeModel.Add( new DisplaySettingByGradeItemModel( "3",
          new DisplaySettingByGradeItemDetailsModel( false ), new DisplaySettingByGradeItemDetailsModel( false ),
          new DisplaySettingByGradeItemDetailsModel( false ), new DisplaySettingByGradeItemDetailsModel( true ) ) ) ;

        _dataDisplaySettingByGradeModel.Add( new DisplaySettingByGradeItemModel( "4",
          new DisplaySettingByGradeItemDetailsModel( false ), new DisplaySettingByGradeItemDetailsModel( false ),
          new DisplaySettingByGradeItemDetailsModel( true ), new DisplaySettingByGradeItemDetailsModel( true ) ) ) ;

        _dataDisplaySettingByGradeModel.Add( new DisplaySettingByGradeItemModel( "5",
          new DisplaySettingByGradeItemDetailsModel( false ), new DisplaySettingByGradeItemDetailsModel( false ),
          new DisplaySettingByGradeItemDetailsModel( true ), new DisplaySettingByGradeItemDetailsModel( false ) ) ) ;

        _dataDisplaySettingByGradeModel.Add( new DisplaySettingByGradeItemModel( "6",
          new DisplaySettingByGradeItemDetailsModel( false ), new DisplaySettingByGradeItemDetailsModel( true ),
          new DisplaySettingByGradeItemDetailsModel( true ), new DisplaySettingByGradeItemDetailsModel( false ) ) ) ;

        _dataDisplaySettingByGradeModel.Add( new DisplaySettingByGradeItemModel( "7",
          new DisplaySettingByGradeItemDetailsModel( true ), new DisplaySettingByGradeItemDetailsModel( true ),
          new DisplaySettingByGradeItemDetailsModel( true ), new DisplaySettingByGradeItemDetailsModel( false ) ) ) ;
      }

      // Set default value for grade selection
      _selectedGradeItemModel = DataDisplaySettingByGradeModel.Find( t => t.GradeMode == _gradeMode ) ??
                                new DisplaySettingByGradeItemModel() ;
    }

    public List<DisplaySettingByGradeItemModel> DataDisplaySettingByGradeModel
    {
      get => _dataDisplaySettingByGradeModel ;
      set
      {
        _dataDisplaySettingByGradeModel = value ;
        OnPropertyChanged() ;
      }
    }

    public DisplaySettingByGradeItemModel SelectedGradeItemModel
    {
      get => _selectedGradeItemModel ;
      set
      {
        _selectedGradeItemModel = value ;
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
      
      SaveDisplaySettingByGradeStorageService() ;

      var applyGradeItemModel = SelectedGradeItemModel ;
      // var applyGradeItemModel = DataDisplaySettingByGradeModel.Find( t => t.GradeMode == _gradeMode ) ;
      var views = _document.GetAllElements<View>()
        .Where( v => v is View3D or ViewPlan { CanBePrinted: true, ViewType: ViewType.FloorPlan } ).ToList() ;
      
      using var setupTransaction = new Transaction( _document, "Setup Display Setting" ) ;
      setupTransaction.Start() ;
      
      SetupDisplayWiring( views, applyGradeItemModel.Wiring.IsVisible ) ;
      SetupDisplayDetailSymbol( views, applyGradeItemModel.DetailSymbol.IsVisible ) ;
      SetupDisplayPullBox( views, applyGradeItemModel.PullBox.IsVisible ) ;
      // SetupDisplayDetailSymbol( views, applyGradeItemModel.DetailSymbol.IsVisible ) ;

      setupTransaction.Commit() ;

      transactionGroup.Commit() ;
      window.DialogResult = true ;
      window.Close() ;
    }

    private void SetupDisplayWiring( List<View> views, bool isVisible )
    {
      // Electrical routing elements (conduits and cable trays)
      var hiddenOrUnhiddenElements = _document.GetAllElements<Element>()
        .OfCategory( BuiltInCategorySets.ElectricalRoutingElements ).ToList() ;

      // Pass points
      hiddenOrUnhiddenElements.AddRange( _document.GetAllElements<Element>()
        .OfCategory( BuiltInCategorySets.PassPoints ) ) ;

      // Notation and rack
      var rackNotationStorable = _document.GetAllStorables<RackNotationStorable>().FirstOrDefault() ??
                                 _document.GetRackNotationStorable() ;
      foreach ( var rackNotationModel in rackNotationStorable.RackNotationModelData ) {
        if ( _document.GetElement( rackNotationModel.RackId ) is { } rack )
          hiddenOrUnhiddenElements.Add( rack ) ;

        if ( _document.GetElement( rackNotationModel.NotationId ) is { } textNote )
          hiddenOrUnhiddenElements.Add( textNote ) ;

        if ( _document.GetElement( rackNotationModel.EndLineLeaderId ) is { } endLine )
          hiddenOrUnhiddenElements.Add( endLine) ;

        foreach ( var otherLineId in rackNotationModel.OtherLineIds )
          if ( _document.GetElement( otherLineId ) is { } otherLine )
            hiddenOrUnhiddenElements.Add( otherLine) ;
      }

      // Boundary rack
      hiddenOrUnhiddenElements.AddRange( _document.GetAllInstances<CurveElement>()
        .Where( x => x.LineStyle.Name == EraseLimitRackCommandBase.BoundaryCableTrayLineStyleName ) ) ;

      // Leak
      var leakageSymbolNames = new HashSet<string> { "LeakageZoneCloth", "LeakageZoneColoring", "LeakageZonePvc" } ;
      hiddenOrUnhiddenElements.AddRange( _document.GetAllInstances<FamilyInstance>()
        .Where( x => leakageSymbolNames.Contains( x.Symbol.Family.Name ) ) ) ;
      hiddenOrUnhiddenElements.AddRange( _document.GetAllInstances<CurveElement>()
        .Where( x => x.LineStyle.Name == "LeakageZone" ) ) ;

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

      UpdateIsEnableButton( _document, isVisible ) ;
    }
    
    private void SetupDisplayPullBox( List<View> views, bool isVisible )
    {
      var hiddenOrUnhiddenElements = new List<Element>() ;
      
      // Pull boxes
      hiddenOrUnhiddenElements.AddRange( _document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_ElectricalFixtures ).Where( e => e.Name == ElectricalRoutingFamilyType.PullBox.GetFamilyName() ) ) ;
      
      // Text notes
      var labelOfPullBoxIds = _document.GetAllDatas<Level, PullBoxInfoModel>().SelectMany( p => p.Data.PullBoxInfoData )
        .Select( p => p.TextNoteUniqueId ) ;
      hiddenOrUnhiddenElements.AddRange( _document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).Where( t => labelOfPullBoxIds.Contains( t.UniqueId ) ) ) ;
      
      HideOrUnhideElements( views, isVisible, hiddenOrUnhiddenElements ) ;
    }
    
    private static void HideOrUnhideElements( List<View> views, bool isVisible, IReadOnlyCollection<Element> hiddenOrUnhiddenElements )
    {
      if ( isVisible ) {
        foreach ( var v in views ) {
          var hiddenElementIds = hiddenOrUnhiddenElements.Where( e => e.IsHidden( v ) ).Select( h => h.Id ).ToList() ;
          if ( hiddenElementIds.Any() )
            v.UnhideElements( hiddenElementIds ) ;
        }
      }
      else {
        foreach ( var v in views ) {
          var unhiddenElementIds = hiddenOrUnhiddenElements.Where( e => ! e.IsHidden( v ) ).Select( h => h.Id ).ToList() ;
          if ( unhiddenElementIds.Any() )
            v.HideElements( unhiddenElementIds ) ;
        }
      }
    }

    private static void UpdateIsEnableButton( Document document, bool isEnable )
    {
      var targetTabName = "Electrical.App.Routing.TabName".GetAppStringByKey() ;
      var selectionTab = UIHelper.GetRibbonTabFromName( targetTabName ) ;
      if ( selectionTab == null ) return ;
      
      foreach ( var panel in selectionTab.Panels ) {
        if ( panel.Source.Title == "Electrical.App.Panels.Routing.Settings".GetAppStringByKeyOrDefault( "Settings" ) ) {
          foreach ( var item in panel.Source.Items ) {
            if ( ! ( item is RibbonButton ribbonButton && ribbonButton.Text == "Electrical.App.Commands.Initialization.CreateDetailSymbolCommand".GetDocumentStringByKeyOrDefault( document, "Create\nDetail Symbol" ) ) ) {
              item.IsEnabled = isEnable ;
            }
          }
        }
      }
    }

    private void SaveDisplaySettingByGradeStorageService()
    {
      using Transaction transaction = new(_document, "Save Display Setting By Grade") ;
      transaction.Start() ;
      _displaySettingByGradeStorageService.Data.DisplaySettingByGradeData = _dataDisplaySettingByGradeModel ;
      _displaySettingByGradeStorageService.SaveChange() ;
      transaction.Commit() ;
    }
  }
}