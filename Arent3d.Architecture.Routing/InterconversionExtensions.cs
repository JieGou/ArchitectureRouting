using System ;
using Arent3d.Architecture.Routing.Core ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Defines converters between Revit data structures and auto routing data structures.
  /// </summary>
  public static class InterconversionExtensions
  {
    public static Vector3d To3d( this XYZ xyz )
    {
      return new( xyz.X, xyz.Y, xyz.Z ) ;
    }

    public static IPipeDiameter DiameterValueToPipeDiameter( this double diameter )
    {
      return new PipeDiameter( diameter ) ;
    }

    private class PipeDiameter : IPipeDiameter
    {
      public PipeDiameter( double diameter )
      {
        Outside = diameter ;
        NPSmm = (int) Math.Floor( diameter ) ;  // provisional
      }

      public double Outside { get ; }
      public int NPSmm { get ; }
    }
  }
}