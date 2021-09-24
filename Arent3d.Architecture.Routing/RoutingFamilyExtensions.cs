using System ;
using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;

namespace Arent3d.Architecture.Routing
{
  public enum RoutingFamilyType
  {
    [NameOnRevit( "Routing Rack Guide" )]
    [FamilyCategory( BuiltInCategory.OST_GenericModel )]
    RackGuide,

    [NameOnRevit( "Routing Pass Point" )]
    [FamilyCategory( BuiltInCategory.OST_MechanicalEquipment )]
    PassPoint,

    [NameOnRevit( "Routing Terminate Point" )]
    [FamilyCategory( BuiltInCategory.OST_MechanicalEquipment )]
    TerminatePoint,

    [NameOnRevit( "Routing Corn Point" )]
    [FamilyCategory( BuiltInCategory.OST_MechanicalEquipment )]
    CornPoint,

    [NameOnRevit( "Routing Connector Point" )]
    [FamilyCategory( BuiltInCategory.OST_GenericModel )]
    ConnectorPoint,

    [NameOnRevit( "Routing Connector In Point" )]
    [FamilyCategory( BuiltInCategory.OST_GenericModel )]
    ConnectorInPoint,

    [NameOnRevit( "Routing Connector Out Point" )]
    [FamilyCategory( BuiltInCategory.OST_GenericModel )]
    ConnectorOutPoint,

    [NameOnRevit("電線管用ファミリ")]
    [FamilyCategory(BuiltInCategory.OST_ElectricalFixtures)]
    ConnectorTwoSide,
    
    [NameOnRevit("電線管用ファミリ(片側のみ)")]
    [FamilyCategory(BuiltInCategory.OST_ElectricalFixtures)]
    ConnectorOneSide,

    [NameOnRevit("ダクト用湿度ｾﾝｻｰ(ロゴあり)")]
    [FamilyCategory(BuiltInCategory.OST_ElectricalEquipment)]
    HumiditySensorForDuctWithLogo,

    [NameOnRevit("ダクト用湿度ｾﾝｻｰ(ロゴなし)")]
    [FamilyCategory(BuiltInCategory.OST_ElectricalEquipment)]
    HumiditySensorForDuctWithoutLogo,

    [NameOnRevit("ダンパ操作器")]
    [FamilyCategory(BuiltInCategory.OST_ElectricalEquipment)]
    DamperActuator,

    [NameOnRevit("室内用湿度ｾﾝｻｰ(ロゴあり)")]
    [FamilyCategory(BuiltInCategory.OST_ElectricalEquipment)]
    IndoorHumiditySensorWithLogo,

    [NameOnRevit("室内用湿度ｾﾝｻｰ(ロゴなし)")]
    [FamilyCategory(BuiltInCategory.OST_ElectricalEquipment)]
    IndoorHumiditySensorWithoutLogo,

    [NameOnRevit("電動二方弁(ロゴあり)")]
    [FamilyCategory(BuiltInCategory.OST_ElectricalEquipment)]
    ElectricTwoWayValveWithLogo,

    [NameOnRevit("電動二方弁(ロゴなし)")]
    [FamilyCategory(BuiltInCategory.OST_ElectricalEquipment)]
    ElectricTwoWayValveWithoutLogo,

    [NameOnRevit( "Routing Envelope" )]
    [FamilyCategory( BuiltInCategory.OST_GenericModel )]
    Envelope,
    
    [NameOnRevit("Cable Tray")]
    [FamilyCategory(BuiltInCategory.OST_CableTrayFitting)]
     CableTray,
    
    [NameOnRevit("Cable Tray Fitting")]
    [FamilyCategory(BuiltInCategory.OST_CableTrayFitting)]
    CableTrayFitting,
  }

  public static class RoutingFamilyExtensions
  {
    public static bool AllRoutingFamiliesAreLoaded( this Document document ) => document.AllFamiliesAreLoaded<RoutingFamilyType>() ;

    public static void MakeCertainAllRoutingFamilies( this Document document ) => document.MakeCertainAllFamilies<RoutingFamilyType>( AssetManager.GetFamilyPath ) ;

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

    private static Level? GetLevel( Document document, string levelName )
    {
      return document.GetAllElements<Level>().FirstOrDefault( l => l.Name == levelName ) ;
    }
  }
}