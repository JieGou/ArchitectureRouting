using System.Collections.Generic ;
using System.ComponentModel ;
using System.Reflection ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.Updater ;
using Arent3d.Architecture.Routing.FittingSizeCalculators ;
using Arent3d.Architecture.Routing.Mechanical.haseko.App.Manager ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Events ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Events ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App
{
  /// <summary>
  /// Entry point of auto routing application. This class calls UI initializers.
  /// </summary>
  [RevitAddin( AppInfo.ApplicationGuid )]
  [DisplayName( AppInfo.ApplicationName )]
  public class RoutingApp : RoutingAppBase
  {
    private static FromToTreeHasekoManager? _fromToTreeHasekoManager = null ;

    public static FromToTreeHasekoManager FromToTreeHasekoManager => _fromToTreeHasekoManager ??= new FromToTreeHasekoManager() ;

    protected override string GetLanguageDirectoryPath()
    {
      return GetDefaultLanguageDirectoryPath( Assembly.GetExecutingAssembly() ) ;
    }

    protected override IAppUIBase? CreateAppUI( UIControlledApplication application )
    {
      RouteCache.CacheRefreshed += ( _, _ ) => FromToTreeHasekoManager.UpdateTreeView( AddInType.Mechanical ) ;

      return RoutingAppUI.Create( application ) ;
    }

    protected override void RegisterEvents( UIControlledApplication application )
    {
    }

    protected override void UnregisterEvents( UIControlledApplication application )
    {
    }

    protected override void OnDocumentListenStarted( DocumentKey documentKey )
    {
      DocumentMapper.Register( documentKey ) ;
      FromToTreeHasekoManager.OnDocumentOpened( AddInType.Mechanical ) ;
    }

    protected override void OnDocumentListenFinished( DocumentKey documentKey )
    {
      DocumentMapper.Unregister( documentKey ) ;
    }

    protected override void OnDocumentChanged( DocumentKey documentKey, DocumentChangedEventArgs e )
    {
      FromToTreeHasekoManager.OnDocumentChanged( e, AddInType.Mechanical ) ;
    }

    protected override void OnApplicationViewChanged( DocumentKey documentKey, ViewActivatedEventArgs e )
    {
      FromToTreeHasekoManager.OnViewActivated( e, AddInType.Mechanical ) ;
    }

    protected override IEnumerable<IDocumentUpdateListener> GetUpdateListeners()
    {
      yield return new AfterReducerCreationListener() ;
      yield return new RoutingUpdateListener( FromToTreeHasekoManager ) ;
    }
  }
}