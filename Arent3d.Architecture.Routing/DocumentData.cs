using Arent3d.Architecture.Routing.Rack ;
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

    /// <summary>
    /// Returns racks within the document.
    /// </summary>
    public RackCollection RackCollection { get ; } = new() ;

    /// <summary>
    /// Returns whether a route will be auto-routed on pipe racks.
    /// </summary>
    /// <param name="route"></param>
    /// <returns></returns>
    public bool IsRoutingOnPipeRacks( Route route )
    {
      return ( 0 < RackCollection.RackCount ) ;
    }
  }
}