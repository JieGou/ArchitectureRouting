using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase.Selection
{
  public class StraightConduitSelectionFilter: ISelectionFilter
  {
    public static ISelectionFilter Instance { get ; } = new StraightConduitSelectionFilter() ;

    private StraightConduitSelectionFilter()
    {
    }

    public bool AllowElement( Element elem )
    {
      return BuiltInCategory.OST_Conduit == elem.GetBuiltInCategory() ;
    }

    public bool AllowReference( Reference reference, XYZ position ) => false ;
  }
}