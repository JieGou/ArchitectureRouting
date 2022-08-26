using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Extensions
{
    public static class ElementExtension
    {
        public static IEnumerable<IndependentTag> GetTagsFromElement( this Element element )
        {
            var tags = element.Document.GetAllInstances<IndependentTag>() ;
            return ! tags.Any() ? Enumerable.Empty<IndependentTag>() : tags.Where( x => x.GetTaggedLocalElements().Any( y => y.Id == element.Id ) ) ;
        }
    }
}