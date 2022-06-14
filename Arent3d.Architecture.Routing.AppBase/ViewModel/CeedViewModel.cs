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
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using MoreLinq ;
using Button = System.Windows.Controls.Button ;
using CheckBox = System.Windows.Controls.CheckBox ;
using ComboBox = System.Windows.Controls.ComboBox ;
using DataGrid = System.Windows.Controls.DataGrid ;
using Label = System.Windows.Controls.Label ;
using ProgressBar = Arent3d.Revit.UI.Forms.ProgressBar ;
using MessageBox = System.Windows.MessageBox ;
using Visibility = System.Windows.Visibility ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class CeedViewModel : NotifyPropertyChanged
  {
    private const string NotExistConnectorFamilyInFolderModelWarningMessage = "excelで指定したモデルはmodelフォルダーに存在していないため、既存のモデルを使用します。" ;
    private readonly Document _document ;
    private readonly ExternalCommandData _commandData ;
    private List<CeedModel> _ceedModels ;
    private List<CeedModel> _usingCeedModel ;
    private List<CeedModel> _usedCeedModels ;
    private List<CeedModel> _previousCeedModels ;

    public DataGrid DtGrid ;

    public ObservableCollection<CeedModel> CeedModels { get ; set ; }

    private CeedStorable? CeedStorable { get ; set ; }

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

    public CeedViewModel( ExternalCommandData commandData )
    {
      _commandData = commandData ;
      _document = commandData.Application.ActiveUIDocument.Document ;
      CeedModels = new() ;
      DtGrid = new DataGrid() ;
      var oldCeedStorable = _document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( oldCeedStorable is null ) {
        _ceedModels = new() ;
        _usingCeedModel = new List<CeedModel>() ;
        _usedCeedModels = new List<CeedModel>() ;
        _previousCeedModels = new List<CeedModel>() ;
        _previewList = new ObservableCollection<CeedModel>() ;
      }
      else {
        CeedStorable = oldCeedStorable ;
        _ceedModels = oldCeedStorable.CeedModelData ;
        _usingCeedModel = oldCeedStorable.CeedModelUsedData ;
        _usedCeedModels = oldCeedStorable.CeedModelData ;
        _previousCeedModels = new List<CeedModel>( _usedCeedModels ) ;
        IsShowCeedModelNumber = oldCeedStorable.IsShowCeedModelNumber ;
        IsShowCondition = oldCeedStorable.IsShowCondition ;
        IsShowOnlyUsingCode = oldCeedStorable.IsShowOnlyUsingCode ;
        AddModelNumber( CeedModels ) ;
        if ( _usingCeedModel.Any() )
          IsExistUsingCode = true ;
        if ( ! _ceedModels.Any() ) IsShowDiff = true ;
        else IsShowDiff = oldCeedStorable.IsDiff ;
        _previewList = new ObservableCollection<CeedModel>() ;
      }

      _selectedCeedSetCode = string.Empty ;
      _selectedModelNumber = string.Empty ;
      _selectedDeviceSymbol = string.Empty ;
    }

    private void LoadData( CeedStorable ceedStorable )
    {
      CeedStorable = ceedStorable ;
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

    private void LoadData( List<CeedModel> ceedModels, CeedStorable? ceedStorable = null )
    {
      if ( ceedStorable != null )
        CeedStorable = ceedStorable ;
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

    private enum CategoryType
    {
      ModelNumberOrSetCodePreview,
      DeviceSymbolPreview
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
      using var progress = ProgressBar.ShowWithNewThread( _commandData.Application ) ;
      progress.Message = "Loading data..." ;
      var ceedStorable = _document.GetCeedStorable() ;
      {
        var ceedModelData = ExcelToModelConverter.GetAllCeedModelNumber( filePath, fileEquipmentSymbolsPath ) ;
        if ( ! ceedModelData.Any() ) return ;
        _previousCeedModels = new List<CeedModel>( CeedModels ) ;
        CheckChangeColor( ceedModelData ) ;
        ceedStorable.CeedModelData = ceedModelData ;
        ceedStorable.CeedModelUsedData = new List<CeedModel>() ;
        ceedStorable.IsShowOnlyUsingCode = false ;
        LoadData( ceedStorable ) ;
        checkBox.Visibility = Visibility.Hidden ;
        checkBox.IsChecked = false ;
        IsShowOnlyUsingCode = false ;
        //IsShowDiff = true ;

        try {
          using Transaction t = new( _document, "Save data" ) ;
          t.Start() ;
          using ( var progressData = progress?.Reserve( 0.5 ) ) {
            ceedStorable.Save() ;
            progressData?.ThrowIfCanceled() ;
          }

          using ( var progressData = progress?.Reserve( 0.9 ) ) {
            _document.MakeCertainAllConnectorFamilies() ;
            progressData?.ThrowIfCanceled() ;
          }

          t.Commit() ;
        }
        catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        }
      }
    }

    public void Save()
    {
      var ceedStorable = _document.GetCeedStorable() ;
      try {
        using Transaction t = new( _document, "Save data" ) ;
        t.Start() ;
        ceedStorable.IsShowCeedModelNumber = IsShowCeedModelNumber ;
        ceedStorable.IsShowCondition = IsShowCondition ;
        ceedStorable.IsShowOnlyUsingCode = IsShowOnlyUsingCode ;
        ceedStorable.IsDiff = IsShowDiff ;
        ceedStorable.Save() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }

    public void Search()
    {
      var data = IsShowOnlyUsingCode ? _usingCeedModel : _ceedModels ;
      CeedModels.Clear() ;
      PreviewList.Clear() ;
      data = string.IsNullOrEmpty( _selectedDeviceSymbol ) ? data : data.Where( c => c.GeneralDisplayDeviceSymbol.Contains( _selectedDeviceSymbol ) ).ToList() ;
      data = string.IsNullOrEmpty( _selectedCeedSetCode ) ? data : data.Where( c => c.CeedSetCode.Contains( _selectedCeedSetCode ) ).ToList() ;
      data = string.IsNullOrEmpty( _selectedModelNumber ) ? data : data.Where( c => c.ModelNumber.Contains( _selectedModelNumber ) ).ToList() ;
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

        usingCeedModel = usingCeedModel.Distinct().ToList() ;
        _usingCeedModel = usingCeedModel ;
        LoadData( _usingCeedModel, ceedStorable ) ;
        checkBox.Visibility = Visibility.Visible ;
        checkBox.IsChecked = true ;

        if ( ! _usingCeedModel.Any() ) return ;
        try {
          using Transaction t = new( _document, "Save data" ) ;
          t.Start() ;
          ceedStorable.CeedModelUsedData = _usingCeedModel ;
          ceedStorable.IsShowOnlyUsingCode = true ;
          ceedStorable.Save() ;
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
      LoadData( _usingCeedModel ) ;
    }

    public void UnShowOnlyUsingCode()
    {
      if ( ! _ceedModels.Any() ) return ;
      LoadData( _ceedModels ) ;
    }

    private void UpdateCeedStorableAfterReplaceFloorPlanSymbol( string connectorFamilyName )
    {
      var ceedStorable = _document.GetAllStorables<CeedStorable>().First() ;
      if ( ceedStorable == null ) return ;
      if ( _ceedModels.Any() ) {
        var ceedModel = _ceedModels.First( c => c.CeedSetCode == SelectedCeedModel!.CeedSetCode && c.GeneralDisplayDeviceSymbol == SelectedCeedModel.GeneralDisplayDeviceSymbol && c.ModelNumber == SelectedCeedModel.ModelNumber ) ;
        if ( ceedModel != null ) {
          ceedModel.FloorPlanType = connectorFamilyName ;
          ceedStorable.CeedModelData = _ceedModels ;
        }
      }

      if ( _usingCeedModel.Any() ) {
        var ceedModel = _usingCeedModel.FirstOrDefault( c => c.CeedSetCode == SelectedCeedModel!.CeedSetCode && c.GeneralDisplayDeviceSymbol == SelectedCeedModel.GeneralDisplayDeviceSymbol && c.ModelNumber == SelectedCeedModel.ModelNumber ) ;
        if ( ceedModel != null ) {
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

    private void UpdateDataGridAfterReplaceFloorPlanSymbol( DataGrid dataGrid, string floorPlanType )
    {
      if ( dataGrid.ItemsSource is not ObservableCollection<CeedModel> newCeedModels ) {
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
        document.LoadFamily( filePath, new CeedViewModel.FamilyOption( true ), out var family ) ;
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
      var ceedStorable = document.GetAllStorables<CeedStorable>().First() ;
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

      var newConnectorFamilyUploadFiles = connectorFamilyFileName.Where( f => ! ceedStorable.ConnectorFamilyUploadData.Contains( f ) ).ToList() ;
      ceedStorable.ConnectorFamilyUploadData.AddRange( newConnectorFamilyUploadFiles ) ;

      try {
        using Transaction t = new( document, "Save CeeD data" ) ;
        t.Start() ;
        ceedStorable.Save() ;
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
  }
}