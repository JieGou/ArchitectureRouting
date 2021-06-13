using System.Collections.Generic ;
using System.ComponentModel ;
using System.Reflection ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.Updater ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Events ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Events ;

namespace Arent3d.Architecture.Routing.Mechanical.App
{
  /// <summary>
  /// Entry point of auto routing application. This class calls UI initializers.
  /// </summary>
  [RevitAddin( AppInfo.ApplicationGuid )]
  [DisplayName( AppInfo.ApplicationName )]
  public class RoutingApp : RoutingAppBase
  {
    private static FromToTreeManager? _fromToTreeManager = null ;

    public static FromToTreeManager FromToTreeManager => _fromToTreeManager ??= new FromToTreeManager() ;
    protected override string GetLanguageDirectoryPath()
    {
      return GetDefaultLanguageDirectoryPath( Assembly.GetExecutingAssembly() ) ;
    }

    protected override IAppUIBase? CreateAppUI( UIControlledApplication application )
    {
      return RoutingAppUI.Create( application ) ;
    }

    protected override void RegisterEvents( UIControlledApplication application )
    {
    }

    protected override void UnregisterEvents( UIControlledApplication application )
    {
    }

    protected override void OnDocumentListenStarted( Document document )
    {
      DocumentMapper.Register( document ) ;
      FromToTreeManager.OnDocumentOpened() ;
    }

    protected override void OnDocumentListenFinished( Document document )
    {
      DocumentMapper.Unregister( document ) ;
    }

    protected override void OnDocumentChanged( Document document, DocumentChangedEventArgs e )
    {
      FromToTreeManager.OnDocumentChanged( e ) ;
    }

    protected override void OnApplicationViewChanged( Document document, ViewActivatedEventArgs e )
    {
      FromToTreeManager.OnViewActivated( e ) ;
    }

    protected override IEnumerable<IDocumentUpdateListener> GetUpdateListeners()
    {
      yield return new RoutingUpdateListener() ;
    }
    
  }
}