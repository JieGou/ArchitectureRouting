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
using System.Windows.Media.Imaging ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.PostCommands ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using MoreLinq ;
using MoreLinq.Extensions ;
using Button = System.Windows.Controls.Button ;
using CategoryModel = Arent3d.Architecture.Routing.AppBase.Model.CategoryModel ;
using ComboBox = System.Windows.Controls.ComboBox ;
using DataGrid = System.Windows.Controls.DataGrid ;
using ImportDwgMappingModel = Arent3d.Architecture.Routing.AppBase.Model.ImportDwgMappingModel ;
using Label = System.Windows.Controls.Label ;
using ProgressBar = Arent3d.Revit.UI.Forms.ProgressBar ;
using MessageBox = System.Windows.MessageBox ;
using Path = System.IO.Path ;
using Visibility = System.Windows.Visibility ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class CeedViewModel : NotifyPropertyChanged
  {
    private const string LegendNoDisplay = "×" ;
    private const string NotExistConnectorFamilyInFolderModelWarningMessage = "excelで指定したモデルはmodelフォルダーに存在していないため、既存のモデルを使用します。" ;
    public UIDocument UiDocument { get ; }
    private readonly Document _document ;
    private List<CeedModel> _ceedModels ;
    private List<CeedModel> _usingCeedModel ;
    private readonly CeedStorable _ceedStorable ;
    private readonly StorageService<Level, CeedUserModel> _storageService ;
    private readonly DefaultSettingStorable _defaultSettingStorable ;
    private readonly IElectricalPostCommandExecutorBase? _postCommandExecutor ;
    private List<string> _ceedModelNumberOfPreviewCategories ;

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
        if ( _isShowCondition.HasValue ) {
          var ceedModels = GetData() ;
          CeedModels.Clear() ;
          if ( ceedModels.Any() ) {
            if ( ! _isShowCondition.Value ) {
              ceedModels = GroupCeedModel( ceedModels ) ;
            }

            ceedModels = GroupCeedModelsByCeedModelNumber( ceedModels ) ;
            CeedModels.AddRange( ceedModels ) ;
          }

          AddModelNumber() ;
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

    private Visibility _isVisibleShowUsingCode ;
    public Visibility IsVisibleShowUsingCode
    {
      get => _isVisibleShowUsingCode ;
      set
      {
        _isVisibleShowUsingCode = value ;
        OnPropertyChanged();
      }
    }

    public bool IsShowDiff { get ; set ; }
    
    private bool _isEnableShowDiff ;
    public bool IsEnableShowDiff
    {
      get => _isEnableShowDiff ;
      set
      {
        _isEnableShowDiff = value ;
        OnPropertyChanged();
      }
    }
    
    private bool _isExistUsingCode ;

    public bool IsExistUsingCode
    {
      get => _isExistUsingCode ;
      set
      {
        _isExistUsingCode = value ;
        IsEnableShowDiff = ! _isExistUsingCode ;
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

    public ICommand SymbolRegistrationCommand => new RelayCommand( LoadUsingCeedModel ) ;
    public ICommand SearchCommand => new RelayCommand( Search ) ;
    public ICommand ResetCommand => new RelayCommand( Reset ) ;
    
    public ICommand OkCommand
    {
      get
      {
        return new RelayCommand<Window>( wd => null != wd, wd =>
        {
          UpdateCeedStorableAndStorageServiceData() ;
          wd.DialogResult = true ;
          wd.Close() ;
        } ) ;
      }
    }

    public CeedViewModel( UIDocument uiDocument, Document document, IElectricalPostCommandExecutorBase? postCommandExecutor )
    {
      UiDocument = uiDocument ;
      _document = document ;
      _defaultSettingStorable = _document.GetDefaultSettingStorable() ;
      _postCommandExecutor = postCommandExecutor ;
      CeedModels = new ObservableCollection<CeedModel>() ;

      var oldCeedStorable = _document.GetCeedStorable() ;
      _ceedStorable = _document.GetCeedStorable() ;
      var level = _document.ActiveView?.GenLevel ?? new FilteredElementCollector(_document).OfClass(typeof(Level)).OfType<Level>().OrderBy(x => x.Elevation).First();
      _storageService = new StorageService<Level, CeedUserModel>( level ) ;
      
      if ( ! oldCeedStorable.CeedModelData.Any() ) {
        _ceedModels = new List<CeedModel>() ;
        _usingCeedModel = new List<CeedModel>() ;
        _previewList = new ObservableCollection<PreviewListInfo>() ;
        Categories = new ObservableCollection<CategoryModel>() ;
        CategoriesPreview = new ObservableCollection<CategoryModel>() ;
        _ceedModelNumberOfPreviewCategories = new List<string>() ;
      }
      else {
        _ceedModels = oldCeedStorable.CeedModelData ;
        _usingCeedModel = oldCeedStorable.CeedModelUsedData ;
        _previewList = new ObservableCollection<PreviewListInfo>() ;
        _ceedModelNumberOfPreviewCategories = CategoryModel.GetCeedModelNumbers( oldCeedStorable.CategoriesWithoutCeedCode ) ;
        IsShowCeedModelNumber = _storageService.Data.IsShowCeedModelNumber ;
        IsShowOnlyUsingCode = _storageService.Data.IsShowOnlyUsingCode ;
        IsExistUsingCode = _storageService.Data.IsExistUsingCode ;
        IsShowDiff = _storageService.Data.IsDiff ;
        IsVisibleShowUsingCode = _usingCeedModel.Any() ? Visibility.Visible : Visibility.Hidden ;
        Categories = new ObservableCollection<CategoryModel>( CategoryModel.ConvertCategoryModel( oldCeedStorable.CategoriesWithCeedCode ) ) ;
        CategoriesPreview = new ObservableCollection<CategoryModel>( CategoryModel.ConvertCategoryModel( oldCeedStorable.CategoriesWithoutCeedCode ) ) ;
        IsShowCondition = _storageService.Data.IsShowCondition ;
      }

      _selectedCeedSetCode = string.Empty ;
      _selectedModelNumber = string.Empty ;
      _selectedDeviceSymbol = string.Empty ;
    }

    private void LoadData()
    {
      CeedModels.Clear() ;
      PreviewList.Clear() ;
      FindSelectedCategory( Categories, true ) ;
      FindSelectedCategory( CategoriesPreview, false ) ;
      AddModelNumber() ;
    }
    
    private List<CeedModel> GetData()
    {
      CeedModels.Clear() ;
      PreviewList.Clear() ;
      FindSelectedCategory( Categories, true ) ;
      FindSelectedCategory( CategoriesPreview, false ) ;

      return CeedModels.ToList() ;
    }

    private void AddModelNumber()
    {
      var ceedModels = IsExistUsingCode ? _usingCeedModel : _ceedModels ;
      if ( IsExistUsingCode && IsShowOnlyUsingCode ) ceedModels = _usingCeedModel.Where( c => c.IsUsingCode ).ToList() ;
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
            if ( ! ModelNumber.Contains( modelNumber.Trim() ) ) ModelNumber.Add( modelNumber.Trim() ) ;
          }
        }
        else if ( ceedModel.ModelNumber.IndexOf( ',' ) >= 0 )  {
          var modelNumbers = ceedModel.ModelNumber.Split( ',' ) ;
          foreach ( var modelNumber in modelNumbers ) {
            if ( ! ModelNumber.Contains( modelNumber.Trim() ) ) ModelNumber.Add( modelNumber.Trim() ) ;
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
            if ( ! DeviceSymbols.Contains( generalDisplayDeviceSymbol.Trim() ) ) DeviceSymbols.Add( generalDisplayDeviceSymbol.Trim() ) ;
          }
        }
        else {
          if ( ! DeviceSymbols.Contains( ceedModel.GeneralDisplayDeviceSymbol ) )
            DeviceSymbols.Add( ceedModel.GeneralDisplayDeviceSymbol ) ;
        }
      }
      
      ResetComboboxValue() ;

      var ceedModelNumbers = ceedModels.Where( ceedModel => ! string.IsNullOrEmpty( ceedModel.CeedModelNumber ) ).Select( c => c.CeedModelNumber ).Distinct().ToList() ;
      SetIsExistModelNumberForCategory( Categories, ceedModelNumbers ) ;
      SetIsExistModelNumberForCategory( CategoriesPreview, ceedModelNumbers ) ;
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
    
    private void SetIsExistModelNumberForCategory( IEnumerable<CategoryModel> categories, List<string> ceedModelNumbers )
    {
      foreach ( var category in categories ) {
        if ( category.CeedCodeNumbers.Any() ) {
          var isExistModelNumberOfCategory = false ;
          foreach ( var ceedCodeNumber in category.CeedCodeNumbers ) {
            var isExistModelNumber = ceedModelNumbers.FirstOrDefault( c => ceedModelNumbers.Contains( ceedCodeNumber.Name ) ) != null ;
            ceedCodeNumber.IsExistModelNumber = isExistModelNumber ;
            if ( isExistModelNumber ) {
              isExistModelNumberOfCategory = isExistModelNumber ;
            }
          }

          category.IsExistModelNumber = isExistModelNumberOfCategory ;
        }
        
        if ( ! category.Categories.Any() )
          continue ;

        SetIsExistModelNumberForCategory( category.Categories, ceedModelNumbers ) ;
        category.IsExistModelNumber = category.Categories.FirstOrDefault( c => c.IsExistModelNumber ) != null ;
      }
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
      var data = IsExistUsingCode ? _usingCeedModel : _ceedModels ;
      if ( IsExistUsingCode && IsShowOnlyUsingCode ) data = _usingCeedModel.Where( c => c.IsUsingCode ).ToList() ;
      CeedModels.Clear() ;
      PreviewList.Clear() ;
      var ceedCodeNumbers = categoryModel.CeedCodeNumbers.Select( c => c.Name ) ;
      data = categoryModel.CeedCodeNumbers.Any() ? data.Where( c => ceedCodeNumbers.Contains( c.CeedModelNumber ) ).ToList() : data ;
      if ( isCategoryWithCeedCode ) {
        if ( _ceedModelNumberOfPreviewCategories.Any() ) {
          data = data.Where( c => ! _ceedModelNumberOfPreviewCategories.Contains( c.CeedModelNumber ) ).ToList() ;
        }

        if ( ! IsShowCondition ) {
          data = GroupCeedModel( data ) ;
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
          firstCeedModel.IsAdded, firstCeedModel.IsEditFloorPlan, firstCeedModel.IsEditInstrumentation, firstCeedModel.IsEditCondition, firstCeedModel.IsUsingCode ) ;
        newCeedModels.Add( ceedModel ) ;
      }

      return newCeedModels ;
    }

    private void SaveCeedStorableAndStorageService()
    {
      try {
        if ( _postCommandExecutor == null ) {
          using Transaction t = new( _document, "Save CeeD data" ) ;
          t.Start() ;
          _ceedStorable.Save() ;
          _storageService.SaveChange() ;
          t.Commit() ;
        }
        else {
          _postCommandExecutor.SaveCeedStorableAndStorageServiceCommand( _ceedStorable, _storageService ) ;
        }
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        MessageBox.Show( "Save CeeD data failed.", "Error" ) ;
      }
    }

    public void Search()
    {
      var data = IsExistUsingCode ? _usingCeedModel : _ceedModels ;
      CeedModels.Clear() ;
      PreviewList.Clear() ;
      if ( IsExistUsingCode && IsShowOnlyUsingCode ) data = _usingCeedModel.Where( c => c.IsUsingCode ).ToList() ;
      if ( ! IsShowCondition ) data = GroupCeedModel( data ) ;
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
      _usingCeedModel = new List<CeedModel>() ;
      IsShowOnlyUsingCode = false ;
      IsExistUsingCode = false ;
      IsShowDiff = true ;
      IsVisibleShowUsingCode = Visibility.Hidden ;
      CeedModels.Clear() ;
      PreviewList.Clear() ;
      ResetSelectedCategory( Categories ) ;
      ResetSelectedCategory( CategoriesPreview ) ;
      AddModelNumber() ;
      UpdateCeedStorableAndStorageServiceData() ;
    }

    public void UpdateCeedStorableAndStorageServiceData()
    {
      _storageService.Data.IsShowCeedModelNumber = IsShowCeedModelNumber ;
      _storageService.Data.IsShowCondition = IsShowCondition ;
      _storageService.Data.IsShowOnlyUsingCode = IsShowOnlyUsingCode ;
      _storageService.Data.IsDiff = IsShowDiff ;
      _storageService.Data.IsExistUsingCode = IsExistUsingCode ;

      _ceedStorable.CeedModelUsedData = _usingCeedModel ;

      SaveCeedStorableAndStorageService() ;
    }
    
    public void ResetData()
    {
      CeedModels.Clear() ;
      PreviewList.Clear() ;
      ResetComboboxValue() ;
      ResetSelectedCategory( Categories ) ;
      ResetSelectedCategory( CategoriesPreview ) ;
      UpdateCeedStorableAndStorageServiceData() ;
    }

    private void ResetComboboxValue()
    {
      SelectedCeedSetCode = string.Empty ;
      SelectedModelNumber = string.Empty ;
      SelectedDeviceSymbolValue = string.Empty ;
    }

    private void LoadUsingCeedModel()
    {
      if ( _ceedStorable.CeedModelData.Any() ) {
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
          var ceedModels = _ceedStorable.CeedModelData.Where( c => c.ModelNumber.Contains( modelNumber ) ).Distinct().ToList() ;
          foreach ( var ceedModel in ceedModels ) {
            ceedModel.IsUsingCode = true ;
          }

          usingCeedModel.AddRange( ceedModels ) ;
        }

        var ceedModelsNoDisplay = _ceedModels.Where( c => c.LegendDisplay == LegendNoDisplay ) ;
        usingCeedModel.AddRange( ceedModelsNoDisplay ) ;

        usingCeedModel = usingCeedModel.Distinct().ToList() ;
        _usingCeedModel = usingCeedModel ;
        IsVisibleShowUsingCode = Visibility.Visible ;
        IsShowDiff = false ;
        IsShowOnlyUsingCode = true ;
        IsExistUsingCode = true ;
        LoadData() ;

        if ( ! _usingCeedModel.Any() ) return ;
        UpdateCeedStorableAndStorageServiceData() ;
      }
      else {
        MessageBox.Show( "Please read csv.", "Message" ) ;
      }
    }

    public void ShowCeedModelNumberColumn( DataGrid dtGrid, Label label, ComboBox comboBox )
    {
      dtGrid.Columns[ 1 ].Visibility = Visibility.Visible ;
      label.Visibility = Visibility.Visible ;
      comboBox.Visibility = Visibility.Visible ;
    }

    private List<CeedModel> GroupCeedModel(IEnumerable<CeedModel> ceedModels )
    {
      return ceedModels.GroupBy( x => x.GeneralDisplayDeviceSymbol ).Select( x => MoreEnumerable.DistinctBy( x.ToList(), y => y.ModelNumber ) ).SelectMany( x => x ).ToList() ;
    }

    public void UnShowCeedModelNumberColumn( DataGrid dtGrid, Label label, ComboBox comboBox )
    {
      dtGrid.Columns[ 1 ].Visibility = Visibility.Hidden ;
      label.Visibility = Visibility.Hidden ;
      comboBox.Visibility = Visibility.Hidden ;
    }

    public void ShowOnlyUsingCode()
    {
      if ( ! _usingCeedModel.Any() ) return ;
      IsExistUsingCode = true ;
      LoadData() ;
    }

    public void UnShowOnlyUsingCode()
    {
      if ( ! _usingCeedModel.Any() ) return ;
      IsExistUsingCode = true ;
      LoadData() ;
    }

    private void UpdateCeedStorableAfterReplaceFloorPlanSymbol( string connectorFamilyName )
    {
      if ( _ceedModels.Any() ) {
        List<CeedModel> ceedModels = GetCeedModels( _ceedModels ) ;
        foreach ( var ceedModel in ceedModels ) {
          ceedModel.FloorPlanType = connectorFamilyName ;
        }
        _ceedStorable.CeedModelData = _ceedModels ;
      }

      if ( _usingCeedModel.Any() ) {
        List<CeedModel> ceedModels = GetCeedModels( _usingCeedModel ) ;
        foreach ( var ceedModel in ceedModels ) {
          ceedModel.FloorPlanType = connectorFamilyName ;
        }
        _ceedStorable.CeedModelUsedData = _usingCeedModel ;
      }

      if ( _previewList.Any() ) {
        foreach ( var previewInfo in _previewList ) {
          previewInfo.FloorPlanType = connectorFamilyName ;
        }
      }

      SaveCeedStorableAndStorageService() ;
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
      var selectConnectorFamilyDialog = new SelectConnectorFamily( _document, _storageService, _postCommandExecutor ) ;
      selectConnectorFamilyDialog.ShowDialog() ;
      if ( ! ( selectConnectorFamilyDialog.DialogResult ?? false ) ) return ;
      _storageService.Data.ConnectorFamilyUploadData = selectConnectorFamilyDialog.StorageService.Data.ConnectorFamilyUploadData ;
      var selectedConnectorFamily = selectConnectorFamilyDialog.ConnectorFamilyList.SingleOrDefault( f => f.IsSelected ) ;
      if ( selectedConnectorFamily == null ) {
        MessageBox.Show( "No connector family selected.", "Error" ) ;
        return ;
      }

      var connectorFamilyFileName = selectedConnectorFamily.ToString() ;
      var connectorFamilyName = connectorFamilyFileName.Replace( ".rfa", "" ) ;
      if ( SelectedCeedModel == null || string.IsNullOrEmpty( connectorFamilyFileName ) ) return ;

      using var progress = ProgressBar.ShowWithNewThread( new UIApplication(_document.Application) ) ;
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
        connectorFamilyPaths = Directory.GetFiles( familyFolder.FullName ).Where( f => Path.GetExtension( f ) == ".rfa" ).ToList() ;
      }
      else {
        MessageBox.Show( "指定したフォルダーにはモデルデータが存在していません。モデルデータをmodelフォルダに入れてください。", "Error" ) ;
        return ;
      }

      if ( connectorFamilyPaths.Any() ) {
        try {
          List<string> connectorFamilyFiles ;
          List<ExcelToModelConverter.ConnectorFamilyReplacement> connectorFamilyReplacements ;
          using var progress = ProgressBar.ShowWithNewThread( new UIApplication(_document.Application) ) ;
          progress.Message = "Processing......." ;
          using ( var progressData = progress.Reserve( 0.3 ) ) {
            connectorFamilyReplacements = ExcelToModelConverter.GetConnectorFamilyReplacements( infoPath ) ;
            connectorFamilyFiles = LoadConnectorFamily( _document, connectorFamilyPaths ) ;
            progressData.ThrowIfCanceled() ;
          }

          if ( connectorFamilyFiles.Any() && connectorFamilyReplacements.Any() ) {
            bool result ;
            using ( var progressData = progress.Reserve( 0.6 ) ) {
              result = IsUpdateCeedStorableAfterReplaceMultipleSymbolsSuccessfully( _ceedModels, _usingCeedModel, connectorFamilyReplacements, connectorFamilyFiles ) ;
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

      if ( _postCommandExecutor == null ) {
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
      }
      else {
        List<LoadFamilyCommandParameter> familyParameters = ( from connectorFamilyPath in connectorFamilyPaths select new LoadFamilyCommandParameter( connectorFamilyPath, string.Empty ) ).ToList() ;
        _postCommandExecutor.LoadFamilyCommand( familyParameters ) ;
        connectorFamilyFiles.AddRange( from familyParameter in familyParameters select Path.GetFileName( familyParameter.FilePath ) ) ;
      }

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

    private bool IsUpdateCeedStorableAfterReplaceMultipleSymbolsSuccessfully( List<CeedModel>? allCeedModels, List<CeedModel>? usingCeedModel, IReadOnlyCollection<ExcelToModelConverter.ConnectorFamilyReplacement> connectorFamilyReplacements, ICollection<string> connectorFamilyFileName )
    {
      List<string> deviceSymbolsNotHaveConnectorFamily = new() ;
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

        _ceedStorable.CeedModelData = allCeedModels ;
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

        _ceedStorable.CeedModelUsedData = usingCeedModel ;
      }

      if ( _previewList.Any() ) {
        foreach ( var connectorFamilyReplacement in connectorFamilyReplacements ) {
          if ( ! connectorFamilyFileName.Contains( connectorFamilyReplacement.ConnectorFamilyFile ) ) continue ;
          var deviceSymbols = connectorFamilyReplacement.DeviceSymbols.Split( '\n' ) ;
          foreach ( var deviceSymbol in deviceSymbols ) {
            var previewInfos = _previewList.Where( c => c.GeneralDisplayDeviceSymbol == deviceSymbol ).ToList() ;
            if ( ! previewInfos.Any() ) continue ;
            var connectorFamilyName = connectorFamilyReplacement.ConnectorFamilyFile.Replace( ".rfa", "" ) ;
            foreach ( var previewInfo in previewInfos ) {
              previewInfo.FloorPlanType = connectorFamilyName ;
            }
          }
        }
      }

      var newConnectorFamilyUploadFiles = connectorFamilyFileName.Where( f => ! _storageService.Data.ConnectorFamilyUploadData.Contains( f ) ).ToList() ;
      _storageService.Data.ConnectorFamilyUploadData.AddRange( newConnectorFamilyUploadFiles ) ;

      SaveCeedStorableAndStorageService() ;

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

    public void CreateConnector()
    {
      const string switch2DSymbol = "2Dシンボル切り替え" ;
      const string symbolMagnification = "シンボル倍率" ;
      const string grade3 = "グレード3" ;
      var defaultSymbolMagnification = ImportDwgMappingModel.GetDefaultSymbolMagnification( _document ) ;
      var defaultConstructionItem = _document.GetDefaultConstructionItem() ;

      if ( SelectedCeedCode == null )
        return ;

      XYZ? point ;
      try {
        point = UiDocument.Selection.PickPoint( "Connectorの配置場所を選択して下さい。" ) ;
        if ( _document.ActiveView is not ViewPlan ) {
          TaskDialog.Show( "Arent", "This view is not the view plan!" ) ;
          return ;
        }
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return ;
      }

      var condition = "屋外" ; // デフォルトの条件

      var symbol = _document.GetFamilySymbols( ElectricalRoutingFamilyType.Room ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      var filter = new FamilyInstanceFilter( _document, symbol.Id ) ;
      var rooms = new FilteredElementCollector( _document ).WherePasses( filter ).OfType<FamilyInstance>().Where( x =>
      {
        var bb = x.get_BoundingBox( null ) ;
        var ol = new Outline( bb.Min, bb.Max ) ;
        return ol.Contains( point, GeometryHelper.Tolerance ) ;
      } ).ToList() ;

      switch ( rooms.Count ) {
        case 0 :
          if ( IsShowCondition ) {
            condition = SelectedCondition ;
          }
          else {
            TaskDialog.Show( "Arent", "部屋の外で電気シンボルを作成することができません。部屋の中の場所を指定してください！" ) ;
            return ;
          }

          break ;
        case > 1 when CreateRoomCommandBase.TryGetConditions( _document, out var conditions ) && conditions.Any() :
          var vm = new ArentRoomViewModel { Conditions = conditions } ;
          var view = new ArentRoomView { DataContext = vm } ;
          view.ShowDialog() ;
          if ( ! vm.IsCreate )
            return ;

          if ( IsShowCondition && SelectedCondition != vm.SelectedCondition ) {
            TaskDialog.Show( "Arent", "指定した条件が部屋の条件と一致していないので、再度ご確認ください。" ) ;
            return ;
          }

          condition = vm.SelectedCondition ;
          break ;
        case > 1 :
          TaskDialog.Show( "Arent", "指定された条件が見つかりませんでした。" ) ;
          return ;
        default :
        {
          if ( rooms.First().TryGetProperty( ElectricalRoutingElementParameter.RoomCondition, out string? value ) && ! string.IsNullOrEmpty( value ) ) {
            if ( IsShowCondition && SelectedCondition != value ) {
              TaskDialog.Show( "Arent", "指定した条件が部屋の条件と一致していないので、再度ご確認ください。" ) ;
              return ;
            }

            condition = value ;
          }

          break ;
        }
      }

      var deviceSymbol = SelectedDeviceSymbol ?? string.Empty ;
      if ( ! OriginCeedModels.Any( cmd => cmd.Condition == condition && cmd.GeneralDisplayDeviceSymbol == deviceSymbol ) ) {
        TaskDialog.Show( "Arent", $"We can not find any ceedmodel \"{SelectedDeviceSymbol}\" match with this room \"{condition}\"。" ) ;
        return ;
      }

      var ecoMode = _defaultSettingStorable.EcoSettingData.IsEcoMode.ToString() ;
      var level = UiDocument.ActiveView.GenLevel ;
      var heightOfConnector = _document.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;
      var element = GenerateConnector( UiDocument, point.X, point.Y, heightOfConnector, level, SelectedFloorPlanType ?? string.Empty, ecoMode ) ;
      var ceedCode = string.Join( ":", SelectedCeedCode, SelectedDeviceSymbol, SelectedModelNum ) ;
      if ( element is FamilyInstance familyInstance ) {
        familyInstance.SetProperty( ElectricalRoutingElementParameter.CeedCode, ceedCode ) ;
        familyInstance.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, defaultConstructionItem ) ;
        if ( ! string.IsNullOrEmpty( deviceSymbol ) ) familyInstance.SetProperty( ElectricalRoutingElementParameter.SymbolContent, deviceSymbol ) ;
        familyInstance.SetProperty( ElectricalRoutingElementParameter.Quantity, string.Empty ) ;
        familyInstance.SetConnectorFamilyType( ConnectorFamilyType.Sensor ) ;
      }

      _postCommandExecutor?.CreateSymbolContentTagCommand( element, point, deviceSymbol ) ;

      if ( element.HasParameter( switch2DSymbol ) )
        element.SetProperty( switch2DSymbol, true ) ;

      if ( element.HasParameter( symbolMagnification ) )
        element.SetProperty( symbolMagnification, defaultSymbolMagnification ) ;

      if ( element.HasParameter( grade3 ) )
        element.SetProperty( grade3, DefaultSettingCommandBase.GradeFrom3To7Collection.Contains( _defaultSettingStorable.GradeSettingData.GradeMode ) ) ;
    }
    
    private Element GenerateConnector( UIDocument uiDocument, double originX, double originY, double originZ, Level level, string floorPlanType, string ecoMode )
    {
      FamilyInstance instance;
      if ( ! string.IsNullOrEmpty( floorPlanType ) ) {
        var connectorOneSideFamilyTypeNames = ToHashSetExtension.ToHashSet( ( (ConnectorOneSideFamilyType[]) Enum.GetValues( typeof( ConnectorOneSideFamilyType ) ) ).Select( f => f.GetFieldName() ) ) ;
        if ( connectorOneSideFamilyTypeNames.Contains( floorPlanType ) ) {
          var connectorOneSideFamilyType = GetConnectorFamilyType( floorPlanType ) ;
          var symbol = uiDocument.Document.GetFamilySymbols( connectorOneSideFamilyType ).FirstOrDefault() ?? ( uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.ConnectorOneSide ).FirstOrDefault() ?? throw new InvalidOperationException() ) ;
          instance = symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
          SetIsEcoMode( instance, ecoMode ) ;
          return instance ;
        }

        if ( new FilteredElementCollector( uiDocument.Document ).OfClass( typeof( Family ) ).FirstOrDefault( f => f.Name == floorPlanType ) is Family family ) {
          foreach ( var familySymbolId in family.GetFamilySymbolIds() ) {
            var symbol = uiDocument.Document.GetElementById<FamilySymbol>( familySymbolId ) ?? throw new InvalidOperationException() ;
            instance = symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
            SetIsEcoMode( instance, ecoMode ) ;
            return instance ;
          }
        }
      }

      var routingSymbol = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.ConnectorOneSide ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      instance = routingSymbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
      SetIsEcoMode( instance, ecoMode ) ;
      return instance ;
    }

    private static ConnectorOneSideFamilyType GetConnectorFamilyType( string floorPlanType )
    {
      var connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide1 ;
      if ( string.IsNullOrEmpty( floorPlanType ) ) return connectorOneSideFamilyType ;
      foreach ( var item in (ConnectorOneSideFamilyType[]) Enum.GetValues( typeof( ConnectorOneSideFamilyType ) ) ) {
        if ( floorPlanType == item.GetFieldName() ) connectorOneSideFamilyType = item ;
      }

      return connectorOneSideFamilyType ;
    }

    private static void SetIsEcoMode( Element instance, string ecoMode )
    {
      if ( false == instance.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? _ ) ) return ;
      instance.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, ecoMode ) ;
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

    public static void CheckChangeColor( List<CeedModel> ceedModels, List<CeedModel> previousCeedModels )
    {
      for ( int i = 0 ; i < ceedModels.Count() ; i++ ) {
        CeedModel item = ceedModels[ i ] ;
        var itemExistCeedModel = previousCeedModels.Find( x => x.CeedSetCode == item.CeedSetCode && x.CeedModelNumber == item.CeedModelNumber && x.GeneralDisplayDeviceSymbol == item.GeneralDisplayDeviceSymbol && x.ModelNumber == item.ModelNumber ) ;
        if ( itemExistCeedModel != null ) {
          item.IsEditFloorPlan = IsChange( string.IsNullOrEmpty( itemExistCeedModel.FloorPlanSymbol ) ? itemExistCeedModel.Base64FloorPlanImages : itemExistCeedModel.FloorPlanSymbol, string.IsNullOrEmpty( item.FloorPlanSymbol ) ? item.Base64FloorPlanImages : item.FloorPlanSymbol ) ;
          item.IsEditInstrumentation = IsChange( string.IsNullOrEmpty( itemExistCeedModel.InstrumentationSymbol ) ? itemExistCeedModel.Base64InstrumentationImageString : itemExistCeedModel.InstrumentationSymbol, string.IsNullOrEmpty( item.InstrumentationSymbol ) ? item.Base64InstrumentationImageString : item.InstrumentationSymbol ) ;
          item.IsEditCondition = IsChange( itemExistCeedModel.Condition, item.Condition ) ;
        }
        else {
          item.IsAdded = true ;
        }
      }
    }

    private static bool IsChange( string oldItem, string newItem )
    {
      return oldItem != newItem ;
    }

    public void ShowPreviewList( CeedModel? ceedModel )
    {
      PreviewList.Clear() ;
      if ( ceedModel == null ) return ;
      var ceedModels = IsExistUsingCode ? _usingCeedModel : _ceedModels ;
      ceedModels = GetCeedModels( ceedModels ) ;
      if ( ! ceedModels.Any() ) return ;
      CreatePreviewList( ceedModels ) ;
    }

    private void CreatePreviewList( List<CeedModel> ceedModels )
    {
      foreach ( var ceedModel in ceedModels ) {
        if ( string.IsNullOrEmpty( ceedModel.Base64FloorPlanImages ) ) continue ;
        var floorPlanImage = CeedModel.BitmapToImageSource( CeedModel.Base64StringToBitmap( ceedModel.Base64FloorPlanImages ) ) ;
        if ( floorPlanImage == null ) continue ;
        PreviewList.Add( new PreviewListInfo( ceedModel.CeedSetCode, ceedModel.ModelNumber, ceedModel.GeneralDisplayDeviceSymbol, ceedModel.Condition, ceedModel.FloorPlanType, floorPlanImage ) ) ;
      }
    }

    public class PreviewListInfo
    {
      public string CeedSetCode { get ; }
      public string ModelNumber { get ; }
      public string GeneralDisplayDeviceSymbol { get ; }
      public string Condition { get ; }
      public string FloorPlanType { get ; set ; }
      public BitmapImage FloorPlanImage { get ; }

      public PreviewListInfo( string ceedSetCode, string modelNumber, string generalDisplayDeviceSymbol, string condition, string floorPlanType, BitmapImage floorPlanImage )
      {
        CeedSetCode = ceedSetCode ;
        ModelNumber = modelNumber ;
        GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
        Condition = condition ;
        FloorPlanType = floorPlanType ;
        FloorPlanImage = floorPlanImage ;
      }
    }
  }
}