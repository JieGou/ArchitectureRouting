using System ;
using System.Collections.Generic ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Utils
{
  public class XyzComparer : IEqualityComparer<XYZ>
  {
    public bool Equals( XYZ x, XYZ y )
    {
      return Math.Abs( x.X - y.X ) < 0.0001 && Math.Abs( x.Y - y.Y ) < 0.0001 && Math.Abs( x.Z - y.Z ) < 0.0001 ;
    }

    public int GetHashCode( XYZ obj )
    {
      return 1 ;
    }
  }
}