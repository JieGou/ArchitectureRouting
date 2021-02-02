using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Events ;
using Autodesk.Revit.UI ;

namespace Arent3d.Revit.UI
{
  public abstract class ExternalApplicationBase : IExternalApplication
  {
    protected abstract IAppUIBase? CreateAppUI( UIControlledApplication application ) ;

    protected virtual void OnDocumentListenStarted( Document document )
    {
    }

    protected virtual void OnDocumentListenFinished( Document document )
    {
    }

    private IAppUIBase? _ui ;

    public Result OnStartup( UIControlledApplication application )
    {
      ThreadDispatcher.UiDispatcher = UiThread.RevitUiDispatcher ;

      if ( null != _ui ) return Result.Failed ;
      _ui = CreateAppUI( application ) ;

      DocumentListener.DocumentListeningStarted += DocumentListener_DocumentListeningStarted ;
      DocumentListener.DocumentListeningFinished += DocumentListener_DocumentListeningFinished ;
      DocumentListener.ListeningDocumentChanged += DocumentListener_ListeningDocumentChanged ;
      DocumentListener.RegisterEvents( application.ControlledApplication ) ;

      return Result.Succeeded ;
    }

    public Result OnShutdown( UIControlledApplication application )
    {
      DocumentListener.DocumentListeningStarted -= DocumentListener_DocumentListeningStarted ;
      DocumentListener.DocumentListeningFinished -= DocumentListener_DocumentListeningFinished ;
      DocumentListener.ListeningDocumentChanged -= DocumentListener_ListeningDocumentChanged ;
      DocumentListener.UnregisterEvents( application.ControlledApplication ) ;

      _ui?.Dispose() ;
      _ui = null ;

      return Result.Succeeded ;
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
      _ui?.UpdateUI( e.GetDocument(), AppUIUpdateType.Change ) ;
    }
  }
}