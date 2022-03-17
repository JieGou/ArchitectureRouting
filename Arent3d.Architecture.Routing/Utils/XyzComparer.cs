using System ;
using System.Collections.Generic ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Utils
{
  public class XyzComparer : IEqualityComparer<XYZ>
  {
    public bool Equals( XYZ x, XYZ y )
    {
      return Math.Abs( x.X - y.X ) < GeometryUtil.Tolerance && Math.Abs( x.Y - y.Y ) < GeometryUtil.Tolerance && Math.Abs( x.Z - y.Z ) < GeometryUtil.Tolerance ;
    }

    public int GetHashCode( XYZ obj )
    {
      return 1 ;
    }
  }
}