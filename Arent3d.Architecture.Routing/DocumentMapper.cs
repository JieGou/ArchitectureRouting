using System ;
using System.Collections.Generic ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Manages current <see cref="Document"/> of Revit.
  /// </summary>
  public static class DocumentMapper
  {
    private static readonly Dictionary<DocumentKey, DocumentData> _mapper = new() ;

    public static DocumentData Get( Document document )
    {
      return _mapper.TryGetValue( DocumentKey.Get( document ), out var data ) ? data : throw new KeyNotFoundException() ;
    }

    public static void Register( DocumentKey documentKey )
    {
      if ( _mapper.ContainsKey( documentKey ) ) return ; // duplicated

      _mapper.Add( documentKey, new DocumentData( documentKey.Document ) ) ;
      
      // TODO: search auto routing families
    }

    public static void Unregister( DocumentKey documentKey )
    {
      _mapper.Remove( documentKey ) ;
    }
  }
}