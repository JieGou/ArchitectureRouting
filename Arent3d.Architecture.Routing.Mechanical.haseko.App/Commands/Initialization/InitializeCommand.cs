using Arent3d.Revit.UI ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.haseko.App.Commands.Initialization.InitializeCommand", DefaultString = "Initialize" )]
  [Image( "resources/Initialize.png", ImageType = ImageType.Large )]
  public class InitializeCommand : InitializeCommandBase
  {
    protected override bool Setup( Document document )
    {
      return base.Setup( document ) ;
    }
  }
}