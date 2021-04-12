using System.Collections.Generic ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  public class FromToItem
  {
    public string? Name { get; set; }
    public List<FromToItem>? Children { get; set; }
  }
}