using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Diagnostics ;
using System.Linq ;
using System.Runtime.CompilerServices ;
using System.Windows ;
using System.Windows.Interop ;
using Arent3d.Architecture.Routing.App.Forms ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.ViewModel
{
  public class FromToWindowViewModel
  {
    private static UIDocument? UiDoc { get ; set ; }
    //Dialog
    private static FromToWindow? _openedDialog ;
    
    public static void ShowFromToWindow( UIDocument uiDocument, IEnumerable<Route> allRoutes)
    {
      UiDoc = uiDocument ;

      if ( _openedDialog != null ) {
        _openedDialog.Close() ;
      }

      ObservableCollection<FromToWindow.FromToItems> fromToItemsList = new ObservableCollection<FromToWindow.FromToItems>() ;

      foreach ( var route in allRoutes ) {
        RouteMEPSystem routeMepSystem = new RouteMEPSystem( UiDoc.Document, route ) ;
        var systemTypeList = new ObservableCollection<MEPSystemType>( routeMepSystem.GetSystemTypes( uiDocument.Document, route.GetReferenceConnector() ).OrderBy( s => s.Name ).ToList() ) ; 
        var systemType = routeMepSystem.MEPSystemType ;
        int systemTypeIndex = systemTypeList.ToList().FindIndex( s => s.Id == systemType.Id  ) ;
        var curveTypeList = new ObservableCollection<MEPCurveType>( routeMepSystem.GetCurveTypes( uiDocument.Document, routeMepSystem.CurveType.GetType() ).OrderBy( s => s.Name ).ToList() ) ;
        var curveType = routeMepSystem.CurveType ;
        int curveTypeIndex = curveTypeList.ToList().FindIndex( c => c.Id == curveType.Id ) ;
        IEnumerable<string> subRouteDiameters = route.SubRoutes.Select(s => (int)Math.Round(UnitUtils.ConvertFromInternalUnits( s.GetDiameter(UiDoc.Document), UnitTypeId.Millimeters )) + " mm" );
        
        var test = route.FirstFromConnector() ;
        fromToItemsList.Add(new FromToWindow.FromToItems()
        {
          Id = route.RouteName,
          From = route.FirstFromConnector(),
          FromType = route.FirstFromConnector()?.ToString().Split(':')[0],
          FromConnectorId = route.FirstFromConnector()?.ConnectorId.ToString(),
          FromElementId = route.FirstFromConnector()?.ElementId.ToString(),
          ToType = route.FirstToConnector()?.ToString().Split(':')[0],
          ToConnectorId = route.FirstToConnector()?.ConnectorId.ToString(),
          ToElementId = route.FirstToConnector()?.ElementId.ToString(),
          Domain = routeMepSystem.CurveType.GetType().Name.Split( 'T' )[ 0 ] + " Type",
          SystemTypes = systemTypeList,
          SystemType = systemType,
          SystemTypeIndex = systemTypeIndex,
          CurveTypes = curveTypeList,
          CurveType = curveType,
          CurveTypeIndex = curveTypeIndex,
          Diameters = string.Join( ",", subRouteDiameters ),
          //PassPoints = 
          Direct = route.GetSubRoute(0)?.IsRoutingOnPipeSpace,
        });
        
      }
      
      //var dialog = new FromToWindow( uiDocument.Document, allRoutes );
      var dialog = new FromToWindow( uiDocument.Document, fromToItemsList );

      System.Windows.Interop.WindowInteropHelper helper = new System.Windows.Interop.WindowInteropHelper(dialog);
      HwndSource? hwndSource = HwndSource.FromHwnd(UiDoc.Application.MainWindowHandle);
      Window? wnd = hwndSource.RootVisual as Window;
      if(wnd != null) {
        dialog.Owner = wnd ;
        dialog.Show() ;
        _openedDialog = dialog ;
      }
    }
  }
}