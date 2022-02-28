using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using NPOI.SS.Formula.Functions ;

namespace Arent3d.Architecture.Routing.AppBase.Updater
{
  public class NotationForRackListener : IDocumentUpdateListener
  {
    public Guid Guid { get ; } = new Guid( "69CA3026-1585-4521-968E-BE6A10E5115B" ) ;
    public string Name => "Update Tag For Rack" ;
    public string Description => "Update for the tag of the rack when having any changes" ;
    public bool IsDocumentSpan => false ;
    public bool IsOptional => false ;
    public ChangePriority ChangePriority => ChangePriority.Annotations ;
    public DocumentUpdateListenType ListenType => DocumentUpdateListenType.Geometry ;

    public NotationForRackListener()
    {
    }

    public ElementFilter GetElementFilter( Document? document )
    {
      return new ElementMulticlassFilter( new List<Type>() { typeof( TextElement ), typeof( CurveElement ) } ) ;
    }

    public bool CanListen( Document document ) => true ;

    public IEnumerable<ParameterProxy> GetListeningParameters( Document? document ) =>
      throw new NotSupportedException() ;

    public void Execute( UpdaterData data )
    {
      try {
        var document = data.GetDocument() ;
        var selection = new UIApplication( data.GetDocument().Application ).ActiveUIDocument.Selection ;

        if ( selection.GetElementIds().Count != 1 )
          return ;

        var rackNotationStorable = document.GetAllStorables<RackNotationStorable>().FirstOrDefault() ??
                                   document.GetRackNotationStorable() ;

        var elementSelected = document.GetElement( selection.GetElementIds().FirstOrDefault() ) ;
        if ( elementSelected is TextNote textNote ) {
          if ( rackNotationStorable.RackNotationModelData.FirstOrDefault( x => x.NotationId == textNote.UniqueId ) is
              not RackNotationModel rackNotationModel )
            return ;

          if ( null == rackNotationModel.EndLineLeaderId ||
               document.GetElement( rackNotationModel.EndLineLeaderId ) is not DetailLine detailLine )
            return ;

          var (endLineLeaderId, ortherLineId) = UpdateNotation( document, rackNotationModel, textNote, detailLine ) ;
          Save( rackNotationStorable, textNote, endLineLeaderId, ortherLineId ) ;
        }
        else if ( elementSelected is DetailLine detailLine)
        {
          if ( rackNotationStorable.RackNotationModelData.FirstOrDefault( x => x.EndLineLeaderId == detailLine.UniqueId ) is
              not RackNotationModel rackNotationModel )
            return ;

          if ( null == rackNotationModel.NotationId ||
               document.GetElement( rackNotationModel.NotationId ) is not TextNote notation )
            return ;
          
          var (endLineLeaderId, ortherLineId) = UpdateNotation( document, rackNotationModel, notation, detailLine ) ;
          Save( rackNotationStorable, notation, endLineLeaderId, ortherLineId ) ;
        }
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException( exception ) ;
      }
    }

    private (string, List<string>) UpdateNotation( Document document, RackNotationModel rackNotationModel,
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

      var detailCurves = new List<DetailCurve>() ;
      foreach ( var curve in curves ) {
        var detailCurve = document.Create.NewDetailCurve( document.ActiveView, curve ) ;
        detailCurves.Add( detailCurve ) ;
      }

      var curveClosestPoint = GeometryHelper.GetCurveClosestPoint( detailCurves, endPoint ) ;
      var endLineLeader = ( curveClosestPoint.Item1?.UniqueId, curveClosestPoint.Item2 ) ;
      var ortherLineId = detailCurves.Select( x => x.UniqueId ).Where( x => x != endLineLeader.Item1 ).ToList() ;

      foreach ( var lineId in rackNotationModel.OrtherLineId ) {
        if ( document.GetElement( lineId ) is Element line ) {
          document.Delete( line.Id ) ;
        }
      }

      return ( endLineLeader.Item1 ?? string.Empty, ortherLineId ) ;
    }

    private void Save(RackNotationStorable rackNotationStorable, TextNote textNote, string endLineLeaderId, IReadOnlyList<string> ortherLineId)
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