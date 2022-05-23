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
    [Family( "Routing Pass Point", BuiltInCategory.OST_MechanicalEquipment )]
    PassPoint,

    [Family( "Routing Terminate Point", BuiltInCategory.OST_MechanicalEquipment )]
    TerminatePoint,

    [Family( "Routing Connector Point", BuiltInCategory.OST_GenericModel )]
    ConnectorPoint,

    [Family( "Routing Connector In Point", BuiltInCategory.OST_GenericModel )]
    ConnectorInPoint,

    [Family( "Routing Connector Out Point", BuiltInCategory.OST_GenericModel )]
    ConnectorOutPoint,

    [Family( "Routing Rack Guide", BuiltInCategory.OST_GenericModel )]
    RackGuide,

    [Family( "Routing Rack Space", BuiltInCategory.OST_GenericModel )]
    RackSpace,

    [Family( "Routing Envelope", BuiltInCategory.OST_GenericModel )]
    Envelope,

    [Family( "Routing Shaft", BuiltInCategory.OST_GenericModel )]
    Shaft,
  }

  public enum ElectricalRoutingFamilyType
  {
    [Family( "電線管用ファミリ_ver1.0", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorTwoSide,

    [Family( "電線管用ファミリ(片側のみ)", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide,

    [Family( "ダクト用湿度ｾﾝｻｰ(ロゴあり)", BuiltInCategory.OST_ElectricalEquipment )]
    HumiditySensorForDuctWithLogo,

    [Family( "ダクト用湿度ｾﾝｻｰ(ロゴなし)", BuiltInCategory.OST_ElectricalEquipment )]
    HumiditySensorForDuctWithoutLogo,

    [Family( "ダンパ操作器", BuiltInCategory.OST_ElectricalEquipment )]
    DamperActuator,

    [Family( "室内用湿度ｾﾝｻｰ(ロゴあり)", BuiltInCategory.OST_ElectricalEquipment )]
    IndoorHumiditySensorWithLogo,

    [Family( "室内用湿度ｾﾝｻｰ(ロゴなし)", BuiltInCategory.OST_ElectricalEquipment )]
    IndoorHumiditySensorWithoutLogo,

    [Family( "電動二方弁(ロゴあり)", BuiltInCategory.OST_ElectricalEquipment )]
    ElectricTwoWayValveWithLogo,

    [Family( "電動二方弁(ロゴなし)", BuiltInCategory.OST_ElectricalEquipment )]
    ElectricTwoWayValveWithoutLogo,

    [Family( "Cable Tray", BuiltInCategory.OST_CableTrayFitting )]
    CableTray,

    [Family( "Cable Tray Elbow", BuiltInCategory.OST_CableTrayFitting )]
    CableTrayFitting,

    [Family( "Arent Room", BuiltInCategory.OST_GenericModel )]
    Room,

    [Family( "Fall Mark", BuiltInCategory.OST_GenericModel )]
    FallMark,

    [Family( "Open End Point Mark", BuiltInCategory.OST_GenericModel )]
    OpenEndPointMark,

    [Family( "自動制御盤", BuiltInCategory.OST_ElectricalEquipment )]
    FromPowerEquipment,

    [Family( "信号取合い先", BuiltInCategory.OST_ElectricalEquipment )]
    ToPowerEquipment,

    [Family( "Jbox To Connector", BuiltInCategory.OST_ElectricalFixtures )]
    ToJboxConnector,
    
    [Family( "M_電線管エルボ - 鉄鋼 - Arent", BuiltInCategory.OST_ConduitFitting )]
    ArentConduitFittingType
  }

  public enum MechanicalRoutingFamilyType
  {
    [Family( "SA_FASU(F4-150 200Φ)", BuiltInCategory.OST_DuctAccessory )]
    FASU_F4_150_200Phi,

    [Family( "SA_FASU(F4-150 250Φ)", BuiltInCategory.OST_DuctAccessory )]
    FASU_F4_150_250Phi,

    [Family( "SA_FASU(F5-150 250Φ)", BuiltInCategory.OST_DuctAccessory )]
    FASU_F5_150_250Phi,

    [Family( "SA_FASU(F6-150 250Φ)", BuiltInCategory.OST_DuctAccessory )]
    FASU_F6_150_250Phi,

    [Family( "SA_FASU(F6-150 300Φ)", BuiltInCategory.OST_DuctAccessory )]
    FASU_F6_150_300Phi,

    [Family( "SA_FASU(F7-150 300Φ)", BuiltInCategory.OST_DuctAccessory )]
    FASU_F7_150_300Phi,

    [Family( "SA_FASU(F8-150 250Φ)", BuiltInCategory.OST_DuctAccessory )]
    FASU_F8_150_250Phi,

    [Family( "SA_FASU(F8-150 300Φ)", BuiltInCategory.OST_DuctAccessory )]
    FASU_F8_150_300Phi,

    [Family( "SA_VAV", BuiltInCategory.OST_DuctAccessory, 1 )]
    SA_VAV,
  }


  public enum ConnectorOneSideFamilyType
  {
    [Family( "電線管用ファミリ(片側のみ)_1", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide1,

    [Family( "電線管用ファミリ(片側のみ)_2", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide2,

    [Family( "電線管用ファミリ(片側のみ)_3", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide3,

    [Family( "電線管用ファミリ(片側のみ)_4", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide4,

    [Family( "電線管用ファミリ(片側のみ)_5", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide5,

    [Family( "電線管用ファミリ(片側のみ)_6", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide6,

    [Family( "電線管用ファミリ(片側のみ)_7", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide7,

    [Family( "電線管用ファミリ(片側のみ)_8", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide8,

    [Family( "電線管用ファミリ(片側のみ)_9", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide9,

    [Family( "電線管用ファミリ(片側のみ)_10", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide10,

    [Family( "電線管用ファミリ(片側のみ)_11", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide11,

    [Family( "電線管用ファミリ(片側のみ)_12", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide12,

    [Family( "電線管用ファミリ(片側のみ)_13", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide13,

    [Family( "電線管用ファミリ(片側のみ)_14", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide14,

    [Family( "電線管用ファミリ(片側のみ)_15", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide15,

    [Family( "電線管用ファミリ(片側のみ)_16", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide16,

    [Family( "電線管用ファミリ(片側のみ)_17", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide17,

    [Family( "電線管用ファミリ(片側のみ)_18", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide18,

    [Family( "電線管用ファミリ(片側のみ)_19", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide19,

    [Family( "電線管用ファミリ(片側のみ)_20", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide20,

    [Family( "電線管用ファミリ(片側のみ)_21", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide21,

    [Family( "電線管用ファミリ(片側のみ)_22", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide22,

    [Family( "電線管用ファミリ(片側のみ)_23", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide23,

    [Family( "電線管用ファミリ(片側のみ)_24", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide24,

    [Family( "電線管用ファミリ(片側のみ)_25", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide25,

    [Family( "電線管用ファミリ(片側のみ)_26", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide26,

    [Family( "電線管用ファミリ(片側のみ)_27", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide27,

    [Family( "電線管用ファミリ(片側のみ)_28", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide28,

    [Family( "電線管用ファミリ(片側のみ)_29", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide29,

    [Family( "電線管用ファミリ(片側のみ)_30", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide30,

    [Family( "電線管用ファミリ(片側のみ)_31", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide31,

    [Family( "電線管用ファミリ(片側のみ)_32", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide32,

    [Family( "電線管用ファミリ(片側のみ)_33", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide33,

    [Family( "電線管用ファミリ(片側のみ)_34", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide34,

    [Family( "電線管用ファミリ(片側のみ)_35", BuiltInCategory.OST_ElectricalFixtures )]
    ConnectorOneSide35,

    [Family( "電線管用ファミリ(片側のみ)_36", BuiltInCategory.OST_ElectricalFixtures )]
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