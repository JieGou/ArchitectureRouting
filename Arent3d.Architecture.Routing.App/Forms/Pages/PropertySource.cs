using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.App.Forms ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
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
        if ( TargetSubRoutes?.FirstOrDefault() is not {} subRoute ) return ;

        var curveType = subRoute.GetMEPCurveType() ;
        
        //Diameter Info
        Diameters = (IList<double>?) curveType.GetNominalDiameters( Doc.Application.VertexTolerance ).ToList() ?? Array.Empty<double>() ;
        var diameter = subRoute.GetDiameter() ;
        DiameterIndex = Diameters.FindIndexByVertexTolerance( diameter, Doc ) ;

        //System Type Info(PipingSystemType in lookup)
        SystemTypes = Doc.GetSystemTypes( subRoute.Route.SystemClassificationInfo ).OrderBy( s => s.Name ).ToList() ;
        var systemTypeId = subRoute.Route.GetMEPSystemType().GetValidId() ;
        SystemTypeIndex = SystemTypes.FindIndex( s => s.Id == systemTypeId ) ;

        //CurveType Info
        var curveTypeId = curveType.GetValidId() ;
        CurveTypes = Doc.GetCurveTypes( curveType ).OrderBy( s => s.Name ).ToList() ;
        CurveTypeIndex = CurveTypes.FindIndex( c => c.Id == curveTypeId ) ;

        //Direct Info
        IsDirect = subRoute.IsRoutingOnPipeSpace ;
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

        IsDirect = route.UniqueIsRoutingOnPipeSpace ;
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