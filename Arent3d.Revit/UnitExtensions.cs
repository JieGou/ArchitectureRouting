using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  public static class UnitExtensions
  {
    #region Lengths

    public static double MillimetersToRevitUnits( this double millimeters )
    {
      return UnitUtils.ConvertToInternalUnits( millimeters, DisplayUnitTypes.Millimeters ) ;
    }
    public static double RevitUnitsToMillimeters( this double units )
    {
      return UnitUtils.ConvertFromInternalUnits( units, DisplayUnitTypes.Millimeters ) ;
    }

    public static double MetersToRevitUnits( this double meters )
    {
      return UnitUtils.ConvertToInternalUnits( meters, DisplayUnitTypes.Meters ) ;
    }
    public static double RevitUnitsToMeters( this double units )
    {
      return UnitUtils.ConvertFromInternalUnits( units, DisplayUnitTypes.Meters ) ;
    }
    public static double RevitUnitsToFeet( this double units )
    {
      return UnitUtils.ConvertFromInternalUnits( units, DisplayUnitTypes.Feet ) ;
    }
    
    #endregion

    #region Weights

    public static double KilogramsToRevitUnits( this double kilograms )
    {
      return UnitUtils.ConvertToInternalUnits( kilograms, DisplayUnitTypes.Kilograms ) ;
    }
    public static double RevitUnitsToKilograms( this double units )
    {
      return UnitUtils.ConvertFromInternalUnits( units, DisplayUnitTypes.Kilograms ) ;
    }
    
    #endregion

    #region Temperatures

    public static double CelsiusToRevitUnits( this double celsius )
    {
      return UnitUtils.ConvertToInternalUnits( celsius, DisplayUnitTypes.Celsius ) ;
    }
    public static double RevitUnitsToCelsius( this double units )
    {
      return UnitUtils.ConvertFromInternalUnits( units, DisplayUnitTypes.Celsius ) ;
    }

    public static double CelsiusIntervalToRevitUnits( this double celsius )
    {
      return UnitUtils.ConvertToInternalUnits( celsius, DisplayUnitTypes.CelsiusInterval ) ;
    }
    public static double RevitUnitsToCelsiusInterval( this double units )
    {
      return UnitUtils.ConvertFromInternalUnits( units, DisplayUnitTypes.CelsiusInterval ) ;
    }
    
    #endregion
  }
}