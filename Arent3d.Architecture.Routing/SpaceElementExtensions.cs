using System ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public enum SpaceType
  {
    GrandParent,
    Parent,
    Child
  }

  public static class SpaceElementExtensions
  {
    public static SpaceType? GetSpaceBranchNumber( this Element space )
    {
      if ( false == space.TryGetProperty( BranchNumberParameter.BranchNumber, out int spaceNum ) ) return null ;
      return GetSpaceType( spaceNum ) ;
    }

    private static SpaceType? GetSpaceType( int spaceNum )
    {
      if ( false == Enum.TryParse( spaceNum.ToString(), true, out SpaceType type ) ) return null ;
      return type ;
    }
  }
}