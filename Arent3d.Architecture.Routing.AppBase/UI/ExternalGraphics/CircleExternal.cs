using System ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics
{
  public class CircleExternal : DrawExternalBase
  {
    public CircleExternal( UIApplication uiApplication ) : base( uiApplication )
    {
    }

    public override void DrawExternal()
    {
      this.DrawingServer.CurveList.Clear() ;

      if ( this.DrawingServer?.BasePoint == null || this.DrawingServer.NextPoint == null || this.DrawingServer.BasePoint.DistanceTo( this.DrawingServer.NextPoint ) <= 0.001 ) {
        return ;
      }

      double startAngle = 0 ;
      double endAngle = Math.PI * 2 ;
      XYZ xAxis = new XYZ( 1, 0, 0 ) ;
      XYZ yAxis = new XYZ( 0, 1, 0 ) ;

      var center = this.DrawingServer.BasePoint ;
      var point = this.DrawingServer.NextPoint ;
      var radius = center.DistanceTo( point ) ;

      if ( center.DistanceTo( point ) > 0.001 ) {
        var circle = Arc.Create( center, radius, startAngle, endAngle, xAxis, yAxis ) ;
        this.DrawingServer.CurveList.Add( circle ) ;
        this.DrawingServer.CurveList.Add( Line.CreateBound( center, point ) ) ;
      }
    }
  }
}