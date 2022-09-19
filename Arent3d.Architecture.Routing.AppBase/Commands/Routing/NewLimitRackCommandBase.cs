using System ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.DB.Electrical ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class NewLimitRackCommandBase : IExternalCommand
  {
    #region Constants & read-only variables

    /// <summary>
    /// Max Distance Tolerance when find Connector Closest
    /// </summary>
    private static readonly double MaxDistanceTolerance = ( 20.0 ).MillimetersToRevitUnits() ;
    private const int MinNumberOfMultiplicity = 5 ;
    private static readonly double MinLengthOfConduit = ( 3.0 ).MetersToRevitUnits() ;
    private static readonly double CableTrayDefaultBendRadius = ( 16.0 ).MillimetersToRevitUnits() ;
    public static readonly double[] CableTrayWidthMapping = { 200.0, 300.0, 400.0, 500.0, 600.0, 800.0, 1000.0, 1200.0 } ;
    private readonly Dictionary<ElementId, List<Connector>> _elbowsToCreate = new() ;
    private readonly Dictionary<string, double> _routeLengthCache = new() ;
    private readonly Dictionary<string, double> _routeMaxWidthDictionary = new() ;
    private static readonly double WidthCableTrayDefault2D = 300d.MillimetersToRevitUnits() ;
    private const string TransactionKey = "TransactionName.Commands.Rack.CreateLimitCableRack" ;
    private static readonly string TransactionName = TransactionKey.GetAppStringByKeyOrDefault( "Create Limit Cable" ) ;

    #endregion

    #region Properties

    protected abstract AddInType GetAddInType() ;
    protected abstract bool IsCircle { get ; }
    protected abstract bool IsSelectionRange { get ; }

    #endregion

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      var uiApp = commandData.Application ;
      var app = uiApp.Application ;
      var rackMaps = new List<RackMap>() ;

      try {
        var result = document.Transaction( TransactionName, _ =>
        {
          var racks = new List<FamilyInstance>() ;
          var fittings = new List<FamilyInstance>() ;
          Dictionary<string, List<MEPCurve>> routingElementGroups ;
          if ( IsSelectionRange ) {
            List<Element> pickedObjects ;
            try {
              pickedObjects = uiDocument.Selection.PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).Where( p => p is Conduit ).ToList() ;
            }
            catch ( OperationCanceledException ) {
              return Result.Cancelled ;
            }
            if ( ! pickedObjects.Any() ) return Result.Cancelled ;

            var pickedMepCurves = new List<MEPCurve>() ;
            foreach ( var pickedObject in pickedObjects )
              if ( pickedObject is MEPCurve mepCurve )
                pickedMepCurves.Add( mepCurve ) ;

            routingElementGroups = document.CollectAllMultipliedRoutingElements( pickedMepCurves, MinNumberOfMultiplicity ) ;
          }
          else {
            routingElementGroups = document.CollectAllMultipliedRoutingElements( MinNumberOfMultiplicity ) ;
          }

          var representativeMepCurvesFromRoutingElements = routingElementGroups.SelectMany( s => s.Value ).Where( p =>
          {
            if ( p.GetSubRouteInfo() is not { } subRouteInfo ) return false;
            return p.GetRepresentativeSubRoute() == subRouteInfo ;
          } ).EnumerateAll() ;
          
          foreach ( var routingElementGroup in routingElementGroups ) {
            foreach ( var representativeMepCurve in routingElementGroup.Value ) {
              if ( representativeMepCurve?.GetSubRouteInfo() is not { } subRouteInfo || representativeMepCurve.GetRepresentativeSubRoute() != subRouteInfo ) 
                continue ;
              
              if ( ! ( GetLengthOfRoute( representativeMepCurve.GetRouteName()!, representativeMepCurvesFromRoutingElements, document ) >= MinLengthOfConduit ) )
                continue ;
            
              var representativeConduit = ( representativeMepCurve as Conduit )! ;

              var cableRackWidth = CalcCableRackWidth( document, routingElementGroup ) ;

              CreateCableRackForConduit( uiDocument, representativeConduit, cableRackWidth, racks, rackMaps ) ;
            }
          }

          foreach ( var elbow in _elbowsToCreate ) {
            CreateElbow( uiDocument, elbow.Key, elbow.Value, fittings, rackMaps ) ;
          }
          
          document.Regenerate() ;

          var newRacks = ConnectedRacks( document, rackMaps ) ;

          //insert notation for racks
          NewRackCommandBase.CreateNotationForRack( document, app, newRacks ) ;

          StoreLimitRackModels(document,rackMaps) ;

          return Result.Succeeded ;
        } ) ;

        return result ;
      }
      catch ( OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    private double CalcCableRackWidth( Document document, KeyValuePair<string, List<MEPCurve>> routingElementGroup)
    {
      double widthCable ;
      if ( _routeMaxWidthDictionary.ContainsKey( routingElementGroup.Key ) ) {
        widthCable = _routeMaxWidthDictionary[ routingElementGroup.Key ] ;
      }
      else {
        var classificationDatas = new List<ClassificationData>() ;
        var oldClassificationDatas = new Dictionary<string, List<ClassificationData>>() ;
        
        foreach ( var mepCurves in routingElementGroup.Value.GroupBy( s => s.GetRouteName() ) ) {
          var routeName = mepCurves.First().GetRouteName() ?? string.Empty ;
          if ( string.IsNullOrEmpty( routeName ) )
            continue ;

          var cds = GetClassificationDatas( document, routeName, oldClassificationDatas ) ;
          if ( !cds.Any() )
            continue ;

          cds.ForEach( x => x.Diameter += 10 ) ;
          classificationDatas.AddRange(cds) ;
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

        widthCable = ( powerCables.Count > 0 ? ( 60 + powerCables.Sum() ) * 1.2 : 0 ) + ( instrumentationCables.Count > 0 ? ( 120 + instrumentationCables.Sum() ) * 0.6 : 0 ) ;

        foreach ( var width in CableTrayWidthMapping ) {
          if ( widthCable > width )
            continue ;

          widthCable = width ;
          break ;
        }

        _routeMaxWidthDictionary.Add( routingElementGroup.Key, widthCable ) ;
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
      if(classificationDataOne is not null)
        classificationDatas.Add(classificationDataOne);
      
      var classificationDataThree = GetClassificationData( csvStorage, hiroiSetMasterModel?.MaterialCode3 ) ;
      if(classificationDataThree is not null)
        classificationDatas.Add(classificationDataThree);
      
      var classificationDataFive = GetClassificationData( csvStorage, hiroiSetMasterModel?.MaterialCode5 ) ;
      if(classificationDataFive is not null)
        classificationDatas.Add(classificationDataFive);
      
      var classificationDataSeven = GetClassificationData( csvStorage, hiroiSetMasterModel?.MaterialCode7 ) ;
      if(classificationDataSeven is not null)
        classificationDatas.Add(classificationDataSeven);
      
      if(classificationDatas.Count == 0)
        return classificationDatas ;
      
      oldClassificationDatas[ ceedCode ] = classificationDatas ;

      return classificationDatas ;
    }

    private static ClassificationData? GetClassificationData( CsvStorable csvStorable, string? materialCode )
    {
      if ( !int.TryParse( materialCode, out var value ) )
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

      if ( ! double.TryParse( wiresAndCablesModelData?.FinishedOuterDiameter, out var diameter ) )
        return null ;

      return new ClassificationData
      {
        Classification = classification!, 
        Diameter = diameter
      } ;
    }
    
    private static void StoreLimitRackModels( Document document,List<RackMap> rackMaps )
    {
      var limitRackStorable = document.GetAllStorables<LimitRackStorable>().FirstOrDefault() ?? document.GetLimitRackStorable() ;
      
      using var transaction = new SubTransaction( document ) ;
      try {
        transaction.Start() ;
        
        RemoveUnusedLimitRackModels( document, limitRackStorable ) ;

        foreach ( var limitRackCache in rackMaps) {
          var limitRackModel = new LimitRackModel( limitRackCache.RackIds, limitRackCache.RackDetailCurveIds ) ;
          limitRackStorable.LimitRackModels.Add( limitRackModel ) ;
        }

        limitRackStorable.Save() ;
        transaction.Commit() ;
      }
      catch {
        transaction.RollBack() ;
      }
    }

    private static void RemoveUnusedLimitRackModels( Document document, LimitRackStorable limitRackStorable )
    {
      var unUsesLimitRackModels = new List<LimitRackModel>() ;
      if ( ! limitRackStorable.LimitRackModels.Any() ) return ;
      foreach ( var limitRackModel in limitRackStorable.LimitRackModels ) {
        var racks = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.RackTypeElements ) ;
        
        if (limitRackModel.RackIds.Any( rackId => racks.Any( rack => rack.UniqueId == rackId ) ) ) continue;
        unUsesLimitRackModels.Add( limitRackModel ) ;
      }

      if ( ! unUsesLimitRackModels.Any() ) return ;
      
      foreach ( var limitRackModel in unUsesLimitRackModels ) {
        limitRackStorable.LimitRackModels.Remove( limitRackModel ) ;
      }

      limitRackStorable.Save() ;
    }

    private IEnumerable<FamilyInstance> ConnectedRacks( Document document,  ICollection<RackMap> rackMaps )
    {
      double tolerance = 10d.MillimetersToRevitUnits() ;
      var cableTrayWidth = WidthCableTrayDefault2D ;
      var newCableTrays = new List<FamilyInstance>() ;

      foreach ( var rackIdMap in rackMaps ) {
        var cableTrays = rackIdMap.CableTrays.Where( MEPModelOnPlan ).ToList() ;
        var fittings = rackIdMap.CableTrayFittings.Where( MEPModelOnPlan ).ToList() ;
        if ( ! cableTrays.Any() ) {
          newCableTrays.AddRange( fittings.Cast<FamilyInstance>() ) ;
          continue;
        }

        var groupCableTrays = GroupRacks( cableTrays ) ;

        var infoCableTrays = new List<(Line LocationLine, double Width)>() ;

        foreach ( var groupCableTray in groupCableTrays ) {
          var locationTempt = GetMaxLength( document, groupCableTray.Select( GetConnector ).SelectMany( x => x ).Select( x => x.Origin ).ToList() ) ;
          if ( null == locationTempt )
            continue ;

          var locationAfterIntersect = IntersectFitting( locationTempt, fittings, tolerance ) ;
          var cableTray = groupCableTray[ 0 ] ;
          newCableTrays.Add( (FamilyInstance)cableTray ) ;
          cableTray.LookupParameter( "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( document, "トレイ長さ" ) ).Set( locationAfterIntersect.Length ) ;
          cableTrayWidth = cableTray.LookupParameter( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ) ).AsDouble() ;
          infoCableTrays.Add( (locationAfterIntersect, cableTrayWidth ) ) ;
          var locationCableTray = ( cableTray.Location as LocationPoint )!.Point ;
          var pointNearest = locationAfterIntersect.GetEndPoint( 0 ).DistanceTo( locationCableTray ) < locationAfterIntersect.GetEndPoint( 1 ).DistanceTo( locationCableTray ) ? locationAfterIntersect.GetEndPoint( 0 ) : locationAfterIntersect.GetEndPoint( 1 ) ;
          ElementTransformUtils.MoveElement( document, cableTray.Id, new XYZ( pointNearest.X, pointNearest.Y, locationCableTray.Z ) - locationCableTray ) ;

          groupCableTray.RemoveAt( 0 ) ;
          RemoveRackIdWhenCombineRacksToOneInCaches( rackMaps.EnumerateAll(), groupCableTray ) ;
          document.Delete( groupCableTray.Select( x => x.UniqueId ).EnumerateAll() ) ;
        }

        if ( ! IsCircle ) {
          var newInfoCableTrays = ExtendCurves( document, infoCableTrays, fittings.Cast<FamilyInstance>().ToList() ) ;
          var curveLoops = GroupCurves( newInfoCableTrays ).Select( x => CurveLoop.CreateViaThicken( x.CurveLoop, cableTrayWidth, XYZ.BasisZ ) ) ;
          var lineStyle = GetLineStyle( document, EraseLimitRackCommandBase.BoundaryCableTrayLineStyleName, new Color( 255, 0, 255 ), 5 ).GetGraphicsStyle( GraphicsStyleType.Projection ) ;
          var detailCurveIds = CreateDetailLines( document, curveLoops, lineStyle ).EnumerateAll() ;
          rackIdMap.RackDetailCurveIds.AddRange( detailCurveIds ) ;
        }
        else {
          var curves = new List<Curve>() ;
          curves.AddRange( infoCableTrays.Select( x => x.LocationLine ) ) ;
          var fittingLocations = GeometryHelper.GetCurveFromElements( document.ActiveView, fittings ) ;
          curves.AddRange( fittingLocations.Select( x => x.Key ) ) ;
          var mergeCurves = MergeCurves( curves ) ;

          var curveLoops = mergeCurves.Select( x => CurveLoop.Create( x.ToList() ) ).Select( x => CurveLoop.CreateViaThicken( x, cableTrayWidth, XYZ.BasisZ ) ) ;
          var lineStyle = GetLineStyle( document, EraseLimitRackCommandBase.BoundaryCableTrayLineStyleName, new Color( 255, 0, 255 ), 5 ).GetGraphicsStyle( GraphicsStyleType.Projection ) ;
          var detailCurveIds = CreateDetailLines( document, curveLoops, lineStyle).EnumerateAll() ;
          rackIdMap.RackDetailCurveIds.AddRange( detailCurveIds ) ;
        }
      }

      return newCableTrays ;
    }

    /// <summary>
    /// Remove rack id in mapping collection when ConnectedRacks
    /// </summary>
    /// <param name="rackMaps"></param>
    /// <param name="racks"></param>
    private static void RemoveRackIdWhenCombineRacksToOneInCaches( IReadOnlyCollection<RackMap> rackMaps, IReadOnlyCollection<Element> racks )
    {
      var rackIdCount = racks.Count() ;
      
      for ( var i = 0; i < rackIdCount; i++ ) {
        var rack = racks.ElementAt( i ) ;
        var rackMap = rackMaps.FirstOrDefault( rm => rm.RackIds.Any( r => r == rack.UniqueId ) ) ;
        if ( rackMap != null && rackMap.CableTrays.Contains( rack ) ) rackMap.CableTrays.Remove( rack ) ;
        if ( rackMap != null && rackMap.CableTrayFittings.Contains( rack ) ) rackMap.CableTrays.Remove( rack ) ;
        rackMap?.RackIds.Remove( rack.UniqueId ) ;
      }
    }

    /// <summary>
    /// caching new rack instance by route name
    /// </summary>
    /// <param name="rackMaps"></param>
    /// <param name="rack"></param>
    /// <param name="routeElement"></param>
    /// /// <param name="isAddToCableTray"> if isAddToCableTray is true, the rack will be add to cable tray collection, else the rack will be add to cable tray fittings collection</param>
    private static void UpdateRouteNameAndRacksCaches( ICollection<RackMap> rackMaps, Element rack, Element routeElement, bool isAddToCableTray = true )
    {
      /*
       *  We need to caches rack instance by routeName because if the new rack instance is not direction with x or y,
       * then we can't map detail curve and rack side by side
       */
      var routeName = routeElement.GetRouteName()! ;

      if ( string.IsNullOrEmpty( routeName ) ) return ;

      var rackMap = rackMaps.FirstOrDefault( rm => rm.RouteName == routeName ) ;

      if ( rackMap is null ) {
        // Add new rack to cable tray collection
        var newRackMap = new RackMap( routeName ) ;
        newRackMap.RackIds.Add( rack.UniqueId ) ;
        if ( isAddToCableTray ) {
          newRackMap.CableTrays.Add( rack ) ;
        }
        // Add new rack to cable tray fitting collection
        else {
          newRackMap.CableTrayFittings.Add( rack ) ;
        }
        rackMaps.Add( newRackMap );
      }
      else {
        // Add new rack to cable tray collection
        if ( isAddToCableTray ) {
          rackMap.CableTrays.Add( rack ) ;
        }
        // Add new rack to cable tray fitting collection
        else {
          rackMap.CableTrayFittings.Add( rack ) ;
        }

        rackMap.RackIds.Add( rack.UniqueId ) ;
      }
    }
    
    private List<List<Curve>> MergeCurves( List<Curve> curves )
    {
      var curvesGroups = new List<List<Curve>>() ;
      var mergeCurves = new List<Curve>() ;

      var cloneCurves = curves.ToList() ;
      while ( cloneCurves.Count > 0 ) {
        var count = cloneCurves.Count ;

        for ( var i = cloneCurves.Count - 1 ; i >= 0 ; i-- ) {
          if ( AddCurve( cloneCurves[ i ], ref mergeCurves ) ) {
            cloneCurves.RemoveAt( i ) ;
          }
        }

        if ( count == cloneCurves.Count ) {
          curvesGroups.Add( mergeCurves ) ;
          mergeCurves = new List<Curve>() ;
        }

        if ( cloneCurves.Count == 0 ) {
          curvesGroups.Add( mergeCurves ) ;
        }
      }

      return curvesGroups ;
    }

    private bool AddCurve( Curve curve, ref List<Curve> curves )
    {
      if ( curves.Count == 0 ) {
        curves.Add( curve ) ;
        return true ;
      }

      var lc = curves.Last() ;

      if ( lc.GetEndPoint( 1 ).IsAlmostEqualTo( curve.GetEndPoint( 0 ), GeometryHelper.Tolerance ) ) {
        if ( lc is Line lf && curve is Line ls ) {
          var l = Line.CreateBound( lf.GetEndPoint( 0 ), ls.GetEndPoint( 1 ) ) ;
          curves.RemoveAt( curves.Count - 1 ) ;
          curves.Add( l ) ;
        }
        else {
          curves.Add( curve ) ;
        }

        return true ;
      }

      if ( lc.GetEndPoint( 1 ).IsAlmostEqualTo( curve.GetEndPoint( 1 ), GeometryHelper.Tolerance ) ) {
        if ( lc is Line lf && curve is Line ls ) {
          var l = Line.CreateBound( lf.GetEndPoint( 0 ), ls.GetEndPoint( 0 ) ) ;
          curves.RemoveAt( curves.Count - 1 ) ;
          curves.Add( l ) ;
        }
        else {
          curves.Add( curve.CreateReversed() ) ;
        }

        return true ;
      }

      var fc = curves.First() ;

      if ( fc.GetEndPoint( 0 ).IsAlmostEqualTo( curve.GetEndPoint( 0 ), GeometryHelper.Tolerance ) ) {
        if ( fc is Line lf && curve is Line ls ) {
          var l = Line.CreateBound( ls.GetEndPoint( 1 ), lf.GetEndPoint( 1 ) ) ;
          curves.RemoveAt( 0 ) ;
          curves.Insert( 0, l ) ;
        }
        else {
          curves.Insert( 0, curve.CreateReversed() ) ;
        }

        return true ;
      }

      if ( fc.GetEndPoint( 0 ).IsAlmostEqualTo( curve.GetEndPoint( 1 ), GeometryHelper.Tolerance ) ) {
        if ( fc is Line lf && curve is Line ls ) {
          var l = Line.CreateBound( ls.GetEndPoint( 0 ), lf.GetEndPoint( 1 ) ) ;
          curves.RemoveAt( 0 ) ;
          curves.Insert( 0, l ) ;
        }
        else {
          curves.Insert( 0, curve ) ;
        }

        return true ;
      }

      return false ;
    }

    private static IEnumerable<string> CreateDetailLines( Document document, IEnumerable<CurveLoop> curveLoops, Element lineStyle)
    {
      var category = Category.GetCategory( document, BuiltInCategory.OST_CableTrayFitting ) ;
      document.ActiveView.SetCategoryHidden( category.Id, true ) ;
      
      foreach ( var curveLoop in curveLoops ) {
        foreach ( var curve in curveLoop ) {
          var detailLine = document.Create.NewDetailCurve( document.ActiveView, curve ) ;
          detailLine.LineStyle = lineStyle ;
          yield return detailLine.UniqueId ;
        }
      }
    }

    private static Category GetLineStyle( Document document, string subCategoryName, Color color, int lineWeight )
    {
      var categories = document.Settings.Categories ;
      var category = document.Settings.Categories.get_Item( BuiltInCategory.OST_Lines ) ;
      Category subCategory ;
      if ( ! category.SubCategories.Contains( subCategoryName ) ) {
        subCategory = categories.NewSubcategory( category, subCategoryName ) ;
        subCategory.LineColor = color ;
        subCategory.SetLineWeight( lineWeight, GraphicsStyleType.Projection ) ;
      }
      else {
        subCategory = category.SubCategories.get_Item( subCategoryName ) ;
      }

      return subCategory ;
    }

    private static IEnumerable<(CurveLoop CurveLoop, double Width)> GroupCurves( IEnumerable<(Line LocationLine, double Width)> infoCableTrays )
    {
      var cloneCurves = infoCableTrays.ToList() ;
      var curveLoops = new List<(CurveLoop CurveLoop, double Width)>() ;
      // Algorithm to group interconnected curves
      while ( cloneCurves.Count > 0 ) {
        var groupCurves = new List<(Line LocationLine, double Width)> { cloneCurves[ 0 ] } ;
        cloneCurves.RemoveAt( 0 ) ;
        if ( cloneCurves.Count == 0 )
          curveLoops.Add( ( CreateCurveLoop( groupCurves.Select( x => x.LocationLine ) ), groupCurves[ 0 ].Width ) ) ;

        int count ;
        // Algorithm to connect curves in the order specified by CurveLoop
        do {
          count = groupCurves.Count ;
          for ( var i = cloneCurves.Count - 1 ; i >= 0 ; i-- ) {
            if ( groupCurves.Count == 1 ) {
              if ( groupCurves[ 0 ].LocationLine.GetEndPoint( 0 ).DistanceTo( cloneCurves[ i ].LocationLine.GetEndPoint( 0 ) ) < GeometryUtil.Tolerance ) {
                groupCurves = new List<(Line LocationLine, double Width)> { ( ( groupCurves[ 0 ].LocationLine.CreateReversed() as Line )!, groupCurves[ 0 ].Width ), cloneCurves[ i ] } ;
                cloneCurves.RemoveAt( i ) ;
              }
              else if ( groupCurves[ 0 ].LocationLine.GetEndPoint( 0 ).DistanceTo( cloneCurves[ i ].LocationLine.GetEndPoint( 1 ) ) < GeometryUtil.Tolerance ) {
                groupCurves = new List<(Line LocationLine, double Width)> { cloneCurves[ i ], groupCurves[ 0 ] } ;
                cloneCurves.RemoveAt( i ) ;
              }
              else if ( groupCurves[ 0 ].LocationLine.GetEndPoint( 1 ).DistanceTo( cloneCurves[ i ].LocationLine.GetEndPoint( 0 ) ) < GeometryUtil.Tolerance ) {
                groupCurves = new List<(Line LocationLine, double Width)> { groupCurves[ 0 ], cloneCurves[ i ] } ;
                cloneCurves.RemoveAt( i ) ;
              }
              else if ( groupCurves[ 0 ].LocationLine.GetEndPoint( 1 ).DistanceTo( cloneCurves[ i ].LocationLine.GetEndPoint( 1 ) ) < GeometryUtil.Tolerance ) {
                groupCurves = new List<(Line LocationLine, double Width)> { groupCurves[ 0 ], ( ( cloneCurves[ i ].LocationLine.CreateReversed() as Line )!, cloneCurves[ i ].Width ) } ;
                cloneCurves.RemoveAt( i ) ;
              }
            }
            else {
              if ( groupCurves.Last().LocationLine.GetEndPoint( 1 ).DistanceTo( cloneCurves[ i ].LocationLine.GetEndPoint( 0 ) ) < GeometryUtil.Tolerance ) {
                groupCurves.Add( cloneCurves[ i ] ) ;
                cloneCurves.RemoveAt( i ) ;
              }
              else if ( groupCurves.Last().LocationLine.GetEndPoint( 1 ).DistanceTo( cloneCurves[ i ].LocationLine.GetEndPoint( 1 ) ) < GeometryUtil.Tolerance ) {
                groupCurves.Add( ( ( cloneCurves[ i ].LocationLine.CreateReversed() as Line )!, cloneCurves[ i ].Width ) ) ;
                cloneCurves.RemoveAt( i ) ;
              }
              else if ( groupCurves.First().LocationLine.GetEndPoint( 0 ).DistanceTo( cloneCurves[ i ].LocationLine.GetEndPoint( 0 ) ) < GeometryUtil.Tolerance ) {
                groupCurves.Insert( 0, ( ( cloneCurves[ i ].LocationLine.CreateReversed() as Line )!, cloneCurves[ i ].Width ) ) ;
                cloneCurves.RemoveAt( i ) ;
              }
              else if ( groupCurves.First().LocationLine.GetEndPoint( 0 ).DistanceTo( cloneCurves[ i ].LocationLine.GetEndPoint( 1 ) ) < GeometryUtil.Tolerance ) {
                groupCurves.Insert( 0, cloneCurves[ i ] ) ;
                cloneCurves.RemoveAt( i ) ;
              }
            }
          }
        } while ( count != groupCurves.Count ) ;


        curveLoops.Add( ( CreateCurveLoop( groupCurves.Select( x => x.LocationLine ) ), groupCurves[ 0 ].Width ) ) ;
      }

      return curveLoops ;
    }

    private static CurveLoop CreateCurveLoop( IEnumerable<Line> curves )
    {
      var curveLoop = new CurveLoop() ;
      foreach ( var curve in curves ) {
        curveLoop.Append( curve ) ;
      }

      return curveLoop ;
    }

    private static List<(Line LocationLine, double Width)> ExtendCurves( Document document, List<( Line LocationLine, double Width )> infoCableTrays, List<FamilyInstance> fittings )
    {
      var newInfoCableTrays = new List<(Line LocationLine, double Width)>() ;
      foreach ( var infoCableTray in infoCableTrays ) {
        var points = new List<XYZ> { infoCableTray.LocationLine.GetEndPoint( 0 ), infoCableTray.LocationLine.GetEndPoint( 1 ) } ;
        var newPoints = new List<XYZ>() ;
        foreach ( var fitting in fittings ) {
          var elbow = GetLengthElbow( fitting ) ;
          if ( elbow.Length == 0 )
            continue ;

          if ( Math.Abs( points[ 0 ].DistanceTo( new XYZ( elbow.Point.X, elbow.Point.Y, points[ 0 ].Z ) ) - elbow.Length ) < GeometryUtil.Tolerance || Math.Abs( points[ 1 ].DistanceTo( new XYZ( elbow.Point.X, elbow.Point.Y, points[ 1 ].Z ) ) - elbow.Length ) < GeometryUtil.Tolerance ) {
            newPoints.Add( new XYZ( elbow.Point.X, elbow.Point.Y, points[ 0 ].Z ) ) ;
          }
        }

        points.AddRange( newPoints ) ;
        var locationLine = GetMaxLength( document, points ) ;
        if ( null == locationLine )
          continue ;

        newInfoCableTrays.Add( ( locationLine, infoCableTray.Width ) ) ;
      }

      return newInfoCableTrays ;
    }

    private static (double Length, XYZ Point) GetLengthElbow( FamilyInstance fitting )
    {
      var point = ( fitting.Location as LocationPoint )!.Point ;
      var connectors = GetConnector( fitting ) ;
      return connectors.Count == 0 ? ( 0, point ) : ( point.DistanceTo( connectors[ 0 ].Origin ), point ) ;
    }

    private static List<List<Element>> GroupRacks( IReadOnlyCollection<Element> cableTrays )
    {
      var groupRacks = new List<List<Element>>() ;
      var cloneCableTrays = cableTrays.ToList() ;
      while ( cloneCableTrays.Any() ) {
        var rack = cloneCableTrays[ 0 ] ;
        cloneCableTrays.RemoveAt( 0 ) ;
        var subRacks = new List<Element> { rack } ;

        if ( ! cloneCableTrays.Any() ) {
          groupRacks.Add( subRacks ) ;
        }
        else {
          int count ;
          do {
            count = subRacks.Count ;
            var flag = false ;

            for ( var i = 0 ; i < cloneCableTrays.Count ; i++ ) {
              foreach ( var con in GetConnector( cloneCableTrays[ i ] ) ) {
                if ( GetConnector( subRacks.Last() ).Any( c => con.Origin.DistanceTo( c.Origin ) < GeometryUtil.Tolerance ) ) {
                  subRacks.Add( cloneCableTrays[ i ] ) ;
                  cloneCableTrays.RemoveAt( i ) ;
                  flag = true ;
                }

                if ( flag )
                  break ;
              }

              if ( flag )
                break ;
            }
          } while ( count != subRacks.Count ) ;

          groupRacks.Add( subRacks ) ;
        }
      }

      return groupRacks ;
    }

    private static Line IntersectFitting( Line locationCableTray, IEnumerable<Element> fittings, double tolerance )
    {
      var pointOnLines = fittings.Select( x => GetConnector( x ).Select( y => y.Origin ) ).SelectMany( x => x ).Where( x =>
      {
        var result = locationCableTray.Project( x ) ;
        if ( null == result )
          return false ;

        return result.Distance < tolerance ;
      } ).ToList() ;

      if ( pointOnLines.Count is > 2 or 0 )
        return locationCableTray ;

      var z = locationCableTray.Origin.Z ;
      if ( pointOnLines.Count == 1 ) {
        return locationCableTray.GetEndPoint( 0 ).DistanceTo( pointOnLines[ 0 ] ) > locationCableTray.GetEndPoint( 1 ).DistanceTo( pointOnLines[ 0 ] ) ? Line.CreateBound( locationCableTray.GetEndPoint( 0 ), new XYZ( pointOnLines[ 0 ].X, pointOnLines[ 0 ].Y, z ) ) : Line.CreateBound( locationCableTray.GetEndPoint( 1 ), new XYZ( pointOnLines[ 0 ].X, pointOnLines[ 0 ].Y, z ) ) ;
      }

      return Line.CreateBound( new XYZ( pointOnLines[ 0 ].X, pointOnLines[ 0 ].Y, z ), new XYZ( pointOnLines[ 1 ].X, pointOnLines[ 1 ].Y, z ) ) ;
    }

    private static Line? GetMaxLength( Document document, IList<XYZ> points )
    {
      if ( ! points.Any() )
        return null ;

      var lines = new List<Line>() ;
      for ( var i = 0 ; i < points.Count - 1 ; i++ ) {
        for ( var j = i + 1 ; j < points.Count ; j++ ) {
          if ( points[ i ].DistanceTo( points[ j ] ) > document.Application.ShortCurveTolerance ) {
            lines.Add( Line.CreateBound( points[ i ], points[ j ] ) ) ;
          }
        }
      }

      return lines.MaxBy( x => x.Length ) ;
    }

    private static bool MEPModelOnPlan( Element element )
    {
      var connectors = GetConnector( element ) ;
      if ( connectors.Count != 2 )
        return false ;

      return Math.Abs( connectors[ 0 ].Origin.Z - connectors[ 1 ].Origin.Z ) < GeometryHelper.Tolerance ;
    }

    private static List<Connector> GetConnector( Element element )
    {
      if ( element is not FamilyInstance familyInstance ) return new List<Connector>() ;
      var connectorSet = familyInstance.MEPModel?.ConnectorManager?.Connectors ;
      return null == connectorSet ? new List<Connector>() : connectorSet.OfType<Connector>().ToList() ;

    }

    private static void SetParameter( FamilyInstance instance, string parameterName, double value )
    {
      instance.ParametersMap.get_Item( parameterName )?.Set( value ) ;
    }

    /// <summary>
    ///   Create cable rack for Conduit
    /// </summary>
    /// <param name="uiDocument"></param>
    /// <param name="conduit"></param>
    /// <param name="cableRackWidth"></param>
    /// <param name="racks"></param>
    /// <param name="rackMaps"></param>
    private void CreateCableRackForConduit( UIDocument uiDocument, Conduit conduit, double cableRackWidth, List<FamilyInstance> racks, ICollection<RackMap> rackMaps )
    {
      var document = uiDocument.Document ;

      using var transaction = new SubTransaction( document ) ;
      try {
        transaction.Start() ;
        var location = ( conduit.Location as LocationCurve )! ;
        var line = ( location.Curve as Line )! ;

        var instance = NewRackCommandBase.CreateRackForStraightConduit( uiDocument, conduit, cableRackWidth ) ;

          // check cable tray exists
          if ( NewRackCommandBase.ExistsCableTray( document, instance ) ) {
            transaction.RollBack() ;
            return ;
          }
          
          UpdateRouteNameAndRacksCaches( rackMaps, instance,conduit ) ;

        racks.Add( instance ) ;

        if ( Math.Abs(Math.Abs(line.Direction.Z) - 1) > GeometryHelper.Tolerance) {
          var elbows = conduit.GetConnectors().SelectMany( c => c.GetConnectedConnectors() ).OfEnd().Select( c => c.Owner ).OfType<FamilyInstance>() ;
          foreach ( var elbow in elbows ) {
            if ( _elbowsToCreate.ContainsKey( elbow.Id ) ) {
              _elbowsToCreate[ elbow.Id ].Add( NewRackCommandBase.GetConnectorClosestTo( instance.GetConnectors().ToList(), ( elbow.Location as LocationPoint )!.Point )! ) ;
            }
            else {
              _elbowsToCreate.Add( elbow.Id, new List<Connector>() { NewRackCommandBase.GetConnectorClosestTo( instance.GetConnectors().ToList(), ( elbow.Location as LocationPoint )!.Point )! } ) ;
            }
          }
        }

        transaction.Commit() ;
      }
      catch {
        transaction.RollBack() ;
      }
    }

    /// <summary>
    /// Create elbow for 2 cable rack
    /// </summary>
    /// <param name="uiDocument"></param>
    /// <param name="elementId"></param>
    /// <param name="connectors"></param>
    /// <param name="racks"></param>
    /// <param name="rackMaps"></param>
    private void CreateElbow( UIDocument uiDocument, ElementId elementId, List<Connector> connectors, List<FamilyInstance> racks, ICollection<RackMap> rackMaps )
    {
      var document = uiDocument.Document ;
      using var transaction = new SubTransaction( document ) ;
      try {
        transaction.Start() ;
        var conduit = document.GetElementById<FamilyInstance>( elementId )! ;

        if ( conduit.FacingOrientation.Z is 1.0 or -1.0 || conduit.HandOrientation.Z is -1.0 or 1.0 ) {
          return ;
        }

        var location = ( conduit.Location as LocationPoint )! ;
        var instance = NewRackCommandBase.CreateRackForFittingConduit( uiDocument, conduit, location, CableTrayDefaultBendRadius ) ;

        // check cable tray exists
        if ( NewRackCommandBase.ExistsCableTray( document, instance ) ) {
          transaction.RollBack() ;
          return ;
        }

        var firstCableRack = connectors.First().Owner ;
        // get cable rack width
        var firstCableRackWidth = firstCableRack.ParametersMap.get_Item( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ) ).AsDouble() ; // TODO may be must change when FamilyType change

        var secondCableRack = connectors.Last().Owner ;
        // get cable rack width
        var secondCableRackWidth = secondCableRack.ParametersMap.get_Item( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ) ).AsDouble() ; // TODO may be must change when FamilyType change

        // set cable rack length
        SetParameter( instance, "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ), firstCableRackWidth >= secondCableRackWidth ? firstCableRackWidth : secondCableRackWidth ) ; // TODO may be must change when FamilyType change

        foreach ( var connector in instance.GetConnectors() ) {
          var otherConnectors = connectors.FindAll( x => ! x.IsConnected && x.Owner.Id != connector.Owner.Id ) ;
          var connectTo = NewRackCommandBase.GetConnectorClosestTo( otherConnectors, connector.Origin, MaxDistanceTolerance ) ;
          if ( connectTo != null ) connector.ConnectTo( connectTo ) ;
        }

        UpdateRouteNameAndRacksCaches( rackMaps, instance, conduit, false ) ;

        racks.Add( instance ) ;

        transaction.Commit() ;
      }
      catch {
        transaction.RollBack() ;
      }
    }

    /// <summary>
    /// Calculate cable rack length
    /// </summary>
    /// <param name="routeName"></param>
    /// <param name="elements"></param>
    /// <param name="document"></param>
    /// <returns></returns>
    private double GetLengthOfRoute( string routeName, IEnumerable<MEPCurve> elements, Document document )
    {
      var routeNameArray = routeName.Split( '_' ) ;
      routeName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
      if ( _routeLengthCache.ContainsKey( routeName ) ) {
        return _routeLengthCache[ routeName ] ;
      }

      var routeLength = elements.Where( x =>
      {
        var rName = x.GetRouteName() ?? string.Empty ;
        if ( string.IsNullOrEmpty( rName ) ) return false ;
        var rNameArray = rName.Split( '_' ) ;
        rName = string.Join( "_", rNameArray.First(), rNameArray.ElementAt( 1 ) ) ;
        return rName == routeName ;
      } ).Sum( x => ( x as Conduit )!.ParametersMap.get_Item( "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( document, "Length" ) ).AsDouble() ) ;

      _routeLengthCache.Add( routeName, routeLength ) ;

      return routeLength ;
    }
    
    #endregion
    
    private class ClassificationData
    {
      public string Classification { get ; init ; } = string.Empty ;
      
      public double Diameter { get ; set ; }
    }
    
  }
}