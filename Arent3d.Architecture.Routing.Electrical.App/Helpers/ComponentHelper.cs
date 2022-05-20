using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Extensions ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using System;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Helpers
{
  public static class ComponentHelper
  {
    public static Dictionary<string, string> RepeatNames => new()
    {
      { "漏水帯（布）", "Circle Repeat" },
      { "漏水帯（発色）", "Square Repeat" },
      { "漏水帯（塩ビ）", "Vertical Repeat" }
    } ;
  }
}