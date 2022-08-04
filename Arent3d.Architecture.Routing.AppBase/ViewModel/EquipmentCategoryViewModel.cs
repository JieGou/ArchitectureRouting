using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class EquipmentCategoryViewModel : NotifyPropertyChanged
  {
    public const string LatestVersion = "最新" ;

    #region Constructors

    public EquipmentCategoryViewModel()
    {
    }

    public EquipmentCategoryViewModel( Document document, Level? level )
    {
      EquipmentCategories = new Dictionary<string, PickUpViewModel.EquipmentCategory?>
      {
        { "個数物のみ", PickUpViewModel.EquipmentCategory.OnlyPieces },
        { "長さ物のみ", PickUpViewModel.EquipmentCategory.OnlyLongItems }
      } ;
      
      if ( level == null ) {
        var dataStorage = document.FindOrCreateDataStorage<PickUpModel>( false ) ;
        var storageService = new StorageService<DataStorage, PickUpModel>( dataStorage ) ;
        PickUpVersions = new List<string>( storageService.Data.PickUpData.Select( p => p.Version ).Distinct() ) { LatestVersion }
          .OrderByDescending( p => p ).ToList() ;
      }
      else {
        var storageServiceByLevel = new StorageService<Level, PickUpModel>( level ) ;
        PickUpVersions = new List<string>( storageServiceByLevel.Data.PickUpData.Select( p => p.Version ).Distinct() ) { LatestVersion }
          .OrderByDescending( p => p ).ToList() ;
      }
    }

    #endregion

    #region Properties

    private Dictionary<string, PickUpViewModel.EquipmentCategory?>? _equipmentCategories ;

    public Dictionary<string, PickUpViewModel.EquipmentCategory?> EquipmentCategories
    {
      get => _equipmentCategories ??= new Dictionary<string, PickUpViewModel.EquipmentCategory?>() ;
      set
      {
        _equipmentCategories = value ;
        OnPropertyChanged() ;
      }
    }

    private PickUpViewModel.EquipmentCategory? _selectedEquipmentCategory ;

    public PickUpViewModel.EquipmentCategory? SelectedEquipmentCategory
    {
      get => _selectedEquipmentCategory ;
      set
      {
        _selectedEquipmentCategory = value ;
        OnPropertyChanged() ;
      }
    }

    private List<string>? _pickUpVersions ;

    public List<string>? PickUpVersions
    {
      get => _pickUpVersions ;
      set
      {
        _pickUpVersions = value ;
        OnPropertyChanged() ;
      }
    }

    private string? _selectedPickUpVersion ;

    public string? SelectedPickUpVersion
    {
      get => _selectedPickUpVersion ??= LatestVersion ;
      set
      {
        _selectedPickUpVersion = value ;
        OnPropertyChanged() ;
      }
    }

    #endregion

    #region Commands

    public RelayCommand<PickUpViewModel.EquipmentCategory?> SelectedChangeCommand => new(SelectedChanged) ;
    public RelayCommand<Window> ExecuteCommand => new(Execute) ;

    private void SelectedChanged( PickUpViewModel.EquipmentCategory? equipmentCategory )
    {
      SelectedEquipmentCategory = equipmentCategory ;
    }

    private void Execute( Window window )
    {
      window.DialogResult = true ;
      window.Close() ;
    }

    #endregion
  }
}