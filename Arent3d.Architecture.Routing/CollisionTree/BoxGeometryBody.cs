using System.Collections.Generic ;
using Arent3d.GeometryLib ;
using MathLib ;

namespace Arent3d.Architecture.Routing.CollisionTree
{
  public class BoxGeometryBody : IGeometryBody
  {
    public readonly IGeometry[] _geoms ;

    public BoxGeometryBody( Box3d box3d )
    {
      _geoms = new[] { Box.Create( new LocalCodSys3d( box3d.Center ), box3d.Size * 0.5 ) } ;
    }

    public IReadOnlyCollection<IGeometry> GetGeometries() => _geoms ;

    public IReadOnlyCollection<IGeometry> GetGlobalGeometries() => _geoms ;

    public Box3d GetGlobalGeometryBox() => _geoms[ 0 ].GetBounds() ;
  }
}