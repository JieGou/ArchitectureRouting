using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Utils
{
  public static class FilterUtil
  {
    public static SelectionFilterElement FindOrCreateSelectionFilter( Document document, string selectionFilterName )
    {
      return document.GetAllInstances<SelectionFilterElement>()
        .SingleOrDefault( x => x.Name == selectionFilterName ) ?? SelectionFilterElement.Create( document, selectionFilterName ) ;
    }
    public static void AddElementToSelectionFilter( string selectionFilterName, Element element )
    {
      var selectionFilter = FindOrCreateSelectionFilter(element.Document, selectionFilterName) ;
      if ( selectionFilter.Contains( element.Id ) )
        return ;

      selectionFilter.AddSingle( element.Id ) ;
    }

    public static void AddElementsToSelectionFilter( Document document, string selectionFilterName, IEnumerable<Element> elements )
    {
      var selectionFilter = FindOrCreateSelectionFilter(document, selectionFilterName) ;
      foreach ( var element in elements ) {
        if ( selectionFilter.Contains( element.Id ) )
          continue ;

        selectionFilter.AddSingle( element.Id ) ;
      }
    }
  }
}