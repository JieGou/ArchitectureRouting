using System ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Events ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App
{
  /// <summary>
  /// Manages current <see cref="Document"/> of Revit.
  /// </summary>
  public static class DocumentManager
  {
    private static Document? _currentDocument = null ;

    /// <summary>
    /// Current document in Revit.
    /// </summary>
    public static Document? CurrentDocument
    {
      get => _currentDocument ;
      private set
      {
        if ( ReferenceEquals( _currentDocument, value ) ) return ;

        var org = _currentDocument ;
        _currentDocument = value ;

        if ( null != org ) {
          DocumentUnloaded?.Invoke( null, new DocumentEventArgs( org ) ) ;
        }

        if ( null != value ) {
          DocumentLoaded?.Invoke( null, new DocumentEventArgs( value ) ) ;
        }
      }
    }

    /// <summary>
    /// Invoked when a new document is loaded, it means `created' or `opened'.
    /// </summary>
    public static event EventHandler<DocumentEventArgs>? DocumentLoaded ;

    /// <summary>
    /// Invoked when a new document is unloaded, it means `closed'.
    /// </summary>
    public static event EventHandler<DocumentEventArgs>? DocumentUnloaded ;

    /// <summary>
    /// Invoked when a new document is refreshed, it means `reloaded'.
    /// </summary>
    public static event EventHandler<DocumentEventArgs>? DocumentRefreshed ;


    public static void Watch( UIControlledApplication application )
    {
      application.ControlledApplication.DocumentCreated += Application_DocumentCreated ;
      application.ControlledApplication.DocumentOpened += Application_DocumentOpened ;
      application.ControlledApplication.DocumentClosed += Application_DocumentClosed ;
      application.ControlledApplication.DocumentReloadedLatest += Application_DocumentReloadedLatest ;
    }

    public static void Unwatch( UIControlledApplication application )
    {
      application.ControlledApplication.DocumentCreated -= Application_DocumentCreated ;
      application.ControlledApplication.DocumentOpened -= Application_DocumentOpened ;
      application.ControlledApplication.DocumentClosed -= Application_DocumentClosed ;
      application.ControlledApplication.DocumentReloadedLatest -= Application_DocumentReloadedLatest ;
    }

    private static void OnCurrentDocumentRefreshed()
    {
      if ( null == CurrentDocument ) return ;

      DocumentRefreshed?.Invoke( null, new DocumentEventArgs( CurrentDocument ) ) ;
    }

    private static void Application_DocumentCreated( object sender, DocumentCreatedEventArgs e )
    {
      CurrentDocument = e.Document ;
    }

    private static void Application_DocumentOpened( object sender, DocumentOpenedEventArgs e )
    {
      CurrentDocument = e.Document ;
    }

    private static void Application_DocumentClosed( object sender, DocumentClosedEventArgs e )
    {
      CurrentDocument = null ;
    }

    private static void Application_DocumentReloadedLatest( object sender, DocumentReloadedLatestEventArgs e )
    {
      if ( ReferenceEquals( CurrentDocument, e.Document ) ) {
        OnCurrentDocumentRefreshed() ;
      }
      else {
        CurrentDocument = e.Document ;
      }
    }
  }
}