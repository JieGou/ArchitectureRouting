#if ! (REVIT2019 || REVIT2020)
using System ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  public class DisplayUnitType
  {
    public static Type NativeType => typeof( ForgeTypeId ) ;

    private readonly ForgeTypeId _value ;
    private DisplayUnitType( ForgeTypeId value ) => _value = value ;

    public static implicit operator DisplayUnitType( ForgeTypeId forgeTypeId ) => new DisplayUnitType( forgeTypeId ) ;
    public static implicit operator ForgeTypeId( DisplayUnitType unitType ) => unitType._value ;

    public string GetTypeCatalogString() => UnitUtils.GetTypeCatalogStringForUnit( _value ) ;
  }

  public class DisplayUnitTypes
  {
    public static DisplayUnitType Custom => UnitTypeId.Custom ;
    public static DisplayUnitType Meters => UnitTypeId.Meters ;
    public static DisplayUnitType Centimeters => UnitTypeId.Centimeters ;
    public static DisplayUnitType Millimeters => UnitTypeId.Millimeters ;
    public static DisplayUnitType Feet => UnitTypeId.Feet ;
    public static DisplayUnitType FeetFractionalInches => UnitTypeId.FeetFractionalInches ;
    public static DisplayUnitType FractionalInches => UnitTypeId.FractionalInches ;
    public static DisplayUnitType Inches => UnitTypeId.Inches ;
    public static DisplayUnitType Acres => UnitTypeId.Acres ;
    public static DisplayUnitType Hectares => UnitTypeId.Hectares ;
    public static DisplayUnitType MetersCentimeters => UnitTypeId.MetersCentimeters ;
    public static DisplayUnitType CubicYards => UnitTypeId.CubicYards ;
    public static DisplayUnitType SquareFeet => UnitTypeId.SquareFeet ;
    public static DisplayUnitType SquareMeters => UnitTypeId.SquareMeters ;
    public static DisplayUnitType CubicFeet => UnitTypeId.CubicFeet ;
    public static DisplayUnitType CubicMeters => UnitTypeId.CubicMeters ;
    public static DisplayUnitType Degrees => UnitTypeId.Degrees ;
    public static DisplayUnitType DegreesMinutes => UnitTypeId.DegreesMinutes ;
    public static DisplayUnitType General => UnitTypeId.General ;
    public static DisplayUnitType Fixed => UnitTypeId.Fixed ;
    public static DisplayUnitType Percentage => UnitTypeId.Percentage ;
    public static DisplayUnitType SquareInches => UnitTypeId.SquareInches ;
    public static DisplayUnitType SquareCentimeters => UnitTypeId.SquareCentimeters ;
    public static DisplayUnitType SquareMillimeters => UnitTypeId.SquareMillimeters ;
    public static DisplayUnitType CubicInches => UnitTypeId.CubicInches ;
    public static DisplayUnitType CubicCentimeters => UnitTypeId.CubicCentimeters ;
    public static DisplayUnitType CubicMillimeters => UnitTypeId.CubicMillimeters ;

    /// <summary>liter (L)</summary>
    public static DisplayUnitType Liters => UnitTypeId.Liters ;

    /// <summary>gallon (U.S.) (gal)</summary>
    public static DisplayUnitType UsGallons => UnitTypeId.UsGallons ;

    /// <summary>kilogram per cubic meter (kg/m³)</summary>
    public static DisplayUnitType KilogramsPerCubicMeter => UnitTypeId.KilogramsPerCubicMeter ;

    /// <summary>pound per cubic foot (lb/ft³)</summary>
    public static DisplayUnitType PoundsMassPerCubicFoot => UnitTypeId.PoundsMassPerCubicFoot ;

    /// <summary>pound per cubic inch (lb/in³)</summary>
    public static DisplayUnitType PoundsMassPerCubicInch => UnitTypeId.PoundsMassPerCubicInch ;

    /// <summary>British thermal unit[IT] (Btu[IT])</summary>
    public static DisplayUnitType BritishThermalUnits => UnitTypeId.BritishThermalUnits ;

    /// <summary>calorie[IT] (cal[IT])</summary>
    public static DisplayUnitType Calories => UnitTypeId.Calories ;

    /// <summary>kilocalorie[IT] (kcal[IT])</summary>
    public static DisplayUnitType Kilocalories => UnitTypeId.Kilocalories ;

    /// <summary>joule (J)</summary>
    public static DisplayUnitType Joules => UnitTypeId.Joules ;

    /// <summary>kilowatt hour (kW · h)</summary>
    public static DisplayUnitType KilowattHours => UnitTypeId.KilowattHours ;

    /// <summary>therm (EC)</summary>
    public static DisplayUnitType Therms => UnitTypeId.Therms ;

    /// <summary>Inches of water per 100 feet</summary>
    public static DisplayUnitType InchesOfWater60DegreesFahrenheitPer100Feet => UnitTypeId.InchesOfWater60DegreesFahrenheitPer100Feet ;

    /// <summary>pascal per meter (N/m)</summary>
    public static DisplayUnitType PascalsPerMeter => UnitTypeId.PascalsPerMeter ;

    /// <summary>watt (W)</summary>
    public static DisplayUnitType Watts => UnitTypeId.Watts ;

    /// <summary>kilowatt (kW)</summary>
    public static DisplayUnitType Kilowatts => UnitTypeId.Kilowatts ;

    /// <summary>British thermal unit[IT] per second (Btu[IT]/s)</summary>
    public static DisplayUnitType BritishThermalUnitsPerSecond => UnitTypeId.BritishThermalUnitsPerSecond ;

    /// <summary>British thermal unit[IT] per hour (Btu[IT]/h)</summary>
    public static DisplayUnitType BritishThermalUnitsPerHour => UnitTypeId.BritishThermalUnitsPerHour ;

    /// <summary>calorie[IT] per second (cal[IT]/s)</summary>
    public static DisplayUnitType CaloriesPerSecond => UnitTypeId.CaloriesPerSecond ;

    /// <summary>kilocalorie[IT] per second (kcal[IT]/s)</summary>
    public static DisplayUnitType KilocaloriesPerSecond => UnitTypeId.KilocaloriesPerSecond ;

    /// <summary>watt per square foot (W/ft²)</summary>
    public static DisplayUnitType WattsPerSquareFoot => UnitTypeId.WattsPerSquareFoot ;

    /// <summary>watt per square meter (W/m²)</summary>
    public static DisplayUnitType WattsPerSquareMeter => UnitTypeId.WattsPerSquareMeter ;

    /// <summary>inch of water (60.8°F)</summary>
    public static DisplayUnitType InchesOfWater60DegreesFahrenheit => UnitTypeId.InchesOfWater60DegreesFahrenheit ;

    /// <summary>pascal (Pa)</summary>
    public static DisplayUnitType Pascals => UnitTypeId.Pascals ;

    /// <summary>kilopascal (kPa)</summary>
    public static DisplayUnitType Kilopascals => UnitTypeId.Kilopascals ;

    /// <summary>megapascal (MPa)</summary>
    public static DisplayUnitType Megapascals => UnitTypeId.Megapascals ;

    /// <summary>pound-force per square inch (psi) (lbf/in2)</summary>
    public static DisplayUnitType PoundsForcePerSquareInch => UnitTypeId.PoundsForcePerSquareInch ;

    /// <summary>inch of mercury  conventional (inHg)</summary>
    public static DisplayUnitType InchesOfMercury32DegreesFahrenheit => UnitTypeId.InchesOfMercury32DegreesFahrenheit ;

    /// <summary>millimeter of mercury  conventional (mmHg)</summary>
    public static DisplayUnitType MillimetersOfMercury => UnitTypeId.MillimetersOfMercury ;

    /// <summary>atmosphere  standard (atm)</summary>
    public static DisplayUnitType Atmospheres => UnitTypeId.Atmospheres ;

    /// <summary>bar (bar)</summary>
    public static DisplayUnitType Bars => UnitTypeId.Bars ;

    /// <summary>degree Fahrenheit (°F)</summary>
    public static DisplayUnitType Fahrenheit => UnitTypeId.Fahrenheit ;

    /// <summary>degree Celsius (°C)</summary>
    public static DisplayUnitType Celsius => UnitTypeId.Celsius ;

    /// <summary>kelvin (K)</summary>
    public static DisplayUnitType Kelvin => UnitTypeId.Kelvin ;

    /// <summary>degree Rankine (°R)</summary>
    public static DisplayUnitType Rankine => UnitTypeId.Rankine ;

    /// <summary>foot per minute (ft/min)</summary>
    public static DisplayUnitType FeetPerMinute => UnitTypeId.FeetPerMinute ;

    /// <summary>meter per second (m/s)</summary>
    public static DisplayUnitType MetersPerSecond => UnitTypeId.MetersPerSecond ;

    /// <summary>centimeter per minute (cm/min)</summary>
    public static DisplayUnitType CentimetersPerMinute => UnitTypeId.CentimetersPerMinute ;

    /// <summary>cubic foot per minute (ft³/min)</summary>
    public static DisplayUnitType CubicFeetPerMinute => UnitTypeId.CubicFeetPerMinute ;

    /// <summary>liter per second (L/s)</summary>
    public static DisplayUnitType LitersPerSecond => UnitTypeId.LitersPerSecond ;

    /// <summary>cubic meter per second (m³/s)</summary>
    public static DisplayUnitType CubicMetersPerSecond => UnitTypeId.CubicMetersPerSecond ;

    /// <summary>cubic meters per hour (m³/h)</summary>
    public static DisplayUnitType CubicMetersPerHour => UnitTypeId.CubicMetersPerHour ;

    /// <summary>gallon (U.S.) per minute (gpm) (gal/min)</summary>
    public static DisplayUnitType UsGallonsPerMinute => UnitTypeId.UsGallonsPerMinute ;

    /// <summary>gallon (U.S.) per hour (gph) (gal/h)</summary>
    public static DisplayUnitType UsGallonsPerHour => UnitTypeId.UsGallonsPerHour ;

    /// <summary>ampere (A)</summary>
    public static DisplayUnitType Amperes => UnitTypeId.Amperes ;

    /// <summary>kiloampere (kA)</summary>
    public static DisplayUnitType Kiloamperes => UnitTypeId.Kiloamperes ;

    /// <summary>milliampere (mA)</summary>
    public static DisplayUnitType Milliamperes => UnitTypeId.Milliamperes ;

    /// <summary>volt (V)</summary>
    public static DisplayUnitType Volts => UnitTypeId.Volts ;

    /// <summary>kilovolt (kV)</summary>
    public static DisplayUnitType Kilovolts => UnitTypeId.Kilovolts ;

    /// <summary>millivolt (mV)</summary>
    public static DisplayUnitType Millivolts => UnitTypeId.Millivolts ;

    /// <summary>hertz (Hz)</summary>
    public static DisplayUnitType Hertz => UnitTypeId.Hertz ;

    public static DisplayUnitType CyclesPerSecond => UnitTypeId.CyclesPerSecond ;

    /// <summary>lux (lx)</summary>
    public static DisplayUnitType Lux => UnitTypeId.Lux ;

    /// <summary>footcandle</summary>
    public static DisplayUnitType Footcandles => UnitTypeId.Footcandles ;

    /// <summary>footlambert</summary>
    public static DisplayUnitType Footlamberts => UnitTypeId.Footlamberts ;

    /// <summary>candela per square meter (cd/m²)</summary>
    public static DisplayUnitType CandelasPerSquareMeter => UnitTypeId.CandelasPerSquareMeter ;

    /// <summary>candela (cd)</summary>
    public static DisplayUnitType Candelas => UnitTypeId.Candelas ;

    /// <summary>lumen (lm)</summary>
    public static DisplayUnitType Lumens => UnitTypeId.Lumens ;

    public static DisplayUnitType VoltAmperes => UnitTypeId.VoltAmperes ;
    public static DisplayUnitType KilovoltAmperes => UnitTypeId.KilovoltAmperes ;

    /// <summary>horsepower (550 ft · lbf/s)</summary>
    public static DisplayUnitType Horsepower => UnitTypeId.Horsepower ;

    public static DisplayUnitType Newtons => UnitTypeId.Newtons ;
    public static DisplayUnitType Dekanewtons => UnitTypeId.Dekanewtons ;
    public static DisplayUnitType Kilonewtons => UnitTypeId.Kilonewtons ;
    public static DisplayUnitType Meganewtons => UnitTypeId.Meganewtons ;
    public static DisplayUnitType Kips => UnitTypeId.Kips ;
    public static DisplayUnitType KilogramsForce => UnitTypeId.KilogramsForce ;
    public static DisplayUnitType TonnesForce => UnitTypeId.TonnesForce ;
    public static DisplayUnitType PoundsForce => UnitTypeId.PoundsForce ;
    public static DisplayUnitType NewtonsPerMeter => UnitTypeId.NewtonsPerMeter ;
    public static DisplayUnitType DekanewtonsPerMeter => UnitTypeId.DekanewtonsPerMeter ;
    public static DisplayUnitType KilonewtonsPerMeter => UnitTypeId.KilonewtonsPerMeter ;
    public static DisplayUnitType MeganewtonsPerMeter => UnitTypeId.MeganewtonsPerMeter ;
    public static DisplayUnitType KipsPerFoot => UnitTypeId.KipsPerFoot ;
    public static DisplayUnitType KilogramsForcePerMeter => UnitTypeId.KilogramsForcePerMeter ;
    public static DisplayUnitType TonnesForcePerMeter => UnitTypeId.TonnesForcePerMeter ;
    public static DisplayUnitType PoundsForcePerFoot => UnitTypeId.PoundsForcePerFoot ;
    public static DisplayUnitType NewtonsPerSquareMeter => UnitTypeId.NewtonsPerSquareMeter ;
    public static DisplayUnitType DekanewtonsPerSquareMeter => UnitTypeId.DekanewtonsPerSquareMeter ;
    public static DisplayUnitType KilonewtonsPerSquareMeter => UnitTypeId.KilonewtonsPerSquareMeter ;
    public static DisplayUnitType MeganewtonsPerSquareMeter => UnitTypeId.MeganewtonsPerSquareMeter ;
    public static DisplayUnitType KipsPerSquareFoot => UnitTypeId.KipsPerSquareFoot ;
    public static DisplayUnitType KilogramsForcePerSquareMeter => UnitTypeId.KilogramsForcePerSquareMeter ;
    public static DisplayUnitType TonnesForcePerSquareMeter => UnitTypeId.TonnesForcePerSquareMeter ;
    public static DisplayUnitType PoundsForcePerSquareFoot => UnitTypeId.PoundsForcePerSquareFoot ;
    public static DisplayUnitType NewtonMeters => UnitTypeId.NewtonMeters ;
    public static DisplayUnitType DekanewtonMeters => UnitTypeId.DekanewtonMeters ;
    public static DisplayUnitType KilonewtonMeters => UnitTypeId.KilonewtonMeters ;
    public static DisplayUnitType MeganewtonMeters => UnitTypeId.MeganewtonMeters ;
    public static DisplayUnitType KipFeet => UnitTypeId.KipFeet ;
    public static DisplayUnitType KilogramForceMeters => UnitTypeId.KilogramForceMeters ;
    public static DisplayUnitType TonneForceMeters => UnitTypeId.TonneForceMeters ;
    public static DisplayUnitType PoundForceFeet => UnitTypeId.PoundForceFeet ;
    public static DisplayUnitType MetersPerKilonewton => UnitTypeId.MetersPerKilonewton ;
    public static DisplayUnitType FeetPerKip => UnitTypeId.FeetPerKip ;
    public static DisplayUnitType SquareMetersPerKilonewton => UnitTypeId.SquareMetersPerKilonewton ;
    public static DisplayUnitType SquareFeetPerKip => UnitTypeId.SquareFeetPerKip ;
    public static DisplayUnitType CubicMetersPerKilonewton => UnitTypeId.CubicMetersPerKilonewton ;
    public static DisplayUnitType CubicFeetPerKip => UnitTypeId.CubicFeetPerKip ;
    public static DisplayUnitType InverseKilonewtons => UnitTypeId.InverseKilonewtons ;
    public static DisplayUnitType InverseKips => UnitTypeId.InverseKips ;

    /// <summary>foot of water  conventional (ftH2O) per 100 ft</summary>
    public static DisplayUnitType FeetOfWater39_2DegreesFahrenheitPer100Feet => UnitTypeId.FeetOfWater39_2DegreesFahrenheitPer100Feet ;

    /// <summary>foot of water  conventional (ftH2O)</summary>
    public static DisplayUnitType FeetOfWater39_2DegreesFahrenheit => UnitTypeId.FeetOfWater39_2DegreesFahrenheit ;

    /// <summary>pascal second (Pa · s)</summary>
    public static DisplayUnitType PascalSeconds => UnitTypeId.PascalSeconds ;

    /// <summary>pound per foot second (lb/(ft · s))</summary>
    public static DisplayUnitType PoundsMassPerFootSecond => UnitTypeId.PoundsMassPerFootSecond ;

    /// <summary>centipoise (cP)</summary>
    public static DisplayUnitType Centipoises => UnitTypeId.Centipoises ;

    /// <summary>foot per second (ft/s)</summary>
    public static DisplayUnitType FeetPerSecond => UnitTypeId.FeetPerSecond ;

    public static DisplayUnitType KipsPerSquareInch => UnitTypeId.KipsPerSquareInch ;

    /// <summary>kilnewtons per cubic meter (kN/m³)</summary>
    public static DisplayUnitType KilonewtonsPerCubicMeter => UnitTypeId.KilonewtonsPerCubicMeter ;

    /// <summary>pound per cubic foot (kip/ft³)</summary>
    public static DisplayUnitType PoundsForcePerCubicFoot => UnitTypeId.PoundsForcePerCubicFoot ;

    /// <summary>pound per cubic foot (kip/in³)</summary>
    public static DisplayUnitType KipsPerCubicInch => UnitTypeId.KipsPerCubicInch ;

    public static DisplayUnitType InverseDegreesFahrenheit => UnitTypeId.InverseDegreesFahrenheit ;
    public static DisplayUnitType InverseDegreesCelsius => UnitTypeId.InverseDegreesCelsius ;
    public static DisplayUnitType NewtonMetersPerMeter => UnitTypeId.NewtonMetersPerMeter ;
    public static DisplayUnitType DekanewtonMetersPerMeter => UnitTypeId.DekanewtonMetersPerMeter ;
    public static DisplayUnitType KilonewtonMetersPerMeter => UnitTypeId.KilonewtonMetersPerMeter ;
    public static DisplayUnitType MeganewtonMetersPerMeter => UnitTypeId.MeganewtonMetersPerMeter ;
    public static DisplayUnitType KipFeetPerFoot => UnitTypeId.KipFeetPerFoot ;
    public static DisplayUnitType KilogramForceMetersPerMeter => UnitTypeId.KilogramForceMetersPerMeter ;
    public static DisplayUnitType TonneForceMetersPerMeter => UnitTypeId.TonneForceMetersPerMeter ;
    public static DisplayUnitType PoundForceFeetPerFoot => UnitTypeId.PoundForceFeetPerFoot ;

    /// <summary>pound per foot hour (lb/(ft · h))</summary>
    public static DisplayUnitType PoundsMassPerFootHour => UnitTypeId.PoundsMassPerFootHour ;

    public static DisplayUnitType KipsPerInch => UnitTypeId.KipsPerInch ;

    /// <summary>pound per cubic foot (kip/ft³)</summary>
    public static DisplayUnitType KipsPerCubicFoot => UnitTypeId.KipsPerCubicFoot ;

    public static DisplayUnitType KipFeetPerDegree => UnitTypeId.KipFeetPerDegree ;
    public static DisplayUnitType KilonewtonMetersPerDegree => UnitTypeId.KilonewtonMetersPerDegree ;
    public static DisplayUnitType KipFeetPerDegreePerFoot => UnitTypeId.KipFeetPerDegreePerFoot ;
    public static DisplayUnitType KilonewtonMetersPerDegreePerMeter => UnitTypeId.KilonewtonMetersPerDegreePerMeter ;

    /// <summary>watt per square meter kelvin (W/(m² · K))</summary>
    public static DisplayUnitType WattsPerSquareMeterKelvin => UnitTypeId.WattsPerSquareMeterKelvin ;

    /// <summary>
    ///    British thermal unit[IT] per hour square foot degree Fahrenheit (Btu[IT]/(h · ft² · °F)
    /// </summary>
    public static DisplayUnitType BritishThermalUnitsPerHourSquareFootDegreeFahrenheit => UnitTypeId.BritishThermalUnitsPerHourSquareFootDegreeFahrenheit ;

    /// <summary>cubic foot per minute square foot</summary>
    public static DisplayUnitType CubicFeetPerMinuteSquareFoot => UnitTypeId.CubicFeetPerMinuteSquareFoot ;

    /// <summary>liter per second square meter</summary>
    public static DisplayUnitType LitersPerSecondSquareMeter => UnitTypeId.LitersPerSecondSquareMeter ;

    public static DisplayUnitType RatioTo10 => UnitTypeId.RatioTo10 ;
    public static DisplayUnitType RatioTo12 => UnitTypeId.RatioTo12 ;
    public static DisplayUnitType SlopeDegrees => UnitTypeId.SlopeDegrees ;
    public static DisplayUnitType RiseOverInches => UnitTypeId.RiseDividedBy12Inches ;
    public static DisplayUnitType RiseDividedBy1Foot => UnitTypeId.RiseDividedBy1Foot ;
    public static DisplayUnitType RiseDividedBy1000Millimeters => UnitTypeId.RiseDividedBy1000Millimeters ;

    /// <summary>watt per cubic foot (W/m³)</summary>
    public static DisplayUnitType WattsPerCubicFoot => UnitTypeId.WattsPerCubicFoot ;

    /// <summary>watt per cubic meter (W/m³)</summary>
    public static DisplayUnitType WattsPerCubicMeter => UnitTypeId.WattsPerCubicMeter ;

    /// <summary>British thermal unit[IT] per hour square foot (Btu[IT]/(h · ft²)</summary>
    public static DisplayUnitType BritishThermalUnitsPerHourSquareFoot => UnitTypeId.BritishThermalUnitsPerHourSquareFoot ;

    /// <summary>British thermal unit[IT] per hour cubic foot (Btu[IT]/(h · ft³)</summary>
    public static DisplayUnitType BritishThermalUnitsPerHourCubicFoot => UnitTypeId.BritishThermalUnitsPerHourCubicFoot ;

    /// <summary>Ton of refrigeration (12 000 Btu[IT]/h)</summary>
    public static DisplayUnitType TonsOfRefrigeration => UnitTypeId.TonsOfRefrigeration ;

    /// <summary>cubic foot per minute cubic foot</summary>
    public static DisplayUnitType CubicFeetPerMinuteCubicFoot => UnitTypeId.CubicFeetPerMinuteCubicFoot ;

    /// <summary>liter per second cubic meter</summary>
    public static DisplayUnitType LitersPerSecondCubicMeter => UnitTypeId.LitersPerSecondCubicMeter ;

    /// <summary>cubic foot per minute ton of refrigeration</summary>
    public static DisplayUnitType CubicFeetPerMinuteTonOfRefrigeration => UnitTypeId.CubicFeetPerMinuteTonOfRefrigeration ;

    /// <summary>liter per second kilowatt</summary>
    public static DisplayUnitType LitersPerSecondKilowatt => UnitTypeId.LitersPerSecondKilowatt ;

    /// <summary>square foot per ton of refrigeration</summary>
    public static DisplayUnitType SquareFeetPerTonOfRefrigeration => UnitTypeId.SquareFeetPerTonOfRefrigeration ;

    /// <summary>square meter per kilowatt</summary>
    public static DisplayUnitType SquareMetersPerKilowatt => UnitTypeId.SquareMetersPerKilowatt ;

    public static DisplayUnitType Currency => UnitTypeId.Currency ;
    public static DisplayUnitType LumensPerWatt => UnitTypeId.LumensPerWatt ;

    /// <summary>square foot per thousand British thermal unit[IT] per hour</summary>
    public static DisplayUnitType SquareFeetPer1000BritishThermalUnitsPerHour => UnitTypeId.SquareFeetPer1000BritishThermalUnitsPerHour ;

    public static DisplayUnitType KilonewtonsPerSquareCentimeter => UnitTypeId.KilonewtonsPerSquareCentimeter ;
    public static DisplayUnitType NewtonsPerSquareMillimeter => UnitTypeId.NewtonsPerSquareMillimeter ;
    public static DisplayUnitType KilonewtonsPerSquareMillimeter => UnitTypeId.KilonewtonsPerSquareMillimeter ;
    public static DisplayUnitType RiseDividedBy120Inches => UnitTypeId.RiseDividedBy120Inches ;
    public static DisplayUnitType OneToRatio => UnitTypeId.OneToRatio ;
    public static DisplayUnitType RiseDividedBy10Feet => UnitTypeId.RiseDividedBy10Feet ;
    public static DisplayUnitType HourSquareFootDegreesFahrenheitPerBritishThermalUnit => UnitTypeId.HourSquareFootDegreesFahrenheitPerBritishThermalUnit ;
    public static DisplayUnitType SquareMeterKelvinsPerWatt => UnitTypeId.SquareMeterKelvinsPerWatt ;
    public static DisplayUnitType BritishThermalUnitsPerDegreeFahrenheit => UnitTypeId.BritishThermalUnitsPerDegreeFahrenheit ;
    public static DisplayUnitType JoulesPerKelvin => UnitTypeId.JoulesPerKelvin ;
    public static DisplayUnitType KilojoulesPerKelvin => UnitTypeId.KilojoulesPerKelvin ;

    /// <summary>kilograms (kg)</summary>
    public static DisplayUnitType Kilograms => UnitTypeId.Kilograms ;

    /// <summary>tonnes (t)</summary>
    public static DisplayUnitType Tonnes => UnitTypeId.Tonnes ;

    /// <summary>pounds (lb)</summary>
    public static DisplayUnitType PoundsMass => UnitTypeId.PoundsMass ;

    /// <summary>meters per second squared (m/s²)</summary>
    public static DisplayUnitType MetersPerSecondSquared => UnitTypeId.MetersPerSecondSquared ;

    /// <summary>kilometers per second squared (km/s²)</summary>
    public static DisplayUnitType KilometersPerSecondSquared => UnitTypeId.KilometersPerSecondSquared ;

    /// <summary>inches per second squared (in/s²)</summary>
    public static DisplayUnitType InchesPerSecondSquared => UnitTypeId.InchesPerSecondSquared ;

    /// <summary>feet per second squared (ft/s²)</summary>
    public static DisplayUnitType FeetPerSecondSquared => UnitTypeId.FeetPerSecondSquared ;

    /// <summary>miles per second squared (mi/s²)</summary>
    public static DisplayUnitType MilesPerSecondSquared => UnitTypeId.MilesPerSecondSquared ;

    /// <summary>feet to the fourth power	(ft^4)</summary>
    public static DisplayUnitType FeetToTheFourthPower => UnitTypeId.FeetToTheFourthPower ;

    /// <summary>inches to the fourth power	(in^4)</summary>
    public static DisplayUnitType InchesToTheFourthPower => UnitTypeId.InchesToTheFourthPower ;

    /// <summary>millimeters to the fourth power	(mm^4)</summary>
    public static DisplayUnitType MillimetersToTheFourthPower => UnitTypeId.MillimetersToTheFourthPower ;

    /// <summary>centimeters to the fourth power	(cm^4)</summary>
    public static DisplayUnitType CentimetersToTheFourthPower => UnitTypeId.CentimetersToTheFourthPower ;

    /// <summary>Meters to the fourth power	(m^4)</summary>
    public static DisplayUnitType MetersToTheFourthPower => UnitTypeId.MetersToTheFourthPower ;

    /// <summary>feet to the sixth power	(ft^6)</summary>
    public static DisplayUnitType FeetToTheSixthPower => UnitTypeId.FeetToTheSixthPower ;

    /// <summary>inches to the sixth power	(in^6)</summary>
    public static DisplayUnitType InchesToTheSixthPower => UnitTypeId.InchesToTheSixthPower ;

    /// <summary>millimeters to the sixth power	(mm^6)</summary>
    public static DisplayUnitType MillimetersToTheSixthPower => UnitTypeId.MillimetersToTheSixthPower ;

    /// <summary>centimeters to the sixth power	(cm^6)</summary>
    public static DisplayUnitType CentimetersToTheSixthPower => UnitTypeId.CentimetersToTheSixthPower ;

    /// <summary>meters to the sixth power	(m^6)</summary>
    public static DisplayUnitType MetersToTheSixthPower => UnitTypeId.MetersToTheSixthPower ;

    /// <summary>square feet per foot	(ft²/ft)</summary>
    public static DisplayUnitType SquareFeetPerFoot => UnitTypeId.SquareFeetPerFoot ;

    /// <summary>square inches per foot	(in²/ft)</summary>
    public static DisplayUnitType SquareInchesPerFoot => UnitTypeId.SquareInchesPerFoot ;

    /// <summary>square millimeters per meter	(mm²/m)</summary>
    public static DisplayUnitType SquareMillimetersPerMeter => UnitTypeId.SquareMillimetersPerMeter ;

    /// <summary>square centimeters per meter	(cm²/m)</summary>
    public static DisplayUnitType SquareCentimetersPerMeter => UnitTypeId.SquareCentimetersPerMeter ;

    /// <summary>square meters per meter	(m²/m)</summary>
    public static DisplayUnitType SquareMetersPerMeter => UnitTypeId.SquareMetersPerMeter ;

    /// <summary>kilograms per meter (kg/m)</summary>
    public static DisplayUnitType KilogramsPerMeter => UnitTypeId.KilogramsPerMeter ;

    /// <summary>pounds per foot (lb/ft)</summary>
    public static DisplayUnitType PoundsMassPerFoot => UnitTypeId.PoundsMassPerFoot ;

    /// <summary>radians</summary>
    public static DisplayUnitType Radians => UnitTypeId.Radians ;

    /// <summary>grads</summary>
    public static DisplayUnitType Gradians => UnitTypeId.Gradians ;

    /// <summary>radians per second</summary>
    public static DisplayUnitType RadiansPerSecond => UnitTypeId.RadiansPerSecond ;

    /// <summary>millisecond</summary>
    public static DisplayUnitType Milliseconds => UnitTypeId.Milliseconds ;

    /// <summary>second</summary>
    public static DisplayUnitType Seconds => UnitTypeId.Seconds ;

    /// <summary>minutes</summary>
    public static DisplayUnitType Minutes => UnitTypeId.Minutes ;

    /// <summary>hours</summary>
    public static DisplayUnitType Hours => UnitTypeId.Hours ;

    /// <summary>kilometers per hour</summary>
    public static DisplayUnitType KilometersPerHour => UnitTypeId.KilometersPerHour ;

    /// <summary>miles per hour</summary>
    public static DisplayUnitType MilesPerHour => UnitTypeId.MilesPerHour ;

    /// <summary>kilojoules</summary>
    public static DisplayUnitType Kilojoules => UnitTypeId.Kilojoules ;

    /// <summary>kilograms per square meter (kg/m²)</summary>
    public static DisplayUnitType KilogramsPerSquareMeter => UnitTypeId.KilogramsPerSquareMeter ;

    /// <summary>pounds per square foot (lb/ft²)</summary>
    public static DisplayUnitType PoundsMassPerSquareFoot => UnitTypeId.PoundsMassPerSquareFoot ;

    /// <summary>Watts per meter kelvin (W/(m·K))</summary>
    public static DisplayUnitType WattsPerMeterKelvin => UnitTypeId.WattsPerMeterKelvin ;

    /// <summary>Joules per gram Celsius (J/(g·°C))</summary>
    public static DisplayUnitType JoulesPerGramDegreeCelsius => UnitTypeId.JoulesPerGramDegreeCelsius ;

    /// <summary>Joules per gram (J/g)</summary>
    public static DisplayUnitType JoulesPerGram => UnitTypeId.JoulesPerGram ;

    /// <summary>Nanograms per pascal second square meter (ng/(Pa·s·m²))</summary>
    public static DisplayUnitType NanogramsPerPascalSecondSquareMeter => UnitTypeId.NanogramsPerPascalSecondSquareMeter ;

    /// <summary>Ohm meters</summary>
    public static DisplayUnitType OhmMeters => UnitTypeId.OhmMeters ;

    /// <summary>BTU per hour foot Fahrenheit (BTU/(h·ft·°F))</summary>
    public static DisplayUnitType BritishThermalUnitsPerHourFootDegreeFahrenheit => UnitTypeId.BritishThermalUnitsPerHourFootDegreeFahrenheit ;

    /// <summary>BTU per pound Fahrenheit (BTU/(lb·°F))</summary>
    public static DisplayUnitType BritishThermalUnitsPerPoundDegreeFahrenheit => UnitTypeId.BritishThermalUnitsPerPoundDegreeFahrenheit ;

    /// <summary>BTU per pound (BTU/lb)</summary>
    public static DisplayUnitType BritishThermalUnitsPerPound => UnitTypeId.BritishThermalUnitsPerPound ;

    /// <summary>Grains per hour square foot inch mercury (gr/(h·ft²·inHg))</summary>
    public static DisplayUnitType GrainsPerHourSquareFootInchMercury => UnitTypeId.GrainsPerHourSquareFootInchMercury ;

    /// <summary>Per mille or per thousand(â€°)</summary>
    public static DisplayUnitType PerMille => UnitTypeId.PerMille ;

    /// <summary>Decimeters</summary>
    public static DisplayUnitType Decimeters => UnitTypeId.Decimeters ;

    /// <summary>Joules per kilogram Celsius (J/(kg·°C))</summary>
    public static DisplayUnitType JoulesPerKilogramDegreeCelsius => UnitTypeId.JoulesPerKilogramDegreeCelsius ;

    /// <summary>Micrometers per meter Celsius (um/(m·°C))</summary>
    public static DisplayUnitType MicrometersPerMeterDegreeCelsius => UnitTypeId.MicrometersPerMeterDegreeCelsius ;

    /// <summary>Microinches per inch Fahrenheit (uin/(in·°F))</summary>
    public static DisplayUnitType MicroinchesPerInchDegreeFahrenheit => UnitTypeId.MicroinchesPerInchDegreeFahrenheit ;

    /// <summary>US tonnes (T, Tons, ST)</summary>
    public static DisplayUnitType UsTonnesMass => UnitTypeId.UsTonnesMass ;

    /// <summary>US tonnes (Tonsf, STf)</summary>
    public static DisplayUnitType UsTonnesForce => UnitTypeId.UsTonnesForce ;

    /// <summary>liters per minute (L/min)</summary>
    public static DisplayUnitType LitersPerMinute => UnitTypeId.LitersPerMinute ;

    /// <summary>degree Fahrenheit difference (delta °F)</summary>
    public static DisplayUnitType FahrenheitInterval => UnitTypeId.FahrenheitInterval ;

    /// <summary>degree Celsius difference (delta °C)</summary>
    public static DisplayUnitType CelsiusInterval => UnitTypeId.CelsiusInterval ;

    /// <summary>kelvin difference (delta K)</summary>
    public static DisplayUnitType KelvinInterval => UnitTypeId.KelvinInterval ;

    /// <summary>degree Rankine difference (delta °R)</summary>
    public static DisplayUnitType RankineInterval => UnitTypeId.RankineInterval ;
  }
}

#endif