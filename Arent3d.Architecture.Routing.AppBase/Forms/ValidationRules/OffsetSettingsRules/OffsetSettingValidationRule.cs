using System.Globalization ;
using System.Windows.Controls ;

namespace Arent3d.Architecture.Routing.AppBase.Forms.ValidationRules.OffsetSettingsRules
{
  internal class OffsetSettingValidationRule : ValidationRule
  {
    private const double MAX_VALUE = 999999 ;

    public override ValidationResult Validate( object value, CultureInfo cultureInfo )
    {
      if ( value == null || string.IsNullOrWhiteSpace( value.ToString() ) ) {
        return new ValidationResult( false, "Value is required." ) ;
      }

      double proposedValue ;
      if ( ! double.TryParse( value.ToString(), out proposedValue ) ) {
        return new ValidationResult( false, "Value must be an integer value." ) ;
      }

      if ( proposedValue < 0 ) {
        return new ValidationResult( false, "Value must be greater than or equal to 0." ) ;
      }

      if ( proposedValue > MAX_VALUE ) {
        return new ValidationResult( false, $"Value must be less than or equal to {MAX_VALUE}." ) ;
      }


      return ValidationResult.ValidResult ;
    }
  }
}