using System.Collections.Generic ;
using System.Linq ;
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
      var underLineText = GeometryHelper.CreateUnderLineText( textNote, endPoint ) ;
      var pointNearest =
        underLineText.GetEndPoint( 0 ).DistanceTo( endPoint ) < underLineText.GetEndPoint( 1 ).DistanceTo( endPoint )
          ? underLineText.GetEndPoint( 0 )
          : underLineText.GetEndPoint( 1 ) ;

      var curves = GeometryHelper.IntersectCurveLeader( document, ( pointNearest, endPoint ) ) ;
      curves.Add( underLineText ) ;

      var view = document.GetElement( detailLine.OwnerViewId ) as View;
      var detailCurves = GeometryHelper.CreateDetailCurve( view, curves ) ;
      var curveClosestPoint = GeometryHelper.GetCurveClosestPoint( detailCurves, endPoint ) ;
      var ortherLineId = detailCurves.Select( x => x.UniqueId ).Where( x => x != curveClosestPoint.detailCurve?.UniqueId ).ToList() ;

      document.Delete( detailLine.Id ) ;
      foreach ( var lineId in rackNotationModel.OrtherLineId ) {
        if ( document.GetElement( lineId ) is {} line ) {
          document.Delete( line.Id ) ;
        }
      }

      return ( curveClosestPoint.detailCurve?.UniqueId ?? string.Empty, ortherLineId ) ;
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
  }
}