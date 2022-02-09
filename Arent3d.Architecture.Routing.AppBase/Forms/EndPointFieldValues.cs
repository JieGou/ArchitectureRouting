using Arent3d.Architecture.Routing.EndPoints ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public static class EndPointFieldValues
  {
    public static IEndPointVisitor<string> IdGetter { get ; } = new EndPointIdGetter() ;

    public static IEndPointVisitor<string> SubIdGetter { get ; } = new EndPointSubIdGetter() ;

    private class EndPointIdGetter : IEndPointVisitor<string>
    {
      public string Visit( ConnectorEndPoint endPoint ) => endPoint.EquipmentUniqueId ;
      public string Visit( RouteEndPoint endPoint ) => endPoint.RouteName ;
      public string Visit( PassPointEndPoint endPoint ) => endPoint.PassPointUniqueId ;
      public string Visit( PassPointBranchEndPoint endPoint ) => endPoint.PassPointUniqueId ;
      public string Visit( TerminatePointEndPoint endPoint ) => endPoint.TerminatePointUniqueId ;
    }

    private class EndPointSubIdGetter : IEndPointVisitor<string>
    {
      public string Visit( ConnectorEndPoint endPoint )=> endPoint.ConnectorIndex.ToString() ;
      public string Visit( RouteEndPoint endPoint ) => string.Empty ;
      public string Visit( PassPointEndPoint endPoint ) => string.Empty ;
      public string Visit( PassPointBranchEndPoint endPoint ) => string.Empty ;
      public string Visit( TerminatePointEndPoint endPoint ) => endPoint.LinkedInstanceUniqueId ;
    }
  }
}