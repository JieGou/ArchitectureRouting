using System.Collections.Generic ;
using Autodesk.Revit.ApplicationServices ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Events ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App
{
  public static class DocumentListener
  {
    private static readonly Dictionary<int, Document> _closingDocuments = new() ;
    
    public static void RegisterEvents( UIControlledApplication application )
    {
      RegisterEvents( application.ControlledApplication ) ;
    }

    public static void UnregisterEvents( UIControlledApplication application )
    {
      UnregisterEvents( application.ControlledApplication ) ;
    }

    public static void RegisterEvents( ControlledApplication application )
    {
      application.DocumentOpened += Document_Opened ;
      application.DocumentCreated += Document_Created ;
      application.DocumentClosing += Document_Closing ;
      application.DocumentClosed += Document_Closed ;
    }

    public static void UnregisterEvents( ControlledApplication application )
    {
      application.DocumentOpened -= Document_Opened ;
      application.DocumentCreated -= Document_Created ;
      application.DocumentClosing -= Document_Closing ;
      application.DocumentClosed -= Document_Closed ;
    }

    private static void Document_Created( object sender, DocumentCreatedEventArgs e )
    {
      DocumentMapper.Register( e.Document ) ;
    }

    private static void Document_Opened( object sender, DocumentOpenedEventArgs e )
    {
      DocumentMapper.Register( e.Document ) ;
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
        DocumentMapper.Unregister( document ) ;
      }
    }
  }
}