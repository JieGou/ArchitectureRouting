using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arent3d.Architecture.Routing.AppBase.Model
{
  public class HeightSettingModel
  {
    public Dictionary<int, double> HeightOfLevels { get; set; } = new Dictionary<int, double>()
    {
      {0, 4000 },
      {1, 8000 }
    };
    public Dictionary<int, double> HeightOfConnectorsByLevel { get; set; } = new Dictionary<int, double>()
    {
      {0, 2000 },
      {1, 2000 }
    };
  }
}
