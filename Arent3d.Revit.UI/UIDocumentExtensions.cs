using System ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Revit.UI
{
  public static class UIDocumentExtensions
  {
    public static UIView? GetActiveUIView( this UIDocument uiDocument )
    {
      var activeViewId = uiDocument.Document.ActiveView.Id ;

      return uiDocument.GetOpenUIViews().FirstOrDefault( uiView => uiView.ViewId.Equals( activeViewId ) ) ;
    }

    public static void SetSelection( this UIDocument uiDocument, Element element )
    {
      uiDocument.ShowElements( element ) ;
      uiDocument.Selection.SetElementIds( new[] { element.Id } ) ;
    }

    public static void SetSelection( this UIDocument uiDocument, IEnumerable<Element> elements )
    {
      var array = elements.Select( e => e.Id ).ToArray() ;
      uiDocument.ShowElements( array ) ;
      uiDocument.Selection.SetElementIds( array ) ;
    }

    public static void ClearSelection( this UIDocument uiDocument )
    {
      uiDocument.Selection.SetElementIds( Array.Empty<ElementId>() ) ;
    }
  }
}