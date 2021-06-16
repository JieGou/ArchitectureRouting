using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  /// <summary>
  /// Property source for selected item's UI
  /// </summary>
  public abstract class PropertySource
  {
    private Document Doc { get ; }


    //For experimental state
    private bool _isExperimental = true ;

    protected PropertySource( Document doc )
    {
      Doc = doc ;
    }


    /// <summary>
    /// PropertySource for RouteItem, SubRouteItem
    /// </summary>
    public class RoutePropertySource : PropertySource
    {
      //Route
      public Route? TargetRoute { get ; }
      public readonly IReadOnlyCollection<SubRoute>? TargetSubRoutes ;

      //Diameter
      public double? Diameter { get ; private set ; }
      public IList<double>? Diameters { get ; }

      //SystemType 
      public MEPSystemType? SystemType { get ; }
      public IList<MEPSystemType>? SystemTypes { get ; }

      //CurveType
      public MEPCurveType? CurveType { get ; private set ; }
      public IList<MEPCurveType>? CurveTypes { get ; }

      //Direct
      public bool? IsDirect { get ; private set ; }

      //HeightSetting
      public bool? OnHeightSetting { get ; private set ; }
      public double? FixedHeight { get ; }

      public AvoidType AvoidType { get ; }
      
      public string? StandardType { get ; }
      
      public IList<string>? StandardTypes { get ; }

      public RoutePropertySource( Document doc, IReadOnlyCollection<SubRoute> subRoutes ) : base( doc )
      {
        TargetSubRoutes = subRoutes ;
        TargetRoute = subRoutes.ElementAt( 0 ).Route ;

        //Set properties
        if ( TargetSubRoutes?.FirstOrDefault() is { } subRoute ) {
          CurveType = subRoute.GetMEPCurveType() ;

          //Diameter Info
          Diameters = (IList<double>?) CurveType.GetNominalDiameters( Doc.Application.VertexTolerance ).ToList() ?? Array.Empty<double>() ;
          Diameter = subRoute.GetDiameter() ;

          //System Type Info(PipingSystemType in lookup)
          SystemTypes = Doc.GetSystemTypes( subRoute.Route.GetSystemClassificationInfo() ).OrderBy( s => s.Name ).ToList() ;
          SystemType = subRoute.Route.GetMEPSystemType() ;

          //CurveType Info
          var curveTypeId = CurveType.GetValidId() ;
          // _isExperimental is true while we treat only round shape
          CurveTypes = _isExperimental ? Doc.GetCurveTypes( CurveType ).Where( c => c.Shape == ConnectorProfileType.Round ).OrderBy( s => s.Name ).ToList() : Doc.GetCurveTypes( CurveType ).OrderBy( s => s.Name ).ToList() ;


          //Direct Info
          IsDirect = subRoute.IsRoutingOnPipeSpace ;

          //Height Info
          OnHeightSetting = ( subRoute.FixedBopHeight != null ) ;
          FixedHeight = subRoute.FixedBopHeight ;

          //AvoidType Info
          AvoidType = subRoute.AvoidType ;

          if ( TargetSubRoutes?.Count > 1 ) {
            IsMultiSelected() ;
          }
        }
      }

      public RoutePropertySource( Document doc ) : base( doc )
      {
        //Diameter Info
        CurveType = Doc.GetAllElements<MEPCurveType>().First() ;
        //Diameters = (IList<double>?) CurveType.GetNominalDiameters( Doc.Application.VertexTolerance ).ToList() ?? Array.Empty<double>();
        //Diameter = Diameters[0];

        //System Type Info(PipingSystemType in lookup)
        SystemTypes = Doc.GetAllElements<MEPSystemType>().OrderBy( s => s.Name ).ToList() ;
        SystemType = SystemTypes[ 0 ] ;

        //CurveType Info
        var curveTypeId = CurveType.GetValidId() ;
        CurveTypes = Doc.GetAllElements<MEPCurveType>().ToList() ;

        //AvoidType Info
        AvoidType = AvoidType.Whichever ;
      }

      public RoutePropertySource( Document doc, MEPSystemClassificationInfo classificationInfo, MEPSystemType? systemType, MEPCurveType? curveType ) : base( doc )
      {
        // For Conduit
        if ( classificationInfo.HasSystemType() ) {
          //Diameter Info
          CurveType = curveType ;
          Diameters = (IList<double>?) curveType?.GetNominalDiameters( Doc.Application.VertexTolerance ).ToList() ?? Array.Empty<double>() ;

          //System Type Info(PipingSystemType in lookup)
          SystemTypes = Doc.GetSystemTypes( classificationInfo ).OrderBy( s => s.Name ).ToList() ;
          SystemType = systemType ;

          //CurveType Info
          var curveTypeId = CurveType.GetValidId() ;
          // _isExperimental is true while we treat only round shape
          CurveTypes = _isExperimental ? Doc.GetCurveTypes( CurveType ).Where( c => c.Shape == ConnectorProfileType.Round ).OrderBy( s => s.Name ).ToList() : Doc.GetCurveTypes( CurveType ).OrderBy( s => s.Name ).ToList() ;

          //AvoidType Info
          AvoidType = AvoidType.Whichever ;
        }
        else {
          //Diameter Info
          CurveType = curveType ;

          //Standard Type Info
          StandardTypes = doc.GetStandardTypes().ToList() ;
          StandardType = StandardTypes[0] ;

          //CurveType Info
          var curveTypeId = CurveType.GetValidId() ;
          // _isExperimental is true while we treat only round shape
          CurveTypes = doc.GetAllElements<MEPCurveType>().Where(c => c.GetType() == typeof( ConduitType )).ToList(); ;

          //AvoidType Info
          AvoidType = AvoidType.Whichever ;
        }
        
      }

      public RoutePropertySource( Document doc, MEPSystemClassificationInfo classificationInfo, MEPCurveType? curveType ) : base( doc )
      {
        //Diameter Info
        CurveType = curveType ;
        Diameters = (IList<double>?) curveType?.GetNominalDiameters( Doc.Application.VertexTolerance ).ToList() ?? Array.Empty<double>() ;

        //System Type Info(PipingSystemType in lookup)
        SystemTypes = Doc.GetSystemTypes( classificationInfo ).OrderBy( s => s.Name ).ToList() ;
        //SystemType = systemType;

        //CurveType Info
        var curveTypeId = CurveType.GetValidId() ;
        // _isExperimental is true while we treat only round shape
        CurveTypes = _isExperimental ? Doc.GetCurveTypes( CurveType ).Where( c => c.Shape == ConnectorProfileType.Round ).OrderBy( s => s.Name ).ToList() : Doc.GetCurveTypes( CurveType ).OrderBy( s => s.Name ).ToList() ;

        //AvoidType Info
        AvoidType = AvoidType.Whichever ;
      }

      private void IsMultiSelected()
      {
        if ( TargetSubRoutes?.ElementAt( 0 ).Route is not { } route ) return ;
        // if Diameter is multi selected, set null
        if ( IsDiameterMultiSelected( route ) ) {
          Diameter = null ;
        }

        // if CurveType is multi selected, set null
        if ( IsCurveTypeMultiSelected( route ) ) {
          CurveType = null ;
        }

        IsDirect = route.UniqueIsRoutingOnPipeSpace ;

        if ( IsHeightSettingMultiSelected( route ) ) {
          OnHeightSetting = null ;
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

      private static bool IsHeightSettingMultiSelected( Route route )
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
    }
  }

  /// <summary>
  /// PropertySource for Connector
  /// </summary>
  public class ConnectorPropertySource : PropertySource
  {
    public Connector TargetConnector { get ; }
    public XYZ ConnectorTransform { get ; }

    public ConnectorPropertySource( Document doc, Connector connector ) : base( doc )
    {
      TargetConnector = connector ;
      ConnectorTransform = connector.Origin ;
    }
  }

  /// <summary>
  /// PropertySource for PassPoint
  /// </summary>
  public class PassPointPropertySource : PropertySource
  {
    public PassPointEndPoint PassPointEndPoint { get ; }
    public XYZ PassPointPosition { get ; }
    public XYZ PassPointDirection { get ; }

    public PassPointPropertySource( Document doc, PassPointEndPoint passPointEndPoint ) : base( doc )
    {
      PassPointEndPoint = passPointEndPoint ;

      PassPointPosition = passPointEndPoint.RoutingStartPosition ;
      PassPointDirection = passPointEndPoint.Direction ;
    }
  }

  /// <summary>
  /// PropertySource for TerminatePoint
  /// </summary>
  public class TerminatePointPropertySource : PropertySource
  {
    public TerminatePointEndPoint TerminatePointEndPoint { get ; }
    public XYZ TerminatePointPosition { get ; }
    public XYZ TerminatePointDirection { get ; }

    public ElementId LinkedElementId { get ; }
    public string? LinkedElementName { get ; }

    public TerminatePointPropertySource( Document doc, TerminatePointEndPoint terminatePointEndPoint ) : base( doc )
    {
      TerminatePointEndPoint = terminatePointEndPoint ;

      TerminatePointPosition = terminatePointEndPoint.RoutingStartPosition ;
      TerminatePointDirection = terminatePointEndPoint.Direction ;

      LinkedElementId = terminatePointEndPoint.LinkedInstanceId ;
      LinkedElementName = doc.GetElement( LinkedElementId )?.Name ;
    }
  }
}