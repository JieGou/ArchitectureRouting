using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Architecture ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using Application = Autodesk.Revit.ApplicationServices.Application ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.haseko.App.Commands.Routing.CreateRoomBoxesCommand", DefaultString = "Create\nRoom And Envelope" )]
  [Image( "resources/room_boxes.png" )]
  public class CreateRoomAndEnvelopeCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiApp = commandData.Application ;
      var uiDoc = uiApp.ActiveUIDocument ;
      var doc = uiDoc.Document ;
      try {
        return Result.Succeeded ;
      }
      catch ( Exception ex ) {
        MessageBox.Show( ex.Message ) ;
        return Result.Cancelled ;
      }
    }
  }
}