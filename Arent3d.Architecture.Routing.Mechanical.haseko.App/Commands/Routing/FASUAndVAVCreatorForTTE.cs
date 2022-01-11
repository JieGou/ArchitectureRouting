using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using MathLib ;
using Line = Autodesk.Revit.DB.Line ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.Routing
{
  internal class FASUAndVAVCreatorForTTE
  {
    private const string VAVDiameterParameterName = "ダクト径" ;
    private const string VAVAirflowName = "風量" ;
    private const double DistanceBetweenFASUAndVAVMillimeter = 45.0 ;

    private Document _document = null! ;
    private Dictionary<RoutingFamilyType, FASUVAVInfo> _fasuTypeToInfoDictionary = new() ;

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

    public bool IsVAVDiameterUpdateRequired( FamilyInstance vav, double airflow )
    {
      const double diameterToleranceMillimeter = 1.0 ;

      var diameterString = _fasuTypeToInfoDictionary[ SelectFASUTypeAndDiameter( airflow ).Item1 ].VAVDiameterString ;
      var diameterParam = vav.LookupParameter( VAVDiameterParameterName ) ;
      if ( ! diameterParam.HasValue ) return false ;

      double.TryParse( diameterString, out var diameter ) ;
      return Math.Abs( diameterParam.AsDouble().RevitUnitsToMillimeters() - diameter ) > diameterToleranceMillimeter ;
    }

    public bool IsVAVAirflowUpdateRequired( FamilyInstance vav, double airflow )
    {
      const double airflowToleranceCubicPerMeter = 0.1 ;

      var airflowParam = vav.LookupParameter( VAVAirflowName ) ;
      if ( airflowParam == null || airflowParam.IsReadOnly ) return true ; // 設定自体がない or 変更できない場合はOKとする
      if ( ! airflowParam.HasValue ) return false ; // 設定自体はあるが、値が入っていない場合はNG

      return Math.Abs( TTEUtil.ConvertDesignSupplyAirflowFromInternalUnits( airflowParam.AsDouble() ) - airflow ) > airflowToleranceCubicPerMeter ;
    }

    public void UpdateVAVDiameter( FamilyInstance vav, double airflow )
    {
      var diameterString = _fasuTypeToInfoDictionary[ SelectFASUTypeAndDiameter( airflow ).Item1 ].VAVDiameterString ;
      var diameterParam = vav.LookupParameter( VAVDiameterParameterName ) ;
      diameterParam.SetValueString( diameterString ) ;
    }

    public void UpdateVAVAirflow( FamilyInstance vav, double airflow )
    {
      var airflowParam = vav.LookupParameter( VAVAirflowName ) ;
      if ( airflowParam is { IsReadOnly: false } ) airflowParam.Set( TTEUtil.ConvertDesignSupplyAirflowToInternalUnits( airflow ) ) ;
    }

    public (bool Success, string ErrorMessage) Setup( Document document )
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
          return ( false, $"{fasuInstance.Name} のコネクタの流れ方向が設定されていません." ) ;
        }

        var vavDiameterParameter = vavInstance.LookupParameter( VAVDiameterParameterName ) ;
        vavDiameterParameter?.SetValueString( diameter ) ;

        var vavInConnectorHeight = 0.0 ;
        var vavOutConnectorHeight = 0.0 ;
        if ( ! GetConnectorHeight( vavInstance, FlowDirectionType.In, out vavInConnectorHeight )
             || ! GetConnectorHeight( vavInstance, FlowDirectionType.Out, out vavOutConnectorHeight ) ) {
          return ( false, "VAVのコネクタの流れ方向が設定されていません" ) ;
        }

        var vavInConnectorNormal = vavInstance.GetConnectors().First( c => c.Direction == FlowDirectionType.In ).CoordinateSystem.BasisZ.To3dDirection() ;
        _fasuTypeToInfoDictionary.Add( fasuType, new FASUVAVInfo( vavInConnectorHeight, vavOutConnectorHeight, vavInConnectorNormal, diameter, fasuInConnectorHeight, fasuInstance.GetTypeId() ) ) ;
      }

      return ( true, string.Empty ) ;
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
      UpdateVAVDiameter( vavInstance, airflow ) ;
      UpdateVAVAirflow( vavInstance, airflow ) ;

      var vavUpstreamConnectorNomralXYZ = info.VAVUpstreamConnectorNormal.ToXYZDirection() ;
      var fasuUpstreamConnectorPosition = fasuInstance.GetConnectors().First( connector => connector.Direction == FlowDirectionType.In ).Origin ;
      var vavDownstreamConnectorPosition = vavInstance.GetConnectors().First( connector => connector.Direction == FlowDirectionType.Out ).Origin ;

      var moveVAVNextToFASUVector = fasuUpstreamConnectorPosition - vavDownstreamConnectorPosition ;

      ElementTransformUtils.MoveElement( _document, vavInstance.Id, moveVAVNextToFASUVector - DistanceBetweenFASUAndVAVMillimeter.MillimetersToRevitUnits() * vavUpstreamConnectorNomralXYZ ) ;
      ElementTransformUtils.RotateElements( _document, new List<ElementId>() { fasuInstance.Id, vavInstance.Id }, Line.CreateBound( fasuPosition2d, fasuPosition2d + XYZ.BasisZ ), rotation ) ;

      return ( fasuInstance, vavInstance ) ;
    }
  }
}