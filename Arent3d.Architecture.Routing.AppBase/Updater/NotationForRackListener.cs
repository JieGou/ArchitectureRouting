using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

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
    public DocumentUpdateListenType ListenType => DocumentUpdateListenType.Any ;

    public ElementFilter GetElementFilter( Document? document )
    {
      return new ElementMulticlassFilter( new List<Type>() { typeof( TextElement ), typeof( CurveElement ), typeof( IndependentTag ) } ) ;
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
              not {} rackNotationModel )
            return ;

          if (document.GetElement( rackNotationModel.EndLineLeaderId ) is not DetailLine detailLine )
            return ;

          var (endLineLeaderId, ortherLineId) = NotationHelper.UpdateNotation( document, rackNotationModel, textNote, detailLine ) ;
          NotationHelper.SaveNotation( rackNotationStorable, textNote, endLineLeaderId, ortherLineId ) ;
        }
        else if ( elementSelected is IndependentTag tag ) {
          if ( rackNotationStorable.RackNotationModelData.FirstOrDefault( x => x.NotationId == tag.UniqueId ) is not { } rackNotationModel )
            return ;

          if ( document.GetElement( rackNotationModel.EndLineLeaderId ) is not DetailLine detailLine )
            return ;

          var (endLineLeaderId, ortherLineId) = NotationHelper.UpdateNotation( document, rackNotationModel, tag, detailLine ) ;
          NotationHelper.SaveNotation( rackNotationStorable, tag, endLineLeaderId, ortherLineId ) ;
        }
        else if ( elementSelected is DetailLine detailLine)
        {
          if ( rackNotationStorable.RackNotationModelData.FirstOrDefault( x => x.EndLineLeaderId == detailLine.UniqueId ) is
              not {} rackNotationModel )
            return ;

          if ( document.GetElement( rackNotationModel.NotationId ) is not TextNote notation )
            return ;
          
          var (endLineLeaderId, ortherLineId) = NotationHelper.UpdateNotation( document, rackNotationModel, notation, detailLine ) ;
          NotationHelper.SaveNotation( rackNotationStorable, notation, endLineLeaderId, ortherLineId ) ;
        }
        
        selection.SetElementIds(new List<ElementId>());
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException( exception ) ;
      }
    }
  }
}