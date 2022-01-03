using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Mechanical ;
using MathLib ;
using Line = Autodesk.Revit.DB.Line ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  // 高砂向け. 頂いているデータを使っているため、他では使わないこと.
  internal class TTEUtil
  {
    private static readonly List<(double diameter, double airFlow)> DiameterToAirFlow = new()
    {
      ( 150, 195 ),
      ( 200, 420 ),
      ( 250, 765 ),
      ( 300, 1240 ),
      ( 350, 1870 ),
      ( 400, 2670 ),
      ( 450, 3650 ),
      ( 500, 4820 ),
      ( 550, 6200 ),
      ( 600, 7800 ),
      ( 650, 9600 ),
      ( 700, 11700 ),
    } ;

    public static double ConvertAirflowToDiameterForTTE( double airFlow )
    {
      // TODO : 仮対応、風量が11700m3/h以上の場合はルート径が700にします。
      foreach ( var relation in DiameterToAirFlow ) {
        if ( airFlow <= relation.airFlow ) return relation.diameter ;
      }

      return DiameterToAirFlow.Last().diameter ;
    }

    private static IList<Element> GetAllSpaces( Document document )
    {
      ElementCategoryFilter filter = new(BuiltInCategory.OST_MEPSpaces) ;
      FilteredElementCollector collector = new(document) ;
      IList<Element> spaces = collector.WherePasses( filter ).WhereElementIsNotElementType().ToElements() ;
      return spaces ;
    }

    public static double? GetAirFlowOfSpace( Document document, Vector3d pointInSpace )
    {
      // TODO Spaceではなく, VAVから取得するようにする
      var spaces = GetAllSpaces( document ).OfType<Space>().ToArray() ;
      var targetSpace = spaces.FirstOrDefault( space => space.get_BoundingBox( document.ActiveView ).ToBox3d().Contains( pointInSpace, 0.0 ) ) ;

#if REVIT2019 || REVIT2020
      return targetSpace == null
        ? null
        : UnitUtils.ConvertFromInternalUnits( targetSpace.DesignSupplyAirflow, Autodesk.Revit.DB.DisplayUnitType.DUT_CUBIC_METERS_PER_HOUR ) ;
#else
      return targetSpace == null
        ? null
        : UnitUtils.ConvertFromInternalUnits( targetSpace.DesignSupplyAirflow, UnitTypeId.CubicMetersPerHour ) ;
#endif
    }

    public static double ConvertDesignSupplyAirflowFromInternalUnits( double designSupplyAirflowInternalUnits )
    {
#if REVIT2019 || REVIT2020
      return UnitUtils.ConvertFromInternalUnits( designSupplyAirflowInternalUnits, Autodesk.Revit.DB.DisplayUnitType.DUT_CUBIC_METERS_PER_HOUR ) ;
#else
      return UnitUtils.ConvertFromInternalUnits( designSupplyAirflowInternalUnits, UnitTypeId.CubicMetersPerHour ) ;
#endif
    }
  }

  internal class FASUAndVAVCreator
  {
    private const string VAVDiameterParameterName = "ダクト径" ;
    private const double DistanceBetweenFASUAndVAV = 0.25 ;

    private Document _document = null! ;
    private Dictionary<RoutingFamilyType, FASUVAVInfo> _fasuTypeToInfoDictionary = new Dictionary<RoutingFamilyType, FASUVAVInfo>() ;

    private class FASUVAVInfo
    {
      private readonly double _vavUpstreamConnectorHeight ;
      private readonly double _vavDownstreamConnectorHeight ;
      private readonly double _fasuUpstreamConnectorHeight ;

      public FASUVAVInfo( double vavUpstreamConnectorHeight, double vavDownstreamConnectorHeight, Vector3d vavUpstreamConnectorNormal, string vavDiameterString, double fasuUpstreamConnectorHeight, ElementId fasuTypeId )
      {
        _vavUpstreamConnectorHeight = vavUpstreamConnectorHeight ;
        _vavDownstreamConnectorHeight = vavDownstreamConnectorHeight ;
        VAVUpstreamConnectorNormal = vavUpstreamConnectorNormal ;
        VAVDiameterString = vavDiameterString ;
        _fasuUpstreamConnectorHeight = fasuUpstreamConnectorHeight ;
        FASUTypeId = fasuTypeId ;
      }

      public double CalcVAVHeight( double vavUpstreamConnectorHeight )
      {
        return vavUpstreamConnectorHeight - _vavUpstreamConnectorHeight ;
      }

      public double CalcFASUHeight( double vavUpstreamConnectorHeight )
      {
        return _vavDownstreamConnectorHeight - _fasuUpstreamConnectorHeight + CalcVAVHeight( vavUpstreamConnectorHeight ) ;
      }

      public double CalcRotationAroundZAxis( Vector3d expectedVAVUpstreamNormal )
      {
        return CalcRadianAngle2D( VAVUpstreamConnectorNormal, expectedVAVUpstreamNormal ) ;
      }

      public Vector3d VAVUpstreamConnectorNormal { get ; }
      public string VAVDiameterString { get ; }
      public ElementId FASUTypeId { get ; }

      private static double ConvertDegreeToRadian( double degreeAngle )
      {
        return degreeAngle * Math.PI / 180 ;
      }

      private static double CalcRadianAngle2D( Vector3d from, Vector3d to )
      {
        var degree = Vector3d.SignedAngle( from, to, new Vector3d( 0, 0, 1 ) ) ;
        if ( degree != 0 ) return ConvertDegreeToRadian( degree ) ;
        return from == to ? 0 : Math.PI ;
      }
    }

    private static IEnumerable<(RoutingFamilyType, string UpstreamDiameter)> GetFASUTypesAndDiameters()
    {
      // PoC では F8 の2つのみ対象とする

      // yield return (RoutingFamilyType.FASU_F4_150_200Phi, "200") ;
      // yield return (RoutingFamilyType.FASU_F4_150_250Phi, "250") ;
      // yield return (RoutingFamilyType.FASU_F5_150_250Phi, "250") ;
      // yield return (RoutingFamilyType.FASU_F6_150_250Phi, "250") ;
      // yield return (RoutingFamilyType.FASU_F6_150_300Phi, "300") ;
      // yield return (RoutingFamilyType.FASU_F7_150_300Phi, "300") ;
      yield return ( RoutingFamilyType.FASU_F8_150_250Phi, "250" ) ;
      yield return ( RoutingFamilyType.FASU_F8_150_300Phi, "300" ) ;
    }

    public static (RoutingFamilyType, string UpstreamDiameter) SelectFASUTypeAndDiameter( double airflow )
    {
      if ( airflow <= 765 ) return GetFASUTypesAndDiameters().First( t => t.UpstreamDiameter == "250" ) ;
      return GetFASUTypesAndDiameters().First( t => t.UpstreamDiameter == "300" ) ;
    }

    public ElementId GetFASUTypeId( double airflow )
    {
      return _fasuTypeToInfoDictionary[ SelectFASUTypeAndDiameter( airflow ).Item1 ].FASUTypeId ;
    }

    public bool IsVAVDiameterAndAirflowSet( FamilyInstance vav, double airflow )
    {
      var diameterString = _fasuTypeToInfoDictionary[ SelectFASUTypeAndDiameter( airflow ).Item1 ].VAVDiameterString ;
      var param = vav.LookupParameter( "ダクト径" ) ;
      if ( ! param.HasValue ) return false ;

      return param.AsString() == diameterString ;
    }

    public void UpdateVAVDiameter( FamilyInstance vav, double airflow )
    {
      var diameterString = _fasuTypeToInfoDictionary[ SelectFASUTypeAndDiameter( airflow ).Item1 ].VAVDiameterString ;
      var param = vav.LookupParameter( "ダクト径" ) ;
      param.SetValueString( diameterString ) ;
    }
    
    public (bool Error, string ErrorMessage) Setup( Document document )
    {
      _document = document ;

      bool GetConnectorHeight( FamilyInstance fi, FlowDirectionType type, out double connectorHeight )
      {
        var targetConnector = fi.GetConnectors().FirstOrDefault( c => c.Direction == type ) ;
        if ( targetConnector != null ) {
          connectorHeight = targetConnector.Origin.Z ;
          return true ;
        }

        connectorHeight = 0 ;
        return false ;
      }

      using var tr = new Transaction( document ) ;
      tr.Start( "Check the flow direction of FASUs and VAV" ) ;

      var origin = new XYZ( 0, 0, 0 ) ;
      var dummyLevelId = ElementId.InvalidElementId ;

      var vavInstance = document.AddVAV( origin, dummyLevelId ) ;

      foreach ( var (fasuType, diameter) in GetFASUTypesAndDiameters() ) {
        var fasuInstance = document.AddFASU( fasuType, new XYZ( 0, 0, 0 ), ElementId.InvalidElementId ) ;
        var fasuInConnectorHeight = 0.0 ;
        if ( ! GetConnectorHeight( fasuInstance, FlowDirectionType.In, out fasuInConnectorHeight ) ) {
          return ( true, $"{fasuInstance.Name} のコネクタの流れ方向が設定されていません." ) ;
        }

        var vavDiameterParameter = vavInstance.LookupParameter( VAVDiameterParameterName ) ;
        vavDiameterParameter?.SetValueString( diameter ) ;

        var vavInConnectorHeight = 0.0 ;
        var vavOutConnectorHeight = 0.0 ;
        if ( ! GetConnectorHeight( vavInstance, FlowDirectionType.In, out vavInConnectorHeight )
             || ! GetConnectorHeight( vavInstance, FlowDirectionType.Out, out vavOutConnectorHeight ) ) {
          return ( true, "VAVのコネクタの流れ方向が設定されていません" ) ;
        }

        var vavInConnectorNormal = vavInstance.GetConnectors().First( c => c.Direction == FlowDirectionType.In ).CoordinateSystem.BasisZ.To3dDirection() ;
        _fasuTypeToInfoDictionary.Add( fasuType, new FASUVAVInfo( vavInConnectorHeight, vavOutConnectorHeight, vavInConnectorNormal, diameter, fasuInConnectorHeight, fasuInstance.GetTypeId() ) ) ;
      }

      return ( false, string.Empty ) ;
    }
    
    public (FamilyInstance FASU, FamilyInstance VAV) Create( XYZ fasuPosition2d, XYZ vavUpstreamDirection, double vavUpstreamHeight, double airflow, ElementId levelId )
    {
      var (type, diameter) = SelectFASUTypeAndDiameter( airflow ) ;
      var info = _fasuTypeToInfoDictionary[ type ] ;
      var rotation = info.CalcRotationAroundZAxis( vavUpstreamDirection.To3dDirection() ) ;

      var fasuPosition = new XYZ( fasuPosition2d.X, fasuPosition2d.Y, info.CalcFASUHeight( vavUpstreamHeight ) ) ;
      var fasuInstance = _document.AddFASU( type, fasuPosition, levelId ) ;

      // TODO FASUに必要な回転角もちゃんと計算する
      ElementTransformUtils.RotateElement( _document, fasuInstance.Id, Line.CreateBound( fasuPosition2d, fasuPosition2d + XYZ.BasisZ ), Math.PI / 2 ) ;

      var vavPosition = new XYZ( fasuPosition2d.X, fasuPosition2d.Y, info.CalcVAVHeight( vavUpstreamHeight ) ) ;
      var vavInstance = _document.AddVAV( vavPosition, levelId ) ;
      UpdateVAVDiameter( vavInstance, airflow );
      
      BoundingBoxXYZ fasuBox = fasuInstance.get_BoundingBox( _document.ActiveView ) ;
      BoundingBoxXYZ vavBox = vavInstance.get_BoundingBox( _document.ActiveView ) ;

      var moveVAVNextToFASUVector = 0.5 * Vector3d.Dot( fasuBox.To3dRaw().Size + vavBox.To3dRaw().Size, vavUpstreamDirection.To3dDirection() ) * vavUpstreamDirection ;

      ElementTransformUtils.MoveElement( _document, vavInstance.Id, moveVAVNextToFASUVector + DistanceBetweenFASUAndVAV * vavUpstreamDirection ) ;
      ElementTransformUtils.RotateElements( _document, new List<ElementId>() { fasuInstance.Id, vavInstance.Id }, Line.CreateBound( fasuPosition2d, fasuPosition2d + XYZ.BasisZ ), rotation ) ;

      return ( fasuInstance, vavInstance ) ;
    }
  }
}