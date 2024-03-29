﻿using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using Arent3d.Utility ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class FromToWindowViewModel : ViewModelBase
  {
    public delegate FromToWindow FromToWindowCreator( UIApplication uiApplication, ObservableCollection<FromToWindow.FromToItems> fromToItemsList ) ;

    public static void ShowFromToWindow( UIApplication uiApplication, AddInType addInType, FromToWindowCreator fromToWindowCreator )
    {
      var uiDocument = uiApplication.ActiveUIDocument ;
      var allRoutes = uiDocument.Document.CollectRoutes( addInType ) ;
      OpenedDialog?.Close() ;

      var fromToItemsList = new ObservableCollection<FromToWindow.FromToItems>() ;

      foreach ( var route in allRoutes ) {
        var systemTypeList = new ObservableCollection<MEPSystemType>( uiDocument.Document.GetSystemTypes( route.GetSystemClassificationInfo() ).OrderBy( s => s.Name ).ToList() ) ;
        var systemTypeId = route.GetMEPSystemType().GetValidId() ;
        int systemTypeIndex = systemTypeList.FindIndex( s => s.Id == systemTypeId ) ;
        var curveTypeList = new ObservableCollection<MEPCurveType>( uiDocument.Document.GetCurveTypes( route.SubRoutes.Select( subRoute => subRoute.GetMEPCurveType() ).FirstOrDefault()?.GetType() ).OrderBy( s => s.Name ).ToList() ) ;
        var curveTypeId = route.UniqueCurveType.GetValidId() ;
        int curveTypeIndex = curveTypeList.FindIndex( c => c.Id == curveTypeId ) ;
        IEnumerable<string> subRouteDiameters = route.SubRoutes.Select( s => (int) Math.Round( s.GetDiameter().RevitUnitsToMillimeters() ) + " mm" ) ;
        IEnumerable<string> allPassPoints = route.GetAllPassPointEndPoints().ToList().Select( p => p.ToString() ) ;

        fromToItemsList.Add( new FromToWindow.FromToItems()
        {
          Id = route.RouteName,
          From = route.FirstFromConnector(),
          To = route.FirstToConnector(),
          Domain = UIHelper.GetTypeLabel( route.UniqueCurveType?.Name ?? string.Empty ),
          SystemTypes = systemTypeList,
          SystemType = route.GetMEPSystemType(),
          SystemTypeIndex = systemTypeIndex,
          CurveTypes = curveTypeList,
          CurveType = route.UniqueCurveType,
          CurveTypeIndex = curveTypeIndex,
          Diameters = string.Join( ",", subRouteDiameters ),
          PassPoints = string.Join( ",", allPassPoints ),
          Direct = route.GetSubRoute( 0 )?.IsRoutingOnPipeSpace,
        } ) ;
      }

      var dialog = fromToWindowCreator( uiApplication, fromToItemsList ) ;

      dialog.ShowDialog() ;
      OpenedDialog = dialog ;
    }
  }
}