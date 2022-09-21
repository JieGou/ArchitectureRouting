using System.Collections.Generic ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics
{
  public class RectangleExternal : DrawExternalBase
  {
    public RectangleExternal( UIApplication uiApplication ) : base( uiApplication )
    {
    }

    public override void DrawExternal()
    {
      this.DrawingServer.CurveList.Clear() ;

      if ( this.DrawingServer?.BasePoint == null || this.DrawingServer.NextPoint == null || this.DrawingServer.BasePoint.DistanceTo( this.DrawingServer.NextPoint ) <= 0.001 ) {
        return ;
      }

      var points = GetCornerPoints() ;
      if ( points != null ) {
        points.Add( points[ 0 ] ) ;
        var lpt = points[ 0 ] ;
        for ( var k = 1 ; k < points.Count ; k++ ) {
          var cpt = points[ k ] ;
          if ( lpt.DistanceTo( cpt ) > 0.001 ) {
            this.DrawingServer.CurveList.Add( Line.CreateBound( lpt, cpt ) ) ;
          }

          lpt = cpt ;
        }
      }
    }

    private List<XYZ>? GetCornerPoints()
    {
      if ( this.DrawingServer == null )
        return null ;

      if ( this.DrawingServer.BasePoint == null || this.DrawingServer.NextPoint == null ) {
        return null ;
      }

      var mpt = ( this.DrawingServer.BasePoint + this.DrawingServer.NextPoint ) * 0.5 ;
      var currView = this.UIApplication.ActiveUIDocument.Document.ActiveView ;
      var plane = Plane.CreateByNormalAndOrigin( currView.RightDirection, mpt ) ;
      var mirrorMat = Transform.CreateReflection( plane ) ;

      var p1 = this.DrawingServer.BasePoint ;
      var p2 = mirrorMat.OfPoint( p1 ) ;
      var p3 = this.DrawingServer.NextPoint ;
      var p4 = mirrorMat.OfPoint( p3 ) ;

      return new List<XYZ> { p1, p2, p3, p4 } ;
    }
  }
}