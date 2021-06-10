using System.ComponentModel ;
using Arent3d.Revit;
using Autodesk.Revit.Attributes ;
using Arent3d.Architecture.Routing.AppBase.Commands.PostCommands ;
using Autodesk.Revit.UI ;


namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.PostCommands
{
    [RevitAddin( Guid )]
    [DisplayName( "Change RouteName of Mechanical Route" )]
    [Transaction( TransactionMode.Manual )]
    public class ApplyChangeRouteNameCommand : ApplyChangeRouteNameCommandBase, IExternalCommand
    {
        private const string Guid = "5E16E5C4-0244-4259-B788-52D84C1E954F";
    }
}