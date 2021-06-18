#if REVIT2019 || REVIT2020
using System ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  public class SpecType
  {
    public static Type NativeType => typeof( UnitType ) ;

    private readonly UnitType _value ;
    private SpecType( UnitType value ) => _value = value ;

    public static implicit operator SpecType( UnitType unitType ) => new SpecType( unitType ) ;
    public static implicit operator UnitType( SpecType specType ) => specType._value ;

    public IEnumerable<DisplayUnitType> GetValidDisplayUnits()
    {
      return UnitUtils.GetValidDisplayUnits( _value ).Select( dut => (DisplayUnitType) dut ) ;
    }

    public string GetTypeCatalogString() => UnitUtils.GetTypeCatalogString( _value ) ;
  }

  public static class SpecTypes
  {
    /// <summary>A custom unit value</summary>
    public static SpecType Custom => UnitType.UT_Custom ;
    /// <summary>Length, e.g. ft, in, m, mm</summary>
    public static SpecType Length => UnitType.UT_Length ;
    /// <summary>Area, e.g. ft², in², m², mm²</summary>
    public static SpecType Area => UnitType.UT_Area ;
    /// <summary>Volume, e.g. ft³, in³, m³, mm³</summary>
    public static SpecType Volume => UnitType.UT_Volume ;
    /// <summary>Angular measurement, e.g. radians, degrees</summary>
    public static SpecType Angle => UnitType.UT_Angle ;
    /// <summary>General format unit, appropriate for general counts or percentages</summary>
    public static SpecType Number => UnitType.UT_Number ;
    /// <summary>Sheet length</summary>
    public static SpecType SheetLength => UnitType.UT_SheetLength ;
    /// <summary>Site angle</summary>
    public static SpecType SiteAngle => UnitType.UT_SiteAngle ;
    /// <summary>Density (HVAC) e.g.	kg/m³</summary>
    public static SpecType HvacDensity => UnitType.UT_HVAC_Density ;
    /// <summary>Energy (HVAC) e.g.	(m² · kg)/s², J</summary>
    public static SpecType HvacEnergy => UnitType.UT_HVAC_Energy ;
    /// <summary>Friction (HVAC) e.g. kg/(m² · s²), Pa/m</summary>
    public static SpecType HvacFriction => UnitType.UT_HVAC_Friction ;
    /// <summary>Power (HVAC) e.g. (m² · kg)/s³, W</summary>
    public static SpecType HvacPower => UnitType.UT_HVAC_Power ;
    /// <summary>Power Density (HVAC), e.g. kg/s³, W/m²</summary>
    public static SpecType HvacPowerDensity => UnitType.UT_HVAC_Power_Density ;
    /// <summary>Pressure (HVAC) e.g. kg/(m · s²), Pa</summary>
    public static SpecType HvacPressure => UnitType.UT_HVAC_Pressure ;
    /// <summary>Temperature (HVAC) e.g. K, C, F</summary>
    public static SpecType HvacTemperature => UnitType.UT_HVAC_Temperature ;
    /// <summary>Velocity (HVAC) e.g. m/s</summary>
    public static SpecType HvacVelocity => UnitType.UT_HVAC_Velocity ;
    /// <summary>Air Flow (HVAC) e.g. m³/s</summary>
    public static SpecType AirFlow => UnitType.UT_HVAC_Airflow ;
    /// <summary>Duct Size (HVAC) e.g. mm, in</summary>
    public static SpecType DuctSize => UnitType.UT_HVAC_DuctSize ;
    /// <summary>Cross Section (HVAC) e.g. mm², in²</summary>
    public static SpecType CrossSection => UnitType.UT_HVAC_CrossSection ;
    /// <summary>Heat Gain (HVAC) e.g. (m² · kg)/s³, W</summary>
    public static SpecType HeatGain => UnitType.UT_HVAC_HeatGain ;
    /// <summary>Current (Electrical) e.g. A</summary>
    public static SpecType Current => UnitType.UT_Electrical_Current ;
    /// <summary>Electrical Potential e.g.	(m² · kg) / (s³· A), V</summary>
    public static SpecType ElectricalPotential => UnitType.UT_Electrical_Potential ;
    /// <summary>Frequency (Electrical) e.g. 1/s, Hz</summary>
    public static SpecType ElectricalFrequency => UnitType.UT_Electrical_Frequency ;
    /// <summary>Illuminance (Electrical) e.g. (cd · sr)/m², lm/m²</summary>
    public static SpecType Illuminance => UnitType.UT_Electrical_Illuminance ;
    /// <summary>Luminous Flux (Electrical) e.g. cd · sr, lm</summary>
    public static SpecType LuminousFlux => UnitType.UT_Electrical_Luminous_Flux ;
    /// <summary>Power (Electrical) e.g.	(m² · kg)/s³, W</summary>
    public static SpecType ElectricalPower => UnitType.UT_Electrical_Power ;
    /// <summary>Roughness factor (HVAC) e,g. ft, in, mm</summary>
    public static SpecType HvacRoughness => UnitType.UT_HVAC_Roughness ;
    /// <summary>Force, e.g. (kg · m)/s², N</summary>
    public static SpecType Force => UnitType.UT_Force ;
    /// <summary>Force per unit length, e.g. kg/s², N/m</summary>
    public static SpecType LinearForce => UnitType.UT_LinearForce ;
    /// <summary>Force per unit area, e.g. kg/(m · s²), N/m²</summary>
    public static SpecType AreaForce => UnitType.UT_AreaForce ;
    /// <summary>Moment, e.g. (kg · m²)/s², N · m</summary>
    public static SpecType Moment => UnitType.UT_Moment ;
    /// <summary>Force scale, e.g. m / N</summary>
    public static SpecType ForceScale => UnitType.UT_ForceScale ;
    /// <summary>Linear force scale, e.g. m² / N</summary>
    public static SpecType LinearForceScale => UnitType.UT_LinearForceScale ;
    /// <summary>Area force scale, e.g. m³ / N</summary>
    public static SpecType AreaForceScale => UnitType.UT_AreaForceScale ;
    /// <summary>Moment scale, e.g. 1 / N</summary>
    public static SpecType MomentScale => UnitType.UT_MomentScale ;
    /// <summary>Apparent Power (Electrical), e.g. (m² · kg)/s³, W</summary>
    public static SpecType ApparentPower => UnitType.UT_Electrical_Apparent_Power ;
    /// <summary>Power Density (Electrical), e.g. kg/s³, W/m²</summary>
    public static SpecType ElectricalPowerDensity => UnitType.UT_Electrical_Power_Density ;
    /// <summary>Density (Piping) e.g. kg/m³</summary>
    public static SpecType PipingDensity => UnitType.UT_Piping_Density ;
    /// <summary>Flow (Piping), e.g. m³/s</summary>
    public static SpecType Flow => UnitType.UT_Piping_Flow ;
    /// <summary>Friction (Piping), e.g. kg/(m² · s²), Pa/m</summary>
    public static SpecType PipingFriction => UnitType.UT_Piping_Friction ;
    /// <summary>Pressure (Piping), e.g. kg/(m · s²), Pa</summary>
    public static SpecType PipingPressure => UnitType.UT_Piping_Pressure ;
    /// <summary>Temperature (Piping), e.g. K</summary>
    public static SpecType PipingTemperature => UnitType.UT_Piping_Temperature ;
    /// <summary>Velocity (Piping), e.g. m/s</summary>
    public static SpecType PipingVelocity => UnitType.UT_Piping_Velocity ;
    /// <summary>Dynamic Viscosity (Piping), e.g. kg/(m · s), Pa · s</summary>
    public static SpecType PipingViscosity => UnitType.UT_Piping_Viscosity ;
    /// <summary>Pipe Size (Piping), e.g.	m</summary>
    public static SpecType PipeSize => UnitType.UT_PipeSize ;
    /// <summary>Roughness factor (Piping), e.g. ft, in, mm</summary>
    public static SpecType PipingRoughness => UnitType.UT_Piping_Roughness ;
    /// <summary>Stress, e.g. kg/(m · s²), ksi, MPa</summary>
    public static SpecType Stress => UnitType.UT_Stress ;
    /// <summary>Unit weight, e.g. N/m³</summary>
    public static SpecType UnitWeight => UnitType.UT_UnitWeight ;
    /// <summary>Thermal expansion, e.g. 1/K</summary>
    public static SpecType ThermalExpansionCoefficient => UnitType.UT_ThermalExpansion ;
    /// <summary>Linear moment, e,g. (N · m)/m, lbf / ft</summary>
    public static SpecType LinearMoment => UnitType.UT_LinearMoment ;
    /// <summary>Linear moment scale, e.g. ft/kip, m/kN</summary>
    public static SpecType LinearMomentScale => UnitType.UT_LinearMomentScale ;
    /// <summary>Point Spring Coefficient, e.g. kg/s², N/m</summary>
    public static SpecType PointSpringCoefficient => UnitType.UT_ForcePerLength ;
    /// <summary>
    ///    Rotational Point Spring Coefficient, e.g. (kg · m²)/(s² · rad), (N · m)/rad
    /// </summary>
    public static SpecType RotationalPointSpringCoefficient => UnitType.UT_ForceLengthPerAngle ;
    /// <summary>Line Spring Coefficient, e.g. kg/(m · s²), (N · m)/m²</summary>
    public static SpecType LineSpringCoefficient => UnitType.UT_LinearForcePerLength ;
    /// <summary>Rotational Line Spring Coefficient, e.g. (kg · m)/(s² · rad), N/rad</summary>
    public static SpecType RotationalLineSpringCoefficient => UnitType.UT_LinearForceLengthPerAngle ;
    /// <summary>Area Spring Coefficient, e.g.  kg/(m² · s²), N/m³</summary>
    public static SpecType AreaSpringCoefficient => UnitType.UT_AreaForcePerLength ;
    /// <summary>Pipe Volume, e.g. gallons, liters</summary>
    public static SpecType PipingVolume => UnitType.UT_Piping_Volume ;
    /// <summary>Dynamic Viscosity (HVAC), e.g. kg/(m · s), Pa · s</summary>
    public static SpecType HvacViscosity => UnitType.UT_HVAC_Viscosity ;
    /// <summary>
    ///    Coefficient of Heat Transfer (U-value) (HVAC), e.g. kg/(s³ · K), W/(m² · K)
    /// </summary>
    public static SpecType HeatTransferCoefficient => UnitType.UT_HVAC_CoefficientOfHeatTransfer ;
    /// <summary>Air Flow Density (HVAC), m³/(s · m²)</summary>
    public static SpecType AirFlowDensity => UnitType.UT_HVAC_Airflow_Density ;
    /// <summary>Slope, rise/run</summary>
    public static SpecType Slope => UnitType.UT_Slope ;
    /// <summary>Cooling load (HVAC), e.g. (m² · kg)/s³, W, kW, Btu/s, Btu/h</summary>
    public static SpecType CoolingLoad => UnitType.UT_HVAC_Cooling_Load ;
    /// <summary>
    ///    Cooling load per unit area (HVAC), e.g. kg/s³, W/m², W/ft², Btu/(h·ft²)
    /// </summary>
    public static SpecType CoolingLoadDividedByArea => UnitType.UT_HVAC_Cooling_Load_Divided_By_Area ;
    /// <summary>
    ///    Cooling load per unit volume (HVAC), e.g. kg/(s³ · m), W/m³, Btu/(h·ft³)
    /// </summary>
    public static SpecType CoolingLoadDividedByVolume => UnitType.UT_HVAC_Cooling_Load_Divided_By_Volume ;
    /// <summary>Heating load (HVAC), e.g. (m² · kg)/s³, W, kW, Btu/s, Btu/h</summary>
    public static SpecType HeatingLoad => UnitType.UT_HVAC_Heating_Load ;
    /// <summary>
    ///    Heating load per unit area (HVAC), e.g. kg/s³, W/m², W/ft², Btu/(h·ft²)
    /// </summary>
    public static SpecType HeatingLoadDividedByArea => UnitType.UT_HVAC_Heating_Load_Divided_By_Area ;
    /// <summary>
    ///    Heating load per unit volume (HVAC), e.g. kg/(s³ · m), W/m³, Btu/(h·ft³)
    /// </summary>
    public static SpecType HeatingLoadDividedByVolume => UnitType.UT_HVAC_Heating_Load_Divided_By_Volume ;
    /// <summary>
    ///    Airflow per unit volume (HVAC), e.g. m³/(s · m³), CFM/ft³, CFM/CF, L/(s·m³)
    /// </summary>
    public static SpecType AirFlowDividedByVolume => UnitType.UT_HVAC_Airflow_Divided_By_Volume ;
    /// <summary>
    ///    Airflow per unit cooling load (HVAC), e.g. (m · s²)/kg, ft²/ton, SF/ton, m²/kW
    /// </summary>
    public static SpecType AirFlowDividedByCoolingLoad => UnitType.UT_HVAC_Airflow_Divided_By_Cooling_Load ;
    /// <summary>Area per unit cooling load (HVAC), e.g.  s³/kg, ft²/ton, m²/kW</summary>
    public static SpecType AreaDividedByCoolingLoad => UnitType.UT_HVAC_Area_Divided_By_Cooling_Load ;
    /// <summary>Wire Size (Electrical), e.g.	mm, inch</summary>
    public static SpecType WireDiameter => UnitType.UT_WireSize ;
    /// <summary>Slope (HVAC)</summary>
    public static SpecType HvacSlope => UnitType.UT_HVAC_Slope ;
    /// <summary>Slope (Piping)</summary>
    public static SpecType PipingSlope => UnitType.UT_Piping_Slope ;
    /// <summary>Currency</summary>
    public static SpecType Currency => UnitType.UT_Currency ;
    /// <summary>Electrical efficacy (lighting), e.g. cd·sr·s³/(m²·kg), lm/W</summary>
    public static SpecType Efficacy => UnitType.UT_Electrical_Efficacy ;
    /// <summary>Wattage (lighting), e.g. (m² · kg)/s³, W</summary>
    public static SpecType Wattage => UnitType.UT_Electrical_Wattage ;
    /// <summary>Color temperature (lighting), e.g. K</summary>
    public static SpecType ColorTemperature => UnitType.UT_Color_Temperature ;
    /// <summary>Sheet length in decimal form, decimal inches, mm</summary>
    public static SpecType DecimalSheetLength => UnitType.UT_DecSheetLength ;
    /// <summary>Luminous Intensity (Lighting), e.g. cd, cd</summary>
    public static SpecType LuminousIntensity => UnitType.UT_Electrical_Luminous_Intensity ;
    /// <summary>Luminance (Lighting), cd/m², cd/m²</summary>
    public static SpecType Luminance => UnitType.UT_Electrical_Luminance ;
    /// <summary>Area per unit heating load (HVAC), e.g.  s³/kg, ft²/ton, m²/kW</summary>
    public static SpecType AreaDividedByHeatingLoad => UnitType.UT_HVAC_Area_Divided_By_Heating_Load ;
    /// <summary>Heating and cooling factor, percentage</summary>
    public static SpecType Factor => UnitType.UT_HVAC_Factor ;
    /// <summary>Temperature (electrical), e.g. F, C</summary>
    public static SpecType ElectricalTemperature => UnitType.UT_Electrical_Temperature ;
    /// <summary>Cable tray size (electrical), e.g. in, mm</summary>
    public static SpecType CableTraySize => UnitType.UT_Electrical_CableTraySize ;
    /// <summary>Conduit size (electrical), e.g. in, mm</summary>
    public static SpecType ConduitSize => UnitType.UT_Electrical_ConduitSize ;
    /// <summary>Structural reinforcement volume, e.g. in³, cm³</summary>
    public static SpecType ReinforcementVolume => UnitType.UT_Reinforcement_Volume ;
    /// <summary>Structural reinforcement length, e.g. mm, in, ft</summary>
    public static SpecType ReinforcementLength => UnitType.UT_Reinforcement_Length ;
    /// <summary>Electrical demand factor, percentage</summary>
    public static SpecType DemandFactor => UnitType.UT_Electrical_Demand_Factor ;
    /// <summary>Duct Insulation Thickness (HVAC), e.g. mm, in</summary>
    public static SpecType DuctInsulationThickness => UnitType.UT_HVAC_DuctInsulationThickness ;
    /// <summary>Duct Lining Thickness (HVAC), e.g. mm, in</summary>
    public static SpecType DuctLiningThickness => UnitType.UT_HVAC_DuctLiningThickness ;
    /// <summary>Pipe Insulation Thickness (Piping), e.g. mm, in</summary>
    public static SpecType PipeInsulationThickness => UnitType.UT_PipeInsulationThickness ;
    /// <summary>Thermal Resistance (HVAC), R Value, e.g. m²·K/W</summary>
    public static SpecType ThermalResistance => UnitType.UT_HVAC_ThermalResistance ;
    /// <summary>Thermal Mass (HVAC), e.g.  J/K, BTU/F</summary>
    public static SpecType ThermalMass => UnitType.UT_HVAC_ThermalMass ;
    /// <summary>Acceleration, e.g. m/s², km/s², in/s², ft/s², mi/s²</summary>
    public static SpecType Acceleration => UnitType.UT_Acceleration ;
    /// <summary>Bar Diameter, e.g. ', LF, ", m, cm, mm</summary>
    public static SpecType BarDiameter => UnitType.UT_Bar_Diameter ;
    /// <summary>Crack Width, e.g. ', LF, ", m, cm, mm</summary>
    public static SpecType CrackWidth => UnitType.UT_Crack_Width ;
    /// <summary>Displacement/Deflection, e.g. ', LF, ", m, cm, mm</summary>
    public static SpecType Displacement => UnitType.UT_Displacement_Deflection ;
    /// <summary>Energy, e.g. J, kJ, kgf-m, lb-ft, N-m</summary>
    public static SpecType Energy => UnitType.UT_Energy ;
    /// <summary>FREQUENCY, Frequency (Structural) e.g. Hz</summary>
    public static SpecType StructuralFrequency => UnitType.UT_Structural_Frequency ;
    /// <summary>Mass, e.g.  kg, lb, t</summary>
    public static SpecType Mass => UnitType.UT_Mass ;
    /// <summary>Mass per Unit Length, e.g. kg/m, lb/ft</summary>
    public static SpecType MassPerUnitLength => UnitType.UT_Mass_per_Unit_Length ;
    /// <summary>Moment of Inertia, e.g. ft^4, in^4, mm^4, cm^4, m^4</summary>
    public static SpecType MomentOfInertia => UnitType.UT_Moment_of_Inertia ;
    /// <summary>Surface Area, e.g. ft²/ft, m²/m</summary>
    public static SpecType SurfaceAreaPerUnitLength => UnitType.UT_Surface_Area ;
    /// <summary>Period, e.g. ms, s, min, h</summary>
    public static SpecType Period => UnitType.UT_Period ;
    /// <summary>Pulsation, e.g. rad/s</summary>
    public static SpecType Pulsation => UnitType.UT_Pulsation ;
    /// <summary>Reinforcement Area, e.g. SF, ft², in², mm², cm², m²</summary>
    public static SpecType ReinforcementArea => UnitType.UT_Reinforcement_Area ;
    /// <summary>
    ///    Reinforcement Area per Unit Length, e.g. ft²/ft, in²/ft, mm²/m, cm²/m, m²/m
    /// </summary>
    public static SpecType ReinforcementAreaPerUnitLength => UnitType.UT_Reinforcement_Area_per_Unit_Length ;
    /// <summary>Reinforcement Cover, e.g. ', LF, ", m, cm, mm</summary>
    public static SpecType ReinforcementCover => UnitType.UT_Reinforcement_Cover ;
    /// <summary>Reinforcement Spacing, e.g. ', LF, ", m, cm, mm</summary>
    public static SpecType ReinforcementSpacing => UnitType.UT_Reinforcement_Spacing ;
    /// <summary>Rotation, e.g. °, rad, grad</summary>
    public static SpecType Rotation => UnitType.UT_Rotation ;
    /// <summary>Section Area, e.g.  ft²/ft, in²/ft, mm²/m, cm²/m, m²/m</summary>
    public static SpecType SectionArea => UnitType.UT_Section_Area ;
    /// <summary>Section Dimension, e.g.  ', LF, ", m, cm, mm</summary>
    public static SpecType SectionDimension => UnitType.UT_Section_Dimension ;
    /// <summary>Section Modulus, e.g. ft^3, in^3, mm^3, cm^3, m^3</summary>
    public static SpecType SectionModulus => UnitType.UT_Section_Modulus ;
    /// <summary>Section Property, e.g.  ', LF, ", m, cm, mm</summary>
    public static SpecType SectionProperty => UnitType.UT_Section_Property ;
    /// <summary>Section Property, e.g. km/h, m/s, ft/min, ft/s, mph</summary>
    public static SpecType StructuralVelocity => UnitType.UT_Structural_Velocity ;
    /// <summary>Warping Constant, e.g. ft^6, in^6, mm^6, cm^6, m^6</summary>
    public static SpecType WarpingConstant => UnitType.UT_Warping_Constant ;
    /// <summary>Weight, e.g. N, daN, kN, MN, kip, kgf, Tf, lb, lbf</summary>
    public static SpecType Weight => UnitType.UT_Weight ;
    /// <summary>
    ///    Weight per Unit Length, e.g. N/m, daN/m, kN/m, MN/m, kip/ft, kgf/m, Tf/m, lb/ft, lbf/ft, kip/in
    /// </summary>
    public static SpecType WeightPerUnitLength => UnitType.UT_Weight_per_Unit_Length ;
    /// <summary>Thermal Conductivity (HVAC), e.g. W/(m·K)</summary>
    public static SpecType ThermalConductivity => UnitType.UT_HVAC_ThermalConductivity ;
    /// <summary>Specific Heat (HVAC), e.g. J/(g·°C)</summary>
    public static SpecType SpecificHeat => UnitType.UT_HVAC_SpecificHeat ;
    /// <summary>Specific Heat of Vaporization, e.g. J/g</summary>
    public static SpecType SpecificHeatOfVaporization => UnitType.UT_HVAC_SpecificHeatOfVaporization ;
    /// <summary>Permeability, e.g. ng/(Pa·s·m²)</summary>
    public static SpecType Permeability => UnitType.UT_HVAC_Permeability ;
    /// <summary>Electrical Resistivity, e.g.</summary>
    public static SpecType ElectricalResistivity => UnitType.UT_Electrical_Resistivity ;
    /// <summary>Mass Density, e.g. kg/m³, lb/ft³</summary>
    public static SpecType MassDensity => UnitType.UT_MassDensity ;
    /// <summary>Mass Per Unit Area, e.g. kg/m², lb/ft²</summary>
    public static SpecType MassPerUnitArea => UnitType.UT_MassPerUnitArea ;
    /// <summary>Length unit for pipe dimension, e.g. in, mm</summary>
    public static SpecType PipeDimension => UnitType.UT_Pipe_Dimension ;
    /// <summary>Mass, e.g.  kg, lb, t</summary>
    public static SpecType PipingMass => UnitType.UT_PipeMass ;
    /// <summary>Mass per Unit Length, e.g. kg/m, lb/ft</summary>
    public static SpecType PipeMassPerUnitLength => UnitType.UT_PipeMassPerUnitLength ;
    /// <summary>Temperature Difference (HVAC) e.g. C, F, K, R</summary>
    public static SpecType HvacTemperatureDifference => UnitType.UT_HVAC_TemperatureDifference ;
    /// <summary>Temperature Difference (Piping), e.g. C, F, K, R</summary>
    public static SpecType PipingTemperatureDifference => UnitType.UT_Piping_TemperatureDifference ;
    /// <summary>Temperature Difference (Electrical), e.g. C, F, K, R</summary>
    public static SpecType ElectricalTemperatureDifference => UnitType.UT_Electrical_TemperatureDifference ;
    /// <summary>Interval of time e.g. ms, s, min, h</summary>
    public static SpecType TimeInterval => UnitType.UT_TimeInterval ;
    /// <summary>Distance interval over time e.g.  m/h etc.</summary>
    public static SpecType Speed => UnitType.UT_Speed ;
  }
}

#endif