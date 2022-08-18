using System.Globalization ;
using System.Text.RegularExpressions ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Data ;

namespace Arent3d.Architecture.Routing.AppBase.Forms.ValidationRules.CnsSettingRules
{
  public class CnsSettingValidationRule : ValidationRule
  {
    public override ValidationResult Validate( object value, CultureInfo cultureInfo )
    {
      if ( value == null || string.IsNullOrEmpty( value.ToString() ) ) {
        return new ValidationResult( false, "Value is required." ) ;
      }

      return ValidationResult.ValidResult ;
    }
  }
}
