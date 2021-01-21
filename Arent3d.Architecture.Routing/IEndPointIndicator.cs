using System ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public interface IEndPointIndicator : IEquatable<IEndPointIndicator>
  {
    EndPoint? GetEndPoint( Document document, SubRoute subRoute, bool isFrom ) ;
  }
}