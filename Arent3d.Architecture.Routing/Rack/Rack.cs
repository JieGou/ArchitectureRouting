using System ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Core ;
using MathLib ;

namespace Arent3d.Architecture.Routing.Rack
{
  public class Rack : IStructureInfo
  {
    public Rack()
    {
      Layers = new ILayerProperty[] { new LayerProperty( this ) } ;
    }

    public IEnumerable<ILayerProperty> Layers { get ; }
    public bool IsPipeRack { get ; } = true ;
    public bool IsMainRack { get ; set ; }
    public string Name { get ; set ; } = string.Empty ;

    public Vector3d Center { get ; set ; }
    public Vector3d Size { get ; set ; }
    public double BeamInterval { get ; set ; }

    private class LayerProperty : ILayerProperty
    {
      private readonly Rack _rack ;

      public LayerProperty( Rack rack )
      {
        _rack = rack ;
        PipingProperties = new[] { new PipingProperty( this ) } ;
      }

      public Vector3d Center => _rack.Center ;
      public Vector3d Size => _rack.Size ;
      public double ConnectionHeight => Center.z - Size.z * 0.5 ;
      public double BeamInterval => _rack.BeamInterval ;
      public bool IsXDirection => _rack.Size.y <= _rack.Size.x ;
      public bool IsReverseDir => false ;
      public IEnumerable<IPipingProperty> PipingProperties { get ; }
      public IEnumerable<ISpaceProperty> UsedSpaces => Array.Empty<ISpaceProperty>() ;
      public IEnumerable<ISpaceProperty> SideUsedSpace => Array.Empty<ISpaceProperty>() ;
    }

    private class PipingProperty : IPipingProperty
    {
      private readonly LayerProperty _layerProperty ;

      public PipingProperty( LayerProperty layerProperty )
      {
        _layerProperty = layerProperty ;
      }

      public RangeD Range
      {
        get
        {
          if ( _layerProperty.IsXDirection ) {
            return RangeD.ConstructFromCenterHalfWidth( _layerProperty.Center.y, _layerProperty.Size.y * 0.5 ) ;
          }
          else {
            return RangeD.ConstructFromCenterHalfWidth( _layerProperty.Center.x, _layerProperty.Size.x * 0.5 ) ;
          }
        }
      }

      public LineType PrimaryPipingType => LineType.Utility ;
      public IReadOnlyCollection<LineType> ExtraPipingTypes => Array.Empty<LineType>() ;
    }
  }
}