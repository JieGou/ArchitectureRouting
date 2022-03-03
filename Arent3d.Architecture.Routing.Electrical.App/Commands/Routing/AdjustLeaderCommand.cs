using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  public class AdjustLeaderCommand :IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var selection = commandData.Application.ActiveUIDocument.Selection ;

      try {
        var rackNotationStorable = document.GetAllStorables<RackNotationStorable>().FirstOrDefault() ?? document.GetRackNotationStorable() ;
        var notations = rackNotationStorable.RackNotationModelData.GroupBy( x => x.NotationId ).ToDictionary(x => x.Key, x => x.ToList()) ;
        if ( ! rackNotationStorable.RackNotationModelData.Any() )
          return Result.Cancelled ;

        foreach ( var notation in notations ) {
          if(document.GetElement(notation.Key) is not TextNote textNote)
            continue;
          
          // if ( null == notations.FirstOrDefault()?.EndLineLeaderId ||
          //      document.GetElement( rackNotationModel.EndLineLeaderId ) is not DetailLine detailLine )
          //   return ;
          //
          // var (endLineLeaderId, ortherLineId) = NotationHelper.UpdateNotation( document, rackNotationModel, textNote, detailLine ) ;
          // NotationHelper.SaveNotation( rackNotationStorable, textNote, endLineLeaderId, ortherLineId ) ;
        }

        return Result.Succeeded ;
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException(exception);
        return Result.Failed ;
      }
    }
  }
}