using System ;
using System.IO ;
using System.Reflection ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Events ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Events ;

namespace Arent3d.Revit.UI
{
  public abstract class ExternalApplicationBase : IExternalApplication
  {
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

    protected abstract void OnDocumentListenStarted( Document document ) ;

    protected abstract void OnDocumentListenFinished( Document document ) ;

    protected virtual void OnDocumentChanged( Document document, DocumentChangedEventArgs e )
    {
    }

    private IAppUIBase? _ui ;

    public Result OnStartup( UIControlledApplication application )
    {
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
      
      return Result.Succeeded ;
    }

    public Result OnShutdown( UIControlledApplication application )
    {
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

    private void Application_ViewActivated( object sender, ViewActivatedEventArgs e )
    {
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