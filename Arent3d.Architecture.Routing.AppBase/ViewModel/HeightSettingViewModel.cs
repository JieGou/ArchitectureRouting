using Arent3d.Architecture.Routing.Extensions;
using Arent3d.Architecture.Routing.Storable;
using Arent3d.Architecture.Routing.Storable.Model;
using Arent3d.Revit;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class HeightSettingViewModel : ViewModelBase
  {
    public List<HeightSettingModel> HeightSettingModels { get; set; }

    public HeightSettingStorable SettingStorable { get; set; }

    public HeightSettingViewModel( HeightSettingStorable settingStorables )
    {
      SettingStorable = settingStorables;
      HeightSettingModels = settingStorables.HeightSettingsData.Values.ToList();
      if (HeightSettingModels == null)
      {
        HeightSettingModels = new List<HeightSettingModel>();
      }
    }
  }

  public class HeightSettingValidationRule : ValidationRule
  {
    public override ValidationResult Validate( object value,
        System.Globalization.CultureInfo cultureInfo )
    {
      HeightSettingViewModel model = (value as BindingGroup).Items[0] as HeightSettingViewModel;
      if (model)
      {
        return new ValidationResult(false, "Start Date must be earlier than End Date.");
      }
      else
      {
        return ValidationResult.ValidResult;
      }
    }
  }
}
