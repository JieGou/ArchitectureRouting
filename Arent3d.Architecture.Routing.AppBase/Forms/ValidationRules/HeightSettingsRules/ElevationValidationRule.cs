using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Arent3d.Architecture.Routing.AppBase.Forms.ValidationRules.HeightSettingsRules
{
  internal class ElevationValidationRule : ValidationRule
  {
    private const double MAX_VALUE = 999999;

    public override ValidationResult Validate( object value, CultureInfo cultureInfo )
    {
      if (value == null)
      {
        return new ValidationResult(false, "Entry is required.");
      }

      double proposedValue;
      if (!double.TryParse(value.ToString(), out proposedValue))
      {
        return new ValidationResult(false, "Value is invalid.");
      }

      if (proposedValue < 0.00)
      {
        return new ValidationResult(false, "Value must be greater than or equal to 0.");
      }

      if (proposedValue > MAX_VALUE)
      {
        return new ValidationResult(false, $"Value must be less than or equal to {MAX_VALUE}.");
      }


      return ValidationResult.ValidResult;
    }
  }
}
