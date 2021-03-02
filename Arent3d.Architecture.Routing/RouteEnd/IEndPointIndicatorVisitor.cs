namespace Arent3d.Architecture.Routing.RouteEnd
{
  public interface IEndPointIndicatorVisitor
  {
    void Visit( ConnectorIndicator indicator ) ;
    void Visit( CoordinateIndicator indicator ) ;
    void Visit( PassPointEndIndicator indicator ) ;
    void Visit( PassPointBranchEndIndicator indicator ) ;
    void Visit( RouteIndicator indicator ) ;
  }

  public interface IEndPointIndicatorVisitor<out T>
  {
    T Visit( ConnectorIndicator indicator ) ;
    T Visit( CoordinateIndicator indicator ) ;
    T Visit( PassPointEndIndicator indicator ) ;
    T Visit( PassPointBranchEndIndicator indicator ) ;
    T Visit( RouteIndicator indicator ) ;
  }
}