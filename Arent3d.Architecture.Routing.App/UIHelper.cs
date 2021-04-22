using System ;
using System.Linq ;
using System.Windows.Controls ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;
using Autodesk.Windows ;

namespace Arent3d.Architecture.Routing.App
{
  public static class UIHelper
  {
    /// <summary>
    /// Get LabelName From CurveType
    /// </summary>
    /// <param name="targetStrings"></param>
    /// <returns></returns>
    public static string GetTypeLabel( string targetStrings )
    {
      if ( targetStrings.EndsWith( "Type" ) ) {
        targetStrings = targetStrings.Substring( 0, targetStrings.Length - 4 ) + " Type" ;
      }

      return targetStrings ;
    }

    public static RibbonTab? GetRibbonTabFromName( string? targetTabName ) => ComponentManager.Ribbon.Tabs.FirstOrDefault( t => t.Id == targetTabName ) ;

    public static RibbonPanel? GetRibbonPanelFromName( string targetPanelName, RibbonTab? targetRibbonTab ) => targetRibbonTab?.Panels.FirstOrDefault( p => p.Source.Title == targetPanelName ) ;

    public static RibbonButton? GetRibbonButtonFromName( string targetButtonCommand, RibbonPanel? targetRibbonPanel )
    {
      var targetItemName = "CustomCtrl_%" + targetRibbonPanel?.Source.Id + "%arent3d.architecture.routing.app.commands.routing." + targetButtonCommand ;
      return targetRibbonPanel?.Source.Items.OfType<RibbonButton>().FirstOrDefault( item => item.Id == targetItemName ) ;
    }

    public static int GetPositionBeforeButton( string s )
    {
      var items = ComponentManager.QuickAccessToolBar.Items.TakeWhile( item => item.Id != s ) ;

      var position = items.Count() + 1 ;

      return position ;
    }

    public static void PlaceButtonOnQuickAccess( int position, Autodesk.Windows.RibbonItem ribbonItem )
    {
      if ( position < ComponentManager.QuickAccessToolBar.Items.Count ) {
        ComponentManager.QuickAccessToolBar.InsertStandardItem( position, ribbonItem ) ;
      }
      else {
        ComponentManager.QuickAccessToolBar.AddStandardItem( ribbonItem ) ;
      }
    }

    public static void RemovePanelFromTab( RibbonTab ribbonTab, Autodesk.Windows.RibbonPanel ribbonPanel )
    {
      ribbonTab.Panels.Remove( ribbonPanel ) ;
    }

    public static void RemoveTabFromRibbon( RibbonTab ribbonTab )
    {
      if ( ribbonTab.Panels.Count != 0 ) {
        return ;
      }

      ribbonTab.IsVisible = false ;
    }
  }
}