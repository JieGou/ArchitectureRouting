using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
using System.Text ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using MoreLinq ;
using Button = System.Windows.Controls.Button ;
using CategoryModel = Arent3d.Architecture.Routing.AppBase.Model.CategoryModel ;
using CheckBox = System.Windows.Controls.CheckBox ;
using ComboBox = System.Windows.Controls.ComboBox ;
using DataGrid = System.Windows.Controls.DataGrid ;
using Label = System.Windows.Controls.Label ;
using Line = Autodesk.Revit.DB.Line ;
using ProgressBar = Arent3d.Revit.UI.Forms.ProgressBar ;
using MessageBox = System.Windows.MessageBox ;
using Path = System.IO.Path ;
using Visibility = System.Windows.Visibility ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class CeedViewModel : NotifyPropertyChanged
  {
    private const string LegendDisplay = "○" ;
    private const string LegendNoDisplay = "×" ;
    private const string NotExistConnectorFamilyInFolderModelWarningMessage = "excelで指定したモデルはmodelフォルダーに存在していないため、既存のモデルを使用します。" ;
    private readonly Document _document ;
    private readonly ExternalCommandData _commandData ;
    private List<CeedModel> _ceedModels ;
    private List<CeedModel> _usingCeedModel ;
    private List<CeedModel> _previousCeedModels ;
    private List<CeedModel> _instrumentationFigureCeedModel ;
    private readonly StorageService<Level, CeedUserModel> _storageService ;
    private List<string> _ceedModelNumberOfPreviewCategories ;
    private readonly List<CanvasChildInfo> _canvasChildInfos ;
    public List<ElementId> DwgImportIds { get ; }

    public DataGrid DtGrid ;

    public IReadOnlyCollection<CeedModel> OriginCeedModels => new ReadOnlyCollection<CeedModel>( _ceedModels );

    public ObservableCollection<CeedModel> CeedModels { get ; set ; }

    public ObservableCollection<string> CeedSetCodes { get ; } = new() ;

    private string _selectedCeedSetCode ;

    public string SelectedCeedSetCode
    {
      get => _selectedCeedSetCode ;
      set
      {
        _selectedCeedSetCode = value ;
        OnPropertyChanged() ;
      }
    }

    public ObservableCollection<string> ModelNumber { get ; } = new() ;

    private string _selectedModelNumber ;

    public string SelectedModelNumber
    {
      get => _selectedModelNumber ;
      set
      {
        _selectedModelNumber = value ;
        OnPropertyChanged() ;
      }
    }

    public ObservableCollection<string> DeviceSymbols { get ; } = new() ;

    private string _selectedDeviceSymbol ;

    public string SelectedDeviceSymbolValue
    {
      get => _selectedDeviceSymbol ;
      set
      {
        _selectedDeviceSymbol = value ;
        OnPropertyChanged() ;
      }
    }

    public bool IsShowCeedModelNumber { get ; set ; }

    private bool? _isShowCondition;
    public bool IsShowCondition
    {
      get => _isShowCondition ??= true ;
      set
      {
        _isShowCondition = value ;
        CeedModels.Clear();
        if ( _isShowCondition.HasValue ) {
          if ( _isShowCondition.Value ) {
            var ceedModels = GroupCeedModelsByCeedModelNumber( _ceedModels ) ;
            CeedModels.AddRange( ceedModels ) ;
          }
          else {
            var ceedModels = GroupCeedModel( _ceedModels ) ;
            ceedModels = GroupCeedModelsByCeedModelNumber( ceedModels ) ;
            CeedModels.AddRange( ceedModels ) ;
          }

          Reset() ;
        }
        OnPropertyChanged();
      }
    }

    private bool _isShowOnlyUsingCode ;

    public bool IsShowOnlyUsingCode
    {
      get => _isShowOnlyUsingCode ;
      set
      {
        _isShowOnlyUsingCode = value ;
        OnPropertyChanged();
      }
    }

    public bool IsShowDiff { get ; set ; }

    public bool IsExistUsingCode { get ; set ; }

    private bool _isShowInstrumentationFigureCode ;

    public bool IsShowInstrumentationFigureCode
    {
      get => _isShowInstrumentationFigureCode ;
      set
      {
        _isShowInstrumentationFigureCode = value ;
        OnPropertyChanged();
      }
    }

    public CeedModel? SelectedCeedModel { get ; set ; }
    public string? SelectedDeviceSymbol { get ; set ; }
    public string? SelectedCondition { get ; set ; }
    public string? SelectedCeedCode { get ; set ; }
    public string? SelectedModelNum { get ; set ; }
    public string? SelectedFloorPlanType { get ; set ; }

    private ObservableCollection<PreviewListInfo> _previewList ;

    public ObservableCollection<PreviewListInfo> PreviewList
    {
      get => _previewList ;
      set
      {
        _previewList = value ;
        OnPropertyChanged() ;
      }
    }

    private ObservableCollection<CategoryModel>? _categories ;

    public ObservableCollection<CategoryModel> Categories
    {
      get
      {
        if ( null != _categories )
          return _categories ;

        var categoryModels = GetCategoryModels() ;
        _categories = new ObservableCollection<CategoryModel>( categoryModels ) ;

        CategorySelected = FindSelectedCategory( _categories, true ) ;

        return _categories ;
      }
      set
      {
        _categories = value ;
        CategorySelected = FindSelectedCategory( _categories, true ) ;
        OnPropertyChanged() ;
      }
    }

    private CategoryModel? _categorySelected ;

    public CategoryModel? CategorySelected
    {
      get { return _categorySelected ??= FindSelectedCategory( Categories, true ) ; }
      set => _categorySelected = value ;
    }

    public ICommand SelectedItemCommand
    {
      get
      {
        return new RelayCommand<System.Windows.Controls.TreeView>( tv => null != tv, _ =>
        {
          CategorySelected = FindSelectedCategory( Categories, true ) ;
        } ) ;
      }
    }

    private ObservableCollection<CategoryModel>? _categoriesPreview ;

    public ObservableCollection<CategoryModel> CategoriesPreview
    {
      get
      {
        if ( null != _categoriesPreview )
          return _categoriesPreview ;

        var categoryModels = GetCategoryModels() ;
        _categoriesPreview = new ObservableCollection<CategoryModel>( categoryModels ) ;

        CategoryPreviewSelected = FindSelectedCategory( _categoriesPreview, false ) ;

        return _categoriesPreview ;
      }
      set
      {
        _categoriesPreview = value ;
        CategoryPreviewSelected = FindSelectedCategory( _categoriesPreview, false ) ;
        OnPropertyChanged() ;
      }
    }

    private CategoryModel? _categoryPreviewSelected ;

    public CategoryModel? CategoryPreviewSelected
    {
      get { return _categoryPreviewSelected ??= FindSelectedCategory( CategoriesPreview, false ) ; }
      set => _categoryPreviewSelected = value ;
    }

    public ICommand SelectedCategoryPreviewCommand
    {
      get
      {
        return new RelayCommand<System.Windows.Controls.TreeView>( tv => null != tv, _ =>
        {
          CategoryPreviewSelected = FindSelectedCategory( CategoriesPreview, false ) ;
        } ) ;
      }
    }

    public ICommand SearchCommand => new RelayCommand( Search ) ;
    public ICommand ResetCommand => new RelayCommand( Reset ) ;
    
    public ICommand OkCommand
    {
      get
      {
        return new RelayCommand<Window>( wd => null != wd, wd =>
        {
          Save() ;
          wd.DialogResult = true ;
          wd.Close() ;
        } ) ;
      }
    }

    public CeedViewModel( ExternalCommandData commandData )
    {
      _commandData = commandData ;
      _document = commandData.Application.ActiveUIDocument.Document ;
      CeedModels = new ObservableCollection<CeedModel>() ;
      DtGrid = new DataGrid() ;
      _canvasChildInfos = new List<CanvasChildInfo>() ;
      DwgImportIds = new List<ElementId>() ;
      
      var oldCeedStorable = _document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      _storageService = new StorageService<Level, CeedUserModel>(((ViewPlan)_document.ActiveView).GenLevel) ;
      
      if ( oldCeedStorable is null ) {
        _ceedModels = new List<CeedModel>() ;
        _usingCeedModel = new List<CeedModel>() ;
        _previousCeedModels = new List<CeedModel>() ;
        _instrumentationFigureCeedModel = new List<CeedModel>() ;
        _previewList = new ObservableCollection<PreviewListInfo>() ;
        Categories = new ObservableCollection<CategoryModel>() ;
        CategoriesPreview = new ObservableCollection<CategoryModel>() ;
        _ceedModelNumberOfPreviewCategories = new List<string>() ;
      }
      else {
        _ceedModels = oldCeedStorable.CeedModelData ;
        _usingCeedModel = oldCeedStorable.CeedModelUsedData ;
        _previousCeedModels = new List<CeedModel>( oldCeedStorable.CeedModelData ) ;
        _previewList = new ObservableCollection<PreviewListInfo>() ;
        _ceedModelNumberOfPreviewCategories = CategoryModel.GetCeedModelNumbers( oldCeedStorable.CategoriesWithoutCeedCode ) ;
        IsShowCeedModelNumber = _storageService.Data.IsShowCeedModelNumber ;
        IsShowCondition = _storageService.Data.IsShowCondition ;
        IsShowOnlyUsingCode = _storageService.Data.IsShowOnlyUsingCode ;
        IsShowInstrumentationFigureCode = _storageService.Data.IsShowInstrumentationFigureCode ;
        _instrumentationFigureCeedModel = new List<CeedModel>() ;
        AddModelNumber( _ceedModels ) ;
        if ( _usingCeedModel.Any() )
          IsExistUsingCode = true ;
        if ( ! _ceedModels.Any() ) IsShowDiff = true ;
        else IsShowDiff = _storageService.Data.IsDiff ;
        Categories = new ObservableCollection<CategoryModel>( CategoryModel.ConvertCategoryModel( oldCeedStorable.CategoriesWithCeedCode ) ) ;
        CategoriesPreview = new ObservableCollection<CategoryModel>( CategoryModel.ConvertCategoryModel( oldCeedStorable.CategoriesWithoutCeedCode ) ) ;
      }

      _selectedCeedSetCode = string.Empty ;
      _selectedModelNumber = string.Empty ;
      _selectedDeviceSymbol = string.Empty ;
    }

    private void LoadData( CeedStorable ceedStorable )
    {
      _ceedModels = ceedStorable.CeedModelData ;
      CeedModels.Clear() ;
      PreviewList.Clear() ;

      var ceedModels = GroupCeedModelsByCeedModelNumber( _ceedModels ) ;
      CeedModels.AddRange( ceedModels ) ;

      AddModelNumber( _ceedModels ) ;
      if ( ceedStorable.CeedModelUsedData.Any() )
        _usingCeedModel = ceedStorable.CeedModelUsedData ;
    }

    private void LoadData( List<CeedModel> ceedModels )
    {
      CeedModels.Clear() ;
      PreviewList.Clear() ;
      var category = FindSelectedCategory( Categories, true ) ;
      var categoryPreview = FindSelectedCategory( CategoriesPreview, false ) ;
      if ( category == null && categoryPreview == null ) {
        var newCeedModels = GroupCeedModelsByCeedModelNumber( ceedModels ) ;
        CeedModels.AddRange( newCeedModels ) ;
      }
      AddModelNumber( ceedModels ) ;
    }

    private List<CeedModel> GetInstrumentationFigureCeedModel()
    {
      var instrumentationFigureCeedModel = _ceedModels.Where( c => c.LegendDisplay == LegendDisplay ).ToList() ;
      return instrumentationFigureCeedModel ;
    }

    private void AddModelNumber( IReadOnlyCollection<CeedModel> ceedModels )
    {
      CeedSetCodes.Clear() ;
      foreach ( var ceedModel in ceedModels.Where( ceedModel => ! string.IsNullOrEmpty( ceedModel.CeedSetCode ) ) ) {
        if ( ! CeedSetCodes.Contains( ceedModel.CeedSetCode ) )
          CeedSetCodes.Add( ceedModel.CeedSetCode ) ;
      }

      ModelNumber.Clear() ;
      foreach ( var ceedModel in ceedModels.Where( ceedModel => ! string.IsNullOrEmpty( ceedModel.ModelNumber ) ) ) {
        if ( ceedModel.ModelNumber.IndexOf( '\n' ) >= 0 ) {
          var modelNumbers = ceedModel.ModelNumber.Split( '\n' ) ;
          foreach ( var modelNumber in modelNumbers ) {
            if ( ! ModelNumber.Contains( modelNumber ) ) ModelNumber.Add( modelNumber.Trim() ) ;
          }
        }
        else if ( ceedModel.ModelNumber.IndexOf( ',' ) >= 0 )  {
          var modelNumbers = ceedModel.ModelNumber.Split( ',' ) ;
          foreach ( var modelNumber in modelNumbers ) {
            if ( ! ModelNumber.Contains( modelNumber ) ) ModelNumber.Add( modelNumber.Trim() ) ;
          }
        }
        else {
          if ( ! ModelNumber.Contains( ceedModel.ModelNumber ) ) ModelNumber.Add( ceedModel.ModelNumber ) ;
        }
      }

      DeviceSymbols.Clear() ;
      foreach ( var ceedModel in ceedModels.Where( ceedModel => ! string.IsNullOrEmpty( ceedModel.GeneralDisplayDeviceSymbol ) ) ) {
        if ( ceedModel.GeneralDisplayDeviceSymbol.IndexOf( ',' ) >= 0 )  {
          var generalDisplayDeviceSymbols = ceedModel.GeneralDisplayDeviceSymbol.Split( ',' ) ;
          foreach ( var generalDisplayDeviceSymbol in generalDisplayDeviceSymbols ) {
            if ( ! DeviceSymbols.Contains( generalDisplayDeviceSymbol ) ) DeviceSymbols.Add( generalDisplayDeviceSymbol.Trim() ) ;
          }
        }
        else {
          if ( ! DeviceSymbols.Contains( ceedModel.GeneralDisplayDeviceSymbol ) )
            DeviceSymbols.Add( ceedModel.GeneralDisplayDeviceSymbol ) ;
        }
      }
      
      ResetComboboxValue() ;
    }

    private List<CategoryModel> GetCategoryModels()
    {
      List<CategoryModel> categoryModels = new() ;

      var categoryModel1 = new CategoryModel { Name = "Category 1", ParentName = string.Empty, IsExpanded = false, IsSelected = false } ;
      categoryModels.Add( categoryModel1 ) ;

      var categoryModel2 = new CategoryModel { Name = "Category 2", ParentName = string.Empty, IsExpanded = false, IsSelected = false } ;
      categoryModels.Add( categoryModel2 ) ;

      return categoryModels ;
    }

    private CategoryModel? FindSelectedCategory( IEnumerable<CategoryModel> categories, bool isCategoryWithCeedCode )
    {
      foreach ( var category in categories ) {
        if ( category.IsSelected && string.IsNullOrEmpty( category.ParentName ) ) {
          category.IsSelected = false ;
          category.IsExpanded = ! category.IsExpanded ;
          return null ;
        }

        if ( category.IsSelected && ! string.IsNullOrEmpty( category.ParentName ) ) {
          return category ;
        }

        if ( ! category.Categories.Any() )
          continue ;

        var subCategory = FindSelectedCategory( category.Categories, isCategoryWithCeedCode ) ;
        if ( subCategory == null ) continue ;
        ShowCeedModelAndPreviewByCategory( subCategory, isCategoryWithCeedCode ) ;
        ResetSelectedCategory( isCategoryWithCeedCode ? CategoriesPreview : Categories ) ;
        ResetComboboxValue() ;
        return category ;
      }

      return null ;
    }
    
    private void ResetSelectedCategory( IEnumerable<CategoryModel> categories )
    {
      foreach ( var category in categories ) {
        category.IsExpanded = false ;
        category.IsSelected = false ;

        if ( ! category.Categories.Any() ) continue ;

        ResetSelectedCategory( category.Categories ) ;
      }
    }
    
    private void ShowCeedModelAndPreviewByCategory( CategoryModel categoryModel, bool isCategoryWithCeedCode )
    {
      var data = IsShowOnlyUsingCode ? _usingCeedModel : ( IsShowInstrumentationFigureCode ? _instrumentationFigureCeedModel : _ceedModels ) ;
      CeedModels.Clear() ;
      PreviewList.Clear() ;
      var ceedCodeNumbers = categoryModel.CeedCodeNumbers.Select( c => c.Name ) ;
      data = categoryModel.CeedCodeNumbers.Any() ? data.Where( c => ceedCodeNumbers.Contains( c.CeedModelNumber ) ).ToList() : data ;
      if ( isCategoryWithCeedCode ) {
        if ( _ceedModelNumberOfPreviewCategories.Any() ) {
          data = data.Where( c => ! _ceedModelNumberOfPreviewCategories.Contains( c.CeedModelNumber ) ).ToList() ;
        }

        var ceedModels = GroupCeedModelsByCeedModelNumber( data ) ;
        CeedModels.AddRange( ceedModels ) ;
      }
      else {
        CreatePreviewList( data ) ;
      }
    }

    private List<CeedModel> GroupCeedModelsByCeedModelNumber( IEnumerable<CeedModel> originCeedModels )
    {
      var newCeedModels = new List<CeedModel>() ;
      var ceedModelGroupByCeedModelNumber = originCeedModels.GroupBy( c => ( c.CeedModelNumber, c.Name ) ) ;
      foreach ( var ceedModels in ceedModelGroupByCeedModelNumber ) {
        var firstCeedModel = ceedModels.First() ;
        var generalDisplayDeviceSymbols = ceedModels.Select( c => c.GeneralDisplayDeviceSymbol ).Distinct() ;
        var generalDisplayDeviceSymbol = string.Join( ", ", generalDisplayDeviceSymbols ) ;
        var modelNumbers = ceedModels.Select( c => c.ModelNumber ).Distinct() ;
        var modelNumber = string.Join( ", ", modelNumbers ) ;
        var ceedModel = new CeedModel( firstCeedModel.LegendDisplay, firstCeedModel.CeedModelNumber, firstCeedModel.CeedSetCode, generalDisplayDeviceSymbol, modelNumber, firstCeedModel.FloorPlanSymbol,
          firstCeedModel.InstrumentationSymbol, firstCeedModel.Name, firstCeedModel.DwgNumber, firstCeedModel.Base64InstrumentationImageString, firstCeedModel.Base64FloorPlanImages, firstCeedModel.FloorPlanType,
          firstCeedModel.IsAdded, firstCeedModel.IsEditFloorPlan, firstCeedModel.IsEditInstrumentation, firstCeedModel.IsEditCondition ) ;
        newCeedModels.Add( ceedModel ) ;
      }

      return newCeedModels ;
    }

    public void Load( CheckBox checkBox )
    {
      MessageBox.Show( "Please select 【CeeD】セットコード一覧表 file.", "Message" ) ;
      OpenFileDialog openFileDialog = new() { Filter = "Csv files (*.xlsx; *.xls)|*.xlsx;*.xls", Multiselect = false } ;
      string filePath = string.Empty ;
      string fileEquipmentSymbolsPath = string.Empty ;
      if ( openFileDialog.ShowDialog() == DialogResult.OK ) {
        filePath = openFileDialog.FileName ;
        MessageBox.Show( "Please select 機器記号一覧表 file.", "Message" ) ;
        OpenFileDialog openFileEquipmentSymbolsDialog = new() { Filter = "Csv files (*.xlsx; *.xls)|*.xlsx;*.xls", Multiselect = false } ;
        if ( openFileEquipmentSymbolsDialog.ShowDialog() == DialogResult.OK ) {
          fileEquipmentSymbolsPath = openFileEquipmentSymbolsDialog.FileName ;
        }
      }

      if ( string.IsNullOrEmpty( filePath ) || string.IsNullOrEmpty( fileEquipmentSymbolsPath ) ) return ;
      using var progress = ProgressBar.ShowWithNewThread( _commandData.Application ) ;
      progress.Message = "Loading data..." ;
      var ceedStorable = _document.GetCeedStorable() ;
      {
        var (ceedModelData, categoriesWithCeedCode, categoriesWithoutCeedCode) = ExcelToModelConverter.GetAllCeedModelNumber( filePath, fileEquipmentSymbolsPath ) ;
        if ( ! ceedModelData.Any() ) return ;
        _previousCeedModels = new List<CeedModel>( CeedModels ) ;
        CheckChangeColor( ceedModelData ) ;
        ceedStorable.CeedModelData = ceedModelData ;
        ceedStorable.CeedModelUsedData = new List<CeedModel>() ;
        ceedStorable.CategoriesWithCeedCode = CategoryModel.ConvertCategoryModel( categoriesWithCeedCode ) ;
        ceedStorable.CategoriesWithoutCeedCode = CategoryModel.ConvertCategoryModel( categoriesWithoutCeedCode ) ;
        _ceedModelNumberOfPreviewCategories = CategoryModel.GetCeedModelNumbers( ceedStorable.CategoriesWithoutCeedCode ) ;
        _storageService.Data.IsShowOnlyUsingCode = false ;
        _storageService.Data.IsShowInstrumentationFigureCode = false ;
        checkBox.Visibility = Visibility.Hidden ;
        checkBox.IsChecked = false ;
        IsShowOnlyUsingCode = false ;
        IsShowInstrumentationFigureCode = false ;
        Categories = new ObservableCollection<CategoryModel>( categoriesWithCeedCode ) ;
        CategoriesPreview = new ObservableCollection<CategoryModel>( categoriesWithoutCeedCode ) ;
        LoadData( ceedStorable ) ;

        try {
          using Transaction t = new( _document, "Save data" ) ;
          t.Start() ;
          using ( var progressData = progress.Reserve( 0.5 ) ) {
            ceedStorable.Save() ;
            _storageService.SaveChange();
            progressData.ThrowIfCanceled() ;
          }

          using ( var progressData = progress.Reserve( 0.9 ) ) {
            _document.MakeCertainAllConnectorFamilies() ;
            progressData.ThrowIfCanceled() ;
          }

          t.Commit() ;
        }
        catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        }
      }
    }

    public void Save()
    {
      try {
        using Transaction t = new( _document, "Save data" ) ;
        t.Start() ;
        _storageService.Data.IsShowCeedModelNumber = IsShowCeedModelNumber ;
        _storageService.Data.IsShowCondition = IsShowCondition ;
        _storageService.Data.IsShowOnlyUsingCode = IsShowOnlyUsingCode ;
        _storageService.Data.IsDiff = IsShowDiff ;
        _storageService.Data.IsShowInstrumentationFigureCode = IsShowInstrumentationFigureCode ;
        _storageService.SaveChange() ;
        
        var ceedStorable = _document.GetCeedStorable() ;
        {
          ceedStorable.CategoriesWithCeedCode = CategoryModel.ConvertCategoryModel( Categories ) ;
          ceedStorable.CategoriesWithoutCeedCode = CategoryModel.ConvertCategoryModel( CategoriesPreview ) ;
        }
        ceedStorable.Save() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }

    public void Search()
    {
      var data = IsShowOnlyUsingCode ? _usingCeedModel : ( IsShowInstrumentationFigureCode ? _instrumentationFigureCeedModel : _ceedModels ) ;
      CeedModels.Clear() ;
      PreviewList.Clear() ;
      data = GroupCeedModelsByCeedModelNumber( data ) ;
      data = string.IsNullOrEmpty( _selectedDeviceSymbol ) ? data : data.Where( c => c.GeneralDisplayDeviceSymbol.ToUpper().Contains( _selectedDeviceSymbol.ToUpper() ) ).ToList() ;
      data = string.IsNullOrEmpty( _selectedCeedSetCode ) ? data : data.Where( c => c.CeedSetCode.ToUpper().Contains( _selectedCeedSetCode.ToUpper() ) ).ToList() ;
      data = string.IsNullOrEmpty( _selectedModelNumber ) ? data : data.Where( c => c.ModelNumber.ToUpper().Contains( _selectedModelNumber.ToUpper() ) ).ToList() ;
      CeedModels.AddRange( data ) ;
      ResetSelectedCategory( Categories ) ;
      ResetSelectedCategory( CategoriesPreview ) ;
    }

    private void Reset()
    {
      ResetComboboxValue() ;
      Search() ;
    }

    private void ResetComboboxValue()
    {
      SelectedCeedSetCode = string.Empty ;
      SelectedModelNumber = string.Empty ;
      SelectedDeviceSymbolValue = string.Empty ;
    }

    public void LoadUsingCeedModel( CheckBox checkBox )
    {
      var ceedStorable = _document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( ceedStorable != null && ceedStorable.CeedModelData.Any() ) {
        OpenFileDialog openFileDialog = new() { Filter = "Csv files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx", Multiselect = false } ;
        string filePath = string.Empty ;
        if ( openFileDialog.ShowDialog() == DialogResult.OK ) {
          filePath = openFileDialog.FileName ;
        }

        if ( string.IsNullOrEmpty( filePath ) ) return ;
        var modelNumberToUse = ExcelToModelConverter.GetModelNumberToUse( filePath ) ;
        if ( ! modelNumberToUse.Any() ) return ;
        List<CeedModel> usingCeedModel = new() ;
        foreach ( var modelNumber in modelNumberToUse ) {
          var ceedModels = ceedStorable.CeedModelData.Where( c => c.ModelNumber.Contains( modelNumber ) ).Distinct().ToList() ;
          usingCeedModel.AddRange( ceedModels ) ;
        }

        var ceedModelsNoDisplay = _ceedModels.Where( c => c.LegendDisplay == LegendNoDisplay ) ;
        usingCeedModel.AddRange( ceedModelsNoDisplay ) ;

        usingCeedModel = usingCeedModel.Distinct().ToList() ;
        _usingCeedModel = usingCeedModel ;
        LoadData( _usingCeedModel ) ;
        checkBox.Visibility = Visibility.Visible ;
        checkBox.IsChecked = true ;
        IsShowInstrumentationFigureCode = false ;

        if ( ! _usingCeedModel.Any() ) return ;
        try {
          using Transaction t = new( _document, "Save data" ) ;
          t.Start() ;
          ceedStorable.CeedModelUsedData = _usingCeedModel ;
          _storageService.Data.IsShowOnlyUsingCode = true ;
          _storageService.Data.IsShowInstrumentationFigureCode = false ;
          ceedStorable.Save() ;
          _storageService.SaveChange();
          t.Commit() ;
        }
        catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        }
      }
      else {
        MessageBox.Show( "Please read csv.", "Message" ) ;
      }
    }

    public void ShowCeedModelNumberColumn( Label label, ComboBox comboBox )
    {
      DtGrid.Columns[ 1 ].Visibility = Visibility.Visible ;
      label.Visibility = Visibility.Visible ;
      comboBox.Visibility = Visibility.Visible ;
    }

    private IEnumerable<CeedModel> GroupCeedModel(IEnumerable<CeedModel> ceedModels )
    {
      return ceedModels.GroupBy( x => x.GeneralDisplayDeviceSymbol ).Select( x => x.ToList().DistinctBy( y => y.ModelNumber ) ).SelectMany( x => x ) ;
    }

    public void UnShowCeedModelNumberColumn( Label label, ComboBox comboBox )
    {
      DtGrid.Columns[ 1 ].Visibility = Visibility.Hidden ;
      label.Visibility = Visibility.Hidden ;
      comboBox.Visibility = Visibility.Hidden ;
    }

    public void ShowOnlyUsingCode()
    {
      if ( ! _usingCeedModel.Any() ) return ;
      IsShowInstrumentationFigureCode = false ;
      LoadData( _usingCeedModel ) ;
    }

    public void UnShowOnlyUsingCode()
    {
      if ( ! _ceedModels.Any() ) return ;
      LoadData( _ceedModels ) ;
    }
    
    public void ShowInstrumentationFigureCode()
    {
      _instrumentationFigureCeedModel = GetInstrumentationFigureCeedModel() ;
      IsShowOnlyUsingCode = false ;
      LoadData( _instrumentationFigureCeedModel ) ;
    }

    private void UpdateCeedStorableAfterReplaceFloorPlanSymbol( string connectorFamilyName )
    {
      var ceedStorable = _document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( ceedStorable == null ) return ;
      if ( _ceedModels.Any() ) {
        List<CeedModel> ceedModels = GetCeedModels( _ceedModels ) ;
        foreach ( var ceedModel in ceedModels ) {
          ceedModel.FloorPlanType = connectorFamilyName ;
          ceedStorable.CeedModelData = _ceedModels ;
        }
      }

      if ( _usingCeedModel.Any() ) {
        List<CeedModel> ceedModels = GetCeedModels( _usingCeedModel ) ;
        foreach ( var ceedModel in ceedModels ) {
          ceedModel.FloorPlanType = connectorFamilyName ;
          ceedStorable.CeedModelUsedData = _usingCeedModel ;
        }
      }

      try {
        using Transaction t = new( _document, "Save CeeD data" ) ;
        t.Start() ;
        ceedStorable.Save() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        MessageBox.Show( "Save CeeD data failed.", "Error" ) ;
      }
    }

    private List<CeedModel> GetCeedModels( IEnumerable<CeedModel> allCeedModels )
    {
      List<CeedModel> ceedModels ;
      if ( string.IsNullOrEmpty( SelectedCeedModel!.CeedModelNumber ) ) {
        if ( string.IsNullOrEmpty( SelectedCeedModel!.GeneralDisplayDeviceSymbol ) ) {
          ceedModels = allCeedModels.Where( c => c.CeedModelNumber == SelectedCeedModel!.CeedModelNumber && c.GeneralDisplayDeviceSymbol == SelectedCeedModel!.GeneralDisplayDeviceSymbol && c.Name == SelectedCeedModel!.Name ).ToList() ;
        }
        else {
          var generalDisplayDeviceSymbols = SelectedCeedModel!.GeneralDisplayDeviceSymbol.Split( ',' ) ;
          ceedModels = allCeedModels.Where( c => c.CeedModelNumber == SelectedCeedModel!.CeedModelNumber && generalDisplayDeviceSymbols.Contains( c.GeneralDisplayDeviceSymbol ) && c.Name == SelectedCeedModel!.Name ).ToList() ;
        }
      }
      else {
        ceedModels = allCeedModels.Where( c => c.CeedModelNumber == SelectedCeedModel!.CeedModelNumber ).ToList() ;
      }

      return ceedModels ;
    }

    private void UpdateDataGridAfterReplaceFloorPlanSymbol( DataGrid dataGrid, string floorPlanType )
    {
      if ( dataGrid.ItemsSource is not ObservableCollection<CeedModel> ) {
        MessageBox.Show( "CeeD model data is incorrect.", "Error" ) ;
        return ;
      }

      var ceedModel = CeedModels.FirstOrDefault( c => c == SelectedCeedModel ) ;
      if ( ceedModel == null ) return ;
      ceedModel.FloorPlanType = floorPlanType ;
    }

    public void ReplaceSymbol( DataGrid dataGrid, Button button )
    {
      var selectConnectorFamilyDialog = new SelectConnectorFamily( _document ) ;
      selectConnectorFamilyDialog.ShowDialog() ;
      if ( ! ( selectConnectorFamilyDialog.DialogResult ?? false ) ) return ;
      var selectedConnectorFamily = selectConnectorFamilyDialog.ConnectorFamilyList.SingleOrDefault( f => f.IsSelected ) ;
      if ( selectedConnectorFamily == null ) {
        MessageBox.Show( "No connector family selected.", "Error" ) ;
        return ;
      }

      var connectorFamilyFileName = selectedConnectorFamily.ToString() ;
      var connectorFamilyName = connectorFamilyFileName.Replace( ".rfa", "" ) ;
      if ( SelectedCeedModel == null || string.IsNullOrEmpty( connectorFamilyFileName ) ) return ;

      using var progress = ProgressBar.ShowWithNewThread( _commandData.Application ) ;
      progress.Message = "Processing......." ;

      using ( var progressData = progress.Reserve( 0.5 ) ) {
        UpdateCeedStorableAfterReplaceFloorPlanSymbol( connectorFamilyName ) ;
        progressData.ThrowIfCanceled() ;
      }

      using ( var progressData = progress.Reserve( 0.9 ) ) {
        UpdateDataGridAfterReplaceFloorPlanSymbol( dataGrid, connectorFamilyName ) ;
        button.IsEnabled = false ;
        progressData.ThrowIfCanceled() ;
      }

      progress.Finish() ;
      MessageBox.Show( "正常にモデルを置き換えました。", "Message" ) ;
    }

    public void ReplaceMultipleSymbols( DataGrid dtGrid )
    {
      const string successfullyMess = "モデルを正常に置き換えました。" ;
      const string failedMess = "モデルの置き換えが失敗しました。" ;
      List<string> connectorFamilyPaths ;
      MessageBox.Show( "モデルフォルダーを選択してください。", "Message" ) ;
      FolderBrowserDialog folderBrowserDialog = new() ;
      if ( folderBrowserDialog.ShowDialog() != DialogResult.OK ) return ;
      string folderPath = folderBrowserDialog.SelectedPath ;
      string infoPath = Directory.GetFiles( folderPath ).FirstOrDefault( f => Path.GetExtension( f ) is ".xls" or ".xlsx" ) ?? string.Empty ;
      if ( string.IsNullOrEmpty( infoPath ) ) {
        MessageBox.Show( "指定したフォルダーにはモデル指定情報のエクセルファイルが存在していません。", "Error" ) ;
        return ;
      }

      DirectoryInfo dirInfo = new( folderPath ) ;
      var familyFolder = dirInfo.GetDirectories().FirstOrDefault() ;
      if ( familyFolder != null ) {
        connectorFamilyPaths = Directory.GetFiles( familyFolder.FullName ).ToList() ;
      }
      else {
        MessageBox.Show( "指定したフォルダーにはモデルデータが存在していません。モデルデータをmodelフォルダに入れてください。", "Error" ) ;
        return ;
      }

      if ( connectorFamilyPaths.Any() ) {
        try {
          List<string> connectorFamilyFiles ;
          List<ExcelToModelConverter.ConnectorFamilyReplacement> connectorFamilyReplacements ;
          using var progress = ProgressBar.ShowWithNewThread( _commandData.Application ) ;
          progress.Message = "Processing......." ;
          using ( var progressData = progress.Reserve( 0.3 ) ) {
            connectorFamilyReplacements = ExcelToModelConverter.GetConnectorFamilyReplacements( infoPath ) ;
            connectorFamilyFiles = LoadConnectorFamily( _document, connectorFamilyPaths ) ;
            progressData.ThrowIfCanceled() ;
          }

          if ( connectorFamilyFiles.Any() && connectorFamilyReplacements.Any() ) {
            bool result ;
            using ( var progressData = progress.Reserve( 0.6 ) ) {
              result = IsUpdateCeedStorableAfterReplaceMultipleSymbolsSuccessfully( _document, _ceedModels, _usingCeedModel, connectorFamilyReplacements, connectorFamilyFiles ) ;
              progressData.ThrowIfCanceled() ;
            }

            if ( result ) {
              using var progressData = progress.Reserve( 0.9 ) ;
              result = IsUpdateUpdateDataGridAfterReplaceMultipleSymbolsSuccessfully( connectorFamilyReplacements, connectorFamilyFiles, dtGrid ) ;
              progressData.ThrowIfCanceled() ;
            }

            progress.Finish() ;
            MessageBox.Show( result ? successfullyMess : failedMess, "Message" ) ;
          }
          else {
            progress.Finish() ;
            MessageBox.Show( failedMess, "Message" ) ;
          }
        }
        catch ( Exception exception ) {
          MessageBox.Show( exception.Message, "Error" ) ;
        }
      }
      else {
        MessageBox.Show( NotExistConnectorFamilyInFolderModelWarningMessage, "Message" ) ;
      }
    }

    private Dictionary<string, string> GetExistedConnectorFamilies( Document document, IEnumerable<string> connectorFamilyPaths )
    {
      Dictionary<string, string> existsConnectorFamilies = new() ;
      foreach ( var connectorFamilyPath in connectorFamilyPaths ) {
        var connectorFamilyFile = Path.GetFileName( connectorFamilyPath ) ;
        var connectorFamilyName = connectorFamilyFile.Replace( ".rfa", "" ) ;
        if ( new FilteredElementCollector( document ).OfClass( typeof( Family ) ).FirstOrDefault( f => f.Name == connectorFamilyName ) is Family ) {
          existsConnectorFamilies.Add( connectorFamilyFile, connectorFamilyPath ) ;
        }
      }

      return existsConnectorFamilies ;
    }

    private List<string> LoadConnectorFamily( Document document, List<string> connectorFamilyPaths )
    {
      List<string> connectorFamilyFiles = new() ;

      var existedConnectorFamilies = GetExistedConnectorFamilies( document, connectorFamilyPaths ) ;
      if ( existedConnectorFamilies.Any() ) {
        var confirmMessage = MessageBox.Show( $"モデルがすでに存在していますが、上書きしますか。\n対象モデル：" + string.Join( ", ", existedConnectorFamilies.Keys ), "Confirm Message", MessageBoxButton.OKCancel ) ;
        if ( confirmMessage == MessageBoxResult.Cancel ) {
          connectorFamilyPaths = connectorFamilyPaths.Where( p => ! existedConnectorFamilies.ContainsValue( p ) ).ToList() ;
          connectorFamilyFiles.AddRange( existedConnectorFamilies.Keys ) ;
        }
      }

      using Transaction loadTransaction = new( document, "Load connector's family" ) ;
      loadTransaction.Start() ;
      foreach ( var connectorFamilyPath in connectorFamilyPaths ) {
        var isLoadFamilySuccessfully = true ;
        var connectorFamilyFile = Path.GetFileName( connectorFamilyPath ) ;
        var connectorFamily = LoadFamily( document, connectorFamilyPath, ref isLoadFamilySuccessfully ) ;
        if ( connectorFamily != null || ( connectorFamily == null && isLoadFamilySuccessfully && existedConnectorFamilies.ContainsValue( connectorFamilyPath ) ) )
          connectorFamilyFiles.Add( connectorFamilyFile ) ;
      }

      loadTransaction.Commit() ;
      return connectorFamilyFiles ;
    }

    private Family? LoadFamily( Document document, string filePath, ref bool isLoadFamilySuccessfully )
    {
      try {
        document.LoadFamily( filePath, new FamilyOption( true ), out var family ) ;
        if ( family == null ) return family ;
        foreach ( ElementId familySymbolId in family.GetFamilySymbolIds() ) {
          document.GetElementById<FamilySymbol>( familySymbolId ) ;
        }

        return family ;
      }
      catch {
        isLoadFamilySuccessfully = false ;
        return null ;
      }
    }

    private bool IsUpdateCeedStorableAfterReplaceMultipleSymbolsSuccessfully( Document document, List<CeedModel>? allCeedModels, List<CeedModel>? usingCeedModel, IReadOnlyCollection<ExcelToModelConverter.ConnectorFamilyReplacement> connectorFamilyReplacements, ICollection<string> connectorFamilyFileName )
    {
      List<string> deviceSymbolsNotHaveConnectorFamily = new() ;
      var ceedStorable = document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( ceedStorable == null ) return false ;
      if ( allCeedModels != null ) {
        foreach ( var connectorFamilyReplacement in connectorFamilyReplacements ) {
          if ( connectorFamilyFileName.Contains( connectorFamilyReplacement.ConnectorFamilyFile ) ) {
            var deviceSymbols = connectorFamilyReplacement.DeviceSymbols.Split( '\n' ) ;
            foreach ( var deviceSymbol in deviceSymbols ) {
              var generalDisplayDeviceSymbol = deviceSymbol.Normalize( NormalizationForm.FormKC ) ;
              var ceedModels = allCeedModels.Where( c => c.GeneralDisplayDeviceSymbol == generalDisplayDeviceSymbol ).ToList() ;
              if ( ! ceedModels.Any() ) continue ;
              var connectorFamilyName = connectorFamilyReplacement.ConnectorFamilyFile.Replace( ".rfa", "" ) ;
              foreach ( var ceedModel in ceedModels ) {
                ceedModel.FloorPlanType = connectorFamilyName ;
              }
            }
          }
          else {
            deviceSymbolsNotHaveConnectorFamily.AddRange( connectorFamilyReplacement.DeviceSymbols.Split( '\n' ) ) ;
          }
        }

        ceedStorable.CeedModelData = allCeedModels ;
      }

      if ( usingCeedModel != null ) {
        foreach ( var connectorFamilyReplacement in connectorFamilyReplacements ) {
          if ( ! connectorFamilyFileName.Contains( connectorFamilyReplacement.ConnectorFamilyFile ) ) continue ;
          var deviceSymbols = connectorFamilyReplacement.DeviceSymbols.Split( '\n' ) ;
          foreach ( var deviceSymbol in deviceSymbols ) {
            var ceedModels = usingCeedModel.Where( c => c.GeneralDisplayDeviceSymbol == deviceSymbol ).ToList() ;
            if ( ! ceedModels.Any() ) continue ;
            var connectorFamilyName = connectorFamilyReplacement.ConnectorFamilyFile.Replace( ".rfa", "" ) ;
            foreach ( var ceedModel in ceedModels ) {
              ceedModel.FloorPlanType = connectorFamilyName ;
            }
          }
        }

        ceedStorable.CeedModelUsedData = usingCeedModel ;
      }

      var newConnectorFamilyUploadFiles = connectorFamilyFileName.Where( f => ! _storageService.Data.ConnectorFamilyUploadData.Contains( f ) ).ToList() ;
      _storageService.Data.ConnectorFamilyUploadData.AddRange( newConnectorFamilyUploadFiles ) ;

      try {
        using Transaction t = new( document, "Save CeeD data" ) ;
        t.Start() ;
        ceedStorable.Save() ;
        _storageService.SaveChange() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        MessageBox.Show( "Save CeeD data failed.", "Error" ) ;
        return false ;
      }

      if ( deviceSymbolsNotHaveConnectorFamily.Any() ) {
        MessageBox.Show( NotExistConnectorFamilyInFolderModelWarningMessage + "対象の一般表示用機器記号：" + string.Join( ", ", deviceSymbolsNotHaveConnectorFamily ), "Message" ) ;
      }

      return true ;
    }

    private bool IsUpdateUpdateDataGridAfterReplaceMultipleSymbolsSuccessfully( IEnumerable<ExcelToModelConverter.ConnectorFamilyReplacement> connectorFamilyReplacements, ICollection<string> connectorFamilyFileName, ItemsControl dtGrid )
    {
      if ( dtGrid.ItemsSource is not ObservableCollection<CeedModel> newCeedModels ) {
        MessageBox.Show( "CeeD model data is incorrect.", "Error" ) ;
        return false ;
      }

      foreach ( var connectorFamilyReplacement in connectorFamilyReplacements ) {
        if ( ! connectorFamilyFileName.Contains( connectorFamilyReplacement.ConnectorFamilyFile ) ) continue ;
        var deviceSymbols = connectorFamilyReplacement.DeviceSymbols.Split( '\n' ) ;
        foreach ( var deviceSymbol in deviceSymbols ) {
          var ceedModels = newCeedModels.Where( c => c.GeneralDisplayDeviceSymbol == deviceSymbol ).ToList() ;
          if ( ! ceedModels.Any() ) continue ;
          var connectorFamilyName = connectorFamilyReplacement.ConnectorFamilyFile.Replace( ".rfa", "" ) ;
          foreach ( var ceedModel in ceedModels ) {
            ceedModel.FloorPlanType = connectorFamilyName ;
          }
        }
      }

      dtGrid.ItemsSource = newCeedModels ;
      return true ;
    }

    public class FamilyOption : IFamilyLoadOptions
    {
      private readonly bool _forceUpdate ;

      public FamilyOption( bool forceUpdate ) => _forceUpdate = forceUpdate ;

      public bool OnFamilyFound( bool familyInUse, out bool overwriteParameterValues )
      {
        if ( familyInUse && ! _forceUpdate ) {
          overwriteParameterValues = false ;
          return false ;
        }

        overwriteParameterValues = true ;
        return true ;
      }

      public bool OnSharedFamilyFound( Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues )
      {
        source = FamilySource.Project ;
        return OnFamilyFound( familyInUse, out overwriteParameterValues ) ;
      }
    }

    private void CheckChangeColor( List<CeedModel> ceedModels )
    {
      for ( int i = 0 ; i < ceedModels.Count() ; i++ ) {
        CeedModel item = ceedModels[ i ] ;
        var existCeedModels = _previousCeedModels ;
        var itemExistCeedModel = existCeedModels.Find( x => x.CeedSetCode == item.CeedSetCode && x.CeedModelNumber == item.CeedModelNumber && x.GeneralDisplayDeviceSymbol == item.GeneralDisplayDeviceSymbol && x.ModelNumber == item.ModelNumber ) ;
        if ( itemExistCeedModel != null ) {
          item.IsEditFloorPlan = IsChange( string.IsNullOrEmpty( itemExistCeedModel.FloorPlanSymbol ) ? itemExistCeedModel.Base64FloorPlanImages : itemExistCeedModel.FloorPlanSymbol, string.IsNullOrEmpty( item.FloorPlanSymbol ) ? item.Base64FloorPlanImages : item.FloorPlanSymbol ) ;
          item.IsEditInstrumentation = IsChange( string.IsNullOrEmpty( itemExistCeedModel.InstrumentationSymbol ) ? itemExistCeedModel.Base64InstrumentationImageString : itemExistCeedModel.InstrumentationSymbol, string.IsNullOrEmpty( item.InstrumentationSymbol ) ? item.Base64InstrumentationImageString : item.InstrumentationSymbol ) ;
          item.IsEditCondition = IsChange( itemExistCeedModel.Condition, item.Condition ) ;
        }
        else {
          // row.Background = Brushes.Orange ;
          item.IsAdded = true ;
        }
      }
    }

    private bool IsChange( string oldItem, string newItem )
    {
      return oldItem != newItem ;
    }

    public void ShowPreviewList( CeedModel? ceedModel )
    {
      PreviewList.Clear() ;
      if ( ceedModel == null ) return ;
      var ceedModels = IsShowOnlyUsingCode ? _usingCeedModel : ( IsShowInstrumentationFigureCode ? _instrumentationFigureCeedModel : _ceedModels ) ;
      ceedModels = GetCeedModels( ceedModels ) ;
      if ( ! ceedModels.Any() ) return ;
      CreatePreviewList( ceedModels ) ;
    }

    private void CreatePreviewList( List<CeedModel> ceedModels )
    {
      var view = _document.ActiveView ;
      DWGImportOptions dwgImportOptions = new()
      {
        ColorMode = ImportColorMode.BlackAndWhite,
        Unit = ImportUnit.Millimeter,
        OrientToView = true,
        Placement = ImportPlacement.Origin,
        ThisViewOnly = false
      } ;

      foreach ( var ceedModel in ceedModels ) {
        var lines = new List<Line>() ;
        var arcs = new List<Arc>() ;
        var polyLines = new List<PolyLine>() ;
        var points = new List<Autodesk.Revit.DB.Point>() ;
        var dwgNumber = ceedModel.DwgNumber ;
        try {
          if ( string.IsNullOrEmpty( ceedModel.DwgNumber ) ) {
            if ( string.IsNullOrEmpty( ceedModel.GeneralDisplayDeviceSymbol ) ) continue ;
            var canvas = DrawCanvasManager.CreateCanvas( lines, arcs, polyLines, ceedModel.GeneralDisplayDeviceSymbol, ceedModel.FloorPlanSymbol, ceedModel.Condition ) ;
            PreviewList.Add( new PreviewListInfo( ceedModel.CeedSetCode, ceedModel.ModelNumber, ceedModel.GeneralDisplayDeviceSymbol, ceedModel.Condition, ceedModel.FloorPlanType, canvas ) ) ;
          }
          else {
            var canvasChildInfo = _canvasChildInfos.SingleOrDefault( c => c.DwgNumber == ceedModel.DwgNumber ) ;
            if ( canvasChildInfo == null ) {
              var filePath = DrawCanvasManager.Get2DSymbolDwgPath( dwgNumber ) ;
              using Transaction t = new( _document, "Import dwg file" ) ;
              t.Start() ;
              _document.Import( filePath, dwgImportOptions, view, out var elementId ) ;
              t.Commit() ;

              if ( elementId == null ) continue ;
              DwgImportIds.Add( elementId ) ;
              if ( _document.GetElement( elementId ) is ImportInstance dwg ) {
                Options opt = new() ;
                foreach ( GeometryObject geoObj in dwg.get_Geometry( opt ) ) {
                  if ( geoObj is not GeometryInstance inst ) continue ;
                  DrawCanvasManager.LoadGeometryFromGeometryObject( inst.SymbolGeometry, lines, arcs, polyLines, points ) ;
                }
              }
              
              _canvasChildInfos.Add( new CanvasChildInfo( ceedModel.DwgNumber, polyLines, lines, arcs ) ) ;
            }
            else {
              polyLines = canvasChildInfo.PolyLines ;
              lines = canvasChildInfo.Lines ;
              arcs = canvasChildInfo.Arcs ;
            }
            
            var canvas = DrawCanvasManager.CreateCanvas( lines, arcs, polyLines, ceedModel.GeneralDisplayDeviceSymbol, string.Empty, ceedModel.Condition ) ;
            PreviewList.Add( new PreviewListInfo( ceedModel.CeedSetCode, ceedModel.ModelNumber, ceedModel.GeneralDisplayDeviceSymbol, ceedModel.Condition, ceedModel.FloorPlanType, canvas ) ) ;
          }
        }
        catch {
          // ignored
        }
      }
    }

    public class PreviewListInfo
    {
      public string CeedSetCode { get ; }
      public string ModelNumber { get ; }
      public string GeneralDisplayDeviceSymbol { get ; }
      public string Condition { get ; }
      public string FloorPlanType { get ; }
      public Canvas Canvas { get ; }

      public PreviewListInfo( string ceedSetCode, string modelNumber, string generalDisplayDeviceSymbol, string condition, string floorPlanType, Canvas canvas )
      {
        CeedSetCode = ceedSetCode ;
        ModelNumber = modelNumber ;
        GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
        Condition = condition ;
        FloorPlanType = floorPlanType ;
        Canvas = canvas ;
      }
    }
    
    private class CanvasChildInfo
    {
      public string DwgNumber { get ; }
      public List<PolyLine> PolyLines { get ; }
      public List<Line> Lines { get ; }
      public List<Arc> Arcs { get ; }

      public CanvasChildInfo( string dwgNumber, List<PolyLine> polyLines, List<Line> lines, List<Arc> arcs )
      {
        DwgNumber = dwgNumber ;
        PolyLines = polyLines ;
        Lines = lines ;
        Arcs = arcs ;
      }
    }
  }
}