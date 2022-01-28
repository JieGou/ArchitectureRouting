using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class CeedViewModel : ViewModelBase
  {
    public List<CeedModel> CeedModels { get ; }
    public CeedStorable CeedStorable { get ; }
    public readonly List<string> CeeDModelNumbers = new List<string>() ;
    public readonly List<string> ModelNumbers = new List<string>() ;

    public CeedViewModel( CeedStorable ceedStorable )
    {
      CeedStorable = ceedStorable ;
      CeedModels = ceedStorable.CeedModelData ;
      AddModelNumber( CeedModels ) ;
    }

    public CeedViewModel( CeedStorable ceedStorable, List<CeedModel> ceedModels )
    {
      CeedStorable = ceedStorable ;
      CeedModels = ceedModels ;
      AddModelNumber( ceedModels ) ;
    }

    private void AddModelNumber( List<CeedModel> ceedModels )
    {
      foreach ( var ceedModel in ceedModels.Where( ceedModel => ! string.IsNullOrEmpty( ceedModel.CeeDModelNumber ) ) ) {
        if ( ! CeeDModelNumbers.Contains( ceedModel.CeeDModelNumber ) ) CeeDModelNumbers.Add( ceedModel.CeeDModelNumber ) ;
      }
      foreach ( var ceedModel in ceedModels.Where( ceedModel => ! string.IsNullOrEmpty( ceedModel.ModelNumber ) ) ) {
        var modelNumbers = ceedModel.ModelNumber.Split( '\n' ) ;
        foreach ( var modelNumber in modelNumbers ) {
          if ( ! ModelNumbers.Contains( modelNumber ) ) ModelNumbers.Add( modelNumber ) ;
        }
      }
    }
  }
}