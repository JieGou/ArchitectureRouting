using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Shaft ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Initialization.UnnitializeCommand", DefaultString = "Erase all addin data" )]
  [Image( "resources/Initialize.png", ImageType = ImageType.Large )]
  public class UninitializeCommand : UninitializeCommandBase
  {
    protected override void UnSetup( Document document )
    {
      document.EraseAllConnectorFamilies() ;
      document.EraseAllElectricalRoutingFamilies();
      CreateCylindricalShaftCommandBase.DeleteAllShaftOpening(document);
      RoutingAppUI.CeedModelDockPanelProvider?.HideDockPane( new UIDocument( document ).Application ) ;
      base.UnSetup( document ) ;
      document.DeleteEntireSchema();
    }
  }
}