using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Reflection ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Events ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Events ;

namespace Arent3d.Revit.UI
{
  public abstract class ExternalApplicationBase : IExternalApplication
  {
    private readonly List<UpdaterId> _updaterIds = new() ;
    
    protected abstract IAppUIBase? CreateAppUI( UIControlledApplication application ) ;

    protected abstract string GetLanguageDirectoryPath() ;

    protected static string GetDefaultLanguageDirectoryPath( Assembly assembly )
    {
      var assemblyPath = assembly.Location ;

      var dirPath = Path.GetDirectoryName( assemblyPath )! ;
      var assemblyName = Path.GetFileNameWithoutExtension( assemblyPath )! ;

      return Path.Combine( dirPath, assemblyName, "Lang" ) ;
    }

    protected virtual void RegisterEvents( UIControlledApplication application )
    {
    }

    protected virtual void UnregisterEvents( UIControlledApplication application )
    {
    }

    protected virtual void OnApplicationViewChanged( Document document, ViewActivatedEventArgs e )
    {
    }

    protected abstract void OnDocumentListenStarted( Document document ) ;

    protected abstract void OnDocumentListenFinished( Document document ) ;

    protected virtual void OnDocumentChanged( Document document, DocumentChangedEventArgs e )
    {
    }

    private IAppUIBase? _ui ;

    public Result OnStartup( UIControlledApplication application )
    {
      var addInId = application.ActiveAddInId ;

      ThreadDispatcher.UiDispatcher = UiThread.RevitUiDispatcher ;

      I18n.LanguageConverter.SetApplicationLanguage( application.ControlledApplication.Language ) ;
      I18n.LanguageConverter.AddLanguageDirectoryPath( GetLanguageDirectoryPath() ) ;

      if ( null != _ui ) return Result.Failed ;
      _ui = CreateAppUI( application ) ;

      DocumentListener.DocumentListeningStarted += DocumentListener_DocumentListeningStarted ;
      DocumentListener.DocumentListeningFinished += DocumentListener_DocumentListeningFinished ;
      DocumentListener.ListeningDocumentChanged += DocumentListener_ListeningDocumentChanged ;
      DocumentListener.RegisterEvents( application.ControlledApplication ) ;

      application.ViewActivated += Application_ViewActivated ;

      RegisterEvents( application ) ;

      foreach ( var listener in GetUpdateListeners() ) {
        if ( RegisterListener( addInId, listener ) is { } updaterId ) {
          _updaterIds.Add( updaterId ) ;
        }
      }

      return Result.Succeeded ;
    }

    public Result OnShutdown( UIControlledApplication application )
    {
      _updaterIds.ForEach( UpdaterRegistry.UnregisterUpdater ) ;
      _updaterIds.Clear() ;

      UnregisterEvents( application ) ;

      application.ViewActivated -= Application_ViewActivated ;

      DocumentListener.DocumentListeningStarted -= DocumentListener_DocumentListeningStarted ;
      DocumentListener.DocumentListeningFinished -= DocumentListener_DocumentListeningFinished ;
      DocumentListener.ListeningDocumentChanged -= DocumentListener_ListeningDocumentChanged ;
      DocumentListener.UnregisterEvents( application.ControlledApplication ) ;

      _ui?.Dispose() ;
      _ui = null ;

      return Result.Succeeded ;
    }

    protected virtual IEnumerable<IDocumentUpdateListener> GetUpdateListeners() => Enumerable.Empty<IDocumentUpdateListener>() ;

    private static UpdaterId? RegisterListener( AddInId addInId, IDocumentUpdateListener listener )
    {
      var updater = new DocumentUpdaterByListener( addInId, listener ) ;
      if ( false == updater.Register() ) return null ;
      
      return updater.GetUpdaterId() ;
    }

    private void Application_ViewActivated( object sender, ViewActivatedEventArgs e )
    {
      OnApplicationViewChanged( e.Document, e ) ;

      _ui?.UpdateUI( e.Document, AppUIUpdateType.ViewChange ) ;
    }

    private void DocumentListener_DocumentListeningStarted( object sender, DocumentEventArgs e )
    {
      OnDocumentListenStarted( e.Document ) ;

      _ui?.UpdateUI( e.Document, AppUIUpdateType.Start ) ;
    }

    private void DocumentListener_DocumentListeningFinished( object sender, DocumentEventArgs e )
    {
      OnDocumentListenFinished( e.Document ) ;

      _ui?.UpdateUI( e.Document, AppUIUpdateType.Finish ) ;
    }

    private void DocumentListener_ListeningDocumentChanged( object sender, DocumentChangedEventArgs e )
    {
      var document = e.GetDocument() ;
      OnDocumentChanged( document, e ) ;
      _ui?.UpdateUI( document, AppUIUpdateType.Change ) ;
    }
  }
}