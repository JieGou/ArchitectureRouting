using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics
{
  public class ExternalDrawingServer : DrawingServer
  {
    public List<Curve> CurveList { get ; set ; }

    public ExternalDrawingServer( Document doc ) : base( doc )
    {
      this.CurveList = new List<Curve>() ;
    }

    public override string GetName()
    {
      return "IMPACT External Drawing Server" ;
    }

    public override string GetDescription()
    {
      return "IMPACT External Drawing Server" ;
    }

    public XYZ? BasePoint { get ; set ; }

    public XYZ? NextPoint { get ; set ; }

    public override List<Curve> PrepareProfile()
    {
      return CurveList ;
    }

    public override bool CanExecute( View view )
    {
      return true ;
    }

    public override Outline? GetBoundingBox( View view )
    {
      if ( this.CurveList.Count <= 0 )
        return null ;

      var vertices = new List<XYZ>() ;
      CurveList.ForEach( x => vertices.AddRange( x.Tessellate() ) ) ;

      var xs = vertices.Select( x => x.X ).EnumerateAll() ;
      var ys = vertices.Select( x => x.Y ).EnumerateAll() ;
      var zs = vertices.Select( x => x.Z ).EnumerateAll() ;

      var offset = 100d.MillimetersToRevitUnits() ;
      var minPoint = new XYZ( xs.Min() - offset, ys.Min() - offset, zs.Min() - offset ) ;
      var maxPoint = new XYZ( xs.Max() + offset, ys.Max() + offset, zs.Max() + offset ) ;

      return new Outline( minPoint, maxPoint ) ;
    }
  }
}