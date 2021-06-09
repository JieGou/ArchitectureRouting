using Arent3d.Architecture.Routing.AppBase.ViewModel;
using Arent3d.Revit;
using Arent3d.Revit.I18n;
using Arent3d.Revit.UI;
using Arent3d.Revit.UI.Forms;
using Arent3d.Utility;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI ;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Arent3d.Architecture.Routing.AppBase.Commands.PostCommands
{
    [RevitAddin( Guid )]
    [DisplayName( "Change RouteName of Route" )]
    [Transaction( TransactionMode.Manual )]
    public class ApplyChangeRouteNameCommand : IExternalCommand
    {
        private const string Guid = "CB41CB80-18CF-494F-AA17-C18512246770";
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