#if !(REVIT2019 || REVIT2020)
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  public class SpecType
  {
    private readonly ForgeTypeId _value ;
    private SpecType( ForgeTypeId value ) => _value = value ;

    public static implicit operator SpecType( ForgeTypeId forgeTypeId ) => new SpecType( forgeTypeId ) ;
    public static implicit operator ForgeTypeId( SpecType specType ) => specType._value ;
  
    public IEnumerable<DisplayUnitType> GetValidDisplayUnits()
    {
      return UnitUtils.GetValidUnits( _value ).Select( dut => (DisplayUnitType) dut ) ;
    }

    public string GetTypeCatalogString() => UnitUtils.GetTypeCatalogStringForSpec( _value ) ;
  }

  public static class SpecTypes
  {
    /// <summary>A custom unit value</summary>
    public static SpecType Custom => SpecTypeId.Custom ;
    /// <summary>Length, e.g. ft, in, m, mm</summary>
    public static SpecType Length => SpecTypeId.Length ;
    /// <summary>Area, e.g. ft², in², m², mm²</summary>
    public static SpecType Area => SpecTypeId.Area ;
    /// <summary>Volume, e.g. ft³, in³, m³, mm³</summary>
    public static SpecType Volume => SpecTypeId.Volume ;
    /// <summary>Angular measurement, e.g. radians, degrees</summary>
    public static SpecType Angle => SpecTypeId.Angle ;
    /// <summary>General format unit, appropriate for general counts or percentages</summary>
    public static SpecType Number => SpecTypeId.Number ;
    /// <summary>Sheet length</summary>
    public static SpecType SheetLength => SpecTypeId.SheetLength ;
    /// <summary>Site angle</summary>
    public static SpecType SiteAngle => SpecTypeId.SiteAngle ;
    /// <summary>Density (HVAC) e.g.	kg/m³</summary>
    public static SpecType HvacDensity => SpecTypeId.HvacDensity ;
    /// <summary>Energy (HVAC) e.g.	(m² · kg)/s², J</summary>
    public static SpecType HvacEnergy => SpecTypeId.HvacEnergy ;
    /// <summary>Friction (HVAC) e.g. kg/(m² · s²), Pa/m</summary>
    public static SpecType HvacFriction => SpecTypeId.HvacFriction ;
    /// <summary>Power (HVAC) e.g. (m² · kg)/s³, W</summary>
    public static SpecType HvacPower => SpecTypeId.HvacPower ;
    /// <summary>Power Density (HVAC), e.g. kg/s³, W/m²</summary>
    public static SpecType HvacPowerDensity => SpecTypeId.HvacPowerDensity ;
    /// <summary>Pressure (HVAC) e.g. kg/(m · s²), Pa</summary>
    public static SpecType HvacPressure => SpecTypeId.HvacPressure ;
    /// <summary>Temperature (HVAC) e.g. K, C, F</summary>
    public static SpecType HvacTemperature => SpecTypeId.HvacTemperature ;
    /// <summary>Velocity (HVAC) e.g. m/s</summary>
    public static SpecType HvacVelocity => SpecTypeId.HvacVelocity ;
    /// <summary>Air Flow (HVAC) e.g. m³/s</summary>
    public static SpecType AirFlow => SpecTypeId.AirFlow ;
    /// <summary>Duct Size (HVAC) e.g. mm, in</summary>
    public static SpecType DuctSize => SpecTypeId.DuctSize ;
    /// <summary>Cross Section (HVAC) e.g. mm², in²</summary>
    public static SpecType CrossSection => SpecTypeId.CrossSection ;
    /// <summary>Heat Gain (HVAC) e.g. (m² · kg)/s³, W</summary>
    public static SpecType HeatGain => SpecTypeId.HeatGain ;
    /// <summary>Current (Electrical) e.g. A</summary>
    public static SpecType Current => SpecTypeId.Current ;
    /// <summary>Electrical Potential e.g.	(m² · kg) / (s³· A), V</summary>
    public static SpecType ElectricalPotential => SpecTypeId.ElectricalPotential ;
    /// <summary>Frequency (Electrical) e.g. 1/s, Hz</summary>
    public static SpecType ElectricalFrequency => SpecTypeId.ElectricalFrequency ;
    /// <summary>Illuminance (Electrical) e.g. (cd · sr)/m², lm/m²</summary>
    public static SpecType Illuminance => SpecTypeId.Illuminance ;
    /// <summary>Luminous Flux (Electrical) e.g. cd · sr, lm</summary>
    public static SpecType LuminousFlux => SpecTypeId.LuminousFlux ;
    /// <summary>Power (Electrical) e.g.	(m² · kg)/s³, W</summary>
    public static SpecType ElectricalPower => SpecTypeId.ElectricalPower ;
    /// <summary>Roughness factor (HVAC) e,g. ft, in, mm</summary>
    public static SpecType HvacRoughness => SpecTypeId.HvacRoughness ;
    /// <summary>Force, e.g. (kg · m)/s², N</summary>
    public static SpecType Force => SpecTypeId.Force ;
    /// <summary>Force per unit length, e.g. kg/s², N/m</summary>
    public static SpecType LinearForce => SpecTypeId.LinearForce ;
    /// <summary>Force per unit area, e.g. kg/(m · s²), N/m²</summary>
    public static SpecType AreaForce => SpecTypeId.AreaForce ;
    /// <summary>Moment, e.g. (kg · m²)/s², N · m</summary>
    public static SpecType Moment => SpecTypeId.Moment ;
    /// <summary>Force scale, e.g. m / N</summary>
    public static SpecType ForceScale => SpecTypeId.ForceScale ;
    /// <summary>Linear force scale, e.g. m² / N</summary>
    public static SpecType LinearForceScale => SpecTypeId.LinearForceScale ;
    /// <summary>Area force scale, e.g. m³ / N</summary>
    public static SpecType AreaForceScale => SpecTypeId.AreaForceScale ;
    /// <summary>Moment scale, e.g. 1 / N</summary>
    public static SpecType MomentScale => SpecTypeId.MomentScale ;
    /// <summary>Apparent Power (Electrical), e.g. (m² · kg)/s³, W</summary>
    public static SpecType ApparentPower => SpecTypeId.ApparentPower ;
    /// <summary>Power Density (Electrical), e.g. kg/s³, W/m²</summary>
    public static SpecType ElectricalPowerDensity => SpecTypeId.ElectricalPowerDensity ;
    /// <summary>Density (Piping) e.g. kg/m³</summary>
    public static SpecType PipingDensity => SpecTypeId.PipingDensity ;
    /// <summary>Flow (Piping), e.g. m³/s</summary>
    public static SpecType Flow => SpecTypeId.Flow ;
    /// <summary>Friction (Piping), e.g. kg/(m² · s²), Pa/m</summary>
    public static SpecType PipingFriction => SpecTypeId.PipingFriction ;
    /// <summary>Pressure (Piping), e.g. kg/(m · s²), Pa</summary>
    public static SpecType PipingPressure => SpecTypeId.PipingPressure ;
    /// <summary>Temperature (Piping), e.g. K</summary>
    public static SpecType PipingTemperature => SpecTypeId.PipingTemperature ;
    /// <summary>Velocity (Piping), e.g. m/s</summary>
    public static SpecType PipingVelocity => SpecTypeId.PipingVelocity ;
    /// <summary>Dynamic Viscosity (Piping), e.g. kg/(m · s), Pa · s</summary>
    public static SpecType PipingViscosity => SpecTypeId.PipingViscosity ;
    /// <summary>Pipe Size (Piping), e.g.	m</summary>
    public static SpecType PipeSize => SpecTypeId.PipeSize ;
    /// <summary>Roughness factor (Piping), e.g. ft, in, mm</summary>
    public static SpecType PipingRoughness => SpecTypeId.PipingRoughness ;
    /// <summary>Stress, e.g. kg/(m · s²), ksi, MPa</summary>
    public static SpecType Stress => SpecTypeId.Stress ;
    /// <summary>Unit weight, e.g. N/m³</summary>
    public static SpecType UnitWeight => SpecTypeId.UnitWeight ;
    /// <summary>Thermal expansion, e.g. 1/K</summary>
    public static SpecType ThermalExpansionCoefficient => SpecTypeId.ThermalExpansionCoefficient ;
    /// <summary>Linear moment, e,g. (N · m)/m, lbf / ft</summary>
    public static SpecType LinearMoment => SpecTypeId.LinearMoment ;
    /// <summary>Linear moment scale, e.g. ft/kip, m/kN</summary>
    public static SpecType LinearMomentScale => SpecTypeId.LinearMomentScale ;
    /// <summary>Point Spring Coefficient, e.g. kg/s², N/m</summary>
    public static SpecType PointSpringCoefficient => SpecTypeId.PointSpringCoefficient ;
    /// <summary>
    ///    Rotational Point Spring Coefficient, e.g. (kg · m²)/(s² · rad), (N · m)/rad
    /// </summary>
    public static SpecType RotationalPointSpringCoefficient => SpecTypeId.RotationalPointSpringCoefficient ;
    /// <summary>Line Spring Coefficient, e.g. kg/(m · s²), (N · m)/m²</summary>
    public static SpecType LineSpringCoefficient => SpecTypeId.LineSpringCoefficient ;
    /// <summary>Rotational Line Spring Coefficient, e.g. (kg · m)/(s² · rad), N/rad</summary>
    public static SpecType RotationalLineSpringCoefficient => SpecTypeId.RotationalLineSpringCoefficient ;
    /// <summary>Area Spring Coefficient, e.g.  kg/(m² · s²), N/m³</summary>
    public static SpecType AreaSpringCoefficient => SpecTypeId.AreaSpringCoefficient ;
    /// <summary>Pipe Volume, e.g. gallons, liters</summary>
    public static SpecType PipingVolume => SpecTypeId.PipingVolume ;
    /// <summary>Dynamic Viscosity (HVAC), e.g. kg/(m · s), Pa · s</summary>
    public static SpecType HvacViscosity => SpecTypeId.HvacViscosity ;
    /// <summary>
    ///    Coefficient of Heat Transfer (U-value) (HVAC), e.g. kg/(s³ · K), W/(m² · K)
    /// </summary>
    public static SpecType HeatTransferCoefficient => SpecTypeId.HeatTransferCoefficient ;
    /// <summary>Air Flow Density (HVAC), m³/(s · m²)</summary>
    public static SpecType AirFlowDensity => SpecTypeId.AirFlowDensity ;
    /// <summary>Slope, rise/run</summary>
    public static SpecType Slope => SpecTypeId.Slope ;
    /// <summary>Cooling load (HVAC), e.g. (m² · kg)/s³, W, kW, Btu/s, Btu/h</summary>
    public static SpecType CoolingLoad => SpecTypeId.CoolingLoad ;
    /// <summary>
    ///    Cooling load per unit area (HVAC), e.g. kg/s³, W/m², W/ft², Btu/(h·ft²)
    /// </summary>
    public static SpecType CoolingLoadDividedByArea => SpecTypeId.CoolingLoadDividedByArea ;
    /// <summary>
    ///    Cooling load per unit volume (HVAC), e.g. kg/(s³ · m), W/m³, Btu/(h·ft³)
    /// </summary>
    public static SpecType CoolingLoadDividedByVolume => SpecTypeId.CoolingLoadDividedByVolume ;
    /// <summary>Heating load (HVAC), e.g. (m² · kg)/s³, W, kW, Btu/s, Btu/h</summary>
    public static SpecType HeatingLoad => SpecTypeId.HeatingLoad ;
    /// <summary>
    ///    Heating load per unit area (HVAC), e.g. kg/s³, W/m², W/ft², Btu/(h·ft²)
    /// </summary>
    public static SpecType HeatingLoadDividedByArea => SpecTypeId.HeatingLoadDividedByArea ;
    /// <summary>
    ///    Heating load per unit volume (HVAC), e.g. kg/(s³ · m), W/m³, Btu/(h·ft³)
    /// </summary>
    public static SpecType HeatingLoadDividedByVolume => SpecTypeId.HeatingLoadDividedByVolume ;
    /// <summary>
    ///    Airflow per unit volume (HVAC), e.g. m³/(s · m³), CFM/ft³, CFM/CF, L/(s·m³)
    /// </summary>
    public static SpecType AirFlowDividedByVolume => SpecTypeId.AirFlowDividedByVolume ;
    /// <summary>
    ///    Airflow per unit cooling load (HVAC), e.g. (m · s²)/kg, ft²/ton, SF/ton, m²/kW
    /// </summary>
    public static SpecType AirFlowDividedByCoolingLoad => SpecTypeId.AirFlowDividedByCoolingLoad ;
    /// <summary>Area per unit cooling load (HVAC), e.g.  s³/kg, ft²/ton, m²/kW</summary>
    public static SpecType AreaDividedByCoolingLoad => SpecTypeId.AreaDividedByCoolingLoad ;
    /// <summary>Wire Size (Electrical), e.g.	mm, inch</summary>
    public static SpecType WireDiameter => SpecTypeId.WireDiameter ;
    /// <summary>Slope (HVAC)</summary>
    public static SpecType HvacSlope => SpecTypeId.HvacSlope ;
    /// <summary>Slope (Piping)</summary>
    public static SpecType PipingSlope => SpecTypeId.PipingSlope ;
    /// <summary>Currency</summary>
    public static SpecType Currency => SpecTypeId.Currency ;
    /// <summary>Electrical efficacy (lighting), e.g. cd·sr·s³/(m²·kg), lm/W</summary>
    public static SpecType Efficacy => SpecTypeId.Efficacy ;
    /// <summary>Wattage (lighting), e.g. (m² · kg)/s³, W</summary>
    public static SpecType Wattage => SpecTypeId.Wattage ;
    /// <summary>Color temperature (lighting), e.g. K</summary>
    public static SpecType ColorTemperature => SpecTypeId.ColorTemperature ;
    /// <summary>Sheet length in decimal form, decimal inches, mm</summary>
    public static SpecType DecimalSheetLength => SpecTypeId.DecimalSheetLength ;
    /// <summary>Luminous Intensity (Lighting), e.g. cd, cd</summary>
    public static SpecType LuminousIntensity => SpecTypeId.LuminousIntensity ;
    /// <summary>Luminance (Lighting), cd/m², cd/m²</summary>
    public static SpecType Luminance => SpecTypeId.Luminance ;
    /// <summary>Area per unit heating load (HVAC), e.g.  s³/kg, ft²/ton, m²/kW</summary>
    public static SpecType AreaDividedByHeatingLoad => SpecTypeId.AreaDividedByHeatingLoad ;
    /// <summary>Heating and cooling factor, percentage</summary>
    public static SpecType Factor => SpecTypeId.Factor ;
    /// <summary>Temperature (electrical), e.g. F, C</summary>
    public static SpecType ElectricalTemperature => SpecTypeId.ElectricalTemperature ;
    /// <summary>Cable tray size (electrical), e.g. in, mm</summary>
    public static SpecType CableTraySize => SpecTypeId.CableTraySize ;
    /// <summary>Conduit size (electrical), e.g. in, mm</summary>
    public static SpecType ConduitSize => SpecTypeId.ConduitSize ;
    /// <summary>Structural reinforcement volume, e.g. in³, cm³</summary>
    public static SpecType ReinforcementVolume => SpecTypeId.ReinforcementVolume ;
    /// <summary>Structural reinforcement length, e.g. mm, in, ft</summary>
    public static SpecType ReinforcementLength => SpecTypeId.ReinforcementLength ;
    /// <summary>Electrical demand factor, percentage</summary>
    public static SpecType DemandFactor => SpecTypeId.DemandFactor ;
    /// <summary>Duct Insulation Thickness (HVAC), e.g. mm, in</summary>
    public static SpecType DuctInsulationThickness => SpecTypeId.DuctInsulationThickness ;
    /// <summary>Duct Lining Thickness (HVAC), e.g. mm, in</summary>
    public static SpecType DuctLiningThickness => SpecTypeId.DuctLiningThickness ;
    /// <summary>Pipe Insulation Thickness (Piping), e.g. mm, in</summary>
    public static SpecType PipeInsulationThickness => SpecTypeId.PipeInsulationThickness ;
    /// <summary>Thermal Resistance (HVAC), R Value, e.g. m²·K/W</summary>
    public static SpecType ThermalResistance => SpecTypeId.ThermalResistance ;
    /// <summary>Thermal Mass (HVAC), e.g.  J/K, BTU/F</summary>
    public static SpecType ThermalMass => SpecTypeId.ThermalMass ;
    /// <summary>Acceleration, e.g. m/s², km/s², in/s², ft/s², mi/s²</summary>
    public static SpecType Acceleration => SpecTypeId.Acceleration ;
    /// <summary>Bar Diameter, e.g. ', LF, ", m, cm, mm</summary>
    public static SpecType BarDiameter => SpecTypeId.BarDiameter ;
    /// <summary>Crack Width, e.g. ', LF, ", m, cm, mm</summary>
    public static SpecType CrackWidth => SpecTypeId.CrackWidth ;
    /// <summary>Displacement/Deflection, e.g. ', LF, ", m, cm, mm</summary>
    public static SpecType Displacement => SpecTypeId.Displacement ;
    /// <summary>Energy, e.g. J, kJ, kgf-m, lb-ft, N-m</summary>
    public static SpecType Energy => SpecTypeId.Energy ;
    /// <summary>FREQUENCY, Frequency (Structural) e.g. Hz</summary>
    public static SpecType StructuralFrequency => SpecTypeId.StructuralFrequency ;
    /// <summary>Mass, e.g.  kg, lb, t</summary>
    public static SpecType Mass => SpecTypeId.Mass ;
    /// <summary>Mass per Unit Length, e.g. kg/m, lb/ft</summary>
    public static SpecType MassPerUnitLength => SpecTypeId.MassPerUnitLength ;
    /// <summary>Moment of Inertia, e.g. ft^4, in^4, mm^4, cm^4, m^4</summary>
    public static SpecType MomentOfInertia => SpecTypeId.MomentOfInertia ;
    /// <summary>Surface Area, e.g. ft²/ft, m²/m</summary>
    public static SpecType SurfaceAreaPerUnitLength => SpecTypeId.SurfaceAreaPerUnitLength ;
    /// <summary>Period, e.g. ms, s, min, h</summary>
    public static SpecType Period => SpecTypeId.Period ;
    /// <summary>Pulsation, e.g. rad/s</summary>
    public static SpecType Pulsation => SpecTypeId.Pulsation ;
    /// <summary>Reinforcement Area, e.g. SF, ft², in², mm², cm², m²</summary>
    public static SpecType ReinforcementArea => SpecTypeId.ReinforcementArea ;
    /// <summary>
    ///    Reinforcement Area per Unit Length, e.g. ft²/ft, in²/ft, mm²/m, cm²/m, m²/m
    /// </summary>
    public static SpecType ReinforcementAreaPerUnitLength => SpecTypeId.ReinforcementAreaPerUnitLength ;
    /// <summary>Reinforcement Cover, e.g. ', LF, ", m, cm, mm</summary>
    public static SpecType ReinforcementCover => SpecTypeId.ReinforcementCover ;
    /// <summary>Reinforcement Spacing, e.g. ', LF, ", m, cm, mm</summary>
    public static SpecType ReinforcementSpacing => SpecTypeId.ReinforcementSpacing ;
    /// <summary>Rotation, e.g. °, rad, grad</summary>
    public static SpecType Rotation => SpecTypeId.Rotation ;
    /// <summary>Section Area, e.g.  ft²/ft, in²/ft, mm²/m, cm²/m, m²/m</summary>
    public static SpecType SectionArea => SpecTypeId.SectionArea ;
    /// <summary>Section Dimension, e.g.  ', LF, ", m, cm, mm</summary>
    public static SpecType SectionDimension => SpecTypeId.SectionDimension ;
    /// <summary>Section Modulus, e.g. ft^3, in^3, mm^3, cm^3, m^3</summary>
    public static SpecType SectionModulus => SpecTypeId.SectionModulus ;
    /// <summary>Section Property, e.g.  ', LF, ", m, cm, mm</summary>
    public static SpecType SectionProperty => SpecTypeId.SectionProperty ;
    /// <summary>Section Property, e.g. km/h, m/s, ft/min, ft/s, mph</summary>
    public static SpecType StructuralVelocity => SpecTypeId.StructuralVelocity ;
    /// <summary>Warping Constant, e.g. ft^6, in^6, mm^6, cm^6, m^6</summary>
    public static SpecType WarpingConstant => SpecTypeId.WarpingConstant ;
    /// <summary>Weight, e.g. N, daN, kN, MN, kip, kgf, Tf, lb, lbf</summary>
    public static SpecType Weight => SpecTypeId.Weight ;
    /// <summary>
    ///    Weight per Unit Length, e.g. N/m, daN/m, kN/m, MN/m, kip/ft, kgf/m, Tf/m, lb/ft, lbf/ft, kip/in
    /// </summary>
    public static SpecType WeightPerUnitLength => SpecTypeId.WeightPerUnitLength ;
    /// <summary>Thermal Conductivity (HVAC), e.g. W/(m·K)</summary>
    public static SpecType ThermalConductivity => SpecTypeId.ThermalConductivity ;
    /// <summary>Specific Heat (HVAC), e.g. J/(g·°C)</summary>
    public static SpecType SpecificHeat => SpecTypeId.SpecificHeat ;
    /// <summary>Specific Heat of Vaporization, e.g. J/g</summary>
    public static SpecType SpecificHeatOfVaporization => SpecTypeId.SpecificHeatOfVaporization ;
    /// <summary>Permeability, e.g. ng/(Pa·s·m²)</summary>
    public static SpecType Permeability => SpecTypeId.Permeability ;
    /// <summary>Electrical Resistivity, e.g.</summary>
    public static SpecType ElectricalResistivity => SpecTypeId.ElectricalResistivity ;
    /// <summary>Mass Density, e.g. kg/m³, lb/ft³</summary>
    public static SpecType MassDensity => SpecTypeId.MassDensity ;
    /// <summary>Mass Per Unit Area, e.g. kg/m², lb/ft²</summary>
    public static SpecType MassPerUnitArea => SpecTypeId.MassPerUnitArea ;
    /// <summary>Length unit for pipe dimension, e.g. in, mm</summary>
    public static SpecType PipeDimension => SpecTypeId.PipeDimension ;
    /// <summary>Mass, e.g.  kg, lb, t</summary>
    public static SpecType PipingMass => SpecTypeId.PipingMass ;
    /// <summary>Mass per Unit Length, e.g. kg/m, lb/ft</summary>
    public static SpecType PipeMassPerUnitLength => SpecTypeId.PipeMassPerUnitLength ;
    /// <summary>Temperature Difference (HVAC) e.g. C, F, K, R</summary>
    public static SpecType HvacTemperatureDifference => SpecTypeId.HvacTemperatureDifference ;
    /// <summary>Temperature Difference (Piping), e.g. C, F, K, R</summary>
    public static SpecType PipingTemperatureDifference => SpecTypeId.PipingTemperatureDifference ;
    /// <summary>Temperature Difference (Electrical), e.g. C, F, K, R</summary>
    public static SpecType ElectricalTemperatureDifference => SpecTypeId.ElectricalTemperatureDifference ;
    /// <summary>Interval of time e.g. ms, s, min, h</summary>
    public static SpecType TimeInterval => SpecTypeId.Time ;
    /// <summary>Distance interval over time e.g.  m/h etc.</summary>
    public static SpecType Speed => SpecTypeId.Speed ;
  }
}
#endif