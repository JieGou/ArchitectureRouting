using Arent3d.Routing ;

namespace Arent3d.Architecture.Routing
{
  internal class MEPSystemPipeSpec : IPipeSpec
  {
    private readonly RouteMEPSystem _sys ;

    public MEPSystemPipeSpec( RouteMEPSystem routeMepSystem )
    {
      _sys = routeMepSystem ;
    }
    
    public double GetLongElbowSize( IPipeDiameter diameter )
    {
      return _sys.Get90ElbowSize( diameter.Outside ) ;
    }

    public double Get45ElbowSize( IPipeDiameter diameter )
    {
      return _sys.Get45ElbowSize( diameter.Outside ) ;
    }

    public double GetTeeBranchLength( IPipeDiameter header, IPipeDiameter branch )
    {
      return _sys.GetTeeBranchLength( header.Outside, branch.Outside ) ;
    }

    public double GetTeeHeaderLength( IPipeDiameter header, IPipeDiameter branch )
    {
      return _sys.GetTeeHeaderLength( header.Outside, branch.Outside ) ;
    }

    private double GetReducerLength( IPipeDiameter header, IPipeDiameter branch )
    {
      return _sys.GetReducerLength( header.Outside, branch.Outside ) ;
    }

    public double GetWeldMinDistance( IPipeDiameter diameter )
    {
      return _sys.GetWeldMinDistance( diameter.Outside ) ;
    }

    public string Name => _sys.MEPSystemType.Name ;
  }
}