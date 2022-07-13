using System ;
using Arent3d.Architecture.Routing.ExtensibleStorages ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Attributes ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Extensions ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using System.Linq;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Demo
{
    [Transaction( TransactionMode.Manual )]
    [DisplayNameKey( "Electrical.App.Commands.Demo.NewStorableCommand", DefaultString = "New Storable" )]
    [Image( "resources/Initialize-16.bmp", ImageType = Revit.UI.ImageType.Normal )]
    [Image( "resources/Initialize-32.bmp", ImageType = Revit.UI.ImageType.Large )]
    public class NewStorableCommand : IExternalCommand
    {
        public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
        {
            try {
                var document = commandData.Application.ActiveUIDocument.Document ;
                var selection = commandData.Application.ActiveUIDocument.Selection ;

                var firstPoint = selection.PickPoint() ;
                var secondPoint = selection.PickPoint() ;

                using var trans = new Transaction( document ) ;
                trans.Start( "New Curve" ) ;

                var detailCurve = document.Create.NewDetailCurve( document.ActiveView, Line.CreateBound( firstPoint, secondPoint ) ) ;
                detailCurve.SetData( new TestModel { UniqueId = detailCurve.UniqueId } ) ;

                trans.Commit() ;

                var uniqueIds = document.GetAllInstances<CurveElement>().Select( x => (x.GetData<TestModel>()?.UniqueId ?? string.Empty) ) ;
                TaskDialog.Show( "Arent", "Unique ID of the detail curve\n" + string.Join( "\n", uniqueIds ) ) ;

                return Result.Succeeded ;
            }
            catch ( Exception exception ) {
                message = exception.Message ;
                return Result.Failed ;
            }
        }
    }

    [Schema( "685551F3-04D4-4A34-94CA-0C2E34B2A6BF", nameof( TestModel ) )]
    public class TestModel : IDataModel
    {
        public string? UniqueId { get ; set ; }
    }
}