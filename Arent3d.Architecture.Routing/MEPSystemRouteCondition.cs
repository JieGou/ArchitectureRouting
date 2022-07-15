using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Routing ;
using Arent3d.Routing.Conditions ;

namespace Arent3d.Architecture.Routing
{
  public class MEPSystemRouteCondition : IRouteCondition
  {
    private const string DefaultFluidPhase = "None" ;

    public IPipeDiameter Diameter { get ; }
    public double DiameterPipeAndInsulation => Diameter.Outside ;
    public double DiameterFlangeAndInsulation => Diameter.Outside ; // provisional
    IPipeSpec IRouteCondition.Spec => Spec ;
    public MEPSystemPipeSpec Spec { get ; }
    public ProcessConstraint ProcessConstraint { get ; }
    public IRoutingPathConstraints RoutingPathConstraints { get ; } = new DummyRoutingPathConstraints() ;

    public string FluidPhase => DefaultFluidPhase ;
    public double LowestHeight => Settings.BottomOfPipe ;
    public bool AllowTiltedPiping { get ; }

    public MEPSystemRouteCondition( MEPSystemPipeSpec pipeSpec, double diameter, AvoidType avoidType, bool allowTiltedPiping )
    {
      Spec = pipeSpec ;
      Diameter = diameter.DiameterValueToPipeDiameter() ;
      ProcessConstraint = (ProcessConstraint) avoidType ;
      AllowTiltedPiping = allowTiltedPiping ;
    }

    private class DummyRoutingPathConstraints : IRoutingPathConstraints
    {
      public IEnumerable<ILayerConstraint> RequiredLayers => Enumerable.Empty<ILayerConstraint>() ;
    }
  }
}