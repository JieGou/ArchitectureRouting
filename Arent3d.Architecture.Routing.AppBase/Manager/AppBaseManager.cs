using System ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Manager
{
  public class AppBaseManager
  {
    private static AppBaseManager? _instance ;
    
    public static AppBaseManager Instance
    {
      get
      {
        if (null == _instance)
        {
          lock (typeof(AppBaseManager))
          {
            if (null == _instance)
            {
              _instance = new AppBaseManager();
            }
          }
        }
        return _instance; 
      }
    }
    
    public bool IsFocusHasekoDockPanel { get ; set ; }
    public DockablePaneId? HasekoDockPanelId { get ; set ; } 
  }
}