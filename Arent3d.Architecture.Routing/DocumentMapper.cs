using System ;
using Autodesk.Revit.ApplicationServices ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Events ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Manages current <see cref="Document"/> of Revit.
  /// </summary>
  public class DocumentMapper : ObjectMapper<DocumentMapper, Document, DocumentData>
  {
    private DocumentMapper()
    {
    }
  }
}