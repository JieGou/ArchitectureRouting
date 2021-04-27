using Arent3d.Architecture.Routing.App.ViewModel;
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

namespace Arent3d.Architecture.Routing.App.Commands.PostCommands
{
    [RevitAddin( Guid )]
    [DisplayName( "Apply Selected From-To Changes" )]
    [Transaction( TransactionMode.Manual )]
    public class ApplyChangeRouteNameCommand : IExternalCommand
    {
        private const string Guid = "2EDE0887-445E-4A84-9840-CE18401A020A";
        public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
        {
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;

            var executor = new RoutingExecutor( document, commandData.View );

            try {
                if ( SelectedFromToViewModel.PropertySourceType is { } propertySource ) {
                    Route? targetRoute = propertySource.TargetRoute;
                    if( targetRoute != null ) { 
                        List<ElementId> elementIds = document.GetAllElementsOfRouteName<Element>( targetRoute.RouteName ).Select( elem => elem.Id ).ToList();

                        foreach(ElementId eid in elementIds ) {

                            Element elem = document.GetElement( eid );

                            ParameterSet parameters = elem.Parameters;

                            foreach ( Parameter param in parameters ) {
                                if ( param.Definition.Name.Equals( "Route Name" ) ) {
                                    using ( Transaction t = new Transaction( document, "Set Parameter" ) ) {
                                        t.Start();
                                        param.Set( SelectedFromToViewModel.FromToItem?.ItemTypeName );
                                        t.Commit();
                                    }
                                }
                            }

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