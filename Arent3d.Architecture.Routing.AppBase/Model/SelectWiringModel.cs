using System ;
using System.IO ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Model
{
  public class SelectWiringModel : NotifyPropertyChanged
  {
    public string Id { get ; set ; }
    public string RouteName { get ; set ; } 
    public string Floor { get ; }
    public string GeneralDisplayDeviceSymbol { get ; }
    public string WireType { get ; }
    public string WireSize { get ; }
    public string WireStrip { get ; }
    public string PipingType { get ; }
    public string PipingSize { get ; }

    public SelectWiringModel( string id, string routeName, string floor, string generalDisplayDeviceSymbol, string wireType,  string wireSize, string wireStrip, string pipingType, string pipingSize )
    {
      Id = id ;
      RouteName = routeName ;
      Floor = floor ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
      WireType = wireType ;
      WireSize = wireSize ;
      WireStrip = wireStrip ;
      PipingType = pipingType ;
      PipingSize = pipingSize ;
    }
  }
}