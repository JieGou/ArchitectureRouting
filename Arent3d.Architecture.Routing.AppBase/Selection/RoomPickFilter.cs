using Autodesk.Revit.DB ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase.Selection
{
  public class RoomPickFilter : ISelectionFilter
  {
    private readonly string _familyName ;

    public RoomPickFilter( string familyName )
    {
      _familyName = familyName ;
    }

    public bool AllowElement( Element e )
    {
      return e.Name == _familyName ;
    }

    public bool AllowReference( Reference r, XYZ p )
    {
      return false ;
    }
  }
}