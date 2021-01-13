using System.Collections ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Core ;

namespace Arent3d.Architecture.Routing.Rack
{
  public class RackCollection : IStructureGraph
  {
    private readonly HashSet<Rack> _racks = new() ;
    private readonly HashSet<(IStructureInfo, IStructureInfo)> _links = new() ;

    public bool AddRack( Rack rack )
    {
      return _racks.Add( rack ) ;
    }

    public bool RemoveRack( Rack rack )
    {
      if ( false == _racks.Remove( rack ) ) return false ;

      _links.RemoveWhere( tuple => ( tuple.Item1 == rack || tuple.Item2 == rack ) ) ;
      
      return true ;
    }

    public bool AddLink( Rack rack1, Rack rack2 )
    {
      if ( ! _racks.Contains( rack1 ) || ! _racks.Contains( rack2 ) ) return false ;

      if ( _links.Contains( ( (IStructureInfo) rack2, (IStructureInfo) rack1 ) ) ) return false ;

      return _links.Add( ( (IStructureInfo) rack1, (IStructureInfo) rack2 ) ) ;
    }

    public bool RemoveLink( Rack rack1, Rack rack2 )
    {
      return _links.Remove( ( (IStructureInfo) rack2, (IStructureInfo) rack1 ) ) || _links.Remove( ( (IStructureInfo) rack1, (IStructureInfo) rack2 ) ) ;
    }

    public void Clear()
    {
      _racks.Clear() ;
      _links.Clear() ;
    }

    public int RackCount => _racks.Count ;
    public int LinkCount => _links.Count ;
    
    public IEnumerable<IStructureInfo> Nodes => _racks ;

    public IEnumerable<(IStructureInfo, IStructureInfo)> Edges => _links ;
  }
}