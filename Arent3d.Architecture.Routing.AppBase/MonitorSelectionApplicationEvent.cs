using System.Collections.Generic ;
using System.Diagnostics ;
using System.Linq ;
using System.Windows.Controls.Ribbon ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB.Events ;
using Autodesk.Revit.DB ;
using Autodesk.Windows ;
using RibbonTab = Autodesk.Windows.RibbonTab ;
using TaskDialog = Autodesk.Revit.UI.TaskDialog ;

namespace Arent3d.Architecture.Routing.AppBase
{
  internal static class MonitorSelectionApplicationEvent
  {
    public static void MonitorSelectionApplicationInitialized( object sender, ApplicationInitializedEventArgs e )
    {
      RibbonTab? selectionTab = null ;
      RibbonPanel? selectionPanel = null ;
      RibbonItem? selectionButton = null ;
      string? targetTabName = "App.Routing.TabName".GetAppStringByKey() ;


      selectionTab = UIHelper.GetRibbonTabFromName( targetTabName ) ;
      if ( selectionTab == null ) {
        return ;
      }
      else {
        foreach ( var panel in selectionTab.Panels ) {
          if ( panel.Source.Title == "App.Panels.Routing.Monitor".GetAppStringByKeyOrDefault("Monitor Selection") ) {
            selectionPanel = panel ;
            foreach ( var item in panel.Source.Items ) {
              if ( item.Id == "CustomCtrl_%CustomCtrl_%" + targetTabName + "%arent3d.architecture.routing.monitor%arent3d.architecture.routing.app_base.commands.monitor_selection_command" ) {
                selectionButton = item ;
                break ;
              }
              else {
                continue ;
              }
            }
          }
          else {
            continue;
          }
        }
      }

      if ( selectionPanel != null && selectionButton != null ) {
        var position = UIHelper.GetPositionAfterButton( "ID_REVIT_FILE_PRINT" ) ;

        UIHelper.PlaceButtonOnQuickAccess( position, selectionButton ) ;
        // Remove Panel
        UIHelper.RemovePanelFromTab(selectionTab, selectionPanel);
      }
    }
  }
}