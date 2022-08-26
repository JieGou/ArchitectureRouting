using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.IO.Compression ;
using System.Linq ;
using System.Text ;
using System.Text.RegularExpressions ;
using System.Windows ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.UI ;
using System ;
using System.Data ;
using Arent3d.Architecture.Routing.Extensions ;
using Autodesk.Revit.DB ;
using MoreLinq.Extensions ;
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
    private const string GradeKey = "Dialog.Electrical.ChangeFamilyGradeDialog.GradeMode.Grade" ;
    private const string GradeDefaultString = "Grade " ;

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

    private int Scale { get ; }

    private readonly List<ImportDwgMappingModel> _oldImportDwgMappingModels ;
    private readonly List<FileComboboxItemType> _oldFileItems ;
    public List<string> DeletedFloorName { get ; set ; }

    public ICommand LoadDwgFilesCommand => new RelayCommand( LoadDwgFiles ) ;

    public ICommand LoadDefaultDbCommand => new RelayCommand( LoadDefaultDb ) ;
    
    public ICommand LoadAllDbCommand => new RelayCommand( LoadAllDb ) ;

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

    public DefaultSettingViewModel( UIDocument uiDocument, DefaultSettingStorable defaultSettingStorable, int scale, string activeViewName )
    {
      SelectedEcoNormalModeIndex = defaultSettingStorable.EcoSettingData.IsEcoMode ? 1 : 0 ;
      SelectedGradeModeIndex = defaultSettingStorable.GradeSettingData.GradeMode - 1 ;
      _importDwgMappingModels = new ObservableCollection<ImportDwgMappingModel>() ;
      _fileItems = new List<FileComboboxItemType>() ;
      _oldImportDwgMappingModels = new List<ImportDwgMappingModel>() ;
      _oldFileItems = new List<FileComboboxItemType>() ;
      DeletedFloorName = new List<string>() ;
      Scale = scale ;
      GetImportDwgMappingModelsAndFileItems( defaultSettingStorable, activeViewName ) ;

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
      GetCsvFiles( defaultSettingStorable ) ;
    }

    private void AddModelBelowCurrentSelectedRow( DataGrid dtGrid )
    {
      const int floorHeightDistance = 3000 ;
      int index = dtGrid.SelectedIndex ;
      if ( ! ImportDwgMappingModels.Any() ) return ;
      if ( index < 0 ) return ;
      var importDwgMappingModels = ImportDwgMappingModels.ToList() ;
      var currentMaxHeight = importDwgMappingModels.Max( x => x.FloorHeight ) ;

      ImportDwgMappingModels.Insert( index + 1, new ImportDwgMappingModel( string.Empty, string.Empty, currentMaxHeight + floorHeightDistance, Scale ) ) ;
    }


    private void MoveUp( DataGrid dtGrid )
    {
      var index = dtGrid.SelectedIndex ;
      if ( index == 0 ) return ;
      Swap( ImportDwgMappingModels, index, index - 1 ) ;
      dtGrid.SelectedIndex = index - 1 ;
    }

    private void MoveDown( DataGrid dtGrid )
    {
      var index = dtGrid.SelectedIndex ;
      if ( index == ImportDwgMappingModels.Count() - 1 ) return ;
      Swap( ImportDwgMappingModels, index, index + 1 ) ;
      dtGrid.SelectedIndex = index + 1 ;
    }

    private void Swap( ObservableCollection<ImportDwgMappingModel> list, int indexA, int indexB )
    {
      ( list[ indexA ], list[ indexB ] ) = ( list[ indexB ], list[ indexA ] ) ;
    }

    private void GetCsvFiles( DefaultSettingStorable defaultSettingStorable )
    {
      if ( defaultSettingStorable.CsvFileData.Any() ) {
        CsvFileModels = new ObservableCollection<CsvFileModel>( defaultSettingStorable.CsvFileData ) ;
      }
    }

    private void GetImportDwgMappingModelsAndFileItems( DefaultSettingStorable defaultSettingStorable, string activeViewName )
    {
      foreach ( var item in defaultSettingStorable.ImportDwgMappingData ) {
        var isDeleted = true ;
        var importDwgMappingModel = new ImportDwgMappingModel( item, isDeleted ) ;
        _oldImportDwgMappingModels.Add( importDwgMappingModel ) ;
        ImportDwgMappingModels.Add( importDwgMappingModel ) ;

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
        } ) ;
      }
    }

    public void DeleteImportDwgMappingItem( ImportDwgMappingModel selectedItem )
    {
      ImportDwgMappingModels.Remove( selectedItem ) ;
      _oldImportDwgMappingModels.Remove( selectedItem ) ;
      if ( ! DeletedFloorName.Contains( selectedItem.FloorName ) ) {
        DeletedFloorName.Add( selectedItem.FloorName ) ;
      }
    }

    public void LoadDwgFile( ImportDwgMappingModel selectedItem )
    {
      OpenFileDialog openFileDialog = new() { Filter = "DWG files (*.dwg )|*.dwg", Multiselect = false } ;
      if ( openFileDialog.ShowDialog() != DialogResult.OK ) return ;
      var fileName = openFileDialog.FileName ;
      FileItems.Add( new FileComboboxItemType( fileName ) ) ;
      var importDwgMappingModel = ImportDwgMappingModels.FirstOrDefault( d => d == selectedItem ) ;
      if ( importDwgMappingModel == null ) return ;
      importDwgMappingModel.FullFilePath = fileName ;
      importDwgMappingModel.FileName = Path.GetFileName( fileName ) ;
    }

    private void LoadDwgFiles()
    {
      OpenFileDialog openFileDialog = new() { Filter = "DWG files (*.dwg )|*.dwg", Multiselect = true } ;
      if ( openFileDialog.ShowDialog() != DialogResult.OK ) return ;
      ImportDwgMappingModels = new ObservableCollection<ImportDwgMappingModel>( _oldImportDwgMappingModels ) ;
      FileItems = _oldFileItems ;
      foreach ( var fileName in openFileDialog.FileNames ) {
        FileItems.Add( new FileComboboxItemType( fileName ) ) ;
        if ( fileName.Contains( "B1" ) ) {
          ImportDwgMappingModels.Add( new ImportDwgMappingModel( fileName, $"B1F", 0, Scale ) ) ;
        }
        else if ( fileName.Contains( "PH1" ) ) {
          ImportDwgMappingModels.Add( new ImportDwgMappingModel( fileName, $"PH1F", 0, Scale ) ) ;
        }
        else {
          var floorNumber = Regex.Match( fileName, @"\d+階" ).Value.Replace( "階", "" ) ;
          if ( int.TryParse( floorNumber, out _ ) ) ImportDwgMappingModels.Add( new ImportDwgMappingModel( fileName, $"{floorNumber}F", 0, Scale ) ) ;
        }
      }

      ChangeNameIfDuplicate() ;
      UpdateDefaultFloorHeight() ;
    }

    private void ChangeNameIfDuplicate()
    {
      var nameFloors = ImportDwgMappingModels.Select( x => x.FloorName ).ToList() ;
      var newNameFloors = new List<string>() ;
      for ( int i = 0 ; i < nameFloors.Count() ; i++ ) {
        string name = nameFloors[ i ] ;
        int count = 0 ;
        while ( newNameFloors.Contains( name ) ) {
          name = $"{nameFloors[ i ]}({++count})" ;
        }

        newNameFloors.Add( name ) ;
      }

      for ( int i = 0 ; i < ImportDwgMappingModels.Count() ; i++ ) {
        ImportDwgMappingModels[ i ].FloorName = newNameFloors[ i ] ;
      }
    }

    private void UpdateDefaultFloorHeight()
    {
      const int floorHeightDistance = 3000 ;
      Dictionary<string, double> defaultHeights = new()
      {
        { "B1F", 0 },
        { "1F", 4200 },
        { "2F", 9200 },
        { "3F", 13900 },
        { "4F", 18300 },
        { "5F", 22700 },
        { "6F", 27100 },
        { "7F", 31500 },
        { "8F", 35900 },
        { "9F", 40300 },
        { "10F", 44700 }
      } ;
      foreach ( var importDwgMappingModel in ImportDwgMappingModels ) {
        if ( _oldImportDwgMappingModels.FirstOrDefault( x => x.Id == importDwgMappingModel.Id ) == null ) {
          var (key, value) = defaultHeights.FirstOrDefault( x => importDwgMappingModel.FloorName.Contains( x.Key ) ) ;
          if ( key != null ) {
            importDwgMappingModel.FloorHeight = value ;
          }
          else {
            if ( importDwgMappingModel.IsEnabled )
              importDwgMappingModel.FloorHeight = ImportDwgMappingModels.Max( x => x.FloorHeight ) + floorHeightDistance ;
          }
        }
      }


      var maxFloorHeight = ImportDwgMappingModels.Max( x => x.FloorHeight ) ;
      var pH1FFloor = ImportDwgMappingModels.FirstOrDefault( x => x.FloorName.Contains( "PH1F" ) ) ;
      if ( pH1FFloor is { IsEnabled: true } ) pH1FFloor.FloorHeight = maxFloorHeight + 6500 ;

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
      using ( var progressData = progress?.Reserve( 0.3 ) ) {
        CsvStorable csvStorable = document.GetCsvStorable() ;
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
            using Transaction t = new Transaction( document, "Save data" ) ;
            t.Start() ;
            csvStorable.Save() ;
            t.Commit() ;
          }
          catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
            MessageBox.Show( "Save CSV Files Failed.", "Error Message" ) ;
            //DialogResult = false ;
          }
        }
        progressData?.ThrowIfCanceled() ;
      }

      using ( var progressData = progress?.Reserve( 0.6 ) ) {
        var ceedStorable = document.GetCeedStorable() ;
        if ( _ceedModelData.Any() ) {
          ceedStorable.CeedModelData = _ceedModelData ;
          ceedStorable.CeedModelUsedData = new List<CeedModel>() ;
        }

        var registrationOfBoardDataStorable = document.GetRegistrationOfBoardDataStorable() ;
        if ( _registrationOfBoardDataModelData.Any() ) {
          registrationOfBoardDataStorable.RegistrationOfBoardData = _registrationOfBoardDataModelData ;
        }

        if ( _ceedModelData.Any() || _registrationOfBoardDataModelData.Any() ) {
          try {
            using Transaction t = new Transaction( document, "Save Ceed and Board data" ) ;
            t.Start() ;
            if ( _ceedModelData.Any() ) {
              ceedStorable.Save() ;
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

        progressData?.ThrowIfCanceled() ;
      }

      using ( var progressData = progress?.Reserve( 0.9 ) ) {
        DefaultSettingStorable defaultSettingStorable = document.GetDefaultSettingStorable() ;
        {
          if ( _csvFileModels.Any() ) {
            defaultSettingStorable.CsvFileData = new List<CsvFileModel>( _csvFileModels ) ;
            try {
              using Transaction t = new Transaction( document, "Save Csv File data" ) ;
              t.Start() ;
              defaultSettingStorable.Save() ;
              t.Commit() ;
            }
            catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
            }
          }
        }
        progressData?.ThrowIfCanceled() ;
      }
    }

    private string? GetFolderCsvPath() => AssetManager.GetFolderCompressionFilePath( AssetManager.AssetPath, CompressionFileName ) ;
    
    private string OpenFileDialog()
    {
      var openFileDialog = new OpenFileDialog { Filter = "Csv files (*.csv)|*.csv", Multiselect = false } ;
      var filePath = string.Empty ;
      if ( openFileDialog.ShowDialog() == DialogResult.OK ) {
        filePath = openFileDialog.FileName ;
      }

      return filePath ;
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
        return;
      LoadData( dialog.SelectedPath ) ;
    }
    
    

    private void LoadData( string folderPath )
    {
      var listCsvFileModel = new ObservableCollection<CsvFileModel>() ;
      string[] fileNames = new[] { "hiroimaster.csv", "hiroisetcdmaster_normal.csv", "hiroisetcdmaster_eco.csv", "hiroisetmaster_eco.csv", "hiroisetmaster_normal.csv", "電線管一覧.csv", "電線・ケーブル一覧.csv" } ;
      bool isLoadedCeedFile = false ;
      var ceedCodeFile = "【CeeD】セットコード一覧表" ;
      string equipmentSymbolsFile = "機器記号一覧表" ;
      var boardFile = "盤間配線確認表" ;
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
        isLoadedCeedFile = LoadCeedCodeFile( equipmentSymbolsFile, ceedCodeXlsFilePath, equipmentSymbolsXlsxFilePath, equipmentSymbolsXlsFilePath, listCsvFileModel ) ;
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
        _ceedModelData = ExcelToModelConverter.GetAllCeedModelNumber( ceedCodeFilePath, equipmentSymbolsXlsxFilePath ) ;
        if ( _ceedModelData.Any() ) {
          listCsvFile.Add( new CsvFileModel( equipmentSymbolsFile, RenamePathToRelative( equipmentSymbolsXlsxFilePath ), equipmentSymbolsFile + ".xlsx" ) ) ;
          return true ;
        }
      }

      if ( File.Exists( equipmentSymbolsXlsFilePath ) ) {
        _ceedModelData = ExcelToModelConverter.GetAllCeedModelNumber( ceedCodeFilePath, equipmentSymbolsXlsFilePath ) ;
        if ( _ceedModelData.Any() ) {
          listCsvFile.Add( new CsvFileModel( equipmentSymbolsFile, RenamePathToRelative( equipmentSymbolsXlsxFilePath ), equipmentSymbolsFile + ".xls" ) ) ;
          return true ;
        }
      }

      _ceedModelData = ExcelToModelConverter.GetAllCeedModelNumber( ceedCodeFilePath, string.Empty ) ;
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
        using StreamReader reader = new StreamReader( path, Encoding.GetEncoding( "shift-jis" ), true ) ;
        List<string> lines = new List<string>() ;
        var startRow = 0 ;
        while ( ! reader.EndOfStream ) {
          var line = reader.ReadLine() ;
          if ( startRow > startLine ) {
            var values = line!.Split( ',' ) ;

            switch ( modelName ) {
              case ModelName.WiresAndCables :
                if ( values.Length < wacColCount ) checkFile = false ;
                else {
                  WiresAndCablesModel wiresAndCablesModel = new WiresAndCablesModel( values[ 0 ], values[ 1 ], values[ 2 ], values[ 3 ], values[ 4 ], values[ 5 ], values[ 6 ], values[ 7 ], values[ 8 ], values[ 9 ] ) ;
                  _allWiresAndCablesModels.Add( wiresAndCablesModel ) ;
                }

                break ;
              case ModelName.Conduits :
                if ( values.Length < conduitColCount ) checkFile = false ;
                else {
                  ConduitsModel conduitsModel = new ConduitsModel( values[ 0 ], values[ 1 ], values[ 2 ], values[ 3 ], values[ 4 ] ) ;
                  _allConduitModels.Add( conduitsModel ) ;
                }

                break ;
              case ModelName.HiroiSetMasterNormal :
                if ( values.Length < hsmColCount ) checkFile = false ;
                else {
                  HiroiSetMasterModel hiroiSetMasterNormalModel = new HiroiSetMasterModel( values[ 0 ], values[ 1 ], values[ 2 ], values[ 3 ], values[ 4 ], values[ 5 ], values[ 6 ], values[ 7 ], values[ 8 ], values[ 9 ], values[ 10 ], values[ 11 ], values[ 12 ], values[ 13 ], values[ 14 ], values[ 15 ], values[ 16 ], values[ 17 ], values[ 18 ], values[ 19 ], values[ 20 ], values[ 21 ], values[ 22 ], values[ 23 ], values[ 24 ], values[ 25 ], values[ 26 ] ) ;
                  _allHiroiSetMasterNormalModels.Add( hiroiSetMasterNormalModel ) ;
                }

                break ;
              case ModelName.HiroiSetMasterEco :
                if ( values.Length < hsmColCount ) checkFile = false ;
                else {
                  HiroiSetMasterModel hiroiSetMasterEcoModel = new HiroiSetMasterModel( values[ 0 ], values[ 1 ], values[ 2 ], values[ 3 ], values[ 4 ], values[ 5 ], values[ 6 ], values[ 7 ], values[ 8 ], values[ 9 ], values[ 10 ], values[ 11 ], values[ 12 ], values[ 13 ], values[ 14 ], values[ 15 ], values[ 16 ], values[ 17 ], values[ 18 ], values[ 19 ], values[ 20 ], values[ 21 ], values[ 22 ], values[ 23 ], values[ 24 ], values[ 25 ], values[ 26 ] ) ;
                  _allHiroiSetMasterEcoModels.Add( hiroiSetMasterEcoModel ) ;
                }

                break ;
              case ModelName.HiroiSetCdMasterNormal :
                if ( values.Length < hsCdmColCount ) checkFile = false ;
                else {
                  var constructionClassification = GetConstructionClassification( values[ 3 ] ) ;
                  HiroiSetCdMasterModel hiroiSetCdMasterNormalModel = new HiroiSetCdMasterModel( values[ 0 ], values[ 1 ], values[ 2 ], constructionClassification ) ;
                  _allHiroiSetCdMasterNormalModels.Add( hiroiSetCdMasterNormalModel ) ;
                }

                break ;
              case ModelName.HiroiSetCdMasterEco :
                if ( values.Length < hsCdmColCount ) checkFile = false ;
                else {
                  var constructionClassification = GetConstructionClassification( values[ 3 ] ) ;
                  HiroiSetCdMasterModel hiroiSetCdMasterEcoModel = new HiroiSetCdMasterModel( values[ 0 ], values[ 1 ], values[ 2 ], constructionClassification ) ;
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
      var check = length - 2 ;
      for ( int i = 1 ; i < length - 2 ; i++ ) {
        stringPath.Append( @"..\" ) ;
      }

      stringPath.Append( filePath[ length - 2 ] + @"\" ) ;
      stringPath.Append( filePath[ length - 1 ] ) ;
      return stringPath.ToString() ;
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