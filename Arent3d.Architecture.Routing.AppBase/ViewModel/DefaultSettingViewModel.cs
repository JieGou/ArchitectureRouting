using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
using System.Text ;
using System.Text.RegularExpressions ;
using System.Windows ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.UI ;
using System ;
using System.Globalization ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Autodesk.Revit.DB ;
using CategoryModel = Arent3d.Architecture.Routing.AppBase.Model.CategoryModel ;
using DataGrid = System.Windows.Controls.DataGrid ;
using ImportDwgMappingModel = Arent3d.Architecture.Routing.AppBase.Model.ImportDwgMappingModel ;
using MessageBox = System.Windows.MessageBox ;
using ProgressBar = Arent3d.Revit.UI.Forms.ProgressBar ;


namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class DefaultSettingViewModel : NotifyPropertyChanged
  {
    private const string EcoModeKey = "Dialog.Electrical.SwitchEcoNormalModeDialog.EcoNormalMode.EcoMode" ;
    private const string EcoModeDefaultString = "Eco Mode" ;
    private const string NormalModeKey = "Dialog.Electrical.SwitchEcoNormalModeDialog.EcoNormalMode.NormalMode" ;
    private const string NormalModeDefaultString = "Normal Mode" ;

    private readonly UIDocument _uiDocument ;
    private const string CompressionFileName = "Csv File.zip" ;
    private List<WiresAndCablesModel> _allWiresAndCablesModels ;
    private List<ConduitsModel> _allConduitModels ;
    private List<HiroiSetMasterModel> _allHiroiSetMasterNormalModels ;
    private List<HiroiSetMasterModel> _allHiroiSetMasterEcoModels ;
    private List<HiroiSetCdMasterModel> _allHiroiSetCdMasterNormalModels ;
    private List<HiroiSetCdMasterModel> _allHiroiSetCdMasterEcoModels ;
    private List<HiroiMasterModel> _allHiroiMasterModels ;
    private List<CeedModel> _ceedModelData ;
    private List<RegistrationOfBoardDataModel> _registrationOfBoardDataModelData ;
    private List<CategoryModel> _categoriesWithCeedCode ;
    private List<CategoryModel> _categoriesWithoutCeedCode ;


    public enum EcoNormalMode
    {
      EcoMode,
      NormalMode
    }

    public IReadOnlyDictionary<EcoNormalMode, string> EcoNormalModes { get ; } = new Dictionary<EcoNormalMode, string> { [ EcoNormalMode.NormalMode ] = NormalModeKey.GetAppStringByKeyOrDefault( NormalModeDefaultString ), [ EcoNormalMode.EcoMode ] = EcoModeKey.GetAppStringByKeyOrDefault( EcoModeDefaultString ) } ;

    public IReadOnlyDictionary<string, string> GradeModeTypes { get ; } = new Dictionary<string, string>
    {
      [ "1" ] = "1",
      [ "2" ] = "2",
      [ "3" ] = "3",
      [ "4" ] = "4",
      [ "5" ] = "5",
      [ "6" ] = "6",
      [ "7" ] = "7"
    } ;

    public int SelectedEcoNormalModeIndex { get ; set ; }
    public EcoNormalMode SelectedEcoNormalMode => 0 == SelectedEcoNormalModeIndex ? EcoNormalMode.NormalMode : EcoNormalMode.EcoMode ;
    public int SelectedGradeModeIndex { get ; set ; }

    public int SelectedGradeMode => SelectedGradeModeIndex + 1 ;

    private ObservableCollection<CsvFileModel> _csvFileModels ;

    public ObservableCollection<CsvFileModel> CsvFileModels
    {
      get => _csvFileModels ;
      set
      {
        _csvFileModels = value ;
        OnPropertyChanged() ;
      }
    }

    private ObservableCollection<ImportDwgMappingModel> _importDwgMappingModels ;

    public ObservableCollection<ImportDwgMappingModel> ImportDwgMappingModels
    {
      get => _importDwgMappingModels ;
      set
      {
        _importDwgMappingModels = value ;
        OnPropertyChanged() ;
      }
    }

    private List<FileComboboxItemType> _fileItems ;

    public List<FileComboboxItemType> FileItems
    {
      get => _fileItems ;
      set
      {
        _fileItems = value ;
        OnPropertyChanged() ;
      }
    }

    private bool _isEnableChangeGrade ;

    public bool IsEnableChangeGrade
    {
      get => _isEnableChangeGrade ;
      set
      {
        _isEnableChangeGrade = value ;
        OnPropertyChanged() ;
      }
    }

    private int Scale { get ; }

    private readonly List<ImportDwgMappingModel> _oldImportDwgMappingModels ;
    private readonly List<FileComboboxItemType> _oldFileItems ;
    public List<string> DeletedFloorName { get ; set ; }

    private readonly Dictionary<string, string> _oldValueFloor ;

    public ICommand LoadDwgFilesCommand => new RelayCommand( LoadDwgFiles ) ;

    public ICommand LoadDefaultDbCommand => new RelayCommand( LoadDefaultDb ) ;

    public ICommand LoadAllDbCommand => new RelayCommand( LoadAllDb ) ;

    public ICommand LoadCeedCodeDataCommand => new RelayCommand( LoadCeedCodeData ) ;

    public ICommand LoadWiresAndCablesDataCommand => new RelayCommand( LoadWiresAndCablesData ) ;

    public ICommand LoadConduitsDataCommand => new RelayCommand( LoadConduitsData ) ;

    public ICommand LoadHiroiSetMasterNormalDataCommand => new RelayCommand( LoadHiroiSetMasterNormalData ) ;

    public ICommand LoadHiroiSetMasterEcoDataCommand => new RelayCommand( LoadHiroiSetMasterEcoData ) ;

    public ICommand LoadHiroiSetCdMasterNormalDataCommand => new RelayCommand( LoadHiroiSetCdMasterNormalData ) ;

    public ICommand LoadHiroiSetCdMasterEcoDataCommand => new RelayCommand( LoadHiroiSetCdMasterEcoData ) ;

    public ICommand LoadHiroiMasterDataCommand => new RelayCommand( LoadHiroiMasterData ) ;

    public ICommand MoveUpCommand => new RelayCommand<DataGrid>( MoveUp ) ;

    public ICommand MoveDownCommand => new RelayCommand<DataGrid>( MoveDown ) ;

    public ICommand AddModelBelowCurrentSelectedRowCommand => new RelayCommand<DataGrid>( AddModelBelowCurrentSelectedRow ) ;

    public DefaultSettingViewModel( UIDocument uiDocument, DefaultSettingStorable defaultSettingStorable, int scale )
    {
      SelectedEcoNormalModeIndex = defaultSettingStorable.EcoSettingData.IsEcoMode ? 1 : 0 ;
      SelectedGradeModeIndex = defaultSettingStorable.GradeSettingData.GradeMode - 1 ;
      _importDwgMappingModels = new ObservableCollection<ImportDwgMappingModel>() ;
      _fileItems = new List<FileComboboxItemType>() ;
      _oldImportDwgMappingModels = new List<ImportDwgMappingModel>() ;
      _oldFileItems = new List<FileComboboxItemType>() ;
      _oldValueFloor = new Dictionary<string, string>() ;
      DeletedFloorName = new List<string>() ;
      Scale = scale ;
      GetImportDwgMappingModelsAndFileItems( defaultSettingStorable ) ;

      _uiDocument = uiDocument ;
      _csvFileModels = new ObservableCollection<CsvFileModel>() ;
      _allWiresAndCablesModels = new List<WiresAndCablesModel>() ;
      _allConduitModels = new List<ConduitsModel>() ;
      _allHiroiSetMasterNormalModels = new List<HiroiSetMasterModel>() ;
      _allHiroiSetMasterEcoModels = new List<HiroiSetMasterModel>() ;
      _allHiroiSetCdMasterNormalModels = new List<HiroiSetCdMasterModel>() ;
      _allHiroiSetCdMasterEcoModels = new List<HiroiSetCdMasterModel>() ;
      _allHiroiMasterModels = new List<HiroiMasterModel>() ;
      _ceedModelData = new List<CeedModel>() ;
      _registrationOfBoardDataModelData = new List<RegistrationOfBoardDataModel>() ;
      _categoriesWithCeedCode = new List<CategoryModel>() ;
      _categoriesWithoutCeedCode = new List<CategoryModel>() ;
      GetCsvFiles( defaultSettingStorable ) ;
      InitOldValueFloor( defaultSettingStorable ) ;

      if ( ! ImportDwgMappingModels.Any() ) return ;
      
      ImportDwgMappingModels.Last().FloorHeightDisplay = "-" ;
      ImportDwgMappingModels.Last().IsEnabledFloorHeight = false ;
    }

    private void AddModelBelowCurrentSelectedRow( DataGrid dtGrid )
    {
      int index = dtGrid.SelectedIndex ;
      if ( ! ImportDwgMappingModels.Any() ) return ;
      if ( index < 0 ) return ;
      var importDwgMappingModelExist = ImportDwgMappingModels[ index ] ;
      var importDwgMappingModel = new ImportDwgMappingModel( string.Empty, string.Empty, importDwgMappingModelExist.FloorHeight, Scale ) ;

      _oldValueFloor.Add( importDwgMappingModel.Id, importDwgMappingModel.FloorHeightDisplay ) ;
      ImportDwgMappingModels.Insert( index + 1, importDwgMappingModel ) ;
      ImportDwgMappingModels = new ObservableCollection<ImportDwgMappingModel> ( ImportDwgMappingModels ) ;
    }


    private void MoveUp( DataGrid dtGrid )
    {
      var index = dtGrid.SelectedIndex ;
      if ( index < 1 ) return ;

      var selectedIndex = index - 1 ;

      if ( ImportDwgMappingModels[ selectedIndex ].FloorHeight == ImportDwgMappingModels[ selectedIndex + 1 ].FloorHeight )
        Swap( ImportDwgMappingModels, selectedIndex, selectedIndex + 1 ) ;
      else {
        var indexB = selectedIndex + 1 ;
        while ( selectedIndex > 0 && ImportDwgMappingModels[ selectedIndex ].FloorHeight == ImportDwgMappingModels[ selectedIndex - 1 ].FloorHeight ) {
          selectedIndex-- ;
        }

        for ( var i = indexB ; i > selectedIndex ; i-- ) {
          Swap( ImportDwgMappingModels, i, i - 1 ) ;
        }
        MoveFloorHeight( selectedIndex, indexB, false ) ;
      }
      dtGrid.SelectedIndex = selectedIndex ;
    }

    private void MoveDown( DataGrid dtGrid )
    {
      var index = dtGrid.SelectedIndex ;
      if ( index == ImportDwgMappingModels.Count - 2 ) return ;

      var selectedIndex = index + 1 ;

      if ( ImportDwgMappingModels[ selectedIndex ].FloorHeight == ImportDwgMappingModels[ selectedIndex - 1 ].FloorHeight )
        Swap( ImportDwgMappingModels, selectedIndex, selectedIndex - 1 ) ;
      else {
        var indexA = selectedIndex - 1 ;
        while ( selectedIndex < ImportDwgMappingModels.Count - 2 && ImportDwgMappingModels[ selectedIndex ].FloorHeight == ImportDwgMappingModels[ selectedIndex + 1 ].FloorHeight ) {
          selectedIndex++ ;
        }
        for ( var i = indexA ; i < selectedIndex ; i++ ) {
          Swap( ImportDwgMappingModels, i, i + 1 ) ;
        };
        MoveFloorHeight( indexA, selectedIndex, true ) ;
      }
      dtGrid.SelectedIndex = selectedIndex ;
    }

    private static void Swap( ObservableCollection<ImportDwgMappingModel> list, int indexA, int indexB )
    {
      ( list[ indexA ], list[ indexB ] ) = ( list[ indexB ], list[ indexA ] ) ;
    }

    private void GetCsvFiles( DefaultSettingStorable defaultSettingStorable )
    {
      if ( defaultSettingStorable.CsvFileData.Any() ) {
        CsvFileModels = new ObservableCollection<CsvFileModel>( defaultSettingStorable.CsvFileData ) ;
      }
    }

    private void GetImportDwgMappingModelsAndFileItems( DefaultSettingStorable defaultSettingStorable )
    {
      foreach ( var item in defaultSettingStorable.ImportDwgMappingData ) {
        var importDwgMappingModel = new ImportDwgMappingModel( item, true ) ;
        _oldImportDwgMappingModels.Add( importDwgMappingModel ) ;
        ImportDwgMappingModels.Add( importDwgMappingModel ) ;

        if ( item.FullFilePath == string.Empty ) continue ;
        var fileItem = new FileComboboxItemType( item.FullFilePath ) ;
        _oldFileItems.Add( fileItem ) ;
        FileItems.Add( fileItem ) ;
      }
    }

    public ICommand ApplyCommand
    {
      get
      {
        return new RelayCommand<Window>( wd => null != wd, wd =>
        {
          //To do check scale validation
          wd.DialogResult = true ;
          wd.Close() ;
          IsEnableChangeGrade = false ;
        } ) ;
      }
    }

    public void DeleteImportDwgMappingItem( ImportDwgMappingModel selectedItem )
    {
      DeleteFloorHeight( selectedItem ) ;
      ImportDwgMappingModels.Remove( selectedItem ) ;
      if ( ImportDwgMappingModels.Any() ) {
        ImportDwgMappingModels.Last().FloorHeightDisplay = "-" ;
        ImportDwgMappingModels.Last().IsEnabledFloorHeight = false ;
      }

      var itemToRemove = _oldImportDwgMappingModels.SingleOrDefault( r => r.Id == selectedItem.Id ) ;
      if ( itemToRemove != null )
        _oldImportDwgMappingModels.Remove( itemToRemove ) ;
      if ( ! DeletedFloorName.Contains( selectedItem.FloorName ) ) {
        DeletedFloorName.Add( selectedItem.FloorName ) ;
      }
    }

    public void LoadDwgFile( ImportDwgMappingModel selectedItem )
    {
      OpenFileDialog openFileDialog = new() { Filter = @"DWG files (*.dwg )|*.dwg", Multiselect = false } ;
      if ( openFileDialog.ShowDialog() != DialogResult.OK ) return ;
      var fileName = openFileDialog.FileName ;
      FileItems.Add( new FileComboboxItemType( fileName ) ) ;
      var importDwgMappingModel = ImportDwgMappingModels.FirstOrDefault( d => d == selectedItem ) ;
      if ( importDwgMappingModel == null ) return ;
      importDwgMappingModel.FullFilePath = fileName ;
      importDwgMappingModel.FileName = Path.GetFileName( fileName ) ;
      ImportDwgMappingModels = new ObservableCollection<ImportDwgMappingModel> ( ImportDwgMappingModels ) ;
    }

    private void LoadDwgFiles()
    {
      OpenFileDialog openFileDialog = new() { Filter = @"DWG files (*.dwg )|*.dwg", Multiselect = true } ;
      if ( openFileDialog.ShowDialog() != DialogResult.OK ) return ;
      ImportDwgMappingModels = new ObservableCollection<ImportDwgMappingModel>( _oldImportDwgMappingModels ) ;
      FileItems = _oldFileItems ;
      const string basementRegEx = @"B\d+階" ;
      const string floorRegEx = @"\d+階" ;
      const string rooftopRegEx = @"PH\d+階" ;
      foreach ( var fileName in openFileDialog.FileNames ) {
        FileItems.Add( new FileComboboxItemType( fileName ) ) ;
        ImportDwgMappingModel? importDwgMappingModel = null ;

        var floorName = Regex.Match( fileName, basementRegEx ).Value.Replace( "階", "" ) ;
        if ( string.IsNullOrEmpty( floorName ) )
          floorName = Regex.Match( fileName, rooftopRegEx ).Value.Replace( "階", "" ) ;
        if ( string.IsNullOrEmpty( floorName ) )
          floorName = Regex.Match( fileName, floorRegEx ).Value.Replace( "階", "" ) ;

        if ( ! string.IsNullOrEmpty( floorName ) && int.TryParse( floorName.Replace( "B", "" ).Replace( "PH", "" ), out _ ) ) {
          importDwgMappingModel = new ImportDwgMappingModel( fileName, $"{floorName}F", 0 ) ;
          ImportDwgMappingModels.Add( importDwgMappingModel ) ;
        }

        if ( importDwgMappingModel != null ) _oldValueFloor.Add( importDwgMappingModel.Id, importDwgMappingModel.FloorHeightDisplay ) ;
      }

      ChangeNameIfDuplicate() ;
      UpdateDefaultFloorHeight() ;
      ImportDwgMappingModels = new ObservableCollection<ImportDwgMappingModel>( CalculateFloorHeight(ImportDwgMappingModels) ) ;
      if ( ImportDwgMappingModels.Any() ) {
        ImportDwgMappingModels.Last().FloorHeightDisplay = "-" ;
        ImportDwgMappingModels.Last().IsEnabledFloorHeight = false ;
      }
      IsEnableChangeGrade = true ;
    }

    private void ChangeNameIfDuplicate()
    {
      var nameFloors = ImportDwgMappingModels.Select( x => x.FloorName ).ToList() ;
      var newNameFloors = new List<string>() ;
      for ( var i = 0 ; i < nameFloors.Count() ; i++ ) {
        var name = nameFloors[ i ] ;
        var count = 0 ;
        while ( newNameFloors.Contains( name ) ) {
          name = $"{nameFloors[ i ]}({++count})" ;
        }

        newNameFloors.Add( name ) ;
      }

      for ( var i = 0 ; i < ImportDwgMappingModels.Count() ; i++ ) {
        ImportDwgMappingModels[ i ].FloorName = newNameFloors[ i ] ;
      }
    }

    private void UpdateDefaultFloorHeight()
    {
      const int floorHeightDistance = 4000 ;
      const int basementHeightDistance = 1000 ;
      const int rooftopHeightDistance = 6500 ;
      const string basementRegEx = @"B\d+" ;
      const string floorRegEx = @"\d+" ;
      const string rooftopRegEx = @"PH\d+" ;

      // Basement
      var newBasementImportDwgMappingModels = ImportDwgMappingModels.Where( x => x.FloorHeight == 0 && x.IsEnabled && Regex.IsMatch( x.FloorName, basementRegEx ) ).OrderBy( x => Convert.ToInt32( Regex.Match( x.FloorName, @"\d+" ).Value ) ).ToList() ;
      var oldMinFloorHeightImportDwgMappingModel = _oldImportDwgMappingModels.MinBy( x => x.FloorHeight ) ;
      var oldMinHeight = oldMinFloorHeightImportDwgMappingModel?.FloorHeight ;
      for ( var i = 0 ; i < newBasementImportDwgMappingModels.Count ; i++ ) {
        newBasementImportDwgMappingModels[ i ].FloorHeight = oldMinHeight != null ? oldMinHeight.Value - basementHeightDistance * ( i + 1 ) : - basementHeightDistance * i ;
      }

      // Floor
      var newFloorImportDwgMappingModels = ImportDwgMappingModels.Where( x => x.FloorHeight == 0 && x.IsEnabled && ! Regex.IsMatch( x.FloorName, basementRegEx ) && ! Regex.IsMatch( x.FloorName, rooftopRegEx ) && Regex.IsMatch( x.FloorName, floorRegEx ) ).OrderBy( x => Convert.ToInt32( Regex.Match( x.FloorName, @"\d+" ).Value ) ).ToList() ;
      var maxFloorHeight = ImportDwgMappingModels.Max( x => x.FloorHeight ) ;
      for ( var i = 0 ; i < newFloorImportDwgMappingModels.Count ; i++ ) {
        newFloorImportDwgMappingModels[ i ].FloorHeight = maxFloorHeight + floorHeightDistance * ( i + 1 ) ;
      }

      // Rooftop
      var newRooftopImportDwgMappingModels = ImportDwgMappingModels.Where( x => x.FloorHeight == 0 && x.IsEnabled && Regex.IsMatch( x.FloorName, rooftopRegEx ) ).OrderBy( x => Convert.ToInt32( Regex.Match( x.FloorName, @"\d+" ).Value ) ).ToList() ;
      maxFloorHeight = ImportDwgMappingModels.Max( x => x.FloorHeight ) ;
      for ( var i = 0 ; i < newRooftopImportDwgMappingModels.Count ; i++ ) {
        newRooftopImportDwgMappingModels[ i ].FloorHeight = maxFloorHeight + rooftopHeightDistance * ( i + 1 ) ;
      }

      var minFloorHeightImportDwgMappingModel = ImportDwgMappingModels.MinBy( x => x.FloorHeight ) ;
      var minHeight = minFloorHeightImportDwgMappingModel?.FloorHeight ?? 0 ;
      if ( minHeight < 0 ) {
        foreach ( var importDwgMappingModel in ImportDwgMappingModels ) {
          importDwgMappingModel.FloorHeight -= minHeight ;
        }
      }

      ImportDwgMappingModels = new ObservableCollection<ImportDwgMappingModel>( ImportDwgMappingModels.OrderBy( x => x.FloorHeight ).ToList() ) ;
    }

    // Button load default db
    public void LoadDefaultDb()
    {
      var folderPath = GetFolderCsvPath() ;
      if ( null == folderPath )
        return ;
      LoadData( folderPath ) ;
      Directory.Delete( folderPath, true ) ;
      SaveData() ;
    }

    public void SaveData()
    {
      var document = _uiDocument.Document ;
      using var progress = ProgressBar.ShowWithNewThread( _uiDocument.Application ) ;
      progress.Message = "Saving data..." ;
      using ( var progressData = progress.Reserve( 0.3 ) ) {
        var csvStorable = document.GetCsvStorable() ;
        {
          if ( _allWiresAndCablesModels.Any() )
            csvStorable.WiresAndCablesModelData = _allWiresAndCablesModels ;
          if ( _allConduitModels.Any() )
            csvStorable.ConduitsModelData = _allConduitModels ;
          if ( _allHiroiSetMasterNormalModels.Any() )
            csvStorable.HiroiSetMasterNormalModelData = _allHiroiSetMasterNormalModels ;
          if ( _allHiroiSetMasterEcoModels.Any() )
            csvStorable.HiroiSetMasterEcoModelData = _allHiroiSetMasterEcoModels ;
          if ( _allHiroiSetCdMasterNormalModels.Any() )
            csvStorable.HiroiSetCdMasterNormalModelData = _allHiroiSetCdMasterNormalModels ;
          if ( _allHiroiSetCdMasterEcoModels.Any() )
            csvStorable.HiroiSetCdMasterEcoModelData = _allHiroiSetCdMasterEcoModels ;
          if ( _allHiroiMasterModels.Any() )
            csvStorable.HiroiMasterModelData = _allHiroiMasterModels ;

          try {
            using var t = new Transaction( document, "Save data" ) ;
            t.Start() ;
            csvStorable.Save() ;
            t.Commit() ;
          }
          catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
            MessageBox.Show( "Save CSV Files Failed.", "Error Message" ) ;
            //DialogResult = false ;
          }
        }
        progressData.ThrowIfCanceled() ;
      }

      using ( var progressData = progress.Reserve( 0.6 ) ) {
        var ceedStorable = document.GetCeedStorable() ;
        var level = document.ActiveView?.GenLevel ?? new FilteredElementCollector( document ).OfClass( typeof( Level ) ).OfType<Level>().OrderBy( x => x.Elevation ).First() ;
        var storageService = new StorageService<Level, CeedUserModel>( level ) ;
        if ( _ceedModelData.Any() ) {
          DrawCanvasManager.SetBase64FloorPlanImages( document, _ceedModelData ) ;
          var previousCeedModels = ceedStorable.CeedModelData ;
          CeedViewModel.CheckChangeColor( _ceedModelData, previousCeedModels ) ;
          ceedStorable.CeedModelData = _ceedModelData ;
          ceedStorable.CeedModelUsedData = new List<CeedModel>() ;
          ceedStorable.CategoriesWithCeedCode = CategoryModel.ConvertCategoryModel( _categoriesWithCeedCode ) ;
          ceedStorable.CategoriesWithoutCeedCode = CategoryModel.ConvertCategoryModel( _categoriesWithoutCeedCode ) ;

          storageService.Data.IsShowOnlyUsingCode = false ;
          storageService.Data.IsDiff = true ;
          storageService.Data.IsExistUsingCode = false ;
        }

        var registrationOfBoardDataStorable = document.GetRegistrationOfBoardDataStorable() ;
        if ( _registrationOfBoardDataModelData.Any() ) {
          registrationOfBoardDataStorable.RegistrationOfBoardData = _registrationOfBoardDataModelData ;
        }

        if ( _ceedModelData.Any() || _registrationOfBoardDataModelData.Any() ) {
          try {
            using var t = new Transaction( document, "Save Ceed and Board data" ) ;
            t.Start() ;
            if ( _ceedModelData.Any() ) {
              ceedStorable.Save() ;
              storageService.SaveChange() ;
              document.MakeCertainAllConnectorFamilies() ;
            }

            if ( _registrationOfBoardDataModelData.Any() ) {
              registrationOfBoardDataStorable.Save() ;
            }

            t.Commit() ;
          }
          catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
          }
        }

        progressData.ThrowIfCanceled() ;
      }

      using ( var progressData = progress.Reserve( 0.9 ) ) {
        var defaultSettingStorable = document.GetDefaultSettingStorable() ;
        {
          if ( _csvFileModels.Any() ) {
            defaultSettingStorable.CsvFileData = new List<CsvFileModel>( _csvFileModels ) ;
            try {
              using var t = new Transaction( document, "Save Csv File data" ) ;
              t.Start() ;
              defaultSettingStorable.Save() ;
              t.Commit() ;
            }
            catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
            }
          }
        }
        progressData.ThrowIfCanceled() ;
      }
    }

    private static string? GetFolderCsvPath() => AssetManager.GetFolderCompressionFilePath( AssetManager.AssetPath, CompressionFileName ) ;

    private static string OpenFileDialog()
    {
      var openFileDialog = new OpenFileDialog { Filter = @"Csv files (*.csv)|*.csv", Multiselect = false } ;
      var filePath = string.Empty ;
      if ( openFileDialog.ShowDialog() == DialogResult.OK ) {
        filePath = openFileDialog.FileName ;
      }

      return filePath ;
    }

    private void LoadCeedCodeData()
    {
      MessageBox.Show( "Please select 【CeeD】セットコード一覧表 file.", "Message" ) ;
      OpenFileDialog openFileDialog = new() { Filter = @"Csv files (*.xlsx; *.xls)|*.xlsx;*.xls", Multiselect = false } ;
      var filePath = string.Empty ;
      var fileEquipmentSymbolsPath = string.Empty ;
      if ( openFileDialog.ShowDialog() == DialogResult.OK ) {
        filePath = openFileDialog.FileName ;
        MessageBox.Show( "Please select 機器記号一覧表 file.", "Message" ) ;
        OpenFileDialog openFileEquipmentSymbolsDialog = new() { Filter = @"Csv files (*.xlsx; *.xls)|*.xlsx;*.xls", Multiselect = false } ;
        if ( openFileEquipmentSymbolsDialog.ShowDialog() == DialogResult.OK ) {
          fileEquipmentSymbolsPath = openFileEquipmentSymbolsDialog.FileName ;
        }
      }

      if ( string.IsNullOrEmpty( filePath ) || string.IsNullOrEmpty( fileEquipmentSymbolsPath ) ) return ;
      var (ceedModelData, categoriesWithCeedCode, categoriesWithoutCeedCode) = ExcelToModelConverter.GetAllCeedModelNumber( filePath, fileEquipmentSymbolsPath ) ;
      if ( ! ceedModelData.Any() ) {
        MessageBox.Show( "Load file failed.", "Error Message" ) ;
        return ;
      }

      _ceedModelData = ceedModelData ;
      _categoriesWithCeedCode = categoriesWithCeedCode ;
      _categoriesWithoutCeedCode = categoriesWithoutCeedCode ;
      MessageBox.Show( "Load file successful.", "Result Message" ) ;

      const string ceedCodeFileName = " 【CeeD】セットコード一覧表" ;
      const string equipmentSymbolsFileName = " 機器記号一覧表" ;
      if ( CsvFileModels.FirstOrDefault( c => c.CsvName == ceedCodeFileName ) is { } ceedCodeFileModel ) {
        ceedCodeFileModel.CsvFilePath = RenamePathToRelative( filePath ) ;
        CsvFileModels = new ObservableCollection<CsvFileModel>( CsvFileModels ) ;
      }
      else
        CsvFileModels.Add( new CsvFileModel( ceedCodeFileName, RenamePathToRelative( filePath ), Path.GetFileName( filePath ) ) ) ;

      if ( CsvFileModels.FirstOrDefault( c => c.CsvName == equipmentSymbolsFileName ) is { } equipmentSymbolsFileNameModel ) {
        equipmentSymbolsFileNameModel.CsvFilePath = RenamePathToRelative( fileEquipmentSymbolsPath ) ;
        CsvFileModels = new ObservableCollection<CsvFileModel>( CsvFileModels ) ;
      }
      else
        CsvFileModels.Add( new CsvFileModel( equipmentSymbolsFileName, RenamePathToRelative( fileEquipmentSymbolsPath ), Path.GetFileName( fileEquipmentSymbolsPath ) ) ) ;
    }

    private void LoadWiresAndCablesData()
    {
      _allWiresAndCablesModels = new List<WiresAndCablesModel>() ;
      var filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      var isGetDataWithoutError = GetData( filePath, 2, ModelName.WiresAndCables, true ) ;
      if ( ! isGetDataWithoutError ) return ;

      const string csvName = " 電線・ケーブル一覧" ;
      if ( CsvFileModels.FirstOrDefault( c => c.CsvName == csvName ) is { } csvFileModel ) {
        csvFileModel.CsvFilePath = RenamePathToRelative( filePath ) ;
        CsvFileModels = new ObservableCollection<CsvFileModel>( CsvFileModels ) ;
      }
      else
        CsvFileModels.Add( new CsvFileModel( csvName, RenamePathToRelative( filePath ), "電線・ケーブル一覧.csv" ) ) ;
    }

    private void LoadConduitsData()
    {
      _allConduitModels = new List<ConduitsModel>() ;
      var filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      var isGetDataWithoutError = GetData( filePath, 2, ModelName.Conduits, true ) ;
      if ( ! isGetDataWithoutError ) return ;

      const string csvName = " 電線管一覧" ;
      if ( CsvFileModels.FirstOrDefault( c => c.CsvName == csvName ) is { } csvFileModel ) {
        csvFileModel.CsvFilePath = RenamePathToRelative( filePath ) ;
        CsvFileModels = new ObservableCollection<CsvFileModel>( CsvFileModels ) ;
      }
      else
        CsvFileModels.Add( new CsvFileModel( csvName, RenamePathToRelative( filePath ), "電線管一覧.csv" ) ) ;
    }

    private void LoadHiroiSetMasterNormalData()
    {
      _allHiroiSetMasterNormalModels = new List<HiroiSetMasterModel>() ;
      var filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      var isGetDataWithoutError = GetData( filePath, 0, ModelName.HiroiSetMasterNormal, true ) ;
      if ( ! isGetDataWithoutError ) return ;

      const string csvName = " Hiroi Set Master Normal" ;
      if ( CsvFileModels.FirstOrDefault( c => c.CsvName == csvName ) is { } csvFileModel ) {
        csvFileModel.CsvFilePath = RenamePathToRelative( filePath ) ;
        CsvFileModels = new ObservableCollection<CsvFileModel>( CsvFileModels ) ;
      }
      else
        CsvFileModels.Add( new CsvFileModel( csvName, RenamePathToRelative( filePath ), "hiroisetmaster_normal.csv" ) ) ;
    }

    private void LoadHiroiSetMasterEcoData()
    {
      _allHiroiSetMasterEcoModels = new List<HiroiSetMasterModel>() ;
      var filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      var isGetDataWithoutError = GetData( filePath, 0, ModelName.HiroiSetMasterEco, true ) ;
      if ( ! isGetDataWithoutError ) return ;

      const string csvName = " Hiroi Set Master ECO" ;
      if ( CsvFileModels.FirstOrDefault( c => c.CsvName == csvName ) is { } csvFileModel ) {
        csvFileModel.CsvFilePath = RenamePathToRelative( filePath ) ;
        CsvFileModels = new ObservableCollection<CsvFileModel>( CsvFileModels ) ;
      }
      else
        CsvFileModels.Add( new CsvFileModel( csvName, RenamePathToRelative( filePath ), "hiroisetcdmaster_eco.csv" ) ) ;
    }

    private void LoadHiroiSetCdMasterNormalData()
    {
      _allHiroiSetCdMasterNormalModels = new List<HiroiSetCdMasterModel>() ;
      var filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      var isGetDataWithoutError = GetData( filePath, 0, ModelName.HiroiSetCdMasterNormal, true ) ;
      if ( ! isGetDataWithoutError ) return ;

      const string csvName = " Hiroi Set CD Master Normal" ;
      if ( CsvFileModels.FirstOrDefault( c => c.CsvName == csvName ) is { } csvFileModel ) {
        csvFileModel.CsvFilePath = RenamePathToRelative( filePath ) ;
        CsvFileModels = new ObservableCollection<CsvFileModel>( CsvFileModels ) ;
      }
      else
        CsvFileModels.Add( new CsvFileModel( csvName, RenamePathToRelative( filePath ), "hiroisetcdmaster_normal.csv" ) ) ;
    }

    private void LoadHiroiSetCdMasterEcoData()
    {
      _allHiroiSetCdMasterEcoModels = new List<HiroiSetCdMasterModel>() ;
      var filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      var isGetDataWithoutError = GetData( filePath, 0, ModelName.HiroiSetCdMasterEco, true ) ;
      if ( ! isGetDataWithoutError ) return ;

      const string csvName = " Hiroi Set CD Master ECO" ;
      if ( CsvFileModels.FirstOrDefault( c => c.CsvName == csvName ) is { } csvFileModel ) {
        csvFileModel.CsvFilePath = RenamePathToRelative( filePath ) ;
        CsvFileModels = new ObservableCollection<CsvFileModel>( CsvFileModels ) ;
      }
      else
        CsvFileModels.Add( new CsvFileModel( csvName, RenamePathToRelative( filePath ), "hiroisetcdmaster_eco.csv" ) ) ;
    }

    private void LoadHiroiMasterData()
    {
      _allHiroiMasterModels = new List<HiroiMasterModel>() ;
      var filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      var isGetDataWithoutError = GetData( filePath, 0, ModelName.HiroiMaster, true ) ;
      if ( ! isGetDataWithoutError ) return ;

      const string csvName = " Hiroi Master" ;
      if ( CsvFileModels.FirstOrDefault( c => c.CsvName == csvName ) is { } csvFileModel ) {
        csvFileModel.CsvFilePath = RenamePathToRelative( filePath ) ;
        CsvFileModels = new ObservableCollection<CsvFileModel>( CsvFileModels ) ;
      }
      else
        CsvFileModels.Add( new CsvFileModel( csvName, RenamePathToRelative( filePath ), "hiroimaster.csv" ) ) ;
    }

    private void LoadAllDb()
    {
      var dialog = new FolderBrowserDialog() ;
      dialog.ShowNewFolderButton = false ;
      dialog.ShowDialog() ;
      if ( string.IsNullOrEmpty( dialog.SelectedPath ) )
        return ;
      LoadData( dialog.SelectedPath ) ;
    }

    private void LoadData( string folderPath )
    {
      var listCsvFileModel = new ObservableCollection<CsvFileModel>() ;
      var fileNames = new[] { "hiroimaster.csv", "hiroisetcdmaster_normal.csv", "hiroisetcdmaster_eco.csv", "hiroisetmaster_eco.csv", "hiroisetmaster_normal.csv", "電線管一覧.csv", "電線・ケーブル一覧.csv" } ;
      var isLoadedCeedFile = false ;
      var ceedCodeFile = "【CeeD】セットコード一覧表" ;
      const string equipmentSymbolsFile = "機器記号一覧表" ;
      const string boardFile = "盤間配線確認表" ;
      foreach ( var fileName in fileNames ) {
        var path = Path.Combine( folderPath, fileName ) ;
        if ( File.Exists( path ) ) {
          bool isGetDataWithoutError ;
          switch ( fileName ) {
            case "hiroimaster.csv" :
              _allHiroiMasterModels = new List<HiroiMasterModel>() ;
              isGetDataWithoutError = GetData( path, 0, ModelName.HiroiMaster, false ) ;
              if ( isGetDataWithoutError ) {
                var csvName = " Hiroi Master" ;
                listCsvFileModel.Add( new CsvFileModel( csvName, RenamePathToRelative( path ), fileName ) ) ;
              }

              break ;
            case "hiroisetcdmaster_normal.csv" :
              _allHiroiSetCdMasterNormalModels = new List<HiroiSetCdMasterModel>() ;
              isGetDataWithoutError = GetData( path, 0, ModelName.HiroiSetCdMasterNormal, false ) ;
              if ( isGetDataWithoutError ) {
                var csvName = " Hiroi Set CD Master Normal" ;
                listCsvFileModel.Add( new CsvFileModel( csvName, RenamePathToRelative( path ), fileName ) ) ;
              }

              break ;
            case "hiroisetcdmaster_eco.csv" :
              _allHiroiSetCdMasterEcoModels = new List<HiroiSetCdMasterModel>() ;
              isGetDataWithoutError = GetData( path, 0, ModelName.HiroiSetCdMasterEco, false ) ;
              if ( isGetDataWithoutError ) {
                var csvName = " Hiroi Set CD Master ECO" ;
                listCsvFileModel.Add( new CsvFileModel( csvName, RenamePathToRelative( path ), fileName ) ) ;
              }

              break ;
            case "hiroisetmaster_eco.csv" :
              _allHiroiSetMasterEcoModels = new List<HiroiSetMasterModel>() ;
              isGetDataWithoutError = GetData( path, 0, ModelName.HiroiSetMasterEco, false ) ;
              if ( isGetDataWithoutError ) {
                var csvName = " Hiroi Set Master ECO" ;
                listCsvFileModel.Add( new CsvFileModel( csvName, RenamePathToRelative( path ), fileName ) ) ;
              }

              break ;
            case "hiroisetmaster_normal.csv" :
              _allHiroiSetMasterNormalModels = new List<HiroiSetMasterModel>() ;
              isGetDataWithoutError = GetData( path, 0, ModelName.HiroiSetMasterNormal, false ) ;
              if ( isGetDataWithoutError ) {
                var csvName = " Hiroi Set Master Normal" ;
                listCsvFileModel.Add( new CsvFileModel( csvName, RenamePathToRelative( path ), fileName ) ) ;
              }

              break ;
            case "電線管一覧.csv" :
              _allConduitModels = new List<ConduitsModel>() ;
              isGetDataWithoutError = GetData( path, 2, ModelName.Conduits, false ) ;
              if ( isGetDataWithoutError ) {
                var csvName = " 電線管一覧" ;
                listCsvFileModel.Add( new CsvFileModel( csvName, RenamePathToRelative( path ), fileName ) ) ;
              }

              break ;
            case "電線・ケーブル一覧.csv" :
              _allWiresAndCablesModels = new List<WiresAndCablesModel>() ;
              isGetDataWithoutError = GetData( path, 2, ModelName.WiresAndCables, false ) ;
              if ( isGetDataWithoutError ) {
                var csvName = " 電線・ケーブル一覧" ;
                listCsvFileModel.Add( new CsvFileModel( csvName, RenamePathToRelative( path ), fileName ) ) ;
              }

              break ;
          }
        }
      }

      // load 【CeeD】セットコード一覧表 and 機器記号一覧表 files
      var ceedCodeXlsxFilePath = Path.Combine( folderPath, ceedCodeFile + ".xlsx" ) ;
      var ceedCodeXlsFilePath = Path.Combine( folderPath, ceedCodeFile + ".xls" ) ;
      var equipmentSymbolsXlsxFilePath = Path.Combine( folderPath, equipmentSymbolsFile + ".xlsx" ) ;
      var equipmentSymbolsXlsFilePath = Path.Combine( folderPath, equipmentSymbolsFile + ".xls" ) ;
      if ( File.Exists( ceedCodeXlsxFilePath ) ) {
        isLoadedCeedFile = LoadCeedCodeFile( equipmentSymbolsFile, ceedCodeXlsxFilePath, equipmentSymbolsXlsxFilePath, equipmentSymbolsXlsFilePath, listCsvFileModel ) ;
        listCsvFileModel.Add( new CsvFileModel( ceedCodeFile, RenamePathToRelative( ceedCodeXlsxFilePath ), ceedCodeFile + ".xlsx" ) ) ;
      }

      if ( File.Exists( ceedCodeXlsFilePath ) && ! isLoadedCeedFile ) {
        LoadCeedCodeFile( equipmentSymbolsFile, ceedCodeXlsFilePath, equipmentSymbolsXlsxFilePath, equipmentSymbolsXlsFilePath, listCsvFileModel ) ;
        listCsvFileModel.Add( new CsvFileModel( ceedCodeFile, RenamePathToRelative( ceedCodeXlsFilePath ), ceedCodeFile + ".xls" ) ) ;
      }

      // load 盤間配線確認表 file
      var boardXlsxFilePath = Path.Combine( folderPath, boardFile + ".xlsx" ) ;
      var boardXlsFilePath = Path.Combine( folderPath, boardFile + ".xls" ) ;
      if ( File.Exists( boardXlsxFilePath ) || File.Exists( boardXlsFilePath ) ) {
        var filePath = File.Exists( boardXlsxFilePath ) ? boardXlsxFilePath : boardXlsFilePath ;
        _registrationOfBoardDataModelData = ExcelToModelConverter.GetAllRegistrationOfBoardDataModel( filePath ) ;
        if ( _registrationOfBoardDataModelData.Any() ) {
          listCsvFileModel.Add( new CsvFileModel( boardFile, RenamePathToRelative( filePath ), Path.GetFileName( filePath ) ) ) ;
        }
      }

      CsvFileModels = listCsvFileModel ;
    }

    private bool LoadCeedCodeFile( string equipmentSymbolsFile, string ceedCodeFilePath, string equipmentSymbolsXlsxFilePath, string equipmentSymbolsXlsFilePath, ObservableCollection<CsvFileModel> listCsvFile )
    {
      if ( File.Exists( equipmentSymbolsXlsxFilePath ) ) {
        ( _ceedModelData, _categoriesWithCeedCode, _categoriesWithoutCeedCode ) = ExcelToModelConverter.GetAllCeedModelNumber( ceedCodeFilePath, equipmentSymbolsXlsxFilePath ) ;
        if ( _ceedModelData.Any() ) {
          listCsvFile.Add( new CsvFileModel( equipmentSymbolsFile, RenamePathToRelative( equipmentSymbolsXlsxFilePath ), equipmentSymbolsFile + ".xlsx" ) ) ;
          return true ;
        }
      }

      if ( File.Exists( equipmentSymbolsXlsFilePath ) ) {
        ( _ceedModelData, _categoriesWithCeedCode, _categoriesWithoutCeedCode ) = ExcelToModelConverter.GetAllCeedModelNumber( ceedCodeFilePath, equipmentSymbolsXlsFilePath ) ;
        if ( _ceedModelData.Any() ) {
          listCsvFile.Add( new CsvFileModel( equipmentSymbolsFile, RenamePathToRelative( equipmentSymbolsXlsxFilePath ), equipmentSymbolsFile + ".xls" ) ) ;
          return true ;
        }
      }

      ( _ceedModelData, _categoriesWithCeedCode, _categoriesWithoutCeedCode ) = ExcelToModelConverter.GetAllCeedModelNumber( ceedCodeFilePath, string.Empty ) ;
      return _ceedModelData.Any() ;
    }

    private bool GetData( string path, int startLine, ModelName modelName, bool showMessageFlag )
    {
      var checkFile = true ;
      const int wacColCount = 10 ;
      const int conduitColCount = 5 ;
      const int hsmColCount = 27 ;
      const int hsCdmColCount = 4 ;
      const int hmColCount = 12 ;
      try {
        using var reader = new StreamReader( path, Encoding.GetEncoding( "shift-jis" ), true ) ;
        var startRow = 0 ;
        while ( ! reader.EndOfStream ) {
          var line = reader.ReadLine() ;
          if ( startRow > startLine ) {
            var values = line!.Split( ',' ) ;

            switch ( modelName ) {
              case ModelName.WiresAndCables :
                if ( values.Length < wacColCount ) checkFile = false ;
                else {
                  WiresAndCablesModel wiresAndCablesModel = new WiresAndCablesModel( 
                    values[ 0 ], 
                    values[ 1 ],
                    values[ 2 ], 
                    values[ 3 ], 
                    values[ 4 ], 
                    values[ 5 ], 
                    values[ 6 ], 
                    values[ 7 ], 
                    values[ 8 ], 
                    values[ 9 ] ) ;
                  _allWiresAndCablesModels.Add( wiresAndCablesModel ) ;
                }

                break ;
              case ModelName.Conduits :
                if ( values.Length < conduitColCount ) checkFile = false ;
                else {
                  ConduitsModel conduitsModel = new ConduitsModel( 
                    values[ 0 ], 
                    values[ 1 ], 
                    values[ 2 ],
                    values[ 3 ], 
                    values[ 4 ] ) ;
                  _allConduitModels.Add( conduitsModel ) ;
                }

                break ;
              case ModelName.HiroiSetMasterNormal :
                if ( values.Length < hsmColCount ) checkFile = false ;
                else {
                  HiroiSetMasterModel hiroiSetMasterNormalModel = new HiroiSetMasterModel( 
                    values[ 0 ], 
                    values[ 1 ], 
                    values[ 2 ], 
                    values[ 3 ], 
                    values[ 4 ], 
                    values[ 5 ], 
                    values[ 6 ], 
                    values[ 7 ], 
                    values[ 8 ], 
                    values[ 9 ], 
                    values[ 10 ], 
                    values[ 11 ], 
                    values[ 12 ], 
                    values[ 13 ], 
                    values[ 14 ], 
                    values[ 15 ], 
                    values[ 16 ], 
                    values[ 17 ], 
                    values[ 18 ], 
                    values[ 19 ], 
                    values[ 20 ], 
                    values[ 21 ], 
                    values[ 22 ], 
                    values[ 23 ], 
                    values[ 24 ], 
                    values[ 25 ], 
                    values[ 26 ] ) ;
                  _allHiroiSetMasterNormalModels.Add( hiroiSetMasterNormalModel ) ;
                }

                break ;
              case ModelName.HiroiSetMasterEco :
                if ( values.Length < hsmColCount ) checkFile = false ;
                else {
                  HiroiSetMasterModel hiroiSetMasterEcoModel = new HiroiSetMasterModel( 
                    values[ 0 ], 
                    values[ 1 ], 
                    values[ 2 ], 
                    values[ 3 ], 
                    values[ 4 ], 
                    values[ 5 ], 
                    values[ 6 ], 
                    values[ 7 ], 
                    values[ 8 ], 
                    values[ 9 ], 
                    values[ 10 ], 
                    values[ 11 ], 
                    values[ 12 ],
                    values[ 13 ], 
                    values[ 14 ], 
                    values[ 15 ], 
                    values[ 16 ], 
                    values[ 17 ], 
                    values[ 18 ], 
                    values[ 19 ], 
                    values[ 20 ], 
                    values[ 21 ], 
                    values[ 22 ], 
                    values[ 23 ], 
                    values[ 24 ], 
                    values[ 25 ], 
                    values[ 26 ] ) ;
                  _allHiroiSetMasterEcoModels.Add( hiroiSetMasterEcoModel ) ;
                }

                break ;
              case ModelName.HiroiSetCdMasterNormal :
                if ( values.Length < hsCdmColCount ) checkFile = false ;
                else {
                  var constructionClassification = GetConstructionClassification( values[ 3 ] ) ;
                  HiroiSetCdMasterModel hiroiSetCdMasterNormalModel = new HiroiSetCdMasterModel(
                    values[ 0 ], 
                    values[ 1 ], 
                    values[ 2 ], 
                    constructionClassification ) ;
                  _allHiroiSetCdMasterNormalModels.Add( hiroiSetCdMasterNormalModel ) ;
                }

                break ;
              case ModelName.HiroiSetCdMasterEco :
                if ( values.Length < hsCdmColCount ) checkFile = false ;
                else {
                  var constructionClassification = GetConstructionClassification( values[ 3 ] ) ;
                  HiroiSetCdMasterModel hiroiSetCdMasterEcoModel = new HiroiSetCdMasterModel(
                    values[ 0 ], 
                    values[ 1 ], 
                    values[ 2 ], 
                    constructionClassification ) ;
                  _allHiroiSetCdMasterEcoModels.Add( hiroiSetCdMasterEcoModel ) ;
                }

                break ;
              case ModelName.HiroiMaster :
                if ( values.Length < hmColCount ) checkFile = false ;
                else {
                  HiroiMasterModel hiroiMasterModel = new HiroiMasterModel( values[ 0 ], values[ 1 ], values[ 2 ], values[ 3 ], values[ 4 ], values[ 5 ], values[ 6 ], values[ 7 ], values[ 8 ], values[ 9 ], values[ 10 ], values[ 11 ] ) ;
                  _allHiroiMasterModels.Add( hiroiMasterModel ) ;
                }

                break ;
              default :
                throw new ArgumentOutOfRangeException( nameof( modelName ), modelName, null ) ;
            }
          }

          if ( ! checkFile ) {
            break ;
          }

          startRow++ ;
        }

        reader.Close() ;
        reader.Dispose() ;
        if ( ! checkFile ) {
          if ( showMessageFlag ) {
            MessageBox.Show( "Incorrect file format.", "Error Message" ) ;
          }

          return false ;
        }
        else {
          if ( showMessageFlag ) {
            MessageBox.Show( "Load file successful.", "Result Message" ) ;
          }

          return true ;
        }
      }
      catch ( Exception ) {
        if ( showMessageFlag ) {
          MessageBox.Show( "Load file failed.", "Error Message" ) ;
        }

        return false ;
      }
    }

    private string GetConstructionClassification( string oldConstructionClassification )
    {
      string newConstructionClassification ;
      if ( oldConstructionClassification == OldConstructionClassificationType.天井ふところ.GetFieldName() ) {
        newConstructionClassification = NewConstructionClassificationType.天井コロガシ.GetFieldName() ;
      }
      else if ( oldConstructionClassification == OldConstructionClassificationType.床隠蔽.GetFieldName() ) {
        newConstructionClassification = NewConstructionClassificationType.打ち込み.GetFieldName() ;
      }
      else if ( oldConstructionClassification == OldConstructionClassificationType.二重床.GetFieldName() ) {
        newConstructionClassification = NewConstructionClassificationType.フリーアクセス.GetFieldName() ;
      }
      else {
        newConstructionClassification = oldConstructionClassification ;
      }

      return newConstructionClassification ;
    }

    private string RenamePathToRelative( string path )
    {
      var filePath = path.Split( '\\' ) ;
      var length = filePath.Length ;
      StringBuilder stringPath = new StringBuilder() ;
      stringPath.Append( filePath[ 0 ] + @"\" ) ;
      for ( int i = 1 ; i < length - 2 ; i++ ) {
        stringPath.Append( @"..\" ) ;
      }

      stringPath.Append( filePath[ length - 2 ] + @"\" ) ;
      stringPath.Append( filePath[ length - 1 ] ) ;
      return stringPath.ToString() ;
    }

    private void InitOldValueFloor( DefaultSettingStorable defaultSettingStorable )
    {
      var importDwgMappingModels = defaultSettingStorable.ImportDwgMappingData ;
      foreach ( var importDwgMappingModel in importDwgMappingModels ) {
        _oldValueFloor.Add( importDwgMappingModel.Id, importDwgMappingModel.FloorHeightDisplay.ToString( CultureInfo.InvariantCulture ) ) ;
      }
    }

    private List<ImportDwgMappingModel> CalculateFloorHeight( IEnumerable<ImportDwgMappingModel> importDwgMappingModels )
    {
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

      foreach ( var importDwgMappingModel in result ) {
        _oldValueFloor[ importDwgMappingModel.Id ] = importDwgMappingModel.FloorHeightDisplay ;
      }

      return result ;
    }

    public void UpdateFloorHeight( ImportDwgMappingModel selectedItem )
    {
      if ( ! _oldValueFloor.ContainsKey( selectedItem.Id ) ) return ;
      var importDwgMappingModelFloorHeightDisplay = _oldValueFloor[ selectedItem.Id ] ;

      var deviant = double.Parse( selectedItem.FloorHeightDisplay ) - double.Parse( importDwgMappingModelFloorHeightDisplay ) ;
      if ( Math.Abs( deviant ) == 0 ) return ;

      var importDwgMappingModel = ImportDwgMappingModels.SingleOrDefault( x => x.Id == selectedItem.Id ) ;
      if ( importDwgMappingModel == null ) return ;
      var lastId = importDwgMappingModel.Id ;
      var newImportDwgMappingModels = ImportDwgMappingModels.Select( x => x.Copy() ).ToList() ;
      var currentIndex = ImportDwgMappingModels.FindIndex( x => x.Id == lastId ) ;

      for ( int i = currentIndex ; i < ImportDwgMappingModels.Count ; i++ ) {
        newImportDwgMappingModels[ i ].FloorHeight += deviant ;
      }

      ImportDwgMappingModels = new ObservableCollection<ImportDwgMappingModel>( newImportDwgMappingModels ) ;
    }

    private void DeleteFloorHeight( ImportDwgMappingModel selectedItem )
    {
      var newImportDwgMappingModels = new List<ImportDwgMappingModel>() ;
      var selectedIndex = ImportDwgMappingModels.FindIndex( x => x.Id == selectedItem.Id ) ;
      if ( selectedIndex == ImportDwgMappingModels.Count - 1 ) return ;

      for ( var i = 0 ; i < ImportDwgMappingModels.Count ; i++ ) {
        if ( i < selectedIndex ) {
          newImportDwgMappingModels.Add( ImportDwgMappingModels[ i ] ) ;
          continue ;
        }

        if ( i == selectedIndex ) continue ;
        var newImportDwgMappingModel = ImportDwgMappingModels[ i ] ;
        
        if ( newImportDwgMappingModel.FloorHeight != ImportDwgMappingModels[ selectedIndex ].FloorHeight )
          newImportDwgMappingModel.FloorHeight -= double.Parse( ImportDwgMappingModels[ selectedIndex ].FloorHeightDisplay ) ;
        newImportDwgMappingModels.Add( newImportDwgMappingModel ) ;
      }

      ImportDwgMappingModels = new ObservableCollection<ImportDwgMappingModel>( CalculateFloorHeight( newImportDwgMappingModels ) ) ;
    }

    private void MoveFloorHeight( int startIndex, int endIndex, bool isMovingDown )
    {
      var newImportDwgMappingModels = new List<ImportDwgMappingModel>() ;
      for ( var i = 0 ; i < ImportDwgMappingModels.Count ; i++ ) {
        var newImportDwgMappingModel = ImportDwgMappingModels[ i ] ;
        if ( i < startIndex || i > endIndex ) {
          newImportDwgMappingModels.Add( newImportDwgMappingModel ) ;
          continue ;
        }
        
        if ( ! isMovingDown ) {
          if ( i == startIndex ) {
            newImportDwgMappingModel.FloorHeight = ImportDwgMappingModels[ endIndex ].FloorHeight ;
            newImportDwgMappingModels.Add( newImportDwgMappingModel ) ;
            continue ;
          }

          if ( startIndex >= i || endIndex < i ) continue ;
          
          newImportDwgMappingModel.FloorHeight = double.Parse( ImportDwgMappingModels[ startIndex ].FloorHeightDisplay ) + ImportDwgMappingModels[ startIndex ].FloorHeight ;
          newImportDwgMappingModels.Add( newImportDwgMappingModel ) ;
        }
        else {
          if ( i >= startIndex && i < endIndex ) {
            newImportDwgMappingModel.FloorHeight = ImportDwgMappingModels[ endIndex ].FloorHeight ;
            newImportDwgMappingModels.Add( newImportDwgMappingModel ) ;
            continue ;
          }

          if ( i != endIndex ) continue ;
        
          var lastNewImportDwgMappingModel = newImportDwgMappingModels.Last() ;
          newImportDwgMappingModel.FloorHeight = double.Parse( ImportDwgMappingModels[ startIndex ].FloorHeightDisplay ) + ImportDwgMappingModels[ startIndex ].FloorHeight ;
          newImportDwgMappingModels.Add( newImportDwgMappingModel ) ;
        }
      }
      
      ImportDwgMappingModels = new ObservableCollection<ImportDwgMappingModel>( newImportDwgMappingModels ) ;
    }

    private enum NewConstructionClassificationType
    {
      天井コロガシ,
      打ち込み,
      フリーアクセス
    }

    private enum OldConstructionClassificationType
    {
      天井ふところ,
      床隠蔽,
      二重床
    }

    private enum ModelName
    {
      WiresAndCables,
      Conduits,
      HiroiSetMasterNormal,
      HiroiSetMasterEco,
      HiroiSetCdMasterNormal,
      HiroiSetCdMasterEco,
      HiroiMaster
    }
  }
}