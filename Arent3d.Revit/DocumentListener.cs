using System ;
using System.Collections.Generic ;
using Autodesk.Revit.ApplicationServices ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Events ;

namespace Arent3d.Revit
{
  public class DocumentEventArgs : EventArgs
  {
    public Document Document { get ; }
    internal DocumentEventArgs( Document document )
    {
      Document = document ;
    }
  }
  
  public static class DocumentListener
  {
    private static readonly Dictionary<int, Document> _closingDocuments = new() ;

    public static event EventHandler<DocumentEventArgs>? DocumentListeningStarted ;
    public static event EventHandler<DocumentEventArgs>? DocumentListeningFinished ;
    public static event EventHandler<DocumentChangedEventArgs>? ListeningDocumentChanged ;

    public static void RegisterEvents( ControlledApplication application )
    {
      application.DocumentChanged += Document_Changed ;
      application.DocumentOpened += Document_Opened ;
      application.DocumentCreated += Document_Created ;
      application.DocumentClosing += Document_Closing ;
      application.DocumentClosed += Document_Closed ;
    }

    public static void UnregisterEvents( ControlledApplication application )
    {
      application.DocumentChanged -= Document_Changed ;
      application.DocumentOpened -= Document_Opened ;
      application.DocumentCreated -= Document_Created ;
      application.DocumentClosing -= Document_Closing ;
      application.DocumentClosed -= Document_Closed ;
    }

    private static void Document_Changed( object sender, DocumentChangedEventArgs e )
    {
      var document = e.GetDocument() ;
      ListeningDocumentChanged?.Invoke( document, e ) ;
    }

    private static void Document_Created( object sender, DocumentCreatedEventArgs e )
    {
      if ( null == e.Document ) return ;
      DocumentListeningStarted?.Invoke( e.Document, new DocumentEventArgs( e.Document ) ) ;
    }

    private static void Document_Opened( object sender, DocumentOpenedEventArgs e )
    {
      if ( null == e.Document ) return ;
      DocumentListeningStarted?.Invoke( e.Document, new DocumentEventArgs( e.Document ) ) ;
    }

    private static void Document_Closing( object sender, DocumentClosingEventArgs e )
    {
      SetDocumentMayBeClosed( e.DocumentId, e.Document ) ;
    }

    private static void SetDocumentMayBeClosed( int documentId, Document document )
    {
      _closingDocuments[ documentId ] = document ;
    }

    private static void Document_Closed( object sender, DocumentClosedEventArgs e )
    {
      if ( _closingDocuments.TryGetValue( e.DocumentId, out var document ) ) {
        _closingDocuments.Remove( e.DocumentId ) ;
        StorableCache.ReleaseCaches( document ) ;
        DocumentListeningFinished?.Invoke( document, new DocumentEventArgs( document ) ) ;
      }
    }
  }
}