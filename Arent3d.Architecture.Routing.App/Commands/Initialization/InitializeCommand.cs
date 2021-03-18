using System.ComponentModel ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "App.Commands.Initialization.InitializeCommand", DefaultString = "Initialize" )]
  //testing image size
  [Image( "resources/Initialize.png", ImageType = ImageType.Large )]
  public class InitializeCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;

      doc.SetupRoutingFamiliesAndParameters() ;

      return Result.Succeeded ;
    }
  }
}