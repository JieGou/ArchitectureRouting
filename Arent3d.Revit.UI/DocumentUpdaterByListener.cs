using System ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit.UI
{
  internal class DocumentUpdaterByListener : IUpdater
  {
    private readonly AddInId _addInId ;
    private readonly UpdaterId _updaterId ;
    private readonly IDocumentUpdateListener _listener ;
    
    public DocumentUpdaterByListener( AddInId addInId, IDocumentUpdateListener listener )
    {
      _addInId = addInId ;
      _updaterId = new UpdaterId( addInId, Guid.NewGuid() ) ;
      _listener = listener ;
    }

    public void Execute( UpdaterData data )
    {
      _listener.Execute( data ) ;
    }

    public UpdaterId GetUpdaterId() => _updaterId ;

    public ChangePriority GetChangePriority() => _listener.ChangePriority ;

    public string GetUpdaterName() => _listener.Name ;

    public string GetAdditionalInformation() => _listener.Description ;

    public bool Register()
    {
      if ( null == GetChangeType( _listener ) ) return false ;

      UpdaterRegistry.RegisterUpdater( this ) ;
      UpdaterRegistry.AddTrigger( _updaterId, _listener.GetElementFilter(), GetChangeType( _listener ) ) ;

      return true ;
    }

    private static ChangeType? GetChangeType( IDocumentUpdateListener listener )
    {
      var listenType = listener.ListenType ;
      if ( listenType == DocumentUpdateListenType.Any ) return Element.GetChangeTypeAny() ;

      var list = new List<ChangeType>() ;
      if ( 0 != ( listenType & DocumentUpdateListenType.Parameter ) ) {
        list.AddRange( listener.GetListeningParameters().Select( parameter => parameter.GetChangeTypeParameter() ) ) ;
      }
      if ( 0 != ( listenType & DocumentUpdateListenType.Geometry ) ) {
        list.Add( Element.GetChangeTypeGeometry() ) ;
      }
      if ( 0 != ( listenType & DocumentUpdateListenType.Addition ) ) {
        list.Add( Element.GetChangeTypeElementAddition() ) ;
      }
      if ( 0 != ( listenType & DocumentUpdateListenType.Deletion ) ) {
        list.Add( Element.GetChangeTypeElementDeletion() ) ;
      }

      if ( 0 == list.Count ) return null ;
      if ( 1 == list.Count ) return list[ 0 ] ;

      var concat = ChangeType.ConcatenateChangeTypes( list[ 0 ], list[ 1 ] ) ;
      for ( int i = 2, n = list.Count ; i < n ; ++i ) {
        concat = ChangeType.ConcatenateChangeTypes( concat, list[ i ] ) ;
      }

      return concat ;
    }
  }
}