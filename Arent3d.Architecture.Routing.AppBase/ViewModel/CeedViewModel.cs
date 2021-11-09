using System ;
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
    public string CeeDNumberSearch { get ; }
    public readonly List<string> CeeDModelNumbers = new List<string>() ;

    public CeedViewModel( CeedStorable ceedStorable )
    {
      CeedStorable = ceedStorable ;
      CeedModels = ceedStorable.CeedModelData.Values.ToList() ;
      CeeDNumberSearch = string.Empty ;
      foreach ( var ceedModel in CeedModels ) {
        CeeDModelNumbers.Add( ceedModel.CeeDModelNumber );
      }
    }
    
    public CeedViewModel( CeedStorable ceedStorable, List<CeedModel> ceedModels, string ceeDNumberSearch )
    {
      CeedStorable = ceedStorable ;
      CeedModels = ceedModels ;
      CeeDNumberSearch = ceeDNumberSearch ;
      foreach ( var ceedModel in ceedStorable.CeedModelData.Values.ToList() ) {
        CeeDModelNumbers.Add( ceedModel.CeeDModelNumber );
      }
    }
  }
}