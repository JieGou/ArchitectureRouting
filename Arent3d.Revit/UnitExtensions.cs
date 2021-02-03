using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  public static class UnitExtensions
  {
    #region Lengths

    public static double MillimetersToRevitUnits( this double millimeters )
    {
      return UnitUtils.ConvertToInternalUnits( millimeters, UnitTypeId.Millimeters ) ;
    }
    public static double RevitUnitsToMillimeters( this double units )
    {
      return UnitUtils.ConvertFromInternalUnits( units, UnitTypeId.Millimeters ) ;
    }

    public static double MetersToRevitUnits( this double meters )
    {
      return UnitUtils.ConvertToInternalUnits( meters, UnitTypeId.Meters ) ;
    }
    public static double RevitUnitsToMeters( this double units )
    {
      return UnitUtils.ConvertFromInternalUnits( units, UnitTypeId.Meters ) ;
    }
    
    #endregion

    #region Weights

    public static double KilogramsToRevitUnits( this double kilograms )
    {
      return UnitUtils.ConvertToInternalUnits( kilograms, UnitTypeId.Kilograms ) ;
    }
    public static double RevitUnitsToKilograms( this double units )
    {
      return UnitUtils.ConvertFromInternalUnits( units, UnitTypeId.Kilograms ) ;
    }
    
    #endregion

    #region Temperatures

    public static double CelsiusToRevitUnits( this double celsius )
    {
      return UnitUtils.ConvertToInternalUnits( celsius, UnitTypeId.Celsius ) ;
    }
    public static double RevitUnitsToCelsius( this double units )
    {
      return UnitUtils.ConvertFromInternalUnits( units, UnitTypeId.Celsius ) ;
    }

    public static double CelsiusIntervalToRevitUnits( this double celsius )
    {
      return UnitUtils.ConvertToInternalUnits( celsius, UnitTypeId.CelsiusInterval ) ;
    }
    public static double RevitUnitsToCelsiusInterval( this double units )
    {
      return UnitUtils.ConvertFromInternalUnits( units, UnitTypeId.CelsiusInterval ) ;
    }
    
    #endregion
  }
}