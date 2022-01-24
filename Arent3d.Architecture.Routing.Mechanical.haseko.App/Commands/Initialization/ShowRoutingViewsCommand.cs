using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.haseko.App.Commands.Initialization.ShowRoutingViewsCommand", DefaultString = "Plans" )]
  [Image( "resources/Plans.png", ImageType = ImageType.Large )]
  public class ShowRoutingViewsCommand : ShowRoutingViewsCommandBase
  {
  }
}