using System.ComponentModel ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.ApplicationServices ;
using Autodesk.Revit.DB.Events ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App
{
  /// <summary>
  /// Entry point of auto routing application. This class calls UI initializers.
  /// </summary>
  [Revit.RevitAddin( "{077B2D5D-D1EE-4511-9349-350745120633}" )]
  [DisplayName( "Routing" )]
  public class RoutingApp : IExternalApplication
  {
    private RoutingAppUI? _ui ;

    public Result OnStartup( UIControlledApplication application )
    {
      ThreadDispatcher.UiDispatcher = UiThread.RevitUiDispatcher ;

      if ( null != _ui ) return Result.Failed ;
      _ui = RoutingAppUI.Create( application ) ;

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
      DocumentMapper.Register( e.Document ) ;

      _ui?.UpdateRibbon( e.Document, RoutingAppUI.UpdateType.Start ) ;
    }

    private void DocumentListener_DocumentListeningFinished( object sender, DocumentEventArgs e )
    {
      DocumentMapper.Unregister( e.Document ) ;

      _ui?.UpdateRibbon( e.Document, RoutingAppUI.UpdateType.Finish ) ;
    }

    private void DocumentListener_ListeningDocumentChanged( object sender, DocumentChangedEventArgs e )
    {
      _ui?.UpdateRibbon( e.GetDocument(), RoutingAppUI.UpdateType.Change ) ;
    }
  }
}