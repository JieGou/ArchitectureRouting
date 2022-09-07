using System.Collections.Generic ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Model
{
  public class RackMap
  {
    public string RouteName { get ; }
    public IList<string> RackIds { get ; } = new List<string>() ;
    public IList<Element> CableTrays { get ; } = new List<Element>() ;
    public IList<Element> CableTrayFittings { get ; } = new List<Element>() ;
    public IList<string> RackDetailCurveIds { get ; } = new List<string>() ;

    public RackMap( string routeName ) => RouteName = routeName ;
  }
}