using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using Arent3d.Architecture.Routing.CollisionTree ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.FittingSizeCalculators ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Routing ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Route generator class. This calculates route paths from routing targets and transforms revit elements.
  /// </summary>
  public class RouteGenerator : RouteGeneratorBase<AutoRoutingTarget>
  {
    private readonly Document _document ;
    private readonly IReadOnlyDictionary<SubRouteInfo, MEPSystemRouteCondition> _routeConditions ;
    private readonly List<Connector[]> _badConnectors = new() ;
    private readonly PassPointConnectorMapper _globalPassPointConnectorMapper = new() ;

    public RouteGenerator( Document document, IReadOnlyCollection<Route> routes, AutoRoutingTargetGenerator autoRoutingTargetGenerator, IFittingSizeCalculator fittingSizeCalculator, ICollisionCheckTargetCollector collector )
    {
      _document = document ;

      _routeConditions = CreateRouteConditions( document, routes, fittingSizeCalculator ) ;
      var targets = autoRoutingTargetGenerator.Create( routes, _routeConditions ) ;
      RoutingTargetGroups = targets.ToList() ;
      ErasePreviousRoutes() ; // Delete before CollisionCheckTree is built.

      CollisionCheckTree = new CollisionTree.CollisionTree( document, collector, _routeConditions ) ;
      StructureGraph = DocumentMapper.Get( document ).RackCollection ;

      Specifications.Set( DiameterProvider.Instance, PipeClearanceProvider.Instance ) ;
    }

    private static IReadOnlyDictionary<SubRouteInfo, MEPSystemRouteCondition> CreateRouteConditions( Document document, IReadOnlyCollection<Route> routes, IFittingSizeCalculator fittingSizeCalculator )
    {
      var dic = new Dictionary<SubRouteInfo, MEPSystemRouteCondition>() ;
      
      foreach ( var route in routes ) {
        foreach ( var subRoute in route.SubRoutes ) {
          var key = new SubRouteInfo( subRoute ) ;
          if ( dic.ContainsKey( key ) ) break ;  // same sub route

          var mepSystem = new RouteMEPSystem( document, subRoute ) ;

          var edgeDiameter = subRoute.GetDiameter() ;
          var spec = new MEPSystemPipeSpec( mepSystem, fittingSizeCalculator ) ;
          var routeCondition = new MEPSystemRouteCondition( spec, edgeDiameter, subRoute.AvoidType ) ;

          dic.Add( key, routeCondition ) ;
        }
      }

      return dic ;
    }

    public IReadOnlyCollection<Connector[]> GetBadConnectorSet() => _badConnectors ;

    protected override IReadOnlyList<IReadOnlyCollection<AutoRoutingTarget>> RoutingTargetGroups { get ; }

    protected override CollisionTree.CollisionTree CollisionCheckTree { get ; }

    protected override IStructureGraph StructureGraph { get ; }

    /// <summary>
    /// Erase all previous ducts and pipes in between routing targets.
    /// </summary>
    private void ErasePreviousRoutes()
    {
      EraseRoutes( _document, RoutingTargetGroups.SelectMany( group => group ).SelectMany( t => t.Routes ).Select( route => route.RouteName ), false ) ;
    }

    public static void EraseRoutes( Document document, IEnumerable<string> routeNames, bool eraseRouteStoragesAndPassPoints, bool isEraseAllRoutes = false, bool isEraseSelectedRoutes = false )
    {
      var hashSet = ( routeNames as ISet<string> ) ?? routeNames.ToHashSet() ;

      var list = document.GetAllElementsOfRoute<Element>().Where( e => e.GetRouteName() is { } routeName && hashSet.Contains( routeName ) ) ;
      if ( false == eraseRouteStoragesAndPassPoints ) {
        // do not erase pass points
        list = list.Where( p => false == ( p is FamilyInstance fi && ( fi.IsFamilyInstanceOf( RoutingFamilyType.PassPoint ) || fi.IsFamilyInstanceOf( RoutingFamilyType.TerminatePoint )) ) );
      }

      ErasePullBoxes( document, hashSet, list.ToList(), isEraseAllRoutes, isEraseSelectedRoutes ) ;
        
      List<string> elementIds = list.Select( elm => elm.UniqueId ).Distinct().ToList() ;
      RemoveRouteDetailSymbol( document, elementIds ) ;

      document.Delete( list.Select( elm => elm.Id ).Distinct().ToArray() ) ;

      if ( eraseRouteStoragesAndPassPoints ) {
        // erase routes, too.
        RouteCache.Get( DocumentKey.Get( document ) ).Drop( hashSet ) ;
      }
    }

    private static void ErasePullBoxes( Document document, ISet<string> routeNames, List<Element> conduits, bool isEraseAllRoutes, bool isEraseSelectedRoutes )
    {
      List<string> pullBoxIds = new() ;
      var pullBoxes = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_ElectricalFixtures ).Where( e => e is FamilyInstance && e.Name == ElectricalRoutingFamilyType.PullBox.GetFamilyName() ).ToList() ;
      if ( isEraseAllRoutes ) {
        pullBoxIds.AddRange( pullBoxes.Select( p => p.UniqueId ) ) ;
      }
      else if ( isEraseSelectedRoutes ) {
        pullBoxes = GetPullBoxes( pullBoxes, conduits ) ;
        if ( pullBoxes.Any() ) {
          var allConduits = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( e => e.GetRouteName() is { } routeName && ! routeNames.Contains( routeName ) ).ToList() ;
          pullBoxes = GetPullBoxIsNotConnected( pullBoxes, allConduits ).Distinct().ToList() ;
        }

        if( pullBoxes.Any() ) pullBoxIds.AddRange( pullBoxes.Select( p => p.UniqueId ) ) ;
      }

      if ( ! pullBoxIds.Any() ) return ;
      {
        var pullBoxInfoStorable = document.GetPullBoxInfoStorable() ;
        var pullBoxInfoModels = pullBoxInfoStorable.PullBoxInfoModelData.Where( p => pullBoxIds.Contains( p.PullBoxUniqueId ) ).ToList() ;
        var textNoteIds = pullBoxInfoModels.Select( p => p.TextNoteUniqueId ).Distinct().ToList() ;
        if ( textNoteIds.Any() ) pullBoxIds.AddRange( textNoteIds ) ;
        document.Delete( pullBoxIds ) ;
        foreach ( var pullBoxInfoModel in pullBoxInfoModels ) {
          pullBoxInfoStorable.PullBoxInfoModelData.Remove( pullBoxInfoModel ) ;
        }
      }
    }
    
    private static List<Element> GetPullBoxIsNotConnected( List<Element> pullBoxes, List<Element> allConduits )
    {
      var pullBoxIsNotConnected = new List<Element>() ;
      foreach ( var pullBox in pullBoxes ) {
        if ( GetPullBoxesOfRoute( new List<Element>{ pullBox }, allConduits, true ) == null && GetPullBoxesOfRoute( new List<Element>{ pullBox }, allConduits, false ) == null )  
          pullBoxIsNotConnected.Add( pullBox ) ;
      }
      
      return pullBoxIsNotConnected ;
    }

    private static List<Element> GetPullBoxes( List<Element> allPullBox, List<Element> conduitsOfRoute )
    {
      List<Element> pullBoxes = new() ;
      var pullBox = GetPullBoxesOfRoute( allPullBox, conduitsOfRoute, true ) ;
      if ( pullBox != null ) pullBoxes.Add( pullBox ) ;
      pullBox = GetPullBoxesOfRoute( allPullBox, conduitsOfRoute, false ) ;
      if ( pullBox != null ) pullBoxes.Add( pullBox ) ;
      return pullBoxes ;
    }
    
    private static Element? GetPullBoxesOfRoute( List<Element> allPullBox, List<Element> conduitsOfRoute, bool isFrom )
    {
      foreach ( var conduit in conduitsOfRoute ) {
        var endPoint = conduit.GetNearestEndPoints( isFrom ).ToList() ;
        if ( ! endPoint.Any() ) continue ;
        var endPointKey = endPoint.First().Key ;
        var elementId = endPointKey.GetElementUniqueId() ;
        if ( string.IsNullOrEmpty( elementId ) ) continue ;
        var pullBox = allPullBox.FirstOrDefault( c => c.UniqueId == elementId ) ;
        if ( pullBox == null || pullBox.IsTerminatePoint() || pullBox.IsPassPoint() ) continue ;
        return pullBox ;
      }

      return null ;
    }

    protected override void OnGenerationStarted()
    {
#if DUMP_LOGS
      RoutingTargets.DumpRoutingTargets( GetTargetsLogFileName( _document ), CollisionCheckTree ) ;
#endif

      // TODO, if needed
    }

    protected override void OnRoutingTargetProcessed( IReadOnlyCollection<AutoRoutingTarget> routingTargets, MergedAutoRoutingResult result )
    {
#if DUMP_LOGS
      result.DebugExport( GetResultLogFileName( _document, routingTarget ) ) ;
#endif

      var mepSystemCreator = new MEPSystemCreator( _document, routingTargets, _routeConditions ) ;

      foreach ( var routeVertex in result.RouteVertices ) {
        if ( routeVertex is not TerminalPoint ) continue ;

        mepSystemCreator.RegisterEndPointConnector( routeVertex ) ;
      }

      var newElements = CreateEdges( mepSystemCreator, result ).ToList() ;
      newElements.AddRange( mepSystemCreator.ConnectAllVertices() ) ;

      _document.Regenerate() ;

      _globalPassPointConnectorMapper.Merge( mepSystemCreator.PassPointConnectorMapper ) ;

      RegisterBadConnectors( mepSystemCreator.GetBadConnectorSet() ) ;
    }

    protected virtual IEnumerable<Element> CreateEdges( MEPSystemCreator mepSystemCreator, MergedAutoRoutingResult result )
    {
      return result.RouteEdges.Select( routeEdge => mepSystemCreator.CreateEdgeElement( routeEdge, mepSystemCreator.GetSubRoute( routeEdge ), result.GetPassingEndPointInfo( routeEdge ) ) ) ;
    }

    private void RegisterBadConnectors( IEnumerable<Connector[]> badConnectorSet )
    {
      _badConnectors.AddRange( badConnectorSet ) ;
    }

    protected override void OnGenerationFinished()
    {
      foreach ( var (route, passPointUniqueId, prevConn, nextConn) in _globalPassPointConnectorMapper.GetPassPointConnections( _document ) ) {
        var element = _document.GetElement( passPointUniqueId ) ;
        if ( element.GetRouteName() == route.RouteName ) {
          element.SetPassPointConnectors( new[] { prevConn }, new[] { nextConn } ) ;
        }

        var (success, fitting) = MEPSystemCreator.ConnectConnectors( _document, new[] { prevConn, nextConn } ) ;
        if ( success && null != fitting ) {
          // set routing id.
          fitting.SetProperty( RoutingParameter.RouteName, route.RouteName ) ;
          if ( ( prevConn.Owner.GetSubRouteIndex() ?? nextConn.Owner.GetSubRouteIndex() ) is { } subRouteIndex ) {
            fitting.SetProperty( RoutingParameter.SubRouteIndex, subRouteIndex ) ;
          }

          // Relate fitting to the pass point.
          element.SetProperty( RoutingParameter.RelatedPassPointUniqueId, passPointUniqueId ) ;
        }
      }
    }

    private static string GetLogDirectoryName( Document document )
    {
      var dir = Path.Combine( Path.GetDirectoryName( document.PathName )!, Path.GetFileNameWithoutExtension( document.PathName ) ) ;
      return Directory.CreateDirectory( dir ).FullName ;
    }

    private static string GetTargetsLogFileName( Document document )
    {
      return Path.Combine( GetLogDirectoryName( document ), "RoutingTargets.xml" ) ;
    }

    private static string GetResultLogFileName( Document document, AutoRoutingTarget routingTarget )
    {
      return Path.Combine( GetLogDirectoryName( document ), routingTarget.LineId + ".log" ) ;
    }

    public static void CorrectEnvelopes( Document document )
    {
      // get all envelope
      var envelopes = document.GetAllFamilyInstances( RoutingFamilyType.Envelope ).ToList() ;
      if ( ! envelopes.Any() ) return ;
      var parentEnvelopes = envelopes.Where( f => string.IsNullOrEmpty( f.GetParentEnvelopeId() ) ).ToList() ;
      if ( ! parentEnvelopes.Any() ) return ;
      
      // get offset value
      OffsetSettingStorable settingStorable = document.GetOffsetSettingStorable() ;
      var offset = settingStorable.OffsetSettingsData.Offset.MillimetersToRevitUnits() ;
      foreach ( var parentEnvelope in parentEnvelopes ) {
        var parentLocation = parentEnvelope.Location as LocationPoint ;
        var childrenEnvelope = envelopes.FirstOrDefault( f => f.GetParentEnvelopeId() == parentEnvelope.UniqueId ) ;
        if ( childrenEnvelope == null ) continue ;
        var childrenLocation = childrenEnvelope.Location as LocationPoint ;
        childrenLocation!.Point = new XYZ( parentLocation!.Point.X, parentLocation!.Point.Y, parentLocation!.Point.Z - offset ) ;
      }
    }

    private static void RemoveRouteDetailSymbol( Document document, List<string> elementIds )
    {
      var allDatas = document.GetAllDatas<Level, DetailSymbolModel>().ToList() ;
      if ( ! allDatas.Any() ) 
        return ;
      
      var detailSymbolItemModels = new List<DetailSymbolItemModel>() ;
      foreach ( var detailSymbolItemModel in allDatas.Select(x => x.Data.DetailSymbolData).SelectMany(x => x).Where( d => elementIds.Contains( d.ConduitUniqueId ) ).ToList() ) {
        // delete symbol
        var symbolId = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).Where( e => e.UniqueId == detailSymbolItemModel.DetailSymbolUniqueId ).Select( t => t.Id ).FirstOrDefault() ;
        if ( symbolId != null ) document.Delete( symbolId ) ;
        foreach ( var lineId in detailSymbolItemModel.LineUniqueIds.Split( ',' ) ) {
          var id = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Lines ).Where( e => e.UniqueId == lineId ).Select( e => e.Id ).FirstOrDefault() ;
          if ( id != null ) document.Delete( id ) ;
        }

        detailSymbolItemModels.Add( detailSymbolItemModel ) ;
      }

      if ( ! detailSymbolItemModels.Any() ) 
        return ;
      foreach ( var (level, data) in allDatas ) {
        foreach ( var detailSymbolModel in detailSymbolItemModels ) {
          if(data.DetailSymbolData.Contains(detailSymbolModel))
            data.DetailSymbolData.Remove( detailSymbolModel ) ;
        }
        level.SetData(data);
      }
      
      var detailSymbols = detailSymbolItemModels.Select( d => d.DetailSymbolUniqueId ).Distinct().ToList() ;
      if ( allDatas.Select(x => x.Data.DetailSymbolData).SelectMany(x => x).Any() && detailSymbols.Count == 1 ) {
        var detailSymbolModel = detailSymbolItemModels.FirstOrDefault() ;
        if ( detailSymbolModel!.IsParentSymbol ) {
          var detailSymbolModelParent = allDatas.Select(x => x.Data.DetailSymbolData).SelectMany(x => x).FirstOrDefault( d => d.DetailSymbol == detailSymbolModel.DetailSymbol && d.Code == detailSymbolModel.Code && d.IsParentSymbol ) ;
          if ( detailSymbolModelParent == null ) {
            UpdateSymbolOfConduitSameSymbolAndDifferentCode( document, allDatas.Select(x => x.Data.DetailSymbolData).SelectMany(x => x).ToList(), detailSymbolModel.DetailSymbol, detailSymbolModel.Code ) ;
          }
        }
      }
    }

    private static void UpdateSymbolOfConduitSameSymbolAndDifferentCode( Document doc, List<DetailSymbolItemModel> detailSymbolItemModels, string detailSymbol, string code )
    {
      var firstChildSymbol = detailSymbolItemModels.FirstOrDefault( d => d.DetailSymbol == detailSymbol && d.Code != code ) ;
      if ( firstChildSymbol == null ) return ;
      {
        var detailSymbolIds = detailSymbolItemModels.Where( d => d.DetailSymbol == firstChildSymbol.DetailSymbol && d.Code == firstChildSymbol.Code ).Select( d => d.DetailSymbolUniqueId ).Distinct().ToList() ;
        foreach ( var id in detailSymbolIds ) {
          var textElement = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).FirstOrDefault( t => t.UniqueId == id ) ;
          if ( textElement == null ) continue ;
          var textNote = ( textElement as TextNote ) ! ;
          CreateNewTextNoteType( doc, textNote, 0 ) ;
        }

        foreach ( var detailSymbolModel in detailSymbolItemModels.Where( d => d.DetailSymbol == firstChildSymbol.DetailSymbol && d.Code == firstChildSymbol.Code ).ToList() ) {
          detailSymbolModel.IsParentSymbol = true ;
        }
      }
    }

    private static void CreateNewTextNoteType( Document doc, TextNote textNote, int color )
    {
      //Create new text type
      string strStyleName = textNote.TextNoteType.Name + "-" + color ;

      var textNoteType = new FilteredElementCollector( doc ).OfClass( typeof( TextNoteType ) ).WhereElementIsElementType().Cast<TextNoteType>().FirstOrDefault( tt => Equals( strStyleName, tt.Name ) ) ;
      if ( textNoteType == null ) {
        // Create new Note type
        Element ele = textNote.TextNoteType.Duplicate( strStyleName ) ;
        textNoteType = ( ele as TextNoteType ) ! ;
        textNoteType.get_Parameter( BuiltInParameter.LINE_COLOR ).Set( color ) ;
      }

      // Change the text notes type to the new type
      textNote.ChangeTypeId( textNoteType!.Id ) ;
    }

    public static void GetAllRelatedBranches( Document document, ICollection<Route> routes, Dictionary<string, string> routeNameDictionary )
    {
      var relatedBranches = new List<Route>() ;
      var dic = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var allRouteNames = routes.Select( r => r.RouteName ).Distinct().ToHashSet() ;
      foreach ( var route in routes ) {
        var routeName = route.RouteName ;
        var conduitOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).FirstOrDefault( e => e.GetRouteName() == routeName ) ;
        if ( conduitOfRoute == null ) continue ;
        var representativeRouteName = conduitOfRoute.GetRepresentativeRouteName() ;
        if ( string.IsNullOrEmpty( representativeRouteName ) ) continue ;
        var allBranchRouteNames = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( e => ! allRouteNames.Contains( e.GetRouteName() ! ) && ( representativeRouteName == routeName ? e.GetRepresentativeRouteName() == routeName : e.GetRepresentativeRouteName() == representativeRouteName ) ).Select( e => e.GetRouteName() ! ).Distinct() ;
        foreach ( var branchRouteName in allBranchRouteNames ) {
          if ( false == dic.TryGetValue( branchRouteName, out var branchRoute ) ) continue ;
          relatedBranches.Add( branchRoute ) ;
          allRouteNames.Add( branchRouteName ) ;
          if ( ! routeNameDictionary.ContainsKey( branchRouteName ) ) {
            routeNameDictionary.Add( branchRouteName, representativeRouteName! ) ;
          }
        }
      }
      
      routes.AddRange( relatedBranches ) ;
    }

    public static void GetRelatedBranchRouteNames( Document document, IEnumerable<string> routeNames, Dictionary<string, string> routeNameDictionary )
    {
      foreach ( var routeName in routeNames ) {
        var conduitOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).FirstOrDefault( e => e.GetRouteName() == routeName ) ;
        if ( conduitOfRoute == null ) continue ;
        var representativeRouteName = conduitOfRoute.GetRepresentativeRouteName() ;
        if ( string.IsNullOrEmpty( representativeRouteName ) ) continue ;
        var allBranchRouteNames = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( e => e.GetRouteName() != routeName && ( representativeRouteName == routeName ? e.GetRepresentativeRouteName() == routeName : e.GetRepresentativeRouteName() == representativeRouteName ) ).Select( e => e.GetRouteName() ! ).Distinct() ;
        foreach ( var branchRouteName in allBranchRouteNames ) {
          if ( ! routeNameDictionary.ContainsKey( branchRouteName ) ) {
            routeNameDictionary.Add( branchRouteName, representativeRouteName! ) ;
          }
        }
      }
    }

    public static void ChangeRepresentativeRouteName( Document document, Dictionary<string, string> routeNameDictionary )
    {
      using Transaction transactionChangeRepresentativeRouteName = new( document ) ;
      transactionChangeRepresentativeRouteName.Start( "Change Representative Route Name" ) ;
      foreach ( var (routeName, parentRouteName ) in routeNameDictionary ) {
        var conduits = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( c => c.GetRouteName() == routeName ).ToList() ;
        foreach ( var conduit in conduits ) {
          conduit.TrySetProperty( RoutingParameter.RepresentativeRouteName, parentRouteName ) ;
        }
      }

      transactionChangeRepresentativeRouteName.Commit() ;
    }
  }
}