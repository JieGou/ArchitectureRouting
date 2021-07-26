using System ;
using System.Linq ;
using Autodesk.Revit.UI ;
using Autodesk.Windows ;
using RibbonPanel = Autodesk.Revit.UI.RibbonPanel ;

namespace Arent3d.Revit.UI
{
  public class RibbonTabEx
  {
    private readonly UIControlledApplication _app ;
    private readonly string _tabName ;
    private readonly RibbonTab _tab ;

    public RibbonTabEx( UIControlledApplication app, string tabName )
    {
      _app = app ;
      _tabName = tabName ;

      app.CreateRibbonTab( tabName ) ;
      _tab = ComponentManager.Ribbon.Tabs.Reverse().FirstOrDefault( tab => tab.Title == tabName ) ?? throw new InvalidOperationException() ;
    }

    public RibbonPanel CreateRibbonPanel( string key )
    {
      return _app.CreateRibbonPanel( _tabName, key ) ;
    }
    public RibbonPanel CreateRibbonPanel( string key, string title )
    {
      var panel = CreateRibbonPanel( key ) ;
      panel.Title = title ;
      return panel ;
    }

    public bool Visible
    {
      get => _tab.IsVisible ;
      set => _tab.IsVisible = value ;
    }

    public bool Enabled
    {
      get => _tab.IsEnabled ;
      set => _tab.IsEnabled = value ;
    }

    public bool PanelEnabled
    {
      get => _tab.IsPanelEnabled ;
      set => _tab.IsPanelEnabled = value ;
    }
  }
}