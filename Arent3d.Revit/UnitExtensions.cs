using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  public static class UnitExtensions
  {
    #region Lengths

    public static double MillimetersToRevitUnits( this double millimeters )
    {
#if REVIT2019 || REVIT2020
      return UnitUtils.ConvertToInternalUnits( millimeters, DisplayUnitType.DUT_MILLIMETERS ) ;
#else
      return UnitUtils.ConvertToInternalUnits( millimeters, UnitTypeId.Millimeters ) ;
#endif
    }
    public static double RevitUnitsToMillimeters( this double units )
    {
#if REVIT2019 || REVIT2020
      return UnitUtils.ConvertFromInternalUnits( units, DisplayUnitType.DUT_MILLIMETERS ) ;
#else
      return UnitUtils.ConvertFromInternalUnits( units, UnitTypeId.Millimeters ) ;
#endif
    }

    public static double MetersToRevitUnits( this double meters )
    {
#if REVIT2019 || REVIT2020
      return UnitUtils.ConvertToInternalUnits( meters, DisplayUnitType.DUT_METERS ) ;
#else
      return UnitUtils.ConvertToInternalUnits( meters, UnitTypeId.Meters ) ;
#endif
    }
    public static double RevitUnitsToMeters( this double units )
    {
#if REVIT2019 || REVIT2020
      return UnitUtils.ConvertFromInternalUnits( units, DisplayUnitType.DUT_METERS ) ;
#else
      return UnitUtils.ConvertFromInternalUnits( units, UnitTypeId.Meters ) ;
#endif
    }
    public static double RevitUnitsToFeet( this double units )
    {
#if REVIT2019 || REVIT2020
          return UnitUtils.ConvertFromInternalUnits( units, DisplayUnitType.DUT_DECIMAL_FEET ) ;
#else
      return UnitUtils.ConvertFromInternalUnits( units, UnitTypeId.Feet ) ;
#endif
    }
    
    #endregion

    #region Weights

    public static double KilogramsToRevitUnits( this double kilograms )
    {
#if REVIT2019 || REVIT2020
      return UnitUtils.ConvertToInternalUnits( kilograms, DisplayUnitType.DUT_KILOGRAMS_MASS ) ;
#else
      return UnitUtils.ConvertToInternalUnits( kilograms, UnitTypeId.Kilograms ) ;
#endif
    }
    public static double RevitUnitsToKilograms( this double units )
    {
#if REVIT2019 || REVIT2020
      return UnitUtils.ConvertFromInternalUnits( units, DisplayUnitType.DUT_KILOGRAMS_MASS ) ;
#else
      return UnitUtils.ConvertFromInternalUnits( units, UnitTypeId.Kilograms ) ;
#endif
    }
    
    #endregion

    #region Temperatures

    public static double CelsiusToRevitUnits( this double celsius )
    {
#if REVIT2019 || REVIT2020
      return UnitUtils.ConvertToInternalUnits( celsius, DisplayUnitType.DUT_CELSIUS ) ;
#else
      return UnitUtils.ConvertToInternalUnits( celsius, UnitTypeId.Celsius ) ;
#endif
    }
    public static double RevitUnitsToCelsius( this double units )
    {
#if REVIT2019 || REVIT2020
      return UnitUtils.ConvertFromInternalUnits( units, DisplayUnitType.DUT_CELSIUS ) ;
#else
      return UnitUtils.ConvertFromInternalUnits( units, UnitTypeId.Celsius ) ;
#endif
    }

    public static double CelsiusIntervalToRevitUnits( this double celsius )
    {
#if REVIT2019 || REVIT2020
      return UnitUtils.ConvertToInternalUnits( celsius, DisplayUnitType.DUT_CELSIUS_DIFFERENCE ) ;
#else
      return UnitUtils.ConvertToInternalUnits( celsius, UnitTypeId.CelsiusInterval ) ;
#endif
    }
    public static double RevitUnitsToCelsiusInterval( this double units )
    {
#if REVIT2019 || REVIT2020
      return UnitUtils.ConvertFromInternalUnits( units, DisplayUnitType.DUT_CELSIUS_DIFFERENCE ) ;
#else
      return UnitUtils.ConvertFromInternalUnits( units, UnitTypeId.CelsiusInterval ) ;
#endif
    }
    
    #endregion
  }
}