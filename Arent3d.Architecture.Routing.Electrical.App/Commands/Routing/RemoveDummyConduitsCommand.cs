using System ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.RemoveDummyConduitsCommand", DefaultString = "Remove\nDummy Conduits" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class RemoveDummyConduitsCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var app = commandData.Application ;
      var uiDocument = app.ActiveUIDocument ;
      var document = uiDocument.Document ;

      try {
        return document.Transaction( "TransactionName.Commands.Routing.RemoveDummyConduitsCommand".GetAppStringByKeyOrDefault( "Remove Dummy Conduits" ), _ =>
        {
          CreateDummyConduitsIn3DViewCommand.RemoveDummyConduits( document ) ;

          return Result.Succeeded ;
        } ) ;
      }
      catch ( Exception exception ) {
        message = exception.Message ;
        return Result.Failed ;
      }
    }
  }
}