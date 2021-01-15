using System ;
using Arent3d.Routing ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Defines converters between Revit data structures and auto routing data structures.
  /// </summary>
  public static class InterconversionExtensions
  {
    public static Vector3d To3d( this XYZ xyz ) => new( xyz.X, xyz.Y, xyz.Z ) ;
    public static Box3d To3d( this BoundingBoxXYZ xyz ) => new( xyz.Min.To3d(), xyz.Max.To3d() ) ;

    public static XYZ ToXYZ( this Vector3d vec ) => new( vec.x, vec.y, vec.z ) ;

    public static IPipeDiameter DiameterValueToPipeDiameter( this double diameter )
    {
      return new PipeDiameter( diameter ) ;
    }

    public static IPipeDiameter GetDiameter( this Connector connector )
    {
      return ( connector.Shape switch
      {
        ConnectorProfileType.Oval => connector.Radius * 2,
        ConnectorProfileType.Rectangular => Math.Max( connector.Width, connector.Height ),
        ConnectorProfileType.Round => connector.Radius * 2,
        _ => throw new ArgumentOutOfRangeException(),
      } ).DiameterValueToPipeDiameter() ;
    }

    public static void SetDiameter( this Connector connector, IPipeDiameter diameter )
    {
      switch ( connector.Shape ) {
        case ConnectorProfileType.Oval :
          connector.Radius = diameter.Outside * 0.5 ;
          break ;

        case ConnectorProfileType.Rectangular :
        {
          var ratio = diameter.Outside / Math.Max( connector.Width, connector.Height ) ;
          connector.Width *= ratio ;
          connector.Height *= ratio ;
          break ;
        }

        case ConnectorProfileType.Round :
          connector.Radius = diameter.Outside * 0.5 ;
          break ;

        default : throw new ArgumentOutOfRangeException() ;
      }
    }

    private class PipeDiameter : IPipeDiameter
    {
      public PipeDiameter( double diameter )
      {
        Outside = diameter ;
        NPSmm = (int) Math.Floor( diameter * 1000 ) ; // provisional
      }

      public double Outside { get ; }
      public int NPSmm { get ; }
    }
  }
}