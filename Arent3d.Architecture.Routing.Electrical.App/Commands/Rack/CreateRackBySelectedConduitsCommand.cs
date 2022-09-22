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
    /// <summary>
    /// 
    /// </summary>
    /// <param name="RouteName"></param>
    /// <param name="FirstSelectedConduit"></param>
    /// <param name="SecondSelectedConduit"></param>
    /// <param name="StartPoint"></param>
    /// <param name="EndPoint"></param>
    /// <param name="RackWidth">Must be in Revit API unit</param>
    private record SelectState( string RouteName, MEPCurve? FirstSelectedConduit, MEPCurve? SecondSelectedConduit, XYZ StartPoint, XYZ EndPoint, double RackWidth, int NumberOfRack, bool IsAutoSizing  ) ;

    private OperationResult<SelectState> OperateUI( ExternalCommandData commandData )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var doc = uiDocument.Document ;

      // only allow command to run in floor plan
      if ( doc.ActiveView is not ViewPlan ) {
        new TaskDialog( "エラー" ) { MainContent = "平面でコマンドを実行してください", MainIcon = TaskDialogIcon.TaskDialogIconInformation, TitleAutoPrefix = false, }.Show() ;
        return OperationResult<SelectState>.Cancelled ;
      }

      var filterConduit = new ConduitRouteNamesSelectionFilter( doc ) ;
      var allConduits = doc.GetAllElements<Conduit>() ;
      try {
        // select point 1 and conduit 1
        var pickedConduitReference = uiDocument.Selection.PickObject( ObjectType.PointOnElement, filterConduit, "始点を選択して下さい。" ) ;
        var firstSelectedPoint = pickedConduitReference.GlobalPoint ;
        if ( doc.GetElement( pickedConduitReference ) is not MEPCurve firstSelectedConduit )
          return OperationResult<SelectState>.Cancelled ;
        if ( firstSelectedPoint.ProjectOn( firstSelectedConduit ) is not { } projectedPoint )
          return OperationResult<SelectState>.Cancelled ;

        // get all conduits that overlapped with conduit 1
        var overlappedConduits = allConduits.Where( cd => cd.HasPoint( projectedPoint ) ).OfType<MEPCurve>().ToList() ;
        overlappedConduits.Add( firstSelectedConduit ) ;

        // get route names of conduit group
        var routeNames = overlappedConduits.Select( cd => cd.GetRouteName() ?? "" ).Where( name => name != "" ).Distinct().ToList() ;

        // select point 2 on conduits that has route name exists in route names selected before
        filterConduit = new ConduitRouteNamesSelectionFilter( doc, routeNames ) ;
        pickedConduitReference = uiDocument.Selection.PickObject( ObjectType.PointOnElement, filterConduit, "終点を選択して下さい。" ) ;
        var secondSelectedPoint = pickedConduitReference.GlobalPoint ;
        var secondSelectedConduit = doc.GetElement( pickedConduitReference ) as MEPCurve ;
        var routeName = secondSelectedConduit?.GetRouteName() ?? "" ;
        firstSelectedConduit = overlappedConduits.FirstOrDefault( cd => cd.GetRouteName() == routeName )! ;

        // show size dialog to determine width
        var dialog = new RackSizeDialog() ;
        if ( dialog.ShowDialog() is false )
          return OperationResult<SelectState>.Cancelled ;

        var rackWidth = dialog.WidthInMillimeter.MillimetersToRevitUnits() ;
        var numberOfRack = dialog.NumberOfRack ;
        var isAutoSizing = dialog.IsAutoSizing ;

        return new OperationResult<SelectState>( new SelectState( routeName, firstSelectedConduit, secondSelectedConduit, firstSelectedPoint, secondSelectedPoint, rackWidth, numberOfRack, isAutoSizing ) ) ;
      }
      catch ( OperationCanceledException ) {
        return OperationResult<SelectState>.Cancelled ;
      }
    }

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiApp = commandData.Application ;
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      
      // select conduit, start point, end point of rack
      var uiResult = OperateUI( commandData ) ;
      if ( ! uiResult.HasValue || uiResult.Value.FirstSelectedConduit is not { } firstSelectedConduit || uiResult.Value.SecondSelectedConduit is not { } secondSelectedConduit ) {
        return Result.Cancelled ;
      }

      // detect conduit array between first and last selected conduit
      var linkedConduits = firstSelectedConduit.GetLinkedMEPCurves( secondSelectedConduit ) ;

      // calculate lengths of first and last rack
      var specialLengthList = new List<(Element Conduit, double StartParam, double EndParam)>() ;
      if ( firstSelectedConduit.Id != secondSelectedConduit.Id ) {
        // first and last selected points are on difference conduits
        var firstParams = firstSelectedConduit.CalculatePositionOfRackOnConduit( uiResult.Value.StartPoint, linkedConduits.ElementAt( 1 ) ) ;
        var lastParams = secondSelectedConduit.CalculatePositionOfRackOnConduit( uiResult.Value.EndPoint, linkedConduits.ElementAt( linkedConduits.Count - 2 ) ) ;
        
        specialLengthList.Add( ( firstSelectedConduit, firstParams.StartParam, firstParams.EndParam ) ) ;
        specialLengthList.Add( ( secondSelectedConduit, lastParams.StartParam, lastParams.EndParam ) ) ;
      }
      else {
        // first and last selected points are on the same conduit
        var firstParams = firstSelectedConduit.CalculatePositionOfRackOnConduit( uiResult.Value.StartPoint, uiResult.Value.EndPoint ) ;
        specialLengthList.Add( ( firstSelectedConduit, firstParams.StartParam, firstParams.EndParam ) ) ;
      }

      // start generate new racks
      using var createRackTransaction = new Transaction( document, "手動でラックを作成する" ) ;
      createRackTransaction.Start() ;
      
      // turn on rack fitting category
      var category = Category.GetCategory( document, BuiltInCategory.OST_CableTrayFitting ) ;
      document.ActiveView.SetCategoryHidden( category.Id, false ) ;
      
      // create racks along with conduits
      var conduitWidthMap = linkedConduits.Where(element => element is Conduit cd && cd.Location as LocationCurve is {} lc && lc.Curve.Length > 20d.MillimetersToRevitUnits() ).Select( conduit => ( conduit, uiResult.Value.RackWidth ) ) ;
      var racksAndFittings = document.CreateRacksAndElbowsAlongConduits( conduitWidthMap, "Limit Rack", uiResult.Value.IsAutoSizing, specialLengthList ) ;

      // resolve overlapped cases
      var modifiedRackLists = document.ResolveOverlapCases( racksAndFittings ) ;

      // create annotations for racks
      NewRackCommandBase.CreateNotationForRack( document, uiApp.Application, modifiedRackLists.OfType<FamilyInstance>().Where(fi => fi.IsRack()) ) ;
      
      createRackTransaction.Commit() ;
      return Result.Succeeded ;
    }
  }
}