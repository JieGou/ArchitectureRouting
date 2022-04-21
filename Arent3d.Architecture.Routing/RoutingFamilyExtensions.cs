using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;

namespace Arent3d.Architecture.Routing
{
  public enum RoutingFamilyType
  {
    [NameOnRevit( "Routing Pass Point" )]
    [FamilyCategory( BuiltInCategory.OST_MechanicalEquipment )]
    PassPoint,

    [NameOnRevit( "Routing Terminate Point" )]
    [FamilyCategory( BuiltInCategory.OST_MechanicalEquipment )]
    TerminatePoint,

    [NameOnRevit( "Routing Connector Point" )]
    [FamilyCategory( BuiltInCategory.OST_GenericModel )]
    ConnectorPoint,

    [NameOnRevit( "Routing Connector In Point" )]
    [FamilyCategory( BuiltInCategory.OST_GenericModel )]
    ConnectorInPoint,

    [NameOnRevit( "Routing Connector Out Point" )]
    [FamilyCategory( BuiltInCategory.OST_GenericModel )]
    ConnectorOutPoint,

    [NameOnRevit( "Routing Rack Guide" )]
    [FamilyCategory( BuiltInCategory.OST_GenericModel )]
    RackGuide,

    [NameOnRevit( "Routing Rack Space" )]
    [FamilyCategory( BuiltInCategory.OST_GenericModel )]
    RackSpace,

    [NameOnRevit( "Routing Envelope" )]
    [FamilyCategory( BuiltInCategory.OST_GenericModel )]
    Envelope,

    [NameOnRevit( "Routing Shaft" )]
    [FamilyCategory( BuiltInCategory.OST_GenericModel )]
    Shaft,
  }

  public enum ElectricalRoutingFamilyType
  {
    [NameOnRevit( "電線管用ファミリ_ver1.0" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorTwoSide,

    [NameOnRevit( "電線管用ファミリ(片側のみ)" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide,

    [NameOnRevit( "ダクト用湿度ｾﾝｻｰ(ロゴあり)" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalEquipment )]
    HumiditySensorForDuctWithLogo,

    [NameOnRevit( "ダクト用湿度ｾﾝｻｰ(ロゴなし)" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalEquipment )]
    HumiditySensorForDuctWithoutLogo,

    [NameOnRevit( "ダンパ操作器" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalEquipment )]
    DamperActuator,

    [NameOnRevit( "室内用湿度ｾﾝｻｰ(ロゴあり)" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalEquipment )]
    IndoorHumiditySensorWithLogo,

    [NameOnRevit( "室内用湿度ｾﾝｻｰ(ロゴなし)" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalEquipment )]
    IndoorHumiditySensorWithoutLogo,

    [NameOnRevit( "電動二方弁(ロゴあり)" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalEquipment )]
    ElectricTwoWayValveWithLogo,

    [NameOnRevit( "電動二方弁(ロゴなし)" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalEquipment )]
    ElectricTwoWayValveWithoutLogo,

    [NameOnRevit( "Cable Tray" )]
    [FamilyCategory( BuiltInCategory.OST_CableTrayFitting )]
    CableTray,

    [NameOnRevit( "Cable Tray Elbow" )]
    [FamilyCategory( BuiltInCategory.OST_CableTrayFitting )]
    CableTrayFitting,

    [NameOnRevit( "Symbol Direction Cylindrical Shaft" )]
    [FamilyCategory( BuiltInCategory.OST_DetailComponents )]
    SymbolDirectionCylindricalShaft,

    [NameOnRevit( "Arent Room" )]
    [FamilyCategory( BuiltInCategory.OST_GenericModel )]
    Room,
    
    [NameOnRevit( "Fall Mark" )]
    [FamilyCategory( BuiltInCategory.OST_GenericModel )]
    FallMark,
    
    [NameOnRevit( "Open End Point Mark" )]
    [FamilyCategory( BuiltInCategory.OST_GenericModel )]
    OpenEndPointMark,
    
    [NameOnRevit( "配電盤_version_2022" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalEquipment )]
    FromPowerConnector,
    
    [NameOnRevit( "スイッチボード_version_2022" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalEquipment )]
    ToPowerConnector,
  }

  public enum MechanicalRoutingFamilyType
  {
    [NameOnRevit( "SA_FASU(F4-150 200Φ)" )]
    [FamilyCategory( BuiltInCategory.OST_DuctAccessory )]
    FASU_F4_150_200Phi,

    [NameOnRevit( "SA_FASU(F4-150 250Φ)" )]
    [FamilyCategory( BuiltInCategory.OST_DuctAccessory )]
    FASU_F4_150_250Phi,

    [NameOnRevit( "SA_FASU(F5-150 250Φ)" )]
    [FamilyCategory( BuiltInCategory.OST_DuctAccessory )]
    FASU_F5_150_250Phi,

    [NameOnRevit( "SA_FASU(F6-150 250Φ)" )]
    [FamilyCategory( BuiltInCategory.OST_DuctAccessory )]
    FASU_F6_150_250Phi,

    [NameOnRevit( "SA_FASU(F6-150 300Φ)" )]
    [FamilyCategory( BuiltInCategory.OST_DuctAccessory )]
    FASU_F6_150_300Phi,

    [NameOnRevit( "SA_FASU(F7-150 300Φ)" )]
    [FamilyCategory( BuiltInCategory.OST_DuctAccessory )]
    FASU_F7_150_300Phi,

    [NameOnRevit( "SA_FASU(F8-150 250Φ)" )]
    [FamilyCategory( BuiltInCategory.OST_DuctAccessory )]
    FASU_F8_150_250Phi,

    [NameOnRevit( "SA_FASU(F8-150 300Φ)" )]
    [FamilyCategory( BuiltInCategory.OST_DuctAccessory )]
    FASU_F8_150_300Phi,

    [NameOnRevit( "SA_VAV" )]
    [FamilyVersion( 1 )]
    [FamilyCategory( BuiltInCategory.OST_DuctAccessory )]
    SA_VAV,
  }


  public enum ConnectorOneSideFamilyType
  {
    [NameOnRevit( "電線管用ファミリ(片側のみ)_1" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide1,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_2" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide2,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_3" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide3,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_4" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide4,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_5" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide5,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_6" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide6,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_7" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide7,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_8" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide8,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_9" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide9,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_10" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide10,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_11" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide11,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_12" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide12,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_13" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide13,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_14" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide14,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_15" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide15,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_16" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide16,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_17" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide17,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_18" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide18,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_19" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide19,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_20" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide20,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_21" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide21,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_22" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide22,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_23" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide23,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_24" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide24,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_25" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide25,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_26" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide26,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_27" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide27,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_28" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide28,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_29" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide29,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_30" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide30,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_31" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide31,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_32" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide32,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_33" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide33,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_34" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide34,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_35" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide35,

    [NameOnRevit( "電線管用ファミリ(片側のみ)_36" )]
    [FamilyCategory( BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide36
  }

  public static class RoutingFamilyExtensions
  {
    public static bool AllRoutingFamiliesAreLoaded( this Document document ) => document.AllFamiliesAreLoaded<RoutingFamilyType>() ;

    public static void MakeCertainAllRoutingFamilies( this Document document ) => document.MakeCertainAllFamilies<RoutingFamilyType>( AssetManager.GetFamilyPath, true ) ;
    public static void EraseAllRoutingFamilies( this Document document ) => document.UnloadAllFamilies<RoutingFamilyType>() ;

    public static void MakeCertainAllConnectorFamilies( this Document document ) => document.MakeCertainAllFamilies<ConnectorOneSideFamilyType>( AssetManager.GetElectricalFamilyPath, true ) ;

    public static void EraseAllConnectorFamilies( this Document document )
    {
      document.UnloadAllFamilies<ConnectorOneSideFamilyType>() ;
      var connectorFamilyIds = new List<ElementId>() ;
      var ceedStorable = document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( ceedStorable == null || ! ceedStorable.ConnectorFamilyUploadData.Any() ) return ;
      foreach ( var connectorFamilyFile in ceedStorable.ConnectorFamilyUploadData ) {
        var connectorFamilyName = connectorFamilyFile.Replace( ".rfa", "" ) ;
        if ( new FilteredElementCollector( document ).OfClass( typeof( Family ) ).SingleOrDefault( f => f.Name == connectorFamilyName ) is Family connectorFamily )
          connectorFamilyIds.Add( connectorFamily.Id ) ;
      }
        
      document.Delete( connectorFamilyIds ) ;
    } 

    public static void MakeCertainAllElectricalRoutingFamilies( this Document document ) => document.MakeCertainAllFamilies<ElectricalRoutingFamilyType>( AssetManager.GetElectricalFamilyPath, true ) ;
    public static void EraseAllElectricalRoutingFamilies( this Document document ) => document.UnloadAllFamilies<ElectricalRoutingFamilyType>() ;

    public static void MakeCertainAllMechanicalRoutingFamilies( this Document document ) => document.MakeCertainAllFamilies<MechanicalRoutingFamilyType>( AssetManager.GetMechanicalFamilyPath, true ) ;
    public static void EraseAllMechanicalRoutingFamilies( this Document document ) => document.UnloadAllFamilies<MechanicalRoutingFamilyType>() ;

    public static FamilyInstance Instantiate( this FamilySymbol symbol, XYZ position, string levelName, StructuralType structuralType )
    {
      var level = GetLevel( symbol.Document, levelName ) ;
      if ( null == level ) throw new InvalidOperationException() ;
      return symbol.Instantiate( position, level, structuralType ) ;
    }

    public static FamilyInstance Instantiate( this FamilySymbol symbol, XYZ position, Level level, StructuralType structuralType )
    {
      var document = symbol.Document ;
      if ( false == symbol.IsActive ) symbol.Activate() ;

      return document.Create.NewFamilyInstance( position, symbol, level, structuralType ) ;
    }
    public static FamilyInstance Instantiate( this FamilySymbol symbol, XYZ position, StructuralType structuralType )
    {
      var document = symbol.Document ;
      if ( false == symbol.IsActive ) symbol.Activate() ;
      return document.Create.NewFamilyInstance( position, symbol, structuralType ) ;
    }

    private static Level? GetLevel( Document document, string levelName )
    {
      return document.GetAllElements<Level>().FirstOrDefault( l => l.Name == levelName ) ;
    }
  }
}