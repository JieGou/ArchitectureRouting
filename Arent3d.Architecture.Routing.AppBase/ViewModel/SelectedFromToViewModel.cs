using System ;
using System.Collections ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public static class SelectedFromToViewModel
  {
    public static UIDocument? UiDoc { get ; set ; }

    //Route
    public static Route? TargetRoute { get ; set ; }

    //Diameter
    public static double SelectedDiameter { get ; private set ; }
    public static IList<double>? Diameters { get ; private set ; }

    //SystemType 
    public static MEPSystemType? SelectedSystemType { get ; private set ; }

    //CurveType
    public static MEPCurveType? SelectedCurveType { get ; private set ; }

    //Direct
    public static bool? IsDirect { get ; set ; }
    
    //FixedHeight
    public static bool? OnHeightSetting { get ; set ; }
    public static double? FixedHeight { get ; private set ; }
    
    public static AvoidType AvoidType { get ; private set ; }

    public static PropertySource.RoutePropertySource? PropertySourceType { get ; set ; }

    //Direct
    public static FromToItem? FromToItem { get; set; }


    static SelectedFromToViewModel()
    {
    }

    /// <summary>
    /// Set Selected Fromt-To Info
    /// </summary>
    /// <param name="uiDoc"></param>
    /// <param name="doc"></param>
    /// <param name="subRoutes"></param>
    /// <param name="fromToItem"></param>
    public static void SetSelectedFromToInfo( UIDocument uiDoc, Document doc, IReadOnlyCollection<SubRoute>? subRoutes, FromToItem fromToItem )
    {
      UiDoc = uiDoc ;
      TargetRoute = subRoutes?.ElementAt( 0 ).Route ;

      if ( fromToItem.PropertySourceType is PropertySource.RoutePropertySource routePropertySource ) {
        PropertySourceType = routePropertySource ;
      }

      FromToItem = fromToItem;
    }


    /// <summary>
    /// Set Dilaog Parameters and send PostCommand
    /// </summary>
    /// <param name="selectedDiameter"></param>
    /// <param name="selectedSystemType"></param>
    /// <param name="selectedDirect"></param>
    /// <returns></returns>
    public static bool ApplySelectedChanges( double selectedDiameter, MEPSystemType selectedSystemType, MEPCurveType selectedCurveType, bool? selectedDirect, bool? heightSetting, double? fixedHeight, AvoidType avoidType )
    {
      if ( UiDoc != null ) {
        SelectedDiameter = selectedDiameter.MillimetersToRevitUnits() ;
        SelectedSystemType = selectedSystemType ;
        SelectedCurveType = selectedCurveType ;
        IsDirect = selectedDirect ;

        OnHeightSetting = heightSetting ;
        if ( OnHeightSetting is true) {
          if ( fixedHeight is { } selectedFixedHeight) {
            FixedHeight = GetTotalHeight( fixedHeight ) ;
          }
        }
        else {
          FixedHeight = null ;
        }

        AvoidType = avoidType ;
        
        //UiDoc.Application.PostCommand<Commands.PostCommands.ApplySelectedFromToChangesCommand>() ;
        return true ;
      }
      else {
        return false ;
      }
    }

    /// <summary>
    /// Reset Diameter List by Curve Type
    /// </summary>
    /// <param name="curveTypeIndex"></param>
    /// <returns></returns>
    public static IEnumerable<double> ResetNominalDiameters( int curveTypeIndex )
    {
      if ( PropertySourceType?.CurveTypes is not { } curveTypes|| null == UiDoc ) return Enumerable.Empty<double>() ;
      if ( curveTypeIndex < 0 || curveTypes.Count <= curveTypeIndex ) return Enumerable.Empty<double>() ;

      //Reset diameter list in PropertySourceType
      Diameters = curveTypes[ curveTypeIndex ].GetNominalDiameters( UiDoc.Document.Application.VertexTolerance ) ;

      return Diameters ;
    }

    public static double GetRouteHeightFromFloor( double totalHeight )
    {
      var fromFloorHeight = 0.0 ;
      if ( GetFloorHeight() is { } floorHeight && TargetRoute?.GetSubRoute(0) is {} subRoute) {
        fromFloorHeight = totalHeight - floorHeight + subRoute.GetDiameter()/2 ;
      }

      return fromFloorHeight ;
    }
    
    public static double GetHeightFromLevel( double totalHeight )
    {
      var fromFloorHeight = 0.0 ;
      if ( GetFloorHeight() is { } floorHeight && TargetRoute?.GetSubRoute(0) is {} subRoute) {
        fromFloorHeight = totalHeight - floorHeight;
      }

      return fromFloorHeight ;
    }

    public static double GetUpLevelTotalHeight()
    {
      var level = GetConnectorLevel() ;
      var levels = GetAllFloors( TargetRoute?.Document ) ;
      if ( level == null || levels == null ) return 0.0;
      return levels.First( l => l.Elevation > level.Elevation ).Elevation ;
    }

    public static double GetUpLevelHeightFromLevel()
    {
      return GetHeightFromLevel(GetUpLevelTotalHeight()) ;
    }
    
    public static IOrderedEnumerable<Level>? GetAllFloors(Document? doc)
    {
      return doc?.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ).OrderBy( l => l.Elevation ) ;
    }


    private static Level? GetConnectorLevel()
    {
      var connector = TargetRoute?.FirstFromConnector()?.GetConnector()?.Owner ;
      var level = connector?.Document.GetElement(connector.LevelId) as Level;

      return level ;
    }
    private static double? GetFloorHeight()
    {
      return GetConnectorLevel()?.Elevation ;
    }

    private static double GetTotalHeight( double? selectedHeight )
    {
      var targetHeight = 0.0 ;
      if ( GetFloorHeight() is { } floorHeight && TargetRoute?.GetSubRoute( 0 )?.GetDiameter() is { } diameter && selectedHeight is { } selected ) {
        targetHeight = selected.MillimetersToRevitUnits() + floorHeight - diameter / 2 ;
      }

      return targetHeight ;
    }
  }
}