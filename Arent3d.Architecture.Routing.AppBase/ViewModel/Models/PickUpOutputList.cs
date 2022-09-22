using System.Collections.Generic ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel.Models
{
  public class PickUpOutputList
  {
    public string ProductCode { get ; }
    
    public List<PickUpOutPutLevelItem> OutPutLevelItems { get ; } = new() ;

    public PickUpOutputList( string productCode )
    {
      ProductCode = productCode ;
    }
  }
}