using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Extensions
{
    public static class ElementExtension
    {
        public static IEnumerable<IndependentTag> GetIndependentTagsFromElement( this Element element )
        {
            return element.GetDependentElements(null).Select(x => element.Document.GetElement(x)).OfType<IndependentTag>();
        }

        public static IEnumerable<Element> GetElementFromIndependentTag( this IndependentTag independentTag )
        {
#if REVIT2022
            return independentTag.GetTaggedLocalElements() ;
#else
          return new List<Element> { independentTag.GetTaggedLocalElement() } ;
#endif
        }
    }
}