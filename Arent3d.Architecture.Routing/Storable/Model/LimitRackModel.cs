using System.Collections.Generic ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class LimitRackModel
  {
    public IList<string> RackIds { get ; }
    public IList<string> RackDetailLineIds { get ; }

    public LimitRackModel( IList<string> rackIds, IList<string> rackDetailLineIds )
    {
      RackIds = rackIds ;
      RackDetailLineIds = rackDetailLineIds ;
    }
  }
}