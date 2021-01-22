using Autodesk.Revit.UI ;

namespace Arent3d.Revit.UI
{
  public class RibbonTabEx
  {
    private readonly UIControlledApplication _app ;
    private readonly string _tabName ;
    public RibbonTabEx( UIControlledApplication app, string tabName )
    {
      _app = app ;
      _tabName = tabName ;

      app.CreateRibbonTab( tabName ) ;
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
  }
}