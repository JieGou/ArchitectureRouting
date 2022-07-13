using Autodesk.Revit.DB;
using Arent3d.Revit;

namespace Arent3d.Architecture.Routing.AppBase.Extensions
{
  public static class ConnectorParameterExtension
  {
    public static bool TryGetProperty( this Element element, string parameterName, out string? paramValue,
      bool asValueString  )
    {
      paramValue = string.Empty ;
      if ( ! asValueString ) return element.TryGetProperty( parameterName, out  paramValue ) ;

      var parameter = element.LookupParameter( parameterName ) ;
      return parameter != null && parameter.TryGetParameterValueString( out paramValue ) ;
    }

    private static bool TryGetParameterValueString(this Parameter param,out string? paramValueString)
    {
      paramValueString = string.Empty ;
      paramValueString = param.AsValueString() ;
      return true ;
    }
    
  }
}