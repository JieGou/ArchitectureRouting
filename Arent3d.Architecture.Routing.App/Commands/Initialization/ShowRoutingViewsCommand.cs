using System.ComponentModel ;
using Arent3d.Architecture.Routing.App.Forms ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "App.Commands.Initialization.ShowRoutingViewsCommand", DefaultString = "Plans" )]
  [Image( "resources/Plans.png", ImageType = ImageType.Large )]
  public class ShowRoutingViewsCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;

      var dialog = new GetLevel( doc ) ;
      if ( true == dialog.ShowDialog() ) {
        doc.CreateRoutingView( dialog.GetSelectedLevels() ) ;
      }

      return Result.Succeeded ;
    }
  }
}