using System.Windows.Controls.Ribbon ;
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
      RibbonTab? SelectionTab = null ;
      RibbonPanel? SelectionPanel = null ;
      RibbonItem? SelectionButton = null ;

      foreach ( var tab in ComponentManager.Ribbon.Tabs ) {
        if ( tab.Id == "自動ルーティング" ) {
          SelectionTab = tab ;

          //Messaging.DebugMessage($"Found Tab: {SelectionTab}");

          foreach ( var panel in tab.Panels ) {
            if ( panel.Source.Title == "From-To" ) {
              SelectionPanel = panel ;

              //Messaging.DebugMessage( $"Found Panel: {SelectionPanel}" ) ;

              foreach ( var item in panel.Source.Items ) {
                if ( item.Id == "CustomCtrl_%CustomCtrl_%自動ルーティング%arent3d.architecture.routing.routing%arent3d.architecture.routing.app.commands.monitor_selection_command" ) {
                  SelectionButton = item ;

                  //Messaging.DebugMessage( $"Found Button: {SelectionButton}" ) ;

                  break ;
                }
              }
            }
          }

          break ;
        }
      }

      if ( SelectionPanel != null && SelectionButton != null ) {
        var position = GetPositionBeforeButton( "ID_REVIT_FILE_PRINT" ) ;

        PlaceButtonOnQuickAccess( position, SelectionButton ) ;
        
      }
    }

    private static int GetPositionBeforeButton( string s )
    {
      var position = 0 ;


      foreach ( var item in ComponentManager.QuickAccessToolBar.Items ) {
        if ( string.IsNullOrWhiteSpace( item.Id ) ) {
          continue ;
        }

        position++ ;

        if ( item.Id == s ) {
          break ;
        }
      }

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