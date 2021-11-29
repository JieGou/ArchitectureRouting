using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public enum SpaceType
  {
    Invalid = -1,
    GrandParent = 0
  }

  public static class SpaceElementExtensions
  {
    public static int GetSpaceBranchNumber( this Element space )
    {
      if ( false == space.TryGetProperty( BranchNumberParameter.BranchNumber, out int spaceNum ) ) return -1 ;
      return spaceNum ;
    }
  }
}