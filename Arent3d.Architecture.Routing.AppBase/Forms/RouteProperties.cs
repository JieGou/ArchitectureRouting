using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public class RouteProperties
  {
    private Document Document { get ; }
    public double VertexTolerance => Document.Application.VertexTolerance ;

    //Diameter
    public double? Diameter { get ; private set ; }

    //SystemType 
    public MEPSystemType? SystemType { get ; }

    //CurveType
    public MEPCurveType? CurveType { get ; private set ; }

    //Direct
    public bool? IsRouteOnPipeSpace { get ; private set ; }

    //HeightSetting
    public bool? UseFixedHeight { get ; private set ; }
    public double FixedHeight { get ; }

    public AvoidType? AvoidType { get ; private set ; }

    public string? StandardType { get ; }

    internal RouteProperties( IReadOnlyCollection<SubRoute> subRoutes )
    {
      if ( 0 == subRoutes.Count ) throw new ArgumentException() ;

      var firstSubRoute = subRoutes.First() ;
      var document = firstSubRoute.Route.Document ;

      //Set properties
      CurveType = firstSubRoute.GetMEPCurveType() ;

      //Diameter Info
      Document = document ;
      Diameter = firstSubRoute.GetDiameter() ;

      //System Type Info(PipingSystemType in lookup)
      var systemClassification = firstSubRoute.Route.GetSystemClassificationInfo() ;
      if ( systemClassification.HasSystemType() ) {
        SystemType = firstSubRoute.Route.GetMEPSystemType() ;
      }
      else {
        SystemType = null ;
      }

      //Direct Info
      IsRouteOnPipeSpace = firstSubRoute.IsRoutingOnPipeSpace ;

      //Height Info
      UseFixedHeight = ( firstSubRoute.FixedBopHeight != null ) ;

      //AvoidType Info
      AvoidType = firstSubRoute.AvoidType ;

      if ( 1 < subRoutes.Count ) {
        SetIndeterminateValues( firstSubRoute.Route ) ;
      }

      FixedHeight = ( true == UseFixedHeight ? GetDisplayFixedHeight( firstSubRoute ) : 0.0 ) ;
    }

    public RouteProperties( Document document, RoutePropertyTypeList spec )
    {
      Document = document ;

      SystemType = spec.SystemTypes?.FirstOrDefault() ;
      CurveType = spec.CurveTypes.FirstOrDefault() ;
      Diameter = null ;

      IsRouteOnPipeSpace = false ;
      UseFixedHeight = false ;
      FixedHeight = 0.0 ;
      AvoidType = Routing.AvoidType.Whichever ;
    }

    public RouteProperties( Document document, MEPSystemClassificationInfo classificationInfo, MEPSystemType? systemType, MEPCurveType? curveType, string? standardType )
    {
      Document = document ;
      
      Diameter = null ;

      // For Conduit
      if ( classificationInfo.HasSystemType() ) {
        SystemType = systemType ;
        CurveType = curveType ;

        IsRouteOnPipeSpace = false ;
        UseFixedHeight = false ;
        FixedHeight = 0.0 ;
        AvoidType = Routing.AvoidType.Whichever ;
      }
      else {
        //Diameter Info
        CurveType = curveType ;

        //Standard Type Info
        StandardType = standardType ;

        IsRouteOnPipeSpace = false ;
        UseFixedHeight = false ;
        FixedHeight = 0.0 ;
        AvoidType = Routing.AvoidType.Whichever ;
      }
    }

    public RouteProperties( Route route, MEPSystemType? systemType, MEPCurveType? curveType, double? diameter, bool? isRouteOnPipeSpace, bool? useFixedHeight, double fixedHeight, AvoidType? avoidType )
    {
      Document = route.Document ;

      Diameter = diameter ;

      CurveType = curveType ;

      SystemType = systemType ;

      IsRouteOnPipeSpace = isRouteOnPipeSpace ;
      UseFixedHeight = useFixedHeight ;
      if ( true == UseFixedHeight ) {
        FixedHeight = GetTrueFixedHeight( route, fixedHeight ) ;
      }
      AvoidType = avoidType ;
    }

    private void SetIndeterminateValues( Route route )
    {
      // if Diameter is multi selected, set null
      if ( IsDiameterMultiSelected( route ) ) {
        Diameter = null ;
      }

      // if CurveType is multi selected, set null
      if ( IsCurveTypeMultiSelected( route ) ) {
        CurveType = null ;
      }

      IsRouteOnPipeSpace = route.UniqueIsRoutingOnPipeSpace ;

      if ( IsUseFixedHeightMultiSelected( route ) ) {
        UseFixedHeight = null ;
      }

      if ( IsAvoidTypeMultiSelected( route ) ) {
        AvoidType = null ;
      }
    }

    /// <summary>
    /// Get CurveType's multi selected state
    /// </summary>
    /// <param name="route"></param>
    /// <returns></returns>
    private static bool IsCurveTypeMultiSelected( Route route )
    {
      return ( null == route.UniqueCurveType ) ;
    }

    /// <summary>
    /// Get Diameter's multi selected state
    /// </summary>
    /// <param name="route"></param>
    /// <returns></returns>
    private static bool IsDiameterMultiSelected( Route route )
    {
      return ( null == route.UniqueDiameter ) ;
    }

    private static bool IsUseFixedHeightMultiSelected( Route route )
    {
      var allNull = true ;
      foreach ( var subRoute in route.SubRoutes ) {
        allNull = ( subRoute.FixedBopHeight == null ) ;
      }

      if ( allNull ) {
        return false ;
      }
      else {
        return ( null == route.UniqueFixedBopHeight ) ;
      }
    }

    /// <summary>
    /// Get AvoidType's multi selected state
    /// </summary>
    /// <param name="route"></param>
    /// <returns></returns>
    private static bool IsAvoidTypeMultiSelected( Route route )
    {
      return ( null == route.UniqueAvoidType ) ;
    }
    
    

    private static double GetDisplayFixedHeight( SubRoute subRoute )
    {
      var fromFloorHeight = 0.0 ;
      if ( GetFloorHeight( subRoute.Route ) is { } floorHeight) {
        fromFloorHeight = subRoute.FixedBopHeight!.Value - floorHeight + subRoute.GetDiameter() / 2 ;
      }

      return fromFloorHeight ;
    }

    // Use for max value of fixed height
    private static double GetUpLevelHeightFromLevel( SubRoute subRoute )
    {
      return GetHeightFromLevel( subRoute, GetUpLevelTotalHeight( subRoute.Route ) ) ;
    }

    private static double GetHeightFromLevel( SubRoute subRoute, double bopHeight )
    {
      var fromFloorHeight = 0.0 ;
      if ( GetFloorHeight( subRoute.Route ) is { } floorHeight ) {
        fromFloorHeight = bopHeight - floorHeight ;
      }

      return fromFloorHeight ;
    }

    private static double GetUpLevelTotalHeight( Route route )
    {
      var level = GetConnectorLevel( route ) ;
      var levels = GetAllFloors( route.Document ) ;
      if ( level == null || levels == null ) return 0.0 ;
      return levels.First( l => l.Elevation > level.Elevation ).Elevation ;
    }

    private static IOrderedEnumerable<Level>? GetAllFloors( Document? doc )
    {
      return doc?.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ).OrderBy( l => l.Elevation ) ;
    }

    private static double? GetFloorHeight( Route route )
    {
      return GetFloorHeight( GetConnectorLevel( route ) ) ;
    }
    private static double? GetFloorHeight( Level? level )
    {
      return level?.Elevation ;
    }

    private static Level? GetConnectorLevel( Route route )
    {
      var connector = route.FirstFromConnector()?.GetConnector()?.Owner ;
      var level = connector?.Document.GetElement( connector.LevelId ) as Level ;

      return level ;
    }

    private static double GetTrueFixedHeight( Route route, double fixedHeight )
    {
      return GetTrueFixedHeight( GetConnectorLevel( route ), route.GetSubRoute( 0 )?.GetDiameter(), fixedHeight ) ;
    }

    public static double GetTrueFixedHeight( Level? level, double? routeDiameter, double fixedHeight )
    {
      var targetHeight = 0.0 ;
      if ( GetFloorHeight( level ) is { } floorHeight && routeDiameter is { } diameter ) {
        targetHeight = fixedHeight + floorHeight - diameter / 2 ;
      }

      return targetHeight ;
    }
  }
}