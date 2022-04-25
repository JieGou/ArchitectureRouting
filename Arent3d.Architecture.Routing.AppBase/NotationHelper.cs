using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase
{
  public static class NotationHelper
  {
    public  static (string, List<string>) UpdateNotation( Document document, RackNotationModel rackNotationModel,
      TextNote textNote, DetailLine detailLine )
    {
      var endPoint = detailLine.GeometryCurve.GetEndPoint( rackNotationModel.EndPoint ) ;
      var underLineText = NewRackCommandBase.CreateUnderLineText( textNote, endPoint.Z ) ;
      var pointNearest =
        underLineText.GetEndPoint( 0 ).DistanceTo( endPoint ) < underLineText.GetEndPoint( 1 ).DistanceTo( endPoint )
          ? underLineText.GetEndPoint( 0 )
          : underLineText.GetEndPoint( 1 ) ;

      if ( document.GetElement( detailLine.OwnerViewId ) is not ViewPlan viewPlan ) return ( string.Empty, new List<string>() ) ;
      var curves = GeometryHelper.GetCurvesAfterIntersection( viewPlan, new List<Curve> { Line.CreateBound( new XYZ( pointNearest.X, pointNearest.Y, viewPlan.GenLevel.Elevation ), new XYZ( endPoint.X, endPoint.Y, viewPlan.GenLevel.Elevation ) ) } ) ;
      curves.Add( underLineText ) ;

      var detailCurves = CreateDetailCurve( viewPlan, curves ) ;
      var curveClosestPoint = GeometryHelper.GetCurveClosestPoint( detailCurves, endPoint ) ;
      var ortherLineId = detailCurves.Select( x => x.UniqueId ).Where( x => x != curveClosestPoint.DetailCurve?.UniqueId ).ToList() ;

      document.Delete( detailLine.Id ) ;
      foreach ( var lineId in rackNotationModel.OrtherLineId ) {
        if ( document.GetElement( lineId ) is {} line ) {
          document.Delete( line.Id ) ;
        }
      }

      return ( curveClosestPoint.DetailCurve?.UniqueId ?? string.Empty, ortherLineId ) ;
    }
    
    public static void SaveNotation(RackNotationStorable rackNotationStorable, TextNote textNote, string endLineLeaderId, IReadOnlyList<string> ortherLineId)
    {
      foreach ( var rackNotation in rackNotationStorable.RackNotationModelData.Where( x =>
                 x.NotationId == textNote.UniqueId ) ) {
        rackNotation.EndLineLeaderId = endLineLeaderId ;
        rackNotation.OrtherLineId = ortherLineId ;
      }

      rackNotationStorable.Save() ;
    }
    
    public static List<DetailCurve> CreateDetailCurve( View? view, IEnumerable<Curve> curves )
    {
      var detailCurves = new List<DetailCurve>() ;
      if ( null == view )
        return detailCurves ;

      var graphicsStyle = view.Document.Settings.Categories.get_Item( BuiltInCategory.OST_CurvesMediumLines ).GetGraphicsStyle( GraphicsStyleType.Projection ) ;

      foreach ( var curve in curves ) {
        var detailCurve = view.Document.Create.NewDetailCurve( view, curve ) ;
        detailCurve.LineStyle = graphicsStyle ;
        detailCurves.Add( detailCurve ) ;
      }

      return detailCurves ;
    }
  }
}