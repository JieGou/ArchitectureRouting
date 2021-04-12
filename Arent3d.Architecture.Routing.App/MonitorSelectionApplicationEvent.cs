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

namespace Arent3d.Architecture.Routing.App
{
  internal static class MonitorSelectionApplicationEvent
  {
    public static void MonitorSelectionApplicationInitialized( object sender, ApplicationInitializedEventArgs e )
    {
      RibbonTab? selectionTab = null ;
      RibbonPanel? selectionPanel = null ;
      RibbonItem? selectionButton = null ;
      string? targetTabName = "App.Routing.TabName".GetAppStringByKey() ;


      selectionTab = GetRibbonTabFromName( targetTabName ) ;
      if ( selectionTab == null ) {
        return ;
      }
      else {
        foreach ( var panel in selectionTab.Panels ) {
          if ( panel.Source.Title == "From-To" ) {
            selectionPanel = panel ;
            foreach ( var item in panel.Source.Items ) {
              if ( item.Id == "CustomCtrl_%CustomCtrl_%" + targetTabName + "%arent3d.architecture.routing.routing%arent3d.architecture.routing.app.commands.monitor_selection_command" ) {
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
        var position = GetPositionBeforeButton( "ID_REVIT_FILE_PRINT" ) ;

        PlaceButtonOnQuickAccess( position, selectionButton ) ;
        
      }
    }

    private static RibbonTab? GetRibbonTabFromName( string? targetTabName ) 
      => ComponentManager.Ribbon.Tabs.ToList().Find( t => t.Id == targetTabName ) ;

    private static int GetPositionBeforeButton( string s )
    {
      var items = ComponentManager.QuickAccessToolBar.Items.TakeWhile( item => item.Id != s ) ;

      var position = items.Count() + 1 ;

      return position ;
    }

    private static void PlaceButtonOnQuickAccess( int position, Autodesk.Windows.RibbonItem ribbonItem )
    {
      if ( position < ComponentManager.QuickAccessToolBar.Items.Count ) {
        ComponentManager.QuickAccessToolBar.InsertStandardItem( position, ribbonItem ) ;
      }
      else {
        ComponentManager.QuickAccessToolBar.AddStandardItem( ribbonItem ) ;
      }
    }
    
  }
}