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
      RibbonTab? selectionTab = null ;
      RibbonPanel? selectionPanel = null ;
      RibbonItem? selectionButton = null ;
      var targetTabName = "Electrical.App.Routing.TabName".GetAppStringByKey() ;


      selectionTab = UIHelper.GetRibbonTabFromName( targetTabName ) ;
      if ( selectionTab == null )
        return ;
      foreach ( var panel in selectionTab.Panels )
        if ( panel.Source.Title == "Electrical.App.Panels.Routing.Monitor".GetAppStringByKeyOrDefault( "Monitor Selection" ) ) {
          selectionPanel = panel ;
          foreach ( var item in panel.Source.Items )
            if ( item.Id == "CustomCtrl_%CustomCtrl_%" + targetTabName + "%arent3d.architecture.routing.monitor%arent3d.architecture.routing.electrical.app.commands.monitor_selection_command" ) {
              selectionButton = item ;
              break ;
            }
        }
        else if ( panel.Source.Title == "Electrical.App.Panels.Routing.Connectors".GetAppStringByKeyOrDefault( "Connectors" ) ) {
          List<RibbonItem> newConnectorButton = new() ;
          RibbonSplitButton ribbonSplitButton = new()
          {
            IsSplit = true,
            Size = RibbonItemSize.Large,
            IsSynchronizedWithCurrentItem = true,
            Text = panel.Source.Items[ 2 ].Text,
            ShowText = true,
            LargeImage = new BitmapImage( new Uri( panel.Source.Items[ 2 ].LargeImage.ToString() ) ),
            Image = new BitmapImage( new Uri( panel.Source.Items[ 2 ].Image.ToString() ) )
          } ;
          foreach ( var item in panel.Source.Items )
            if ( item.Text is "New Connector" || item.Text == "Electrical.App.Commands.Routing.Connectors.NewConnectorBsCommand".GetAppStringByKeyOrDefault( "New Connector&#xA;(Both Sides)" ) )
              newConnectorButton.Add( item ) ;
            else
              ribbonSplitButton.Items.Add( item ) ;
          panel.Source.Items.Clear() ;
          foreach ( var item in newConnectorButton ) panel.Source.Items.Add( item ) ;

          panel.Source.Items.Add( ribbonSplitButton ) ;
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