using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI.Selection ;
using Autodesk.Revit.DB.Electrical ;

namespace Arent3d.Architecture.Routing.AppBase.Selection
{
  public class ConduitWithStartEndSelectionFilter : ISelectionFilter
  {
    public static ISelectionFilter Instance { get ; } = new ConduitWithStartEndSelectionFilter() ;

    private ConduitWithStartEndSelectionFilter()
    {
    }

    public bool AllowElement( Element elem )
    {
      return ( BuiltInCategory.OST_Conduit == elem.GetBuiltInCategory() || 
               BuiltInCategory.OST_ConduitFitting == elem.GetBuiltInCategory() ||
               BuiltInCategory.OST_ConduitRun == elem.GetBuiltInCategory() ||
               BuiltInCategory.OST_MechanicalEquipment == elem.GetBuiltInCategory() ||
               BuiltInCategory.OST_ElectricalFixtures == elem.GetBuiltInCategory() ||
               BuiltInCategory.OST_ElectricalEquipment == elem.GetBuiltInCategory())
               && elem is FamilyInstance or Conduit ;
    }

    public bool AllowReference( Reference reference, XYZ position ) => false ;
  }
}