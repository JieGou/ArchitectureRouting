#if REVIT2019 || REVIT2020
using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  public class DisplayUnitType
  {
    private readonly Autodesk.Revit.DB.DisplayUnitType _value ;
    private DisplayUnitType( Autodesk.Revit.DB.DisplayUnitType value ) => _value = value ;

    public static implicit operator DisplayUnitType( Autodesk.Revit.DB.DisplayUnitType unitType ) => new DisplayUnitType( unitType ) ;
    public static implicit operator Autodesk.Revit.DB.DisplayUnitType( DisplayUnitType unitType ) => unitType._value ;

    public string GetTypeCatalogString() => UnitUtils.GetTypeCatalogString( _value ) ;
  }

  public class DisplayUnitTypes
  {
    public static DisplayUnitType Undefined => Autodesk.Revit.DB.DisplayUnitType.DUT_UNDEFINED ;
    public static DisplayUnitType Custom => Autodesk.Revit.DB.DisplayUnitType.DUT_CUSTOM ;
    public static DisplayUnitType Meters => Autodesk.Revit.DB.DisplayUnitType.DUT_METERS ;
    public static DisplayUnitType Centimeters => Autodesk.Revit.DB.DisplayUnitType.DUT_CENTIMETERS ;
    public static DisplayUnitType Millimeters => Autodesk.Revit.DB.DisplayUnitType.DUT_MILLIMETERS ;
    public static DisplayUnitType Feet => Autodesk.Revit.DB.DisplayUnitType.DUT_DECIMAL_FEET ;
    public static DisplayUnitType FeetFractionalInches => Autodesk.Revit.DB.DisplayUnitType.DUT_FEET_FRACTIONAL_INCHES ;
    public static DisplayUnitType FractionalInches => Autodesk.Revit.DB.DisplayUnitType.DUT_FRACTIONAL_INCHES ;
    public static DisplayUnitType Inches => Autodesk.Revit.DB.DisplayUnitType.DUT_DECIMAL_INCHES ;
    public static DisplayUnitType Acres => Autodesk.Revit.DB.DisplayUnitType.DUT_ACRES ;
    public static DisplayUnitType Hectares => Autodesk.Revit.DB.DisplayUnitType.DUT_HECTARES ;
    public static DisplayUnitType MetersCentimeters => Autodesk.Revit.DB.DisplayUnitType.DUT_METERS_CENTIMETERS ;
    public static DisplayUnitType CubicYards => Autodesk.Revit.DB.DisplayUnitType.DUT_CUBIC_YARDS ;
    public static DisplayUnitType SquareFeet => Autodesk.Revit.DB.DisplayUnitType.DUT_SQUARE_FEET ;
    public static DisplayUnitType SquareMeters => Autodesk.Revit.DB.DisplayUnitType.DUT_SQUARE_METERS ;
    public static DisplayUnitType CubicFeet => Autodesk.Revit.DB.DisplayUnitType.DUT_CUBIC_FEET ;
    public static DisplayUnitType CubicMeters => Autodesk.Revit.DB.DisplayUnitType.DUT_CUBIC_METERS ;
    public static DisplayUnitType Degrees => Autodesk.Revit.DB.DisplayUnitType.DUT_DECIMAL_DEGREES ;
    public static DisplayUnitType DegreesMinutes => Autodesk.Revit.DB.DisplayUnitType.DUT_DEGREES_AND_MINUTES ;
    public static DisplayUnitType General => Autodesk.Revit.DB.DisplayUnitType.DUT_GENERAL ;
    public static DisplayUnitType Fixed => Autodesk.Revit.DB.DisplayUnitType.DUT_FIXED ;
    public static DisplayUnitType Percentage => Autodesk.Revit.DB.DisplayUnitType.DUT_PERCENTAGE ;
    public static DisplayUnitType SquareInches => Autodesk.Revit.DB.DisplayUnitType.DUT_SQUARE_INCHES ;
    public static DisplayUnitType SquareCentimeters => Autodesk.Revit.DB.DisplayUnitType.DUT_SQUARE_CENTIMETERS ;
    public static DisplayUnitType SquareMillimeters => Autodesk.Revit.DB.DisplayUnitType.DUT_SQUARE_MILLIMETERS ;
    public static DisplayUnitType CubicInches => Autodesk.Revit.DB.DisplayUnitType.DUT_CUBIC_INCHES ;
    public static DisplayUnitType CubicCentimeters => Autodesk.Revit.DB.DisplayUnitType.DUT_CUBIC_CENTIMETERS ;
    public static DisplayUnitType CubicMillimeters => Autodesk.Revit.DB.DisplayUnitType.DUT_CUBIC_MILLIMETERS ;
    /// <summary>liter (L)</summary>
    public static DisplayUnitType Liters => Autodesk.Revit.DB.DisplayUnitType.DUT_LITERS ;
    /// <summary>gallon (U.S.) (gal)</summary>
    public static DisplayUnitType UsGallons => Autodesk.Revit.DB.DisplayUnitType.DUT_GALLONS_US ;
    /// <summary>kilogram per cubic meter (kg/m³)</summary>
    public static DisplayUnitType KilogramsPerCubicMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOGRAMS_PER_CUBIC_METER ;
    /// <summary>pound per cubic foot (lb/ft³)</summary>
    public static DisplayUnitType PoundsMassPerCubicFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_POUNDS_MASS_PER_CUBIC_FOOT ;
    /// <summary>pound per cubic inch (lb/in³)</summary>
    public static DisplayUnitType PoundsMassPerCubicInch => Autodesk.Revit.DB.DisplayUnitType.DUT_POUNDS_MASS_PER_CUBIC_INCH ;
    /// <summary>British thermal unit[IT] (Btu[IT])</summary>
    public static DisplayUnitType BritishThermalUnits => Autodesk.Revit.DB.DisplayUnitType.DUT_BRITISH_THERMAL_UNITS ;
    /// <summary>calorie[IT] (cal[IT])</summary>
    public static DisplayUnitType Calories => Autodesk.Revit.DB.DisplayUnitType.DUT_CALORIES ;
    /// <summary>kilocalorie[IT] (kcal[IT])</summary>
    public static DisplayUnitType Kilocalories => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOCALORIES ;
    /// <summary>joule (J)</summary>
    public static DisplayUnitType Joules => Autodesk.Revit.DB.DisplayUnitType.DUT_JOULES ;
    /// <summary>kilowatt hour (kW · h)</summary>
    public static DisplayUnitType KilowattHours => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOWATT_HOURS ;
    /// <summary>therm (EC)</summary>
    public static DisplayUnitType Therms => Autodesk.Revit.DB.DisplayUnitType.DUT_THERMS ;
    /// <summary>Inches of water per 100 feet</summary>
    public static DisplayUnitType InchesOfWater60DegreesFahrenheitPer100Feet => Autodesk.Revit.DB.DisplayUnitType.DUT_INCHES_OF_WATER_PER_100FT ;
    /// <summary>pascal per meter (N/m)</summary>
    public static DisplayUnitType PascalsPerMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_PASCALS_PER_METER ;
    /// <summary>watt (W)</summary>
    public static DisplayUnitType Watts => Autodesk.Revit.DB.DisplayUnitType.DUT_WATTS ;
    /// <summary>kilowatt (kW)</summary>
    public static DisplayUnitType Kilowatts => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOWATTS ;
    /// <summary>British thermal unit[IT] per second (Btu[IT]/s)</summary>
    public static DisplayUnitType BritishThermalUnitsPerSecond => Autodesk.Revit.DB.DisplayUnitType.DUT_BRITISH_THERMAL_UNITS_PER_SECOND ;
    /// <summary>British thermal unit[IT] per hour (Btu[IT]/h)</summary>
    public static DisplayUnitType BritishThermalUnitsPerHour => Autodesk.Revit.DB.DisplayUnitType.DUT_BRITISH_THERMAL_UNITS_PER_HOUR ;
    /// <summary>calorie[IT] per second (cal[IT]/s)</summary>
    public static DisplayUnitType CaloriesPerSecond => Autodesk.Revit.DB.DisplayUnitType.DUT_CALORIES_PER_SECOND ;
    /// <summary>kilocalorie[IT] per second (kcal[IT]/s)</summary>
    public static DisplayUnitType KilocaloriesPerSecond => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOCALORIES_PER_SECOND ;
    /// <summary>watt per square foot (W/ft²)</summary>
    public static DisplayUnitType WattsPerSquareFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_WATTS_PER_SQUARE_FOOT ;
    /// <summary>watt per square meter (W/m²)</summary>
    public static DisplayUnitType WattsPerSquareMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_WATTS_PER_SQUARE_METER ;
    /// <summary>inch of water (60.8°F)</summary>
    public static DisplayUnitType InchesOfWater60DegreesFahrenheit => Autodesk.Revit.DB.DisplayUnitType.DUT_INCHES_OF_WATER ;
    /// <summary>pascal (Pa)</summary>
    public static DisplayUnitType Pascals => Autodesk.Revit.DB.DisplayUnitType.DUT_PASCALS ;
    /// <summary>kilopascal (kPa)</summary>
    public static DisplayUnitType Kilopascals => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOPASCALS ;
    /// <summary>megapascal (MPa)</summary>
    public static DisplayUnitType Megapascals => Autodesk.Revit.DB.DisplayUnitType.DUT_MEGAPASCALS ;
    /// <summary>pound-force per square inch (psi) (lbf/in2)</summary>
    public static DisplayUnitType PoundsForcePerSquareInch => Autodesk.Revit.DB.DisplayUnitType.DUT_POUNDS_FORCE_PER_SQUARE_INCH ;
    /// <summary>inch of mercury  conventional (inHg)</summary>
    public static DisplayUnitType InchesOfMercury32DegreesFahrenheit => Autodesk.Revit.DB.DisplayUnitType.DUT_INCHES_OF_MERCURY ;
    /// <summary>millimeter of mercury  conventional (mmHg)</summary>
    public static DisplayUnitType MillimetersOfMercury => Autodesk.Revit.DB.DisplayUnitType.DUT_MILLIMETERS_OF_MERCURY ;
    /// <summary>atmosphere  standard (atm)</summary>
    public static DisplayUnitType Atmospheres => Autodesk.Revit.DB.DisplayUnitType.DUT_ATMOSPHERES ;
    /// <summary>bar (bar)</summary>
    public static DisplayUnitType Bars => Autodesk.Revit.DB.DisplayUnitType.DUT_BARS ;
    /// <summary>degree Fahrenheit (°F)</summary>
    public static DisplayUnitType Fahrenheit => Autodesk.Revit.DB.DisplayUnitType.DUT_FAHRENHEIT ;
    /// <summary>degree Celsius (°C)</summary>
    public static DisplayUnitType Celsius => Autodesk.Revit.DB.DisplayUnitType.DUT_CELSIUS ;
    /// <summary>kelvin (K)</summary>
    public static DisplayUnitType Kelvin => Autodesk.Revit.DB.DisplayUnitType.DUT_KELVIN ;
    /// <summary>degree Rankine (°R)</summary>
    public static DisplayUnitType Rankine => Autodesk.Revit.DB.DisplayUnitType.DUT_RANKINE ;
    /// <summary>foot per minute (ft/min)</summary>
    public static DisplayUnitType FeetPerMinute => Autodesk.Revit.DB.DisplayUnitType.DUT_FEET_PER_MINUTE ;
    /// <summary>meter per second (m/s)</summary>
    public static DisplayUnitType MetersPerSecond => Autodesk.Revit.DB.DisplayUnitType.DUT_METERS_PER_SECOND ;
    /// <summary>centimeter per minute (cm/min)</summary>
    public static DisplayUnitType CentimetersPerMinute => Autodesk.Revit.DB.DisplayUnitType.DUT_CENTIMETERS_PER_MINUTE ;
    /// <summary>cubic foot per minute (ft³/min)</summary>
    public static DisplayUnitType CubicFeetPerMinute => Autodesk.Revit.DB.DisplayUnitType.DUT_CUBIC_FEET_PER_MINUTE ;
    /// <summary>liter per second (L/s)</summary>
    public static DisplayUnitType LitersPerSecond => Autodesk.Revit.DB.DisplayUnitType.DUT_LITERS_PER_SECOND ;
    /// <summary>cubic meter per second (m³/s)</summary>
    public static DisplayUnitType CubicMetersPerSecond => Autodesk.Revit.DB.DisplayUnitType.DUT_CUBIC_METERS_PER_SECOND ;
    /// <summary>cubic meters per hour (m³/h)</summary>
    public static DisplayUnitType CubicMetersPerHour => Autodesk.Revit.DB.DisplayUnitType.DUT_CUBIC_METERS_PER_HOUR ;
    /// <summary>gallon (U.S.) per minute (gpm) (gal/min)</summary>
    public static DisplayUnitType UsGallonsPerMinute => Autodesk.Revit.DB.DisplayUnitType.DUT_GALLONS_US_PER_MINUTE ;
    /// <summary>gallon (U.S.) per hour (gph) (gal/h)</summary>
    public static DisplayUnitType UsGallonsPerHour => Autodesk.Revit.DB.DisplayUnitType.DUT_GALLONS_US_PER_HOUR ;
    /// <summary>ampere (A)</summary>
    public static DisplayUnitType Amperes => Autodesk.Revit.DB.DisplayUnitType.DUT_AMPERES ;
    /// <summary>kiloampere (kA)</summary>
    public static DisplayUnitType Kiloamperes => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOAMPERES ;
    /// <summary>milliampere (mA)</summary>
    public static DisplayUnitType Milliamperes => Autodesk.Revit.DB.DisplayUnitType.DUT_MILLIAMPERES ;
    /// <summary>volt (V)</summary>
    public static DisplayUnitType Volts => Autodesk.Revit.DB.DisplayUnitType.DUT_VOLTS ;
    /// <summary>kilovolt (kV)</summary>
    public static DisplayUnitType Kilovolts => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOVOLTS ;
    /// <summary>millivolt (mV)</summary>
    public static DisplayUnitType Millivolts => Autodesk.Revit.DB.DisplayUnitType.DUT_MILLIVOLTS ;
    /// <summary>hertz (Hz)</summary>
    public static DisplayUnitType Hertz => Autodesk.Revit.DB.DisplayUnitType.DUT_HERTZ ;
    public static DisplayUnitType CyclesPerSecond => Autodesk.Revit.DB.DisplayUnitType.DUT_CYCLES_PER_SECOND ;
    /// <summary>lux (lx)</summary>
    public static DisplayUnitType Lux => Autodesk.Revit.DB.DisplayUnitType.DUT_LUX ;
    /// <summary>footcandle</summary>
    public static DisplayUnitType Footcandles => Autodesk.Revit.DB.DisplayUnitType.DUT_FOOTCANDLES ;
    /// <summary>footlambert</summary>
    public static DisplayUnitType Footlamberts => Autodesk.Revit.DB.DisplayUnitType.DUT_FOOTLAMBERTS ;
    /// <summary>candela per square meter (cd/m²)</summary>
    public static DisplayUnitType CandelasPerSquareMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_CANDELAS_PER_SQUARE_METER ;
    /// <summary>candela (cd)</summary>
    public static DisplayUnitType Candelas => Autodesk.Revit.DB.DisplayUnitType.DUT_CANDELAS ;
    /// <summary>lumen (lm)</summary>
    public static DisplayUnitType Lumens => Autodesk.Revit.DB.DisplayUnitType.DUT_LUMENS ;
    public static DisplayUnitType VoltAmperes => Autodesk.Revit.DB.DisplayUnitType.DUT_VOLT_AMPERES ;
    public static DisplayUnitType KilovoltAmperes => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOVOLT_AMPERES ;
    /// <summary>horsepower (550 ft · lbf/s)</summary>
    public static DisplayUnitType Horsepower => Autodesk.Revit.DB.DisplayUnitType.DUT_HORSEPOWER ;
    public static DisplayUnitType Newtons => Autodesk.Revit.DB.DisplayUnitType.DUT_NEWTONS ;
    public static DisplayUnitType Dekanewtons => Autodesk.Revit.DB.DisplayUnitType.DUT_DECANEWTONS ;
    public static DisplayUnitType Kilonewtons => Autodesk.Revit.DB.DisplayUnitType.DUT_KILONEWTONS ;
    public static DisplayUnitType Meganewtons => Autodesk.Revit.DB.DisplayUnitType.DUT_MEGANEWTONS ;
    public static DisplayUnitType Kips => Autodesk.Revit.DB.DisplayUnitType.DUT_KIPS ;
    public static DisplayUnitType KilogramsForce => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOGRAMS_FORCE ;
    public static DisplayUnitType TonnesForce => Autodesk.Revit.DB.DisplayUnitType.DUT_TONNES_FORCE ;
    public static DisplayUnitType PoundsForce => Autodesk.Revit.DB.DisplayUnitType.DUT_POUNDS_FORCE ;
    public static DisplayUnitType NewtonsPerMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_NEWTONS_PER_METER ;
    public static DisplayUnitType DekanewtonsPerMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_DECANEWTONS_PER_METER ;
    public static DisplayUnitType KilonewtonsPerMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_KILONEWTONS_PER_METER ;
    public static DisplayUnitType MeganewtonsPerMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_MEGANEWTONS_PER_METER ;
    public static DisplayUnitType KipsPerFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_KIPS_PER_FOOT ;
    public static DisplayUnitType KilogramsForcePerMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOGRAMS_FORCE_PER_METER ;
    public static DisplayUnitType TonnesForcePerMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_TONNES_FORCE_PER_METER ;
    public static DisplayUnitType PoundsForcePerFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_POUNDS_FORCE_PER_FOOT ;
    public static DisplayUnitType NewtonsPerSquareMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_NEWTONS_PER_SQUARE_METER ;
    public static DisplayUnitType DekanewtonsPerSquareMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_DECANEWTONS_PER_SQUARE_METER ;
    public static DisplayUnitType KilonewtonsPerSquareMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_KILONEWTONS_PER_SQUARE_METER ;
    public static DisplayUnitType MeganewtonsPerSquareMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_MEGANEWTONS_PER_SQUARE_METER ;
    public static DisplayUnitType KipsPerSquareFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_KIPS_PER_SQUARE_FOOT ;
    public static DisplayUnitType KilogramsForcePerSquareMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOGRAMS_FORCE_PER_SQUARE_METER ;
    public static DisplayUnitType TonnesForcePerSquareMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_TONNES_FORCE_PER_SQUARE_METER ;
    public static DisplayUnitType PoundsForcePerSquareFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_POUNDS_FORCE_PER_SQUARE_FOOT ;
    public static DisplayUnitType NewtonMeters => Autodesk.Revit.DB.DisplayUnitType.DUT_NEWTON_METERS ;
    public static DisplayUnitType DekanewtonMeters => Autodesk.Revit.DB.DisplayUnitType.DUT_DECANEWTON_METERS ;
    public static DisplayUnitType KilonewtonMeters => Autodesk.Revit.DB.DisplayUnitType.DUT_KILONEWTON_METERS ;
    public static DisplayUnitType MeganewtonMeters => Autodesk.Revit.DB.DisplayUnitType.DUT_MEGANEWTON_METERS ;
    public static DisplayUnitType KipFeet => Autodesk.Revit.DB.DisplayUnitType.DUT_KIP_FEET ;
    public static DisplayUnitType KilogramForceMeters => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOGRAM_FORCE_METERS ;
    public static DisplayUnitType TonneForceMeters => Autodesk.Revit.DB.DisplayUnitType.DUT_TONNE_FORCE_METERS ;
    public static DisplayUnitType PoundForceFeet => Autodesk.Revit.DB.DisplayUnitType.DUT_POUND_FORCE_FEET ;
    public static DisplayUnitType MetersPerKilonewton => Autodesk.Revit.DB.DisplayUnitType.DUT_METERS_PER_KILONEWTON ;
    public static DisplayUnitType FeetPerKip => Autodesk.Revit.DB.DisplayUnitType.DUT_FEET_PER_KIP ;
    public static DisplayUnitType SquareMetersPerKilonewton => Autodesk.Revit.DB.DisplayUnitType.DUT_SQUARE_METERS_PER_KILONEWTON ;
    public static DisplayUnitType SquareFeetPerKip => Autodesk.Revit.DB.DisplayUnitType.DUT_SQUARE_FEET_PER_KIP ;
    public static DisplayUnitType CubicMetersPerKilonewton => Autodesk.Revit.DB.DisplayUnitType.DUT_CUBIC_METERS_PER_KILONEWTON ;
    public static DisplayUnitType CubicFeetPerKip => Autodesk.Revit.DB.DisplayUnitType.DUT_CUBIC_FEET_PER_KIP ;
    public static DisplayUnitType InverseKilonewtons => Autodesk.Revit.DB.DisplayUnitType.DUT_INV_KILONEWTONS ;
    public static DisplayUnitType InverseKips => Autodesk.Revit.DB.DisplayUnitType.DUT_INV_KIPS ;
    /// <summary>foot of water  conventional (ftH2O) per 100 ft</summary>
    public static DisplayUnitType FeetOfWater39_2DegreesFahrenheitPer100Feet => Autodesk.Revit.DB.DisplayUnitType.DUT_FEET_OF_WATER_PER_100FT ;
    /// <summary>foot of water  conventional (ftH2O)</summary>
    public static DisplayUnitType FeetOfWater39_2DegreesFahrenheit => Autodesk.Revit.DB.DisplayUnitType.DUT_FEET_OF_WATER ;
    /// <summary>pascal second (Pa · s)</summary>
    public static DisplayUnitType PascalSeconds => Autodesk.Revit.DB.DisplayUnitType.DUT_PASCAL_SECONDS ;
    /// <summary>pound per foot second (lb/(ft · s))</summary>
    public static DisplayUnitType PoundsMassPerFootSecond => Autodesk.Revit.DB.DisplayUnitType.DUT_POUNDS_MASS_PER_FOOT_SECOND ;
    /// <summary>centipoise (cP)</summary>
    public static DisplayUnitType Centipoises => Autodesk.Revit.DB.DisplayUnitType.DUT_CENTIPOISES ;
    /// <summary>foot per second (ft/s)</summary>
    public static DisplayUnitType FeetPerSecond => Autodesk.Revit.DB.DisplayUnitType.DUT_FEET_PER_SECOND ;
    public static DisplayUnitType KipsPerSquareInch => Autodesk.Revit.DB.DisplayUnitType.DUT_KIPS_PER_SQUARE_INCH ;
    /// <summary>kilnewtons per cubic meter (kN/m³)</summary>
    public static DisplayUnitType KilonewtonsPerCubicMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_KILONEWTONS_PER_CUBIC_METER ;
    /// <summary>pound per cubic foot (kip/ft³)</summary>
    public static DisplayUnitType PoundsForcePerCubicFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_POUNDS_FORCE_PER_CUBIC_FOOT ;
    /// <summary>pound per cubic foot (kip/in³)</summary>
    public static DisplayUnitType KipsPerCubicInch => Autodesk.Revit.DB.DisplayUnitType.DUT_KIPS_PER_CUBIC_INCH ;
    public static DisplayUnitType InverseDegreesFahrenheit => Autodesk.Revit.DB.DisplayUnitType.DUT_INV_FAHRENHEIT ;
    public static DisplayUnitType InverseDegreesCelsius => Autodesk.Revit.DB.DisplayUnitType.DUT_INV_CELSIUS ;
    public static DisplayUnitType NewtonMetersPerMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_NEWTON_METERS_PER_METER ;
    public static DisplayUnitType DekanewtonMetersPerMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_DECANEWTON_METERS_PER_METER ;
    public static DisplayUnitType KilonewtonMetersPerMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_KILONEWTON_METERS_PER_METER ;
    public static DisplayUnitType MeganewtonMetersPerMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_MEGANEWTON_METERS_PER_METER ;
    public static DisplayUnitType KipFeetPerFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_KIP_FEET_PER_FOOT ;
    public static DisplayUnitType KilogramForceMetersPerMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOGRAM_FORCE_METERS_PER_METER ;
    public static DisplayUnitType TonneForceMetersPerMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_TONNE_FORCE_METERS_PER_METER ;
    public static DisplayUnitType PoundForceFeetPerFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_POUND_FORCE_FEET_PER_FOOT ;
    /// <summary>pound per foot hour (lb/(ft · h))</summary>
    public static DisplayUnitType PoundsMassPerFootHour => Autodesk.Revit.DB.DisplayUnitType.DUT_POUNDS_MASS_PER_FOOT_HOUR ;
    public static DisplayUnitType KipsPerInch => Autodesk.Revit.DB.DisplayUnitType.DUT_KIPS_PER_INCH ;
    /// <summary>pound per cubic foot (kip/ft³)</summary>
    public static DisplayUnitType KipsPerCubicFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_KIPS_PER_CUBIC_FOOT ;
    public static DisplayUnitType KipFeetPerDegree => Autodesk.Revit.DB.DisplayUnitType.DUT_KIP_FEET_PER_DEGREE ;
    public static DisplayUnitType KilonewtonMetersPerDegree => Autodesk.Revit.DB.DisplayUnitType.DUT_KILONEWTON_METERS_PER_DEGREE ;
    public static DisplayUnitType KipFeetPerDegreePerFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_KIP_FEET_PER_DEGREE_PER_FOOT ;
    public static DisplayUnitType KilonewtonMetersPerDegreePerMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_KILONEWTON_METERS_PER_DEGREE_PER_METER ;
    /// <summary>watt per square meter kelvin (W/(m² · K))</summary>
    public static DisplayUnitType WattsPerSquareMeterKelvin => Autodesk.Revit.DB.DisplayUnitType.DUT_WATTS_PER_SQUARE_METER_KELVIN ;
    /// <summary>
    ///    British thermal unit[IT] per hour square foot degree Fahrenheit (Btu[IT]/(h · ft² · °F)
    /// </summary>
    public static DisplayUnitType BritishThermalUnitsPerHourSquareFootDegreeFahrenheit => Autodesk.Revit.DB.DisplayUnitType.DUT_BRITISH_THERMAL_UNITS_PER_HOUR_SQUARE_FOOT_FAHRENHEIT ;
    /// <summary>cubic foot per minute square foot</summary>
    public static DisplayUnitType CubicFeetPerMinuteSquareFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_CUBIC_FEET_PER_MINUTE_SQUARE_FOOT ;
    /// <summary>liter per second square meter</summary>
    public static DisplayUnitType LitersPerSecondSquareMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_LITERS_PER_SECOND_SQUARE_METER ;
    public static DisplayUnitType RatioTo10 => Autodesk.Revit.DB.DisplayUnitType.DUT_RATIO_10 ;
    public static DisplayUnitType RatioTo12 => Autodesk.Revit.DB.DisplayUnitType.DUT_RATIO_12 ;
    public static DisplayUnitType SlopeDegrees => Autodesk.Revit.DB.DisplayUnitType.DUT_SLOPE_DEGREES ;
    public static DisplayUnitType RiseDividedBy12Inches => Autodesk.Revit.DB.DisplayUnitType.DUT_RISE_OVER_INCHES ;
    public static DisplayUnitType RiseDividedBy1Foot => Autodesk.Revit.DB.DisplayUnitType.DUT_RISE_OVER_FOOT ;
    public static DisplayUnitType RiseDividedBy1000Millimeters => Autodesk.Revit.DB.DisplayUnitType.DUT_RISE_OVER_MMS ;
    /// <summary>watt per cubic foot (W/m³)</summary>
    public static DisplayUnitType WattsPerCubicFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_WATTS_PER_CUBIC_FOOT ;
    /// <summary>watt per cubic meter (W/m³)</summary>
    public static DisplayUnitType WattsPerCubicMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_WATTS_PER_CUBIC_METER ;
    /// <summary>British thermal unit[IT] per hour square foot (Btu[IT]/(h · ft²)</summary>
    public static DisplayUnitType BritishThermalUnitsPerHourSquareFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_BRITISH_THERMAL_UNITS_PER_HOUR_SQUARE_FOOT ;
    /// <summary>British thermal unit[IT] per hour cubic foot (Btu[IT]/(h · ft³)</summary>
    public static DisplayUnitType BritishThermalUnitsPerHourCubicFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_BRITISH_THERMAL_UNITS_PER_HOUR_CUBIC_FOOT ;
    /// <summary>Ton of refrigeration (12 000 Btu[IT]/h)</summary>
    public static DisplayUnitType TonsOfRefrigeration => Autodesk.Revit.DB.DisplayUnitType.DUT_TON_OF_REFRIGERATION ;
    /// <summary>cubic foot per minute cubic foot</summary>
    public static DisplayUnitType CubicFeetPerMinuteCubicFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_CUBIC_FEET_PER_MINUTE_CUBIC_FOOT ;
    /// <summary>liter per second cubic meter</summary>
    public static DisplayUnitType LitersPerSecondCubicMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_LITERS_PER_SECOND_CUBIC_METER ;
    /// <summary>cubic foot per minute ton of refrigeration</summary>
    public static DisplayUnitType CubicFeetPerMinuteTonOfRefrigeration => Autodesk.Revit.DB.DisplayUnitType.DUT_CUBIC_FEET_PER_MINUTE_TON_OF_REFRIGERATION ;
    /// <summary>liter per second kilowatt</summary>
    public static DisplayUnitType LitersPerSecondKilowatt => Autodesk.Revit.DB.DisplayUnitType.DUT_LITERS_PER_SECOND_KILOWATTS ;
    /// <summary>square foot per ton of refrigeration</summary>
    public static DisplayUnitType SquareFeetPerTonOfRefrigeration => Autodesk.Revit.DB.DisplayUnitType.DUT_SQUARE_FEET_PER_TON_OF_REFRIGERATION ;
    /// <summary>square meter per kilowatt</summary>
    public static DisplayUnitType SquareMetersPerKilowatt => Autodesk.Revit.DB.DisplayUnitType.DUT_SQUARE_METERS_PER_KILOWATTS ;
    public static DisplayUnitType Currency => Autodesk.Revit.DB.DisplayUnitType.DUT_CURRENCY ;
    public static DisplayUnitType LumensPerWatt => Autodesk.Revit.DB.DisplayUnitType.DUT_LUMENS_PER_WATT ;
    /// <summary>square foot per thousand British thermal unit[IT] per hour</summary>
    public static DisplayUnitType SquareFeetPer1000BritishThermalUnitsPerHour => Autodesk.Revit.DB.DisplayUnitType.DUT_SQUARE_FEET_PER_THOUSAND_BRITISH_THERMAL_UNITS_PER_HOUR ;
    public static DisplayUnitType KilonewtonsPerSquareCentimeter => Autodesk.Revit.DB.DisplayUnitType.DUT_KILONEWTONS_PER_SQUARE_CENTIMETER ;
    public static DisplayUnitType NewtonsPerSquareMillimeter => Autodesk.Revit.DB.DisplayUnitType.DUT_NEWTONS_PER_SQUARE_MILLIMETER ;
    public static DisplayUnitType KilonewtonsPerSquareMillimeter => Autodesk.Revit.DB.DisplayUnitType.DUT_KILONEWTONS_PER_SQUARE_MILLIMETER ;
    public static DisplayUnitType RiseDividedBy120Inches => Autodesk.Revit.DB.DisplayUnitType.DUT_RISE_OVER_120_INCHES ;
    public static DisplayUnitType OneToRatio => Autodesk.Revit.DB.DisplayUnitType.DUT_1_RATIO ;
    public static DisplayUnitType RiseDividedBy10Feet => Autodesk.Revit.DB.DisplayUnitType.DUT_RISE_OVER_10_FEET ;
    public static DisplayUnitType HourSquareFootDegreesFahrenheitPerBritishThermalUnit => Autodesk.Revit.DB.DisplayUnitType.DUT_HOUR_SQUARE_FOOT_FAHRENHEIT_PER_BRITISH_THERMAL_UNIT ;
    public static DisplayUnitType SquareMeterKelvinsPerWatt => Autodesk.Revit.DB.DisplayUnitType.DUT_SQUARE_METER_KELVIN_PER_WATT ;
    public static DisplayUnitType BritishThermalUnitsPerDegreeFahrenheit => Autodesk.Revit.DB.DisplayUnitType.DUT_BRITISH_THERMAL_UNIT_PER_FAHRENHEIT ;
    public static DisplayUnitType JoulesPerKelvin => Autodesk.Revit.DB.DisplayUnitType.DUT_JOULES_PER_KELVIN ;
    public static DisplayUnitType KilojoulesPerKelvin => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOJOULES_PER_KELVIN ;
    /// <summary>kilograms (kg)</summary>
    public static DisplayUnitType Kilograms => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOGRAMS_MASS ;
    /// <summary>tonnes (t)</summary>
    public static DisplayUnitType Tonnes => Autodesk.Revit.DB.DisplayUnitType.DUT_TONNES_MASS ;
    /// <summary>pounds (lb)</summary>
    public static DisplayUnitType PoundsMass => Autodesk.Revit.DB.DisplayUnitType.DUT_POUNDS_MASS ;
    /// <summary>meters per second squared (m/s²)</summary>
    public static DisplayUnitType MetersPerSecondSquared => Autodesk.Revit.DB.DisplayUnitType.DUT_METERS_PER_SECOND_SQUARED ;
    /// <summary>kilometers per second squared (km/s²)</summary>
    public static DisplayUnitType KilometersPerSecondSquared => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOMETERS_PER_SECOND_SQUARED ;
    /// <summary>inches per second squared (in/s²)</summary>
    public static DisplayUnitType InchesPerSecondSquared => Autodesk.Revit.DB.DisplayUnitType.DUT_INCHES_PER_SECOND_SQUARED ;
    /// <summary>feet per second squared (ft/s²)</summary>
    public static DisplayUnitType FeetPerSecondSquared => Autodesk.Revit.DB.DisplayUnitType.DUT_FEET_PER_SECOND_SQUARED ;
    /// <summary>miles per second squared (mi/s²)</summary>
    public static DisplayUnitType MilesPerSecondSquared => Autodesk.Revit.DB.DisplayUnitType.DUT_MILES_PER_SECOND_SQUARED ;
    /// <summary>feet to the fourth power	(ft^4)</summary>
    public static DisplayUnitType FeetToTheFourthPower => Autodesk.Revit.DB.DisplayUnitType.DUT_FEET_TO_THE_FOURTH_POWER ;
    /// <summary>inches to the fourth power	(in^4)</summary>
    public static DisplayUnitType InchesToTheFourthPower => Autodesk.Revit.DB.DisplayUnitType.DUT_INCHES_TO_THE_FOURTH_POWER ;
    /// <summary>millimeters to the fourth power	(mm^4)</summary>
    public static DisplayUnitType MillimetersToTheFourthPower => Autodesk.Revit.DB.DisplayUnitType.DUT_MILLIMETERS_TO_THE_FOURTH_POWER ;
    /// <summary>centimeters to the fourth power	(cm^4)</summary>
    public static DisplayUnitType CentimetersToTheFourthPower => Autodesk.Revit.DB.DisplayUnitType.DUT_CENTIMETERS_TO_THE_FOURTH_POWER ;
    /// <summary>Meters to the fourth power	(m^4)</summary>
    public static DisplayUnitType MetersToTheFourthPower => Autodesk.Revit.DB.DisplayUnitType.DUT_METERS_TO_THE_FOURTH_POWER ;
    /// <summary>feet to the sixth power	(ft^6)</summary>
    public static DisplayUnitType FeetToTheSixthPower => Autodesk.Revit.DB.DisplayUnitType.DUT_FEET_TO_THE_SIXTH_POWER ;
    /// <summary>inches to the sixth power	(in^6)</summary>
    public static DisplayUnitType InchesToTheSixthPower => Autodesk.Revit.DB.DisplayUnitType.DUT_INCHES_TO_THE_SIXTH_POWER ;
    /// <summary>millimeters to the sixth power	(mm^6)</summary>
    public static DisplayUnitType MillimetersToTheSixthPower => Autodesk.Revit.DB.DisplayUnitType.DUT_MILLIMETERS_TO_THE_SIXTH_POWER ;
    /// <summary>centimeters to the sixth power	(cm^6)</summary>
    public static DisplayUnitType CentimetersToTheSixthPower => Autodesk.Revit.DB.DisplayUnitType.DUT_CENTIMETERS_TO_THE_SIXTH_POWER ;
    /// <summary>meters to the sixth power	(m^6)</summary>
    public static DisplayUnitType MetersToTheSixthPower => Autodesk.Revit.DB.DisplayUnitType.DUT_METERS_TO_THE_SIXTH_POWER ;
    /// <summary>square feet per foot	(ft²/ft)</summary>
    public static DisplayUnitType SquareFeetPerFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_SQUARE_FEET_PER_FOOT ;
    /// <summary>square inches per foot	(in²/ft)</summary>
    public static DisplayUnitType SquareInchesPerFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_SQUARE_INCHES_PER_FOOT ;
    /// <summary>square millimeters per meter	(mm²/m)</summary>
    public static DisplayUnitType SquareMillimetersPerMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_SQUARE_MILLIMETERS_PER_METER ;
    /// <summary>square centimeters per meter	(cm²/m)</summary>
    public static DisplayUnitType SquareCentimetersPerMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_SQUARE_CENTIMETERS_PER_METER ;
    /// <summary>square meters per meter	(m²/m)</summary>
    public static DisplayUnitType SquareMetersPerMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_SQUARE_METERS_PER_METER ;
    /// <summary>kilograms per meter (kg/m)</summary>
    public static DisplayUnitType KilogramsPerMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOGRAMS_MASS_PER_METER ;
    /// <summary>pounds per foot (lb/ft)</summary>
    public static DisplayUnitType PoundsMassPerFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_POUNDS_MASS_PER_FOOT ;
    /// <summary>radians</summary>
    public static DisplayUnitType Radians => Autodesk.Revit.DB.DisplayUnitType.DUT_RADIANS ;
    /// <summary>grads</summary>
    public static DisplayUnitType Gradians => Autodesk.Revit.DB.DisplayUnitType.DUT_GRADS ;
    /// <summary>radians per second</summary>
    public static DisplayUnitType RadiansPerSecond => Autodesk.Revit.DB.DisplayUnitType.DUT_RADIANS_PER_SECOND ;
    /// <summary>millisecond</summary>
    public static DisplayUnitType Milliseconds => Autodesk.Revit.DB.DisplayUnitType.DUT_MILISECONDS ;
    /// <summary>second</summary>
    public static DisplayUnitType Seconds => Autodesk.Revit.DB.DisplayUnitType.DUT_SECONDS ;
    /// <summary>minutes</summary>
    public static DisplayUnitType Minutes => Autodesk.Revit.DB.DisplayUnitType.DUT_MINUTES ;
    /// <summary>hours</summary>
    public static DisplayUnitType Hours => Autodesk.Revit.DB.DisplayUnitType.DUT_HOURS ;
    /// <summary>kilometers per hour</summary>
    public static DisplayUnitType KilometersPerHour => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOMETERS_PER_HOUR ;
    /// <summary>miles per hour</summary>
    public static DisplayUnitType MilesPerHour => Autodesk.Revit.DB.DisplayUnitType.DUT_MILES_PER_HOUR ;
    /// <summary>kilojoules</summary>
    public static DisplayUnitType Kilojoules => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOJOULES ;
    /// <summary>kilograms per square meter (kg/m²)</summary>
    public static DisplayUnitType KilogramsPerSquareMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_KILOGRAMS_MASS_PER_SQUARE_METER ;
    /// <summary>pounds per square foot (lb/ft²)</summary>
    public static DisplayUnitType PoundsMassPerSquareFoot => Autodesk.Revit.DB.DisplayUnitType.DUT_POUNDS_MASS_PER_SQUARE_FOOT ;
    /// <summary>Watts per meter kelvin (W/(m·K))</summary>
    public static DisplayUnitType WattsPerMeterKelvin => Autodesk.Revit.DB.DisplayUnitType.DUT_WATTS_PER_METER_KELVIN ;
    /// <summary>Joules per gram Celsius (J/(g·°C))</summary>
    public static DisplayUnitType JoulesPerGramDegreeCelsius => Autodesk.Revit.DB.DisplayUnitType.DUT_JOULES_PER_GRAM_CELSIUS ;
    /// <summary>Joules per gram (J/g)</summary>
    public static DisplayUnitType JoulesPerGram => Autodesk.Revit.DB.DisplayUnitType.DUT_JOULES_PER_GRAM ;
    /// <summary>Nanograms per pascal second square meter (ng/(Pa·s·m²))</summary>
    public static DisplayUnitType NanogramsPerPascalSecondSquareMeter => Autodesk.Revit.DB.DisplayUnitType.DUT_NANOGRAMS_PER_PASCAL_SECOND_SQUARE_METER ;
    /// <summary>Ohm meters</summary>
    public static DisplayUnitType OhmMeters => Autodesk.Revit.DB.DisplayUnitType.DUT_OHM_METERS ;
    /// <summary>BTU per hour foot Fahrenheit (BTU/(h·ft·°F))</summary>
    public static DisplayUnitType BritishThermalUnitsPerHourFootDegreeFahrenheit => Autodesk.Revit.DB.DisplayUnitType.DUT_BRITISH_THERMAL_UNITS_PER_HOUR_FOOT_FAHRENHEIT ;
    /// <summary>BTU per pound Fahrenheit (BTU/(lb·°F))</summary>
    public static DisplayUnitType BritishThermalUnitsPerPoundDegreeFahrenheit => Autodesk.Revit.DB.DisplayUnitType.DUT_BRITISH_THERMAL_UNITS_PER_POUND_FAHRENHEIT ;
    /// <summary>BTU per pound (BTU/lb)</summary>
    public static DisplayUnitType BritishThermalUnitsPerPound => Autodesk.Revit.DB.DisplayUnitType.DUT_BRITISH_THERMAL_UNITS_PER_POUND ;
    /// <summary>Grains per hour square foot inch mercury (gr/(h·ft²·inHg))</summary>
    public static DisplayUnitType GrainsPerHourSquareFootInchMercury => Autodesk.Revit.DB.DisplayUnitType.DUT_GRAINS_PER_HOUR_SQUARE_FOOT_INCH_MERCURY ;
    /// <summary>Per mille or per thousand(â€°)</summary>
    public static DisplayUnitType PerMille => Autodesk.Revit.DB.DisplayUnitType.DUT_PER_MILLE ;
    /// <summary>Decimeters</summary>
    public static DisplayUnitType Decimeters => Autodesk.Revit.DB.DisplayUnitType.DUT_DECIMETERS ;
    /// <summary>Joules per kilogram Celsius (J/(kg·°C))</summary>
    public static DisplayUnitType JoulesPerKilogramDegreeCelsius => Autodesk.Revit.DB.DisplayUnitType.DUT_JOULES_PER_KILOGRAM_CELSIUS ;
    /// <summary>Micrometers per meter Celsius (um/(m·°C))</summary>
    public static DisplayUnitType MicrometersPerMeterDegreeCelsius => Autodesk.Revit.DB.DisplayUnitType.DUT_MICROMETERS_PER_METER_CELSIUS ;
    /// <summary>Microinches per inch Fahrenheit (uin/(in·°F))</summary>
    public static DisplayUnitType MicroinchesPerInchDegreeFahrenheit => Autodesk.Revit.DB.DisplayUnitType.DUT_MICROINCHES_PER_INCH_FAHRENHEIT ;
    /// <summary>US tonnes (T, Tons, ST)</summary>
    public static DisplayUnitType UsTonnesMass => Autodesk.Revit.DB.DisplayUnitType.DUT_USTONNES_MASS ;
    /// <summary>US tonnes (Tonsf, STf)</summary>
    public static DisplayUnitType UsTonnesForce => Autodesk.Revit.DB.DisplayUnitType.DUT_USTONNES_FORCE ;
    /// <summary>liters per minute (L/min)</summary>
    public static DisplayUnitType LitersPerMinute => Autodesk.Revit.DB.DisplayUnitType.DUT_LITERS_PER_MINUTE ;
    /// <summary>degree Fahrenheit difference (delta °F)</summary>
    public static DisplayUnitType FahrenheitInterval => Autodesk.Revit.DB.DisplayUnitType.DUT_FAHRENHEIT_DIFFERENCE ;
    /// <summary>degree Celsius difference (delta °C)</summary>
    public static DisplayUnitType CelsiusInterval => Autodesk.Revit.DB.DisplayUnitType.DUT_CELSIUS_DIFFERENCE ;
    /// <summary>kelvin difference (delta K)</summary>
    public static DisplayUnitType KelvinInterval => Autodesk.Revit.DB.DisplayUnitType.DUT_KELVIN_DIFFERENCE ;
    /// <summary>degree Rankine difference (delta °R)</summary>
    public static DisplayUnitType RankineInterval => Autodesk.Revit.DB.DisplayUnitType.DUT_RANKINE_DIFFERENCE ;
  }
}

#endif