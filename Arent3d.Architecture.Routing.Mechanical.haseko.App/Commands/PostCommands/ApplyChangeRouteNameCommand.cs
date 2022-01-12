using System.ComponentModel ;
using Arent3d.Revit;
using Autodesk.Revit.Attributes ;
using Arent3d.Architecture.Routing.AppBase.Commands.PostCommands ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.PostCommands
{
    [RevitAddin( Guid )]
    [DisplayName( "Change RouteName of Mechanical Route" )]
    [Transaction( TransactionMode.Manual )]
    public class ApplyChangeRouteNameCommand : ApplyChangeRouteNameCommandBase
    {
        private const string Guid = "871740C5-749F-439F-BCF6-26052D4B9D95";
    }
}