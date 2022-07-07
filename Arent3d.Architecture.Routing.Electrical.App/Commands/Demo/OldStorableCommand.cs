using System ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Demo
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Demo.DemoStorableCommand", DefaultString = "Old Storable" )]
  [Image( "resources/Initialize-16.bmp", ImageType = Revit.UI.ImageType.Normal )]
  [Image( "resources/Initialize-32.bmp", ImageType = Revit.UI.ImageType.Large )]
  public class OldStorableCommand : IExternalCommand
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

        var storable = document.GetDemoStorable() ;
        storable.UniqueIdDetailCurveData.Add(detailCurve.UniqueId);
        storable.Save();

        trans.Commit() ;
        
        TaskDialog.Show( "Arent", "Unique ID of the detail curve\n" + string.Join( "\n", storable.UniqueIdDetailCurveData ) ) ;

        return Result.Succeeded ;
      }
      catch ( Exception exception ) {
        message = exception.Message ;
        return Result.Failed ;
      }
    }
  }
}