using Arent3d.Revit.UI ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Autodesk.Revit.Attributes ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Initialization.InitializeCommand", DefaultString = "Initialize" )]
  [Image( "resources/Initialize.png", ImageType = ImageType.Large )]
  public class InitializeCommand : InitializeCommandBase
  {
  }
}