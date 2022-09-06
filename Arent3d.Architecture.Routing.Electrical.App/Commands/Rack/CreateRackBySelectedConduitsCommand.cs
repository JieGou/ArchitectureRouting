using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.AppBase.Utils ;
using Arent3d.Architecture.Routing.Electrical.App.Forms ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using ImageType = Arent3d.Revit.UI.ImageType ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Rack
{
  
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Rack.CreateRackBySelectedConduitsCommand", DefaultString = "Manually\nCreate Rack" )]
  [Image( "resources/Initialize-16.bmp", ImageType = ImageType.Normal )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class CreateRackBySelectedConduitsCommand : IExternalCommand
  {
    private record SelectState( MEPCurve? FirstSelectedConduit, MEPCurve? SecondSelectedConduit, XYZ StartPoint, XYZ EndPoint,double RackWidth, bool IsRound ) ;
    private OperationResult<SelectState> OperateUI( ExternalCommandData commandData )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var doc = uiDocument.Document ;
      var filterConduit = new ConduitRouteNamesFilter( doc ) ;
      var allConduits = doc.GetAllElements<Conduit>() ;
      try {
        // select point 1 and conduit 1
        var pickedConduitReference = uiDocument.Selection.PickObject( ObjectType.PointOnElement, filterConduit, "始点を選択して下さい。" ) ;
        var firstSelectedPoint = pickedConduitReference.GlobalPoint ;
        if(doc.GetElement( pickedConduitReference ) is not MEPCurve firstSelectedConduit)
          return OperationResult<SelectState>.Cancelled;
        if ( firstSelectedPoint.ProjectOn( firstSelectedConduit ) is not {} projectedPoint)
          return OperationResult<SelectState>.Cancelled;
        
        // get all conduits that overlapped with conduit 1
        var overlappedConduits = allConduits.Where( cd => cd.HasPoint(projectedPoint) ).OfType<MEPCurve>().ToList() ;
        overlappedConduits.Add( firstSelectedConduit ) ;
        
        // get route names of conduit group
        var routeNames = overlappedConduits.Select( cd => cd.GetRouteName()?? "" ).Where(name => name != "").Distinct().ToList() ;

        // select point 2 on conduits that has route name exists in route names selected before
        filterConduit = new ConduitRouteNamesFilter( doc , routeNames ) ;
        pickedConduitReference = uiDocument.Selection.PickObject( ObjectType.PointOnElement, filterConduit, "終点を選択して下さい。" ) ;
        var secondSelectedPoint = pickedConduitReference.GlobalPoint ;
        var secondSelectedConduit = doc.GetElement( pickedConduitReference ) as MEPCurve ;
        var routeName = secondSelectedConduit?.GetRouteName() ?? "" ;
        firstSelectedConduit = overlappedConduits.FirstOrDefault( cd => cd.GetRouteName() == routeName )! ;
        
        // show size dialog
        var dialog = new RackSizeDialog() ;
        if(dialog.ShowDialog() is false)
          return OperationResult<SelectState>.Cancelled;
        
        // width is currently in mm unit !!!
        var (rackWidth, isRound) = dialog ;

        return new OperationResult<SelectState>( new SelectState( firstSelectedConduit ,secondSelectedConduit, firstSelectedPoint, secondSelectedPoint , rackWidth, isRound ) ) ;

      }
      catch ( OperationCanceledException ) {
        return OperationResult<SelectState>.Cancelled;
      }
    }

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiApp = commandData.Application ;
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      // select conduit, start point, end point of rack
      var uiResult = OperateUI( commandData ) ;
      if ( !uiResult.HasValue || uiResult.Value.FirstSelectedConduit is not { } firstSelectedConduit|| uiResult.Value.SecondSelectedConduit is not { } secondSelectedConduit ) {
        return Result.Cancelled ;
      }
      
      // make cable racks:
      using var createRackTransaction = new Transaction( document, "手動でラックを作成する" ) ;
      var linkedConduits = firstSelectedConduit.GetLinkedMEPCurves( secondSelectedConduit ) ;
      var specialLengthList = new List<(Element Conduit, double StartParam, double EndParam)>() ;
      if ( firstSelectedConduit.Id != secondSelectedConduit.Id ) {
        var firstParams = firstSelectedConduit.CalculateParam( uiResult.Value.StartPoint, linkedConduits.ElementAt( 1 )) ;
        var lastParams = secondSelectedConduit.CalculateParam( uiResult.Value.EndPoint, linkedConduits.ElementAt( linkedConduits.Count - 2 )) ;
        specialLengthList.Add( (firstSelectedConduit, firstParams.StartParam, firstParams.EndParam) );
        specialLengthList.Add( (secondSelectedConduit, lastParams.StartParam, lastParams.EndParam) );
      }
      else {
        var firstParams = firstSelectedConduit.CalculateParam( uiResult.Value.StartPoint, uiResult.Value.EndPoint) ;
        specialLengthList.Add( (firstSelectedConduit, firstParams.StartParam, firstParams.EndParam) );
      }

      createRackTransaction.Start() ;
      var racks = new List<FamilyInstance>() ;
      // create racks along with conduits
      NewRackCommandBase.CreateRackForConduit2( uiDocument, linkedConduits, racks, uiResult.Value.RackWidth, specialLengthList ) ;
      
      // resolve overlapped cases
      document.ResolveOverlapCases( racks ) ;
      
      // create annotations for racks
      NewRackCommandBase.CreateNotationForRack( document, uiApp.Application, racks ) ;
      createRackTransaction.Commit() ;
      return Result.Succeeded ;
    }
  }
}