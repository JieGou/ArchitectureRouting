using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.App.Forms ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.ViewModel
{
  static class SelectedFromToViewModel
  {
    private static UIDocument? UiDoc { get ; set ; }

    //Route
    public static Route? TargetRoute { get ; set ; }

    public static IReadOnlyCollection<SubRoute>? TargetSubRoutes { get ; set ; }


    //Selecting PickInfo 
    public static PointOnRoutePicker.PickInfo? TargetPickInfo { get ; private set ; }

    //Diameter
    public static int SelectedDiameterIndex { get ; private set ; }
    private static IList<double>? Diameters { get ; set ; }

    //SystemType 
    public static int SelectedSystemTypeIndex { get ; private set ; }
    private static IList<MEPSystemType>? SystemTypes { get ; set ; }

    //CurveType
    public static int SelectedCurveTypeIndex { get ; private set ; }
    private static IList<MEPCurveType>? CurveTypes { get ; set ; }

    //Direct
    public static bool? IsDirect { get ; set ; }

    //Dialog
    private static SelectedFromTo? _openedDialog ;

    public static PropertySource.RoutePropertySource? PropertySourceType { get ; set ; }


    static SelectedFromToViewModel()
    {
    }

    /// <summary>
    /// Show SelectedFromTo.xaml
    /// </summary>
    /// <param name="uiDocument"></param>
    /// <param name="targetIndex"></param>
    /// <param name="diameterList"></param>
    /// <param name="direct"></param>
    /// <param name="selectedPickInfo"></param>
    public static void ShowSelectedFromToDialog( UIDocument uiDocument, int diameterIndex, IList<double> diameters, int systemTypeIndex, IList<MEPSystemType> systemTypes, int curveTypeIndex, IList<MEPCurveType> curveTypes, Type type, bool direct, PointOnRoutePicker.PickInfo selectedPickInfo )
    {
      UiDoc = uiDocument ;
      TargetPickInfo = selectedPickInfo ;
      Diameters = diameters ;
      SystemTypes = systemTypes ;
      CurveTypes = curveTypes ;
      IsDirect = direct ;

      _openedDialog?.Close() ;

      var dialog = new SelectedFromTo( uiDocument, diameters, diameterIndex, systemTypes, systemTypeIndex, CurveTypes, curveTypeIndex, type, direct ) ;

      dialog.ShowDialog() ;
      _openedDialog = dialog ;
    }

    /// <summary>
    /// Set Selected Fromt-To Info 
    /// </summary>
    /// <param name="uiDoc"></param>
    /// <param name="doc"></param>
    /// <param name="subRoute"></param>
    public static void SetSelectedFromToInfo( UIDocument uiDoc, Document doc, IReadOnlyCollection<SubRoute>? subRoutes, FromToItem fromToItem )
    {
      UiDoc = uiDoc ;
      TargetRoute = subRoutes?.ElementAt( 0 ).Route ;

      if ( fromToItem.PropertySourceType is PropertySource.RoutePropertySource routePropertySource ) {
        PropertySourceType = routePropertySource ;
      }
    }


    /// <summary>
    /// Set Dilaog Parameters and send PostCommand
    /// </summary>
    /// <param name="selectedDiameter"></param>
    /// <param name="selectedSystemType"></param>
    /// <param name="selectedDirect"></param>
    /// <returns></returns>
    public static bool ApplySelectedChanges( int selectedDiameter, int selectedSystemType, int selectedCurveType, bool? selectedDirect )
    {
      if ( UiDoc != null ) {
        SelectedDiameterIndex = selectedDiameter ;
        SelectedSystemTypeIndex = selectedSystemType ;
        SelectedCurveTypeIndex = selectedCurveType ;
        IsDirect = selectedDirect ;
        UiDoc.Application.PostCommand<Commands.PostCommands.ApplySelectedFromToChangesCommand>() ;
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
    public static IList<double> ResetNominalDiameters( int curveTypeIndex )
    {
      IList<double> resultDiameters = new List<double>() ;

      if ( UiDoc != null && PropertySourceType?.CurveTypes != null && TargetRoute != null ) {
        RouteMEPSystem routeMepSystem = new RouteMEPSystem( UiDoc.Document, TargetRoute ) ;
        resultDiameters = routeMepSystem.GetNominalDiameters( PropertySourceType.CurveTypes[ curveTypeIndex ] ) ;
      }

      Diameters = resultDiameters ;

      return resultDiameters ;
    }
  }
}