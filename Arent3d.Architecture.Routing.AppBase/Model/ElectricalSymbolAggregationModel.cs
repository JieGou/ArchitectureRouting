using System ;
using System.IO ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Model
{
  public class ElectricalSymbolAggregationModel : NotifyPropertyChanged
  {
    public string ProductCode { get ; set ; }
    public string ProductName { get ; set ; }
    public int Number { get ; set ; }
    public string Unit { get ; set ; }
    public string Floor { get ; set ; }
    public string SetCode { get ; set ; }
    public string ConstructionItem { get ; set ; }

    public ElectricalSymbolAggregationModel( string floor, string setCode, string constructionItem, string code, string name, int number, string unit )
    {
      Floor = floor ;
      SetCode = setCode ;
      ConstructionItem = constructionItem ;
      ProductCode = code ;
      ProductName = name ;
      Number = number ;
      Unit = unit ;
    }
  }
}