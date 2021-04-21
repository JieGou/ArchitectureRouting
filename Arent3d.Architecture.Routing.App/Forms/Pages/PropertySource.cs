using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.App.Forms ;
using Arent3d.Architecture.Routing.RouteEnd ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  /// <summary>
  /// Property source for selected item's UI
  /// </summary>
  public abstract class PropertySource
  {
    private Document Doc { get ; set ; }

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
      public Route? TargetRoute { get ; private init ; }
      public readonly IReadOnlyCollection<SubRoute>? TargetSubRoutes ;

      //Diameter
      public int DiameterIndex { get ; private set ; }
      public IList<double>? Diameters { get ; private set ; }

      //SystemType 
      public int SystemTypeIndex { get ; private set ; }
      public IList<MEPSystemType>? SystemTypes { get ; private set ; }

      //CurveType
      public int CurveTypeIndex { get ; private set ; }
      public IList<MEPCurveType>? CurveTypes { get ; private set ; }

      //Direct
      public bool? IsDirect { get ; set ; }

      public RoutePropertySource( Document doc, IReadOnlyCollection<SubRoute> subRoutes ) : base( doc )
      {
        TargetSubRoutes = subRoutes ;
        TargetRoute = subRoutes.ElementAt( 0 ).Route ;
        SetProperties() ;
        if ( TargetSubRoutes.Count > 1 ) {
          IsMultiSelected() ;
        }
      }

      private void SetProperties()
      {
        if ( TargetSubRoutes?.ElementAt( 0 ).Route is not { } route ) return ;
        var routeMepSystem = new RouteMEPSystem( Doc, route ) ;

        //Diameter Info
        Diameters = routeMepSystem.GetNominalDiameters( routeMepSystem.CurveType ).ToList() ;
        double? diameter = TargetSubRoutes.ElementAt( 0 ).GetDiameter( Doc ) ;


        DiameterIndex = Diameters.FindDoubleIndex( diameter, Doc ) ;

        //System Type Info(PinpingSystemType in lookup)
        var connector = TargetSubRoutes.ElementAt( 0 ).GetReferenceConnector() ;
        SystemTypes = routeMepSystem.GetSystemTypes( Doc, connector ).OrderBy( s => s.Name ).ToList() ;
        var systemType = routeMepSystem.MEPSystemType ;
        SystemTypeIndex = SystemTypes.ToList().FindIndex( s => s.Id == systemType?.Id ) ;

        //CurveType Info
        var curveType = routeMepSystem.CurveType ;
        var type = curveType.GetType() ;
        CurveTypes = routeMepSystem.GetCurveTypes( Doc, type ).OrderBy( s => s.Name ).ToList() ;
        CurveTypeIndex = CurveTypes.ToList().FindIndex( c => c.Id == curveType?.Id ) ;

        //Direct Info
        IsDirect = TargetSubRoutes.ElementAt( 0 ).IsRoutingOnPipeSpace ;
      }

      private void IsMultiSelected()
      {
        if ( TargetSubRoutes?.ElementAt( 0 ).Route is not { } route ) return ;
        // if Diameter is multi selected, set null
        if ( IsDiameterMultiSelected( route ) ) {
          DiameterIndex = -1 ;
        }

        // if CurveType is multi selected, set null
        if ( IsCurveTypeMultiSelected( route ) ) {
          CurveTypeIndex = -1 ;
        }

        // if Direct is multi selected, set null
        if ( IsViaPsMultiSelected( route ) ) {
          IsDirect = null ;
        }
      }

      /// <summary>
      /// Get CuveType's multi selected state
      /// </summary>
      /// <param name="route"></param>
      /// <returns></returns>
      private bool IsCurveTypeMultiSelected( Route route )
      {
        var routeMepSystem = new RouteMEPSystem( Doc, route ) ;
        var curveTypes = new List<MEPCurveType>() ;

        foreach ( var subRoute in route.SubRoutes ) {
          if ( ! curveTypes.Contains( routeMepSystem.CurveType ) ) {
            curveTypes.Add( routeMepSystem.CurveType ) ;
          }
        }

        var curveTypeResult = false ;
        try {
          var singleCurveType = curveTypes.SingleOrDefault() ;
          curveTypeResult = false ;
        }
        catch {
          curveTypeResult = true ;
        }

        return curveTypeResult ;
      }

      /// <summary>
      /// Get Diameter's multi selected state
      /// </summary>
      /// <param name="route"></param>
      /// <returns></returns>
      private bool IsDiameterMultiSelected( Route route )
      {
        var diameters = new List<double>() ;

        foreach ( var subRoute in route.SubRoutes ) {
          if ( ! diameters.Contains( subRoute.GetDiameter( Doc ) ) ) {
            diameters.Add( subRoute.GetDiameter( Doc ) ) ;
          }
        }

        var diameterResult = false ;
        try {
          var singleDiameter = diameters.SingleOrDefault() ;
          diameterResult = false ;
        }
        catch {
          diameterResult = true ;
        }

        return diameterResult ;
      }

      /// <summary>
      /// Get ViaPs's multi selected state
      /// </summary>
      /// <param name="route"></param>
      /// <returns></returns>
      private bool IsViaPsMultiSelected( Route route )
      {
        var directs = new List<bool>() ;

        foreach ( var subRoute in route.SubRoutes ) {
          if ( ! directs.Contains( ( subRoute.IsRoutingOnPipeSpace ) ) ) {
            directs.Add( subRoute.IsRoutingOnPipeSpace ) ;
          }
        }

        var directResult = false ;
        try {
          var singleDirect = directs.SingleOrDefault() ;
          directResult = false ;
        }
        catch {
          directResult = true ;
        }

        return directResult ;
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
    public PassPointEndIndicator PassPointEndIndicator { get ; }
    public XYZ PassPointTransform { get ; }
    public PassPointPropertySource( Document doc, PassPointEndIndicator passPointEndIndicator ) : base( doc )
    {
      PassPointEndIndicator = passPointEndIndicator ;
      if ( doc.GetElement( new ElementId( passPointEndIndicator.ElementId ) ).Location is LocationPoint locationPoint ) {
        PassPointTransform = locationPoint.Point;
      }
      else {
        PassPointTransform = new XYZ() ;
      }
      
      
    }
  }
}