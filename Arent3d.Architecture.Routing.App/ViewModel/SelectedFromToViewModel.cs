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

    //Selecting PickInfo 
    public static PointOnRoutePicker.PickInfo? TargetPickInfo { get ; set ; }

    //Diameter
    public static int DiameterIndex { get ; set ; }
    public static int SelectedDiameterIndex { get ; set ; }
    public static IList<double>? Diameters { get ; set ; }

    //SystemType 
    public static int SystemTypeIndex { get ; set ; }
    public static int SelectedSystemTypeIndex { get ; set ; }
    public static IList<MEPSystemType>? SystemTypes { get ; set ; }

    //CurveType
    public static int CurveTypeIndex { get ; set ; }
    public static int SelectedCurveTypeIndex { get ; set ; }
    public static IList<MEPCurveType>? CurveTypes { get ; set ; }

    //Direct
    public static bool IsDirect { get ; set ; }

    //Dialog
    private static SelectedFromTo? _openedDialog ;


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

      if ( _openedDialog != null ) {
        _openedDialog.Close() ;
      }

      var dialog = new SelectedFromTo( uiDocument, diameters, diameterIndex, systemTypes, systemTypeIndex, CurveTypes, curveTypeIndex, type, direct ) ;

      dialog.ShowDialog() ;
      _openedDialog = dialog ;
    }

    /// <summary>
    /// Set Selected Fromt-To Info 
    /// </summary>
    /// <param name="uiDoc"></param>
    /// <param name="doc"></param>
    /// <param name="route"></param>
    public static void SetSelectedFromToInfo( UIDocument uiDoc, Document doc, Route route )
    {
      UiDoc = uiDoc ;

      TargetRoute = route ;
      var routeMepSystem = new RouteMEPSystem( doc, route ) ;

      //Diameter Info
      Diameters = routeMepSystem.GetNominalDiameters( routeMepSystem.CurveType ).ToList() ;
      var diameter = route.GetSubRoute( 0 )?.GetDiameter( doc ) ;
      if ( diameter != null ) {
        DiameterIndex = Diameters.FindDoubleIndex( diameter, doc ) ;
      }

      //System Type Info(PinpingSystemType in lookup)
      var connector = route.GetReferenceConnector() ;
      SystemTypes = routeMepSystem.GetSystemTypes( doc, connector ).OrderBy( s => s.Name ).ToList() ;
      var systemType = routeMepSystem.MEPSystemType ;
      SystemTypeIndex = SystemTypes.ToList().FindIndex( s => s.Id == systemType.Id ) ;
      //CurveType Info
      var curveType = routeMepSystem.CurveType ;
      var type = curveType.GetType() ;
      CurveTypes = routeMepSystem.GetCurveTypes( doc, type ).OrderBy( s => s.Name ).ToList() ;
      CurveTypeIndex = CurveTypes.ToList().FindIndex( c => c.Id == curveType.Id ) ;
      //Direct Info
      IsDirect = route.GetSubRoute( 0 )?.IsRoutingOnPipeSpace ?? throw new ArgumentNullException( nameof( IsDirect ) ) ;
    }

    /// <summary>
    /// Set Dilaog Parameters and send PostCommand
    /// </summary>
    /// <param name="selectedDiameter"></param>
    /// <param name="selectedSystemType"></param>
    /// <param name="selectedDirect"></param>
    /// <returns></returns>
    public static bool ApplySelectedChanges( int selectedDiameter, int selectedSystemType, int selectedCurveType, bool selectedDirect )
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

      if ( UiDoc != null && CurveTypes != null && TargetRoute != null ) {
        RouteMEPSystem routeMepSystem = new RouteMEPSystem( UiDoc.Document, TargetRoute ) ;
        resultDiameters = routeMepSystem.GetNominalDiameters( CurveTypes[ curveTypeIndex ] ) ;
      }

      Diameters = resultDiameters ;

      return resultDiameters ;
    }
  }
}