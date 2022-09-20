using System ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.DB.Electrical ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.Utils ;
using Arent3d.Revit.UI ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Utility ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class NewLimitRackCommandBase : IExternalCommand
  {
    #region Constants & Variables

    /// <summary>
    /// Max Distance Tolerance when find Connector Closest
    /// </summary>
    private static readonly double MaxDistanceTolerance = 20d.MillimetersToRevitUnits() ;

    private const int MinNumberOfMultiplicity = 5 ;
    private static readonly double CableTrayDefaultBendRadius = 16d.MillimetersToRevitUnits() ;
    public static readonly double[] CableTrayWidthMapping = { 200.0, 300.0, 400.0, 500.0, 600.0, 800.0, 1000.0, 1200.0 } ;
    private readonly Dictionary<ElementId, List<Connector>> _elbowsToCreate = new() ;
    private readonly Dictionary<string, double> _routeLengthCache = new() ;
    private readonly Dictionary<string, double> _routeMaxWidthDictionary = new() ;
    private const string TransactionKey = "TransactionName.Commands.Rack.CreateLimitCableRack" ;
    private static readonly string TransactionName = TransactionKey.GetAppStringByKeyOrDefault( "Create Limit Cable" ) ;
    private static readonly double MinLengthConduit = 50d.MillimetersToRevitUnits() ;

    #endregion

    #region Properties

    protected abstract AddInType GetAddInType() ;
    protected abstract bool IsSelectionRange { get ; }

    #endregion

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      try {
        var result = uiDocument.Document.Transaction( TransactionName, _ =>
        {
          Dictionary<string, List<MEPCurve>> routingElementGroups ;

          if ( IsSelectionRange ) {
            List<Element> pickedObjects ;

            try {
              pickedObjects = uiDocument.Selection.PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).Where( p => p is Conduit ).ToList() ;
            }
            catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
              return Result.Cancelled ;
            }

            if ( ! pickedObjects.Any() )
              return Result.Cancelled ;

            var pickedMepCurves = new List<MEPCurve>() ;
            foreach ( var pickedObject in pickedObjects )
              if ( pickedObject is MEPCurve mepCurve )
                pickedMepCurves.Add( mepCurve ) ;

            routingElementGroups = uiDocument.Document.CollectAllMultipliedRoutingElements( pickedMepCurves, MinNumberOfMultiplicity ) ;
          }
          else {
            routingElementGroups = uiDocument.Document.CollectAllMultipliedRoutingElements( MinNumberOfMultiplicity ) ;
          }
          
          

          foreach ( var routingElementGroup in routingElementGroups ) {
            
            
            var representativeMepCurves = routingElementGroup.Value.Where( p =>
            {
              if ( p.GetSubRouteInfo() is not { } subRouteInfo ) return false ;
              return p.GetRepresentativeSubRoute() == subRouteInfo ;
            } ).OfType<Conduit>().EnumerateAll() ;
            
            uiDocument.Selection.SetElementIds(representativeMepCurves.Select(x => x.Id).ToList());
            break ;
          }

          // var horizontalConduits = representativeMepCurves.Where( x => ! x.IsVertical() && ( x.get_Parameter( BuiltInParameter.CURVE_ELEM_LENGTH )?.AsDouble() ?? 0 ) > MinLengthConduit ) ;
          // var verticalConduits = representativeMepCurves.Where( x => x.IsVertical() && ( x.get_Parameter( BuiltInParameter.CURVE_ELEM_LENGTH )?.AsDouble() ?? 0 ) > MinLengthConduit ) ;
          //
          // var rackss = uiDocument.Document.CreateRacksAlignToConduits( horizontalConduits, 400d.MillimetersToRevitUnits() ).OfType<FamilyInstance>() ;
          //
          // NewRackCommandBase.CreateNotationForRack( uiDocument.Document, rackss ) ;

          return Result.Succeeded ;
        } ) ;

        return result ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }


    private double CalcCableRackWidth( Document document, KeyValuePair<string, List<MEPCurve>> routingGroup )
    {
      double widthCable ;
      if ( _routeMaxWidthDictionary.ContainsKey( routingGroup.Key ) ) {
        widthCable = _routeMaxWidthDictionary[ routingGroup.Key ] ;
      }
      else {
        var classificationDatas = new List<ClassificationData>() ;
        var oldClassificationDatas = new Dictionary<string, List<ClassificationData>>() ;

        foreach ( var conduits in routingGroup.Value.GroupBy( s => s.GetRouteName() ) ) {
          var routeName = conduits.First().GetRouteName() ?? string.Empty ;
          if ( string.IsNullOrEmpty( routeName ) )
            continue ;

          var cds = GetClassificationDatas( document, routeName, oldClassificationDatas ) ;
          if ( ! cds.Any() )
            continue ;

          classificationDatas.AddRange( cds ) ;
        }

        var powerCables = new List<double>() ;
        var instrumentationCables = new List<double>() ;

        foreach ( var classificationData in classificationDatas ) {
          if ( classificationData.Classification == $"{CreateDetailTableCommandBase.SignalType.低電圧}" || classificationData.Classification == $"{CreateDetailTableCommandBase.SignalType.動力}" ) {
            powerCables.Add( classificationData.Diameter ) ;
          }
          else {
            instrumentationCables.Add( classificationData.Diameter ) ;
          }
        }

        widthCable = ( powerCables.Count > 0 ? ( 60 + powerCables.Sum( x => x + 10 ) ) * 1.2 : 0 ) + ( instrumentationCables.Count > 0 ? ( 120 + instrumentationCables.Sum( x => x + 10 ) ) * 0.6 : 0 ) ;

        foreach ( var width in CableTrayWidthMapping ) {
          if ( widthCable > width )
            continue ;

          widthCable = width ;
          break ;
        }

        _routeMaxWidthDictionary.Add( routingGroup.Key, widthCable ) ;
      }

      return widthCable ;
    }

    #region Methods

    private static List<ClassificationData> GetClassificationDatas( Document document, string routeName, Dictionary<string, List<ClassificationData>> oldClassificationDatas )
    {
      var classificationDatas = new List<ClassificationData>() ;

      var toConnector = ConduitUtil.GetConnectorOfRoute( document, routeName, false ) ;
      if ( null == toConnector )
        return classificationDatas ;

      var ceedCode = toConnector.GetPropertyString( ElectricalRoutingElementParameter.CeedCode ) ?? string.Empty ;
      if ( string.IsNullOrEmpty( ceedCode ) )
        return classificationDatas ;

      if ( ! oldClassificationDatas.ContainsKey( ceedCode ) ) {
        oldClassificationDatas.Add( ceedCode, new ClassificationData() ) ;
      }
      else {
        return oldClassificationDatas[ ceedCode ] ;
      }

      var ceedStorage = document.GetCeedStorable() ;
      var ceedModel = ceedStorage.CeedModelData.FirstOrDefault( x => $"{x.CeedSetCode}:{x.GeneralDisplayDeviceSymbol}:{x.ModelNumber}" == ceedCode ) ;
      if ( null == ceedModel )
        return classificationDatas ;

      var hasProperty = toConnector.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? value ) ;
      if ( ! hasProperty || string.IsNullOrEmpty( value ) )
        return classificationDatas ;

      var isEcoMode = bool.Parse( value ) ;

      var csvStorage = document.GetCsvStorable() ;
      var parentPartModelNumber = ( isEcoMode ? csvStorage.HiroiSetCdMasterEcoModelData : csvStorage.HiroiSetCdMasterNormalModelData ).FirstOrDefault( x => x.SetCode == ceedCode.Split( ':' ).First() )?.LengthParentPartModelNumber ;
      if ( string.IsNullOrEmpty( parentPartModelNumber ) )
        return classificationDatas ;

      var hiroiSetMasterModel = ( isEcoMode ? csvStorage.HiroiSetMasterEcoModelData : csvStorage.HiroiSetMasterNormalModelData ).FirstOrDefault( x => x.ParentPartModelNumber == parentPartModelNumber ) ;

      var classificationDataOne = GetClassificationData( csvStorage, hiroiSetMasterModel?.MaterialCode1 ) ;
      if ( classificationDataOne is not null )
        classificationDatas.Add( classificationDataOne ) ;

      var classificationDataThree = GetClassificationData( csvStorage, hiroiSetMasterModel?.MaterialCode3 ) ;
      if ( classificationDataThree is not null )
        classificationDatas.Add( classificationDataThree ) ;

      var classificationDataFive = GetClassificationData( csvStorage, hiroiSetMasterModel?.MaterialCode5 ) ;
      if ( classificationDataFive is not null )
        classificationDatas.Add( classificationDataFive ) ;

      var classificationDataSeven = GetClassificationData( csvStorage, hiroiSetMasterModel?.MaterialCode7 ) ;
      if ( classificationDataSeven is not null )
        classificationDatas.Add( classificationDataSeven ) ;

      if ( classificationDatas.Count == 0 )
        return classificationDatas ;

      oldClassificationDatas[ ceedCode ] = classificationDatas ;

      return classificationDatas ;
    }

    private static ClassificationData? GetClassificationData( CsvStorable csvStorable, string? materialCode )
    {
      if ( ! int.TryParse( materialCode, out var value ) )
        return null ;

      var wireIdentifier = PickUpViewModel.FormatRyakumeicd( csvStorable.HiroiMasterModelData.FirstOrDefault( x => x.Buzaicd == value.ToString( "D6" ) )?.Ryakumeicd ?? string.Empty ) ;
      if ( string.IsNullOrEmpty( wireIdentifier ) )
        return null ;

      var wiresAndCablesModelData = csvStorable.WiresAndCablesModelData.FirstOrDefault( x =>
      {
        var numberOfHeartsOrLogarithm = int.TryParse( x.NumberOfHeartsOrLogarithm, out var result ) && result > 0 ? x.NumberOfHeartsOrLogarithm : string.Empty ;
        var wireId = $"{x.WireType}{x.DiameterOrNominal}{( ! string.IsNullOrEmpty( numberOfHeartsOrLogarithm ) ? $"x{numberOfHeartsOrLogarithm}{x.COrP}" : string.Empty )}" ;
        return wireId == wireIdentifier ;
      } ) ;

      var classification = wiresAndCablesModelData?.Classification ;
      if ( string.IsNullOrEmpty( classification ) )
        return null ;

      return double.TryParse( wiresAndCablesModelData?.FinishedOuterDiameter, out var diameter ) ? new ClassificationData { Classification = classification!, Diameter = diameter } : null ;
    }

    #endregion

    private class ClassificationData
    {
      public string Classification { get ; init ; } = string.Empty ;

      public double Diameter { get ; init ; }
    }
  }
}