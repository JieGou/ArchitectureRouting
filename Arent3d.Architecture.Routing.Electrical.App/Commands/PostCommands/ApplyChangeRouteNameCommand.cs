using Arent3d.Revit;
using Autodesk.Revit.Attributes ;
using System.ComponentModel;
using Arent3d.Architecture.Routing.AppBase.Commands.PostCommands ;
using Autodesk.Revit.UI ;


namespace Arent3d.Architecture.Routing.Electrical.App.Commands.PostCommands
{
    [RevitAddin( Guid )]
    [DisplayName( "Change RouteName of Electrical Route" )]
    [Transaction( TransactionMode.Manual )]
    public class ApplyChangeRouteNameCommand : ApplyChangeRouteNameCommandBase, IExternalCommand
    { 
        private const string Guid = "1FCA462D-60E7-4293-8B55-47D1CBF49792";
    }
}