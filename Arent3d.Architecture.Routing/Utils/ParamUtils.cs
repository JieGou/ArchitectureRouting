using System ;

namespace Arent3d.Architecture.Routing.Utils
{
  public static class ParamUtils
  {
    public static int ToColorParameterValue(
      int red, 
      int green, 
      int blue)
    {
      return red + (green << 8) + (blue << 16);
    }
  }
}