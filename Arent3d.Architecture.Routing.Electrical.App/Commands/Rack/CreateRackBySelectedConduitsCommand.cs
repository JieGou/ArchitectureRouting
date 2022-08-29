using System.Collections ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Commands.Rack ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Rack
{
  public class ConduitFilter : ISelectionFilter
  {
    public static ISelectionFilter Instance { get ; } = new ConduitFilter() ;

    private ConduitFilter()
    {
    }

    public bool AllowElement( Element elem )
    {
      return BuiltInCategory.OST_Conduit == elem.GetBuiltInCategory() ;
    }

    public bool AllowReference( Reference reference, XYZ position ) => false ;
  }

  public record SelectState( List<MEPCurve?> Conduits, XYZ? StartPoint, XYZ? EndPoint ) ;


  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Rack.CreateRackBySelectedConduitsCommand",
    DefaultString = "Manually\nCreate Rack" )]
  [Image( "resources/Initialize-16.bmp", ImageType = ImageType.Normal )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class CreateRackBySelectedConduitsCommand : IExternalCommand
  {
    public OperationResult<SelectState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var doc = uiDocument.Document ;
      var conduits = new List<MEPCurve?>() ;
      var rf = uiDocument.Selection.PickObject( ObjectType.Element, ConduitFilter.Instance, "電線管を選択して下さい。" ) ;
      conduits.Add( doc.GetElement( rf ) as MEPCurve ) ;

      XYZ p1 = uiDocument.Selection.PickPoint( ObjectSnapTypes.Nearest, "first point" ) ;
      XYZ p2 = uiDocument.Selection.PickPoint( ObjectSnapTypes.Nearest, "second point" ) ;

      return new OperationResult<SelectState>( new SelectState( conduits, p1, p2 ) ) ;
    }

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiApp = commandData.Application ;
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      // select conduit, start point, end point of rack
      var uiResult = OperateUI( commandData, elements ) ;
      var conduit = uiResult.Value.Conduits.First() ;
      if ( conduit == null ) {
        return Result.Cancelled ;
      }

      // make route:
      var routeName = conduit.GetRouteName() ?? "" ;
      var racks = new List<FamilyInstance>() ;
      var conduits = document.GetAllElementsOfRouteName<Element>( routeName ) ;
      using var ts = new Transaction( document, "create rack for conduits" ) ;
      ts.Start() ;
      NewRackCommandBase.CreateRackForConduit( uiDocument, uiApp.Application, conduits, racks ) ;
      ts.Commit() ;
      return Result.Succeeded ;
    }
  }
}