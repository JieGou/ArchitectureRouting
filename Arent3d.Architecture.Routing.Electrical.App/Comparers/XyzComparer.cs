using System.Collections.Generic;
using Arent3d.Architecture.Routing.AppBase ;
using Autodesk.Revit.DB;

namespace Arent3d.Architecture.Routing.Electrical.App.Comparers
{
  public class XyzComparer : IEqualityComparer<XYZ>
  {
    public bool Equals( XYZ firstPoint, XYZ secondPoint )
    {
      return firstPoint.IsAlmostEqualTo( secondPoint, GeometryHelper.Tolerance ) ;
    }

    public int GetHashCode( XYZ point )
    {
      return point.GetHashCode() * 2 ;
    }
  }
}