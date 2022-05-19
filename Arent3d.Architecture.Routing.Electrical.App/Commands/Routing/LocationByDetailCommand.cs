using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.Electrical.App.Forms ;
using Arent3d.Architecture.Routing.Electrical.App.Helpers ;
using Arent3d.Architecture.Routing.Electrical.App.ViewModels ;
using Arent3d.Architecture.Routing.Utils ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI.Selection ;
using MoreLinq;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.LocationByDetailCommand", DefaultString = "Location\nBy Detail" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class LocationByDetailCommand : IExternalCommand
  {
    private readonly Func<FamilyInstance, XYZ> _getCenterPoint = familyInsatance =>
    {
      var connectors = familyInsatance.MEPModel.ConnectorManager.Connectors.OfType<Connector>().ToList() ;
      return 0.5 * ( connectors[ 0 ].Origin + connectors[ 1 ].Origin ) ;
    } ;
    
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      try {
        var uiDocument = commandData.Application.ActiveUIDocument ;

        if ( uiDocument.ActiveView is not ViewPlan ) {
          message = "Only active in the view plan!" ;
          return Result.Cancelled ;
        }
        
        ComponentHelper.InitialComponent(uiDocument.Document);

        var elements = uiDocument.Selection.PickObjects( ObjectType.Element, new ChangeLocationTypeFilter( new List<Category>
        {
          Category.GetCategory( uiDocument.Document, BuiltInCategory.OST_Conduit ),
          Category.GetCategory( uiDocument.Document, BuiltInCategory.OST_ConduitFitting )
        } ), "Please select conduit, conduit fitting in project!" ).Select( x => uiDocument.Document.GetElement( x ) ).ToList() ;

        var conduits = elements.OfType<Conduit>().ToList() ;
        var curveConduits = GetCurveFromElements( uiDocument.Document, conduits ) ;

        var conduitFittings = elements.OfType<FamilyInstance>().ToList() ;
        var fittingHorizontals = conduitFittings.Where( x => Math.Abs( x.GetTransform().OfVector( XYZ.BasisZ ).Z - 1 ) < GeometryUtil.Tolerance ).ToList() ;
        var fittingVerticals = conduitFittings.Where( x => Math.Abs( x.GetTransform().OfVector( XYZ.BasisZ ).Z ) < GeometryUtil.Tolerance ).ToList() ;

        var lineConduits = curveConduits.OfType<Line>().ToList() ;
        var lineVerticalFittings = GetLineVerticalFittings( uiDocument.Document, fittingVerticals ) ;
        
        lineConduits.AddRange(lineVerticalFittings);
        var lines = ConnectLines( lineConduits ) ;
        var curves = GetCurveHorizontalFittings( uiDocument.Document, fittingHorizontals ) ;
        
        var externalEventHandler = new ExternalEventHandler() ;
        var viewModel = new LocationByDetailViewModel( uiDocument, elements, lines, curves ) { ExternalEventHandler = externalEventHandler } ;
        externalEventHandler.ExternalEvent = ExternalEvent.Create( viewModel.ExternalEventHandler ) ;
        var view = new LocationByDetailView { DataContext = viewModel } ;
        view.Show() ;

        return Result.Succeeded ;
      }
      catch ( Exception exception ) {
        message = exception.Message ;
        return Result.Failed ;
      }
    }

    private List<Curve> GetCurveHorizontalFittings(Document document, IEnumerable<FamilyInstance> fittingHorizontals )
    {
      var comparer = new XyzComparer() ;
      fittingHorizontals = fittingHorizontals.Where( x => x.MEPModel.ConnectorManager.Connectors.Size == 2 ) ;
      fittingHorizontals = fittingHorizontals.DistinctBy( x => _getCenterPoint(x), comparer ) ;
      return GetCurveFromElements( document, fittingHorizontals ) ;
    }

    private List<Line> GetLineVerticalFittings( Document document, IEnumerable<FamilyInstance> fittingVerticals )
    {
      var comparer = new XyzComparer() ;
      var connectors = fittingVerticals.DistinctBy( x => ( (LocationPoint) x.Location ).Point, comparer ).Select( x => x.MEPModel.ConnectorManager.Connectors.OfType<Connector>().ToList() ) ;
      
      var lines = new List<Line>() ;
      foreach ( var connector in connectors ) {
        if(connector.Count != 2)
          continue;

        var maxZ = connector[ 0 ].Origin.Z > connector[ 1 ].Origin.Z ? connector[ 0 ].Origin.Z : connector[ 1 ].Origin.Z ;
        lines.Add(Line.CreateBound(new XYZ(connector[0].Origin.X, connector[0].Origin.Y, maxZ), new XYZ(connector[1].Origin.X, connector[1].Origin.Y, maxZ)));
      }
      
      return lines ;
    }

    private List<Line> ConnectLines( List<Line> lines )
    {
      var lineConnects = new List<Line>() ;
      while ( lines.Any() ) {
        var line = lines[ 0 ] ;
        lines.RemoveAt( 0 ) ;

        if ( lines.Count > 0 ) {
          int count ;
          do {
            count = lines.Count ;

            var middleFirst = line.Evaluate( 0.5, true ) ;
            for ( var i = lines.Count - 1 ; i >= 0 ; i-- ) {
              var middleSecond = lines[ i ].Evaluate( 0.5, true ) ;
              if ( middleFirst.DistanceTo( middleSecond ) < GeometryHelper.Tolerance ) {
                if ( lines[ i ].Length > line.Length )
                  line = lines[ i ] ;
                lines.RemoveAt( i ) ;
              }
              else {
                var lineTemp = Line.CreateBound( middleFirst, middleSecond ) ;
                if ( Math.Abs( Math.Abs( lineTemp.Direction.DotProduct( line.Direction ) ) - 1 ) < GeometryHelper.Tolerance && 0.5 * line.Length + 0.5 * lines[ i ].Length + GeometryHelper.Tolerance >= lineTemp.Length ) {
                  if ( GeometryHelper.GetMaxLengthLine( line, lines[ i ] ) is { } ml )
                    line = ml ;
                  lines.RemoveAt( i ) ;
                }
              }
            }
          } while ( count != lines.Count ) ;
        }

        lineConnects.Add( line ) ;
      }

      return lineConnects ;
    }

    private List<Curve> GetCurveFromElements( Document document, IEnumerable<Element> elements )
    {
      using var transaction = new Transaction( document ) ;
      transaction.Start( "Get Geometry" ) ;

      var detailLevel = document.ActiveView.DetailLevel ;
      document.ActiveView.DetailLevel = ViewDetailLevel.Coarse ;

      var curves = new List<Curve>() ;
      var options = new Options { View = document.ActiveView } ;

      foreach ( var element in elements ) {
        if ( element.get_Geometry( options ) is { } geometryElement )
          RecursiveCurves( geometryElement, ref curves ) ;
      }

      document.ActiveView.DetailLevel = detailLevel ;
      transaction.Commit() ;

      return curves ;
    }

    private void RecursiveCurves( GeometryElement geometryElement, ref List<Curve> curves )
    {
      foreach ( var geometry in geometryElement ) {
        switch ( geometry ) {
          case GeometryInstance geometryInstance :
          {
            if ( geometryInstance.GetInstanceGeometry() is { } subGeometryElement )
              RecursiveCurves( subGeometryElement, ref curves ) ;
            break ;
          }
          case Curve curve :
            curves.Add( curve.Clone() ) ;
            break ;
        }
      }
    }
  }
}