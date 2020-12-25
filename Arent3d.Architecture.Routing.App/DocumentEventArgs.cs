using System ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.App
{
  /// <summary>
  /// Argument of <see cref="Document"/> events of <see cref="DocumentManager"/>.
  /// </summary>
  public class DocumentEventArgs : EventArgs
  {
    /// <summary>
    /// <see cref="Document"/> related to an event.
    /// </summary>
    public Document Document { get ; }

    internal DocumentEventArgs( Document doc )
    {
      Document = doc ;
    }
  }
}