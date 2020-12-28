using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public class DocumentData : IMappedObject<Document>
  {
    /// <summary>
    /// Returns the owner <see cref="Document"/>.
    /// </summary>
    public Document Document { get ; }
    
    Document IMappedObject<Document>.BaseObject => Document ;

    private DocumentData( Document document )
    {
      Document = document ;
    }
  }
}