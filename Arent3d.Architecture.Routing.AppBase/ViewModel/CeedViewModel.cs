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
using Arent3d.Architecture.Routing.AppBase.Commands.PostCommands ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.AppBase.Model ;
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
using CheckBox = System.Windows.Controls.CheckBox ;
using ComboBox = System.Windows.Controls.ComboBox ;
using DataGrid = System.Windows.Controls.DataGrid ;
using ImportDwgMappingModel = Arent3d.Architecture.Routing.AppBase.Model.ImportDwgMappingModel ;
using Label = System.Windows.Controls.Label ;
using ProgressBar = Arent3d.Revit.UI.Forms.ProgressBar ;
using MessageBox = System.Windows.MessageBox ;
using Visibility = System.Windows.Visibility ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class CeedViewModel : NotifyPropertyChanged
  {
    private const string NotExistConnectorFamilyInFolderModelWarningMessage = "excelで指定したモデルはmodelフォルダーに存在していないため、既存のモデルを使用します。" ;
    private readonly UIDocument _uiDocument ;
    private readonly Document _document ;
    private List<CeedModel> _ceedModels ;
    private List<CeedModel> _usingCeedModel ;
    private List<CeedModel> _previousCeedModels ;
    private readonly CeedStorable _ceedStorable ;
    private readonly StorageService<Level, CeedUserModel> _storageService ;
    private readonly DefaultSettingStorable _defaultSettingStorable ;
    private readonly IPostCommandExecutorBase? _postCommandExecutor ;

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
            CeedModels.AddRange( _ceedModels );
          }
          else {
            CeedModels.AddRange( GroupCeedModel(_ceedModels) );
          }
        }
        OnPropertyChanged();
      }
    }

    public bool IsShowOnlyUsingCode { get ; set ; }

    public bool IsShowDiff { get ; set ; }

    public bool IsExistUsingCode { get ; set ; }

    public CeedModel? SelectedCeedModel { get ; set ; }
    public string? SelectedDeviceSymbol { get ; set ; }
    public string? SelectedCondition { get ; set ; }
    public string? SelectedCeedCode { get ; set ; }
    public string? SelectedModelNum { get ; set ; }
    public string? SelectedFloorPlanType { get ; set ; }

    private ObservableCollection<CeedModel> _previewList ;

    public ObservableCollection<CeedModel> PreviewList
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

        CategorySelected = FindSelectedCategory( _categories ) ;

        return _categories ;
      }
      set
      {
        _categories = value ;
        CategorySelected = FindSelectedCategory( _categories ) ;
        OnPropertyChanged() ;
      }
    }

    private CategoryModel? _categorySelected ;

    public CategoryModel? CategorySelected
    {
      get { return _categorySelected ??= FindSelectedCategory( Categories ) ; }
      set => _categorySelected = value ;
    }

    public ICommand SelectedItemCommand
    {
      get
      {
        return new RelayCommand<System.Windows.Controls.TreeView>( tv => null != tv, _ =>
        {
          CategorySelected = FindSelectedCategory( Categories ) ;
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

        CategoryPreviewSelected = FindSelectedCategory( _categoriesPreview ) ;

        return _categoriesPreview ;
      }
      set
      {
        _categoriesPreview = value ;
        CategoryPreviewSelected = FindSelectedCategory( _categoriesPreview ) ;
        OnPropertyChanged() ;
      }
    }

    private CategoryModel? _categoryPreviewSelected ;

    public CategoryModel? CategoryPreviewSelected
    {
      get { return _categoryPreviewSelected ??= FindSelectedCategory( CategoriesPreview ) ; }
      set => _categoryPreviewSelected = value ;
    }

    public ICommand SelectedCategoryPreviewCommand
    {
      get
      {
        return new RelayCommand<System.Windows.Controls.TreeView>( tv => null != tv, _ =>
        {
          CategoryPreviewSelected = FindSelectedCategory( CategoriesPreview ) ;
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

    public CeedViewModel( UIDocument uiDocument, Document document, IPostCommandExecutorBase? postCommandExecutor )
    {
      _uiDocument = uiDocument ;
      _document = document ;
      _defaultSettingStorable = _document.GetDefaultSettingStorable() ;
      _postCommandExecutor = postCommandExecutor ;
      CeedModels = new ObservableCollection<CeedModel>() ;

      var oldCeedStorable = _document.GetCeedStorable() ;
      _ceedStorable = _document.GetCeedStorable() ;
      _storageService = new StorageService<Level, CeedUserModel>(((ViewPlan)_document.ActiveView).GenLevel) ;
      
      if ( ! oldCeedStorable.CeedModelData.Any() ) {
        _ceedModels = new List<CeedModel>() ;
        _usingCeedModel = new List<CeedModel>() ;
        _previousCeedModels = new List<CeedModel>() ;
        _previewList = new ObservableCollection<CeedModel>() ;
      }
      else {
        _ceedModels = oldCeedStorable.CeedModelData ;
        _usingCeedModel = oldCeedStorable.CeedModelUsedData ;
        _previousCeedModels = new List<CeedModel>( oldCeedStorable.CeedModelData ) ;
        IsShowCeedModelNumber = _storageService.Data.IsShowCeedModelNumber ;
        IsShowCondition = _storageService.Data.IsShowCondition ;
        IsShowOnlyUsingCode = _storageService.Data.IsShowOnlyUsingCode ;
        AddModelNumber( CeedModels ) ;
        if ( _usingCeedModel.Any() )
          IsExistUsingCode = true ;
        if ( ! _ceedModels.Any() ) IsShowDiff = true ;
        else IsShowDiff = _storageService.Data.IsDiff ;
        _previewList = new ObservableCollection<CeedModel>( oldCeedStorable.CeedModelData ) ;
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
      foreach ( var dataModel in _ceedModels ) {
        CeedModels.Add( dataModel ) ;
        PreviewList.Add( dataModel ) ;
      }

      AddModelNumber( CeedModels ) ;
      if ( ceedStorable.CeedModelUsedData.Any() )
        _usingCeedModel = ceedStorable.CeedModelUsedData ;
    }

    private void LoadData( List<CeedModel> ceedModels )
    {
      CeedModels.Clear() ;
      PreviewList.Clear() ;
      foreach ( var dataModel in ceedModels ) {
        CeedModels.Add( dataModel ) ;
        PreviewList.Add( dataModel ) ;
      }

      AddModelNumber( CeedModels ) ;
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
        var modelNumbers = ceedModel.ModelNumber.Split( '\n' ) ;
        foreach ( var modelNumber in modelNumbers ) {
          if ( ! ModelNumber.Contains( modelNumber ) ) ModelNumber.Add( modelNumber ) ;
        }
      }

      DeviceSymbols.Clear() ;
      foreach ( var ceedModel in ceedModels.Where( ceedModel => ! string.IsNullOrEmpty( ceedModel.GeneralDisplayDeviceSymbol ) ) ) {
        if ( ! DeviceSymbols.Contains( ceedModel.GeneralDisplayDeviceSymbol ) )
          DeviceSymbols.Add( ceedModel.GeneralDisplayDeviceSymbol ) ;
      }
      
      ResetComboboxValue() ;
    }

    private List<CategoryModel> GetCategoryModels()
    {
      List<CategoryModel> categoryModels = new() ;

      var categoryModel1 = new CategoryModel { Name = "Category 1", ParentName = string.Empty, IsExpanded = false, IsSelected = true } ;
      categoryModels.Add( categoryModel1 ) ;

      var categoryModel2 = new CategoryModel { Name = "Category 2", ParentName = string.Empty, IsExpanded = false, IsSelected = false } ;
      categoryModels.Add( categoryModel2 ) ;

      return categoryModels ;
    }

    private CategoryModel? FindSelectedCategory( IEnumerable<CategoryModel> categories )
    {
      foreach ( var category in categories ) {
        if ( category.IsSelected )
          //find ceed model by category
          return category ;

        if ( ! category.SubCategories.Any() )
          continue ;

        var subCategory = FindSelectedCategory( category.SubCategories ) ;
        if ( null != subCategory )
          //find ceed model by category
          return subCategory ;
      }

      return null ;
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
      using var progress = ProgressBar.ShowWithNewThread( new UIApplication(_document.Application) ) ;
      progress.Message = "Loading data..." ;
      //var ceedStorable = _document.GetCeedStorable() ;
      var ceedModelData = ExcelToModelConverter.GetAllCeedModelNumber( filePath, fileEquipmentSymbolsPath ) ;
        if ( ! ceedModelData.Any() ) return ;
        _previousCeedModels = new List<CeedModel>( CeedModels ) ;
        CheckChangeColor( ceedModelData ) ;
        _ceedStorable.CeedModelData = ceedModelData ;
        _ceedStorable.CeedModelUsedData = new List<CeedModel>() ;
        _storageService.Data.IsShowOnlyUsingCode = false ;
        LoadData( _ceedStorable ) ;
        checkBox.Visibility = Visibility.Hidden ;
        checkBox.IsChecked = false ;
        IsShowOnlyUsingCode = false ;
        //IsShowDiff = true ;

        try {
          //using Transaction t = new( _document, "Save data" ) ;
          //t.Start() ;
          // using ( var progressData = progress.Reserve( 0.5 ) ) {
          //   _ceedStorable.Save() ;
          //   _storageService.SaveChange();
          //   progressData.ThrowIfCanceled() ;
          // }

          using var progressData = progress.Reserve( 0.9 ) ;
          _document.MakeCertainAllConnectorFamilies() ;
          progressData.ThrowIfCanceled() ;

          //t.Commit() ;
        }
        catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        }
    }

    public void Save()
    {
      try {
        //using Transaction t = new( _document, "Save data" ) ;
        //t.Start() ;
        _storageService.Data.IsShowCeedModelNumber = IsShowCeedModelNumber ;
        _storageService.Data.IsShowCondition = IsShowCondition ;
        _storageService.Data.IsShowOnlyUsingCode = IsShowOnlyUsingCode ;
        _storageService.Data.IsDiff = IsShowDiff ;
        _storageService.SaveChange() ;
        //t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }

    public void Search()
    {
      var data = IsShowOnlyUsingCode ? _usingCeedModel : _ceedModels ;
      CeedModels.Clear() ;
      PreviewList.Clear() ;
      data = string.IsNullOrEmpty( _selectedDeviceSymbol ) ? data : data.Where( c => c.GeneralDisplayDeviceSymbol.ToUpper().Contains( _selectedDeviceSymbol.ToUpper() ) ).ToList() ;
      data = string.IsNullOrEmpty( _selectedCeedSetCode ) ? data : data.Where( c => c.CeedSetCode.ToUpper().Contains( _selectedCeedSetCode.ToUpper() ) ).ToList() ;
      data = string.IsNullOrEmpty( _selectedModelNumber ) ? data : data.Where( c => c.ModelNumber.ToUpper().Contains( _selectedModelNumber.ToUpper() ) ).ToList() ;
      foreach ( var dataModel in data ) {
        CeedModels.Add( dataModel ) ;
        PreviewList.Add( dataModel ) ;
      }
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
          usingCeedModel.AddRange( ceedModels ) ;
        }

        usingCeedModel = usingCeedModel.Distinct().ToList() ;
        _usingCeedModel = usingCeedModel ;
        LoadData( _usingCeedModel ) ;
        checkBox.Visibility = Visibility.Visible ;
        checkBox.IsChecked = true ;

        // if ( ! _usingCeedModel.Any() ) return ;
        // try {
        //   //using Transaction t = new( _document, "Save data" ) ;
        //   //t.Start() ;
           _ceedStorable.CeedModelUsedData = _usingCeedModel ;
           _storageService.Data.IsShowOnlyUsingCode = true ;
        //   _ceedStorable.Save() ;
        //   _storageService.SaveChange();
        //   //t.Commit() ;
        // }
        // catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        // }
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

    private IEnumerable<CeedModel> GroupCeedModel(IEnumerable<CeedModel> ceedModels )
    {
      return ceedModels.GroupBy( x => x.GeneralDisplayDeviceSymbol ).Select( x => MoreEnumerable.DistinctBy( x.ToList(), y => y.ModelNumber ) ).SelectMany( x => x ) ;
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
      LoadData( _usingCeedModel ) ;
    }

    public void UnShowOnlyUsingCode()
    {
      if ( ! _ceedModels.Any() ) return ;
      LoadData( _ceedModels ) ;
    }

    private void UpdateCeedStorableAfterReplaceFloorPlanSymbol( string connectorFamilyName )
    {
      if ( _ceedModels.Any() ) {
        var ceedModel = _ceedModels.First( c => c.CeedSetCode == SelectedCeedModel!.CeedSetCode && c.GeneralDisplayDeviceSymbol == SelectedCeedModel.GeneralDisplayDeviceSymbol && c.ModelNumber == SelectedCeedModel.ModelNumber ) ;
        if ( ceedModel != null ) {
          ceedModel.FloorPlanType = connectorFamilyName ;
          _ceedStorable.CeedModelData = _ceedModels ;
        }
      }

      if ( _usingCeedModel.Any() ) {
        var ceedModel = _usingCeedModel.FirstOrDefault( c => c.CeedSetCode == SelectedCeedModel!.CeedSetCode && c.GeneralDisplayDeviceSymbol == SelectedCeedModel.GeneralDisplayDeviceSymbol && c.ModelNumber == SelectedCeedModel.ModelNumber ) ;
        if ( ceedModel != null ) {
          ceedModel.FloorPlanType = connectorFamilyName ;
          _ceedStorable.CeedModelUsedData = _usingCeedModel ;
        }
      }

      // try {
      //   //using Transaction t = new( _document, "Save CeeD data" ) ;
      //   //t.Start() ;
      //   _ceedStorable.Save() ;
      //   //t.Commit() ;
      // }
      // catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      //   MessageBox.Show( "Save CeeD data failed.", "Error" ) ;
      // }
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

      //using Transaction loadTransaction = new( document, "Load connector's family" ) ;
      //loadTransaction.Start() ;
      List<LoadFamilyCommandParameter> familyParameters = ( from connectorFamilyPath in connectorFamilyPaths select new LoadFamilyCommandParameter( connectorFamilyPath, string.Empty ) ).ToList() ;

      _postCommandExecutor?.LoadFamilyCommand( familyParameters ) ;
      foreach ( var familyParameter in familyParameters ) {
        //if ( ! existedConnectorFamilies.ContainsValue( familyParameter.FilePath ) ) continue ;
        var connectorFamilyFile = Path.GetFileName( familyParameter.FilePath ) ;
        connectorFamilyFiles.Add( connectorFamilyFile ) ;
      }

      //loadTransaction.Commit() ;
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

      var newConnectorFamilyUploadFiles = connectorFamilyFileName.Where( f => ! _storageService.Data.ConnectorFamilyUploadData.Contains( f ) ).ToList() ;
      _storageService.Data.ConnectorFamilyUploadData.AddRange( newConnectorFamilyUploadFiles ) ;

      // try {
      //   //using Transaction t = new( document, "Save CeeD data" ) ;
      //   //t.Start() ;
      //   _ceedStorable.Save() ;
      //   _storageService.SaveChange() ;
      //   //t.Commit() ;
      // }
      // catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      //   MessageBox.Show( "Save CeeD data failed.", "Error" ) ;
      //   return false ;
      // }

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
    
    public bool CreateConnector()
    {
      const string switch2DSymbol = "2Dシンボル切り替え" ;
      const string symbolMagnification = "シンボル倍率" ;
      const string grade3 = "グレード3" ;
      var defaultSymbolMagnification = ImportDwgMappingModel.GetDefaultSymbolMagnification( _document ) ;
      var defaultConstructionItem = _document.GetDefaultConstructionItem() ;

      if ( string.IsNullOrEmpty( SelectedDeviceSymbol ) )
        return true ;
      
      XYZ? point ;
      try {
        point = _uiDocument.Selection.PickPoint( "Connectorの配置場所を選択して下さい。" ) ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return false ;
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
            return false ;
          }

          break ;
        case > 1 when CreateRoomCommandBase.TryGetConditions( _uiDocument.Document, out var conditions ) && conditions.Any() :
          var vm = new ArentRoomViewModel { Conditions = conditions } ;
          var view = new ArentRoomView { DataContext = vm } ;
          view.ShowDialog() ;
          if ( ! vm.IsCreate )
            return false ;

          if ( IsShowCondition && SelectedCondition != vm.SelectedCondition ) {
            TaskDialog.Show( "Arent", "指定した条件が部屋の条件と一致していないので、再度ご確認ください。" ) ;
            return false ;
          }

          condition = vm.SelectedCondition ;
          break ;
        case > 1 :
          TaskDialog.Show( "Arent", "指定された条件が見つかりませんでした。" ) ;
          return false ;
        default :
        {
          if ( rooms.First().TryGetProperty( ElectricalRoutingElementParameter.RoomCondition, out string? value ) && ! string.IsNullOrEmpty( value ) ) {
            if ( IsShowCondition && SelectedCondition != value ) {
              TaskDialog.Show( "Arent", "指定した条件が部屋の条件と一致していないので、再度ご確認ください。" ) ;
              return false ;
            }

            condition = value ;
          }

          break ;
        }
      }

      if ( ! OriginCeedModels.Any( cmd => cmd.Condition == condition && cmd.GeneralDisplayDeviceSymbol == SelectedDeviceSymbol ) ) {
        TaskDialog.Show( "Arent", $"We can not find any ceedmodel \"{SelectedDeviceSymbol}\" match with this room \"{condition}\"。" ) ;
        return false ;
      }
      
      var ecoMode = _defaultSettingStorable.EcoSettingData.IsEcoMode.ToString() ;
      var level = _uiDocument.ActiveView.GenLevel ;
      var heightOfConnector = _document.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;
      var element = GenerateConnector( _uiDocument, point.X, point.Y, heightOfConnector, level, SelectedFloorPlanType ?? string.Empty, ecoMode ) ;
      var ceedCode = string.Join( ":", SelectedCeedCode, SelectedDeviceSymbol, SelectedModelNum ) ;
      if ( element is FamilyInstance familyInstance ) {
        familyInstance.SetProperty( ElectricalRoutingElementParameter.CeedCode, ceedCode ) ;
        familyInstance.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, defaultConstructionItem ) ;
        familyInstance.SetProperty( ElectricalRoutingElementParameter.SymbolContent, SelectedDeviceSymbol ?? string.Empty ) ;
        familyInstance.SetConnectorFamilyType( ConnectorFamilyType.Sensor ) ;
      }

      _postCommandExecutor?.CreateSymbolContentTagCommand( element, point ) ;

      if ( element.HasParameter( switch2DSymbol ) )
        element.SetProperty( switch2DSymbol, true ) ;

      if ( element.HasParameter( symbolMagnification ) )
        element.SetProperty( symbolMagnification, defaultSymbolMagnification ) ;

      if ( element.HasParameter( grade3 ) )
        element.SetProperty( grade3, _defaultSettingStorable.GradeSettingData.GradeMode == 3 ) ;

      return true ;
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
  }
}