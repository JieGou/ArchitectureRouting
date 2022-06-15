using System.Collections.Generic ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class LimitRackModel
  {
    public List<string> LimitRackIds { get ; set ; } = new() ;

    public List<string> LitmitRackFittingIds { get ; set ; } = new() ;
    public List<string> LimitRackDetailIds { get ; set ; } = new() ;
  }
}