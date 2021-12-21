using System ;
using System.Collections.Generic ;
using System.Windows.Media.Imaging ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB.Events ;
using Autodesk.Windows ;

namespace Arent3d.Architecture.Routing.Electrical.App
{
  internal static class MonitorSelectionApplicationEvent
  {
    public static void MonitorSelectionApplicationInitialized( object sender, ApplicationInitializedEventArgs e )
    {
      var targetTabName = "Electrical.App.Routing.TabName".GetAppStringByKey() ;
      var selectionTab = UIHelper.GetRibbonTabFromName( targetTabName ) ;
      if ( selectionTab == null ) return ;

      RibbonPanel? selectionPanel = null ;
      RibbonItem? selectionButton = null ;
      foreach ( var panel in selectionTab.Panels ) {
        if ( panel.Source.Title != "Electrical.App.Panels.Routing.Monitor".GetAppStringByKeyOrDefault( "Monitor Selection" ) ) continue ;

        selectionPanel = panel ;
        foreach ( var item in panel.Source.Items ) {
          if ( item.Id != "CustomCtrl_%CustomCtrl_%" + targetTabName + "%arent3d.architecture.routing.monitor%arent3d.architecture.routing.electrical.app.commands.monitor_selection_command" ) continue ;
          selectionButton = item ;
          break ;
        }
      }

      if ( selectionPanel != null && selectionButton != null ) {
        var position = UIHelper.GetPositionAfterButton( "ID_REVIT_FILE_PRINT" ) ;

        UIHelper.PlaceButtonOnQuickAccess( position, selectionButton ) ;
        // Remove Panel
        UIHelper.RemovePanelFromTab( selectionTab, selectionPanel ) ;
      }
    }
  }
}