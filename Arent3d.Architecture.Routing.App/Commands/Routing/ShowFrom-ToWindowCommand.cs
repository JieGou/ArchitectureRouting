using System ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.Routing
{
    [Transaction( TransactionMode.Manual )]
    [DisplayNameKey( "App.Commands.Routing.ShowFrom_ToWindowCommand", DefaultString = "From-To\nWindow" )]
    [Image( "resources/From-ToWindow.png")]
    public class ShowFrom_ToWindowCommand : IExternalCommand
    {
        private UIDocument? _uiDocument = null ;
        public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
        {
            _uiDocument = commandData.Application.ActiveUIDocument ;
            try {
                FromToWindowViewModel.ShowFromToWindow( _uiDocument ) ;
            }
            catch ( Exception e ) {
                TaskDialog.Show( "ShowFrom_ToWindowCommand", e.Message ) ;
            }
            
            return Result.Succeeded;
        }
        
    }
}