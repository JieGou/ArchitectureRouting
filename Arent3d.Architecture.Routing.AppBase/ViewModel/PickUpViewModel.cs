using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class PickUpViewModel : ViewModelBase
  {
    private PickUpStorable PickUpStorable { get ; }
    public List<PickUpModel> PickUpModels { get ; }

    public PickUpViewModel( PickUpStorable pickUpStorable, List<PickUpModel> pickUpData )
    {
      PickUpStorable = pickUpStorable ;
      PickUpModels = pickUpData ;
    }
  }
}