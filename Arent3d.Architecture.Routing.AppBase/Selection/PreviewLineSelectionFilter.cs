using System.Collections.Generic ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase.Selection
{
  public class PreviewLineSelectionFilter : ISelectionFilter
  {
    private readonly List<ElementId> _lineIds ;

    public PreviewLineSelectionFilter( List<ElementId> lineIds )
    {
      _lineIds = lineIds ;
    }

    public bool AllowElement( Element e )
    {
      return _lineIds.Contains( e.Id ) ;
    }

    public bool AllowReference( Reference r, XYZ p )
    {
      return false ;
    }
  }
}