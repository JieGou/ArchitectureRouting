using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Initialization.ShowCeedModelsCommand", DefaultString = "View\nSet Code" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class ShowCeedModelsCommand : ShowCeedModelsCommandBase
  {
    protected override ElectricalRoutingFamilyType ElectricalRoutingFamilyType => ElectricalRoutingFamilyType.ConnectorOneSide ;
    protected override string FullClass => typeof( ShowCeedModelsCommand ).FullName ;
    protected override string TabName => AppInfo.ApplicationName ;
  }
}