using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.AdjustLeaderCommand", DefaultString = "Adjust Leader" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class AdjustLeaderCommand :IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;

      try {
        var rackNotationStorable = document.GetAllStorables<RackNotationStorable>().FirstOrDefault() ?? document.GetRackNotationStorable() ;
        var notations = rackNotationStorable.RackNotationModelData.GroupBy( x => x.NotationId ).ToDictionary(x => x.Key, x => x.ToList()) ;
        if ( ! rackNotationStorable.RackNotationModelData.Any() )
          return Result.Cancelled ;

        using Transaction transaction = new Transaction( document ) ;
        transaction.Start( "Adjust Leader" ) ;

        foreach ( var notation in notations ) {
          if(document.GetElement(notation.Key) is not TextNote textNote)
            continue;

          var rackNotationModel = notation.Value.First() ;
          var oldEndLineLeaderId = rackNotationModel.EndLineLeaderId ;
          if ( string.IsNullOrEmpty(oldEndLineLeaderId) ||
               document.GetElement( oldEndLineLeaderId ) is not DetailLine detailLine )
            continue ;
          
          var (endLineLeaderId, ortherLineId) = NotationHelper.UpdateNotation( document, rackNotationModel, textNote, detailLine ) ;

          foreach ( var model in notation.Value ) {
            model.EndLineLeaderId = endLineLeaderId ;
            model.OtherLineIds = ortherLineId ;
          }
        }
        rackNotationStorable.Save();
        
        transaction.Commit() ;
        return Result.Succeeded ;
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException(exception);
        return Result.Failed ;
      }
    }
  }
}