using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
using System.Text.RegularExpressions ;
using System.Windows ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;

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

    public enum EcoNormalMode
    {
      EcoMode,
      NormalMode
    }

    public enum GradeModes
    {
      Grade3,
      Grade1Grade2
    }

    public IReadOnlyDictionary<EcoNormalMode, string> EcoNormalModes { get ; } = new Dictionary<EcoNormalMode, string> { [ EcoNormalMode.NormalMode ] = NormalModeKey.GetAppStringByKeyOrDefault( NormalModeDefaultString ), [ EcoNormalMode.EcoMode ] = EcoModeKey.GetAppStringByKeyOrDefault( EcoModeDefaultString ) } ;

    public IReadOnlyDictionary<GradeModes, string> GradeModeTypes { get ; } = new Dictionary<GradeModes, string> { [ GradeModes.Grade3 ] = $"{GradeKey.GetAppStringByKeyOrDefault( GradeDefaultString )}3", [ GradeModes.Grade1Grade2 ] = $"{GradeKey.GetAppStringByKeyOrDefault( GradeDefaultString )}1-2", } ;

    public int SelectedEcoNormalModeIndex { get ; set ; }
    public EcoNormalMode SelectedEcoNormalMode => 0 == SelectedEcoNormalModeIndex ? EcoNormalMode.NormalMode : EcoNormalMode.EcoMode ;
    public int SelectedGradeModeIndex { get ; set ; }
    public GradeModes SelectedGradeMode => 0 == SelectedGradeModeIndex ? GradeModes.Grade3 : GradeModes.Grade1Grade2 ;

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
    public ICommand AddImportDwgMappingModelCommand => new RelayCommand( AddImportDwgMappingModel ) ;

    public DefaultSettingViewModel( DefaultSettingStorable defaultSettingStorable, int scale, string activeViewName )
    {
      SelectedEcoNormalModeIndex = defaultSettingStorable.EcoSettingData.IsEcoMode ? 1 : 0 ;
      SelectedGradeModeIndex = defaultSettingStorable.GradeSettingData.IsInGrade3Mode ? 0 : 1 ;
      _importDwgMappingModels = new ObservableCollection<ImportDwgMappingModel>() ;
      _fileItems = new List<FileComboboxItemType>() ;
      _oldImportDwgMappingModels = new List<ImportDwgMappingModel>() ;
      _oldFileItems = new List<FileComboboxItemType>() ;
      DeletedFloorName = new List<string>() ;
      Scale = scale ;
      GetImportDwgMappingModelsAndFileItems( defaultSettingStorable, activeViewName  ) ;
    }

    private void GetImportDwgMappingModelsAndFileItems( DefaultSettingStorable defaultSettingStorable, string activeViewName )
    {
      foreach ( var item in defaultSettingStorable.ImportDwgMappingData ) {
        var isDeleted = item.FloorName != activeViewName ;
        var importDwgMappingModel = new ImportDwgMappingModel( item, isDeleted ) ;
        _oldImportDwgMappingModels.Add( importDwgMappingModel ) ;
        ImportDwgMappingModels.Add( importDwgMappingModel ) ;

        var fileItem = new FileComboboxItemType( item.FullFilePath ) ;
        _oldFileItems.Add( fileItem ) ;
        FileItems.Add( fileItem ) ;
      }
    }

    private void AddImportDwgMappingModel()
    {
      const int floorHeightDistance = 3000 ;
      if ( ! ImportDwgMappingModels.Any() ) return ;
      var importDwgMappingModels = ImportDwgMappingModels.ToList() ;
      var currentMaxHeight = importDwgMappingModels.Max( x => x.FloorHeight ) ;
      ImportDwgMappingModels.Add( new ImportDwgMappingModel( string.Empty, string.Empty, currentMaxHeight + floorHeightDistance, Scale ) ) ;
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

      UpdateDefaultFloorHeight() ;
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
        var (key, value) = defaultHeights.FirstOrDefault( x => x.Key.Equals( importDwgMappingModel.FloorName ) ) ;
        if ( key != null ) {
          importDwgMappingModel.FloorHeight = value ;
        }
        else {
          importDwgMappingModel.FloorHeight = ImportDwgMappingModels.Max( x => x.FloorHeight ) + floorHeightDistance ;
        }
      }

      var maxFloorHeight = ImportDwgMappingModels.Max( x => x.FloorHeight ) ;
      var pH1FFloor = ImportDwgMappingModels.FirstOrDefault( x => x.FloorName.Equals( "PH1F" ) ) ;
      if ( pH1FFloor != null ) pH1FFloor.FloorHeight = maxFloorHeight + 6500 ;

      ImportDwgMappingModels = new ObservableCollection<ImportDwgMappingModel>( ImportDwgMappingModels.OrderBy( x => x.FloorHeight ).ToList() ) ;
    }
  }
}