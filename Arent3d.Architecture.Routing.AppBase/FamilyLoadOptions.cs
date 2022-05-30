using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase
{
  public class FamilyLoadOptions : IFamilyLoadOptions
  {
    public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
    {
      overwriteParameterValues = true;
      return true;
    }

    public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
    {
      source = FamilySource.Family;
      overwriteParameterValues = true;
      return true;
    }
  }
}