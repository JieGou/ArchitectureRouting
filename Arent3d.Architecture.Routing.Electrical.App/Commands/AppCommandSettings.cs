using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.FittingSizeCalculators ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands
{
  public static class AppCommandSettings
  {
    public static AddInType AddInType => AddInType.Electrical ;

    public static RoutingExecutor CreateRoutingExecutor( Document document, View view ) => new ElectricalRoutingExecutor( document, view, FittingSizeCalculator ) ;
    public static IFittingSizeCalculator FittingSizeCalculator => DefaultFittingSizeCalculator.Instance ;
  }
}