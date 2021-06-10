using Arent3d.Architecture.Routing.AppBase.ViewModel;
using Arent3d.Revit;
using Arent3d.Revit.I18n;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI ;
using System;
using System.ComponentModel;
using Arent3d.Architecture.Routing.AppBase.Commands ;


namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.PostCommands
{
    [RevitAddin( Guid )]
    [DisplayName( "Change RouteName of Route" )]
    [Transaction( TransactionMode.Manual )]
    public class ApplyChangeRouteNameCommand : IExternalCommand
    {
        private const string Guid = "5E16E5C4-0244-4259-B788-52D84C1E954F";
        public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
        {
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;

            try {
                if ( SelectedFromToViewModel.PropertySourceType is { } propertySource ) {
                    Route? targetRoute = propertySource.TargetRoute;
                    if( targetRoute != null && SelectedFromToViewModel.FromToItem != null) { 
                        
                        using ( Transaction t = new Transaction( document, "TransactionName.Commands.PostCommands.ApplyChangeRouteNameCommand".GetAppStringByKeyOrDefault(" Rename RouteName") ) ) {
                            t.Start();
                            targetRoute.Rename(SelectedFromToViewModel.FromToItem.ItemTypeName);
                            t.Commit();
                        }
                    }
                }
                return Result.Succeeded;
            }
            catch ( Exception e ) {
                CommandUtils.DebugAlertException( e );
                return Result.Failed;
            }
        }
    }
}