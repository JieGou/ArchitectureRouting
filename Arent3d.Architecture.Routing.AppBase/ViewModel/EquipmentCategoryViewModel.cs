using System ;
using System.Collections.Generic;
using System.Linq;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class EquipmentCategoryViewModel : NotifyPropertyChanged
  {

    #region Constructors

    public EquipmentCategoryViewModel()
    {
      EquipmentCategories = new Dictionary<string, PickUpViewModel.EquipmentCategory?>() ;
      EquipmentCategories.Add( AllEquipmentCategories, null ) ;
      EquipmentCategories.Add( "電気", PickUpViewModel.EquipmentCategory.ElectricalEquipment ) ;
      EquipmentCategories.Add( "長さ物", PickUpViewModel.EquipmentCategory.MechanicalEquipment ) ;
    }

    #endregion

    #region Properties
    private const string AllEquipmentCategories = "ALL";

    private Dictionary<string, PickUpViewModel.EquipmentCategory?>? _equipmentCategories ;
    public Dictionary<string, PickUpViewModel.EquipmentCategory?> EquipmentCategories
    {
      get => _equipmentCategories ??= new Dictionary<string, PickUpViewModel.EquipmentCategory?>() ;
      set
      {
        _equipmentCategories = value ;
        OnPropertyChanged();
      }
    }

    private PickUpViewModel.EquipmentCategory? _selectedEquipmentCategory ;
    public PickUpViewModel.EquipmentCategory? SelectedEquipmentCategory
    {
      get => _selectedEquipmentCategory ??= EquipmentCategories[AllEquipmentCategories] ;
      set { _selectedEquipmentCategory = value ; OnPropertyChanged(); }
    }
    #endregion
  }
}