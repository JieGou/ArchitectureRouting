using System.Collections.Generic ;
using System.ComponentModel ;
using System.Reflection ;
using System.Runtime.Remoting.Channels ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.Updater ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Events ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Events ;

namespace Arent3d.Architecture.Routing.Electrical.App
{
  /// <summary>
  /// Entry point of auto routing application. This class calls UI initializers.
  /// </summary>
  [RevitAddin( AppInfo.ApplicationGuid )]
  [DisplayName( AppInfo.ApplicationName )]
  internal class RoutingApp : RoutingAppBase<RoutingApp>
  {
    private static FromToTreeManager? _fromToTreeManager = null ;

    public static FromToTreeManager FromToTreeManager => _fromToTreeManager ??= new FromToTreeManager() ;

    protected override string GetLanguageDirectoryPath()
    {
      return GetDefaultLanguageDirectoryPath( Assembly.GetExecutingAssembly() ) ;
    }

    protected override IAppUIBase? CreateAppUI( UIControlledApplication application )
    {
      RouteCache.CacheRefreshed += ( _, _ ) => FromToTreeManager.UpdateTreeView( AddInType.Electrical ) ;

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
      FromToTreeManager.OnDocumentOpened( AddInType.Electrical ) ;
    }

    protected override void OnDocumentListenFinished( DocumentKey documentKey )
    {
      DocumentMapper.Unregister( documentKey ) ;
    }

    protected override void OnDocumentChanged( DocumentKey documentKey, DocumentChangedEventArgs e )
    {
      FromToTreeManager.OnDocumentChanged( e, AddInType.Electrical ) ;
    }

    protected override void OnApplicationViewChanged( DocumentKey documentKey, ViewActivatedEventArgs e )
    {
      FromToTreeManager.OnViewActivated( e, AddInType.Electrical ) ;
    }

    protected override IEnumerable<IDocumentUpdateListener> GetUpdateListeners()
    {
      yield return new RoutingUpdateListener( FromToTreeManager ) ;
      yield return new NotationForRackListener() ;
    }
  }
}