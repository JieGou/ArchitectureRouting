using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arent3d.Architecture.Routing.AppBase.Model
{
  public class HeightSettingModel
  {
    public string? LevelName { get; set; }
    public double Elevation { get; set; }
    public double HeightOfLevel { get; set; }
    public double HeightOfConnectors { get; set; }
  }
}
