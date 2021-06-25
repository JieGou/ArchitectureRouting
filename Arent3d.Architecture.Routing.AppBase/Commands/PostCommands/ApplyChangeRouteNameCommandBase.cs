using Arent3d.Architecture.Routing.AppBase.ViewModel;
using Arent3d.Revit.I18n;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI ;
using System;


namespace Arent3d.Architecture.Routing.AppBase.Commands.PostCommands
{
    public abstract class ApplyChangeRouteNameCommandBase : IExternalCommand
    {
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