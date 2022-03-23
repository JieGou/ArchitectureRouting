using Autodesk.Revit.DB ;
using Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Autodesk.Revit.ApplicationServices ;
using Autodesk.Revit.UI ;
using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB.Electrical ;
using System.Linq ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Shaft
{
  public class CreateCylindricalShaftCommandBase : IExternalCommand
  {
    private const double RotateAngle = Math.PI / 3 ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      UIApplication uiApp = commandData.Application ;
      UIDocument uiDocument = uiApp.ActiveUIDocument ;
      Document document = uiDocument.Document ;
      Application app = uiApp.Application ;
      var selection = uiDocument.Selection ;
      bool checkEx = false ;
      try {
        // Pick first point 
        XYZ firstPoint = selection.PickPoint( "Pick first point" ) ;
        XYZ? secondPoint = null ;
        // This is the object to render the guide line
        CircleExternal circleExternal = new CircleExternal( uiApp ) ;
        try {
          // Add first point to list picked points
          circleExternal.PickedPoints.Add( firstPoint ) ;
          // Assign first point
          circleExternal.DrawingServer.BasePoint = firstPoint ;
          // Render the guide line
          circleExternal.DrawExternal() ;

          // Pick next point 
          secondPoint = selection.PickPoint( "Pick next point" ) ;
        }
        catch ( Exception ) {
          checkEx = true ;
        }
        finally {
          // End to render guide line
          circleExternal.Dispose() ;
        }

        // If second point is null. Return failed to end command
        if ( secondPoint == null || checkEx ) return Result.Failed ;

        // Get height setting
        HeightSettingStorable heightSetting = document.GetHeightSettingStorable() ;
        var levels = heightSetting.Levels.OrderBy( x => x.Elevation ).ToList() ;
        // Get lowest and highest level
        Level? lowestLevel = levels.FirstOrDefault() ;
        Level? highestLevel = levels.LastOrDefault() ;
        if ( lowestLevel == null && highestLevel == null ) return Result.Failed ;

        using ( Transaction trans = new Transaction( document, "Create Arent Shaft" ) ) {
          trans.Start() ;

          // Create CurveArray for NewOpening method from list selected points
          CurveArray shaftProfile = app.Create.NewCurveArray() ;
          double radius = firstPoint.DistanceTo( secondPoint ) ;
          double startAngle = 0 ;
          const double endAngle = Math.PI * 2 ;
          XYZ xAxis = new XYZ( 1, 0, 0 ) ;
          XYZ yAxis = new XYZ( 0, 1, 0 ) ;
          if ( radius > 0.001 ) {
            Curve cylinderCurve = Arc.Create( firstPoint, radius, startAngle, endAngle, xAxis, yAxis ) ;
            shaftProfile.Append( cylinderCurve ) ;
          }

          // Create Shaft opening
          Opening shaftOpening = document.Create.NewOpening( lowestLevel, highestLevel, shaftProfile ) ;
          // Set offset from top
          shaftOpening.get_Parameter( BuiltInParameter.WALL_TOP_OFFSET ).Set( 0 ) ;
          // Set offset from base
          shaftOpening.get_Parameter( BuiltInParameter.WALL_BASE_OFFSET ).Set( 0 ) ;
          // Set base level is lowest level
          shaftOpening.get_Parameter( BuiltInParameter.WALL_BASE_CONSTRAINT ).Set( lowestLevel!.Id ) ;
          // Set top level is highest level
          shaftOpening.get_Parameter( BuiltInParameter.WALL_HEIGHT_TYPE ).Set( highestLevel!.Id ) ;

          var lengthOfDirection = radius * 5 ;

          if ( 2 * lengthOfDirection <= document.Application.ShortCurveTolerance ) {
            message = $"Direction symbol length must be greater than {document.Application.ShortCurveTolerance.RevitUnitsToMillimeters()}mm!" ;
            return Result.Cancelled ;
          }

          var symbolDirection = document.GetFamilySymbols( ElectricalRoutingFamilyType.SymbolDirectionCylindricalShaft ).FirstOrDefault() ?? throw new InvalidOperationException() ;
          if ( ! symbolDirection.IsActive ) symbolDirection.Activate() ;

          if ( document.ActiveView.ViewType != ViewType.FloorPlan ) {
            message = "Only created in floor plan view!" ;
            return Result.Cancelled ;
          }

          var subCategoryForBodyDirection = GetLineStyle( document, "SubCategoryForDirectionCylindricalShaft", new Color( 255, 0, 255 ), 1 ) ;
          var lineBodyDirection = Line.CreateBound( Transform.CreateTranslation( XYZ.BasisX * lengthOfDirection ).OfPoint( firstPoint ), Transform.CreateTranslation( -XYZ.BasisX * lengthOfDirection ).OfPoint( firstPoint ) ).CreateTransformed( Transform.CreateRotationAtPoint( document.ActiveView.ViewDirection, RotateAngle, firstPoint ) ) ;


          var detailLineBodyDirection = document.Create.NewDetailCurve( document.ActiveView, lineBodyDirection ) ;
          detailLineBodyDirection.LineStyle = subCategoryForBodyDirection.GetGraphicsStyle( GraphicsStyleType.Projection ) ;


          //Create green circle on the outer shape of the shaft
          var subCategoryForOuterShape = GetLineStyle( document, "SubCategoryForCylindricalShaft", new Color( 0, 250, 0 ), 7 ) ;
          var allFloorPlanView = document.GetAllElements<ViewPlan>().Where( v => v.GenLevel != null ).ToList() ;
          foreach ( var viewPlan in allFloorPlanView ) {
            var heightCircle = viewPlan.GenLevel.Elevation ;
            var circleCurve = Arc.Create( new XYZ( firstPoint.X, firstPoint.Y, heightCircle ), radius, startAngle, endAngle, xAxis, yAxis ) ;
            var greenCircle = document.Create.NewDetailCurve( viewPlan, circleCurve ) ;
            greenCircle.LineStyle = subCategoryForOuterShape.GetGraphicsStyle( GraphicsStyleType.Projection ) ;
          }

          trans.Commit() ;
        }

        return Result.Succeeded ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        message = e.Message ;
        return Result.Failed ;
      }
    }

    private static Category GetLineStyle( Document document, string subCategoryName, Color color, int lineWeight )
    {
      var categories = document.Settings.Categories ;
      var category = document.Settings.Categories.get_Item( BuiltInCategory.OST_GenericAnnotation ) ;
      Category subCategory ;
      if ( ! category.SubCategories.Contains( subCategoryName ) ) {
        subCategory = categories.NewSubcategory( category, subCategoryName ) ;
        subCategory.LineColor = color ;
        subCategory.SetLineWeight( lineWeight, GraphicsStyleType.Projection ) ;
      }
      else {
        subCategory = category.SubCategories.get_Item( subCategoryName ) ;
      }

      return subCategory ;
    }

    private static IEnumerable<Curve>? GetCurvesIntersectElement( Document document, Curve bodyDirection )
    {
      var curveIntersects = new List<Curve>() { bodyDirection } ;

      var class2DFilters = new ElementMulticlassFilter( new List<Type>()
      {
        typeof( Wire ),
        typeof( TextNote )
      } ) ;
      var class3DFilters = new ElementMulticlassFilter( new List<Type>()
      {
        typeof( CableTray ),
        typeof( Conduit ),
        typeof( FamilyInstance )
      } ) ;
      var classFilters = new LogicalOrFilter( class2DFilters, class3DFilters ) ;
      
      
      var elementInViews = new FilteredElementCollector( document, document.ActiveView.Id ).WherePasses( classFilters ).ToElements() ;
      if ( ! elementInViews.Any() )
        return curveIntersects ;

      var wireTextNote = elementInViews.Where( x => x is Wire or TextNote ) ;
      var elementIntersect = new List<Element>() ;

      var solid = CreateSolidFromCurve( document, bodyDirection ) ;
      if ( null != solid ) {
        var filter = new ElementIntersectsSolidFilter( solid ) ;
        var elementIntersectSolids = new FilteredElementCollector( document, elementInViews.Select( x => x.Id ).ToList() ).WherePasses( filter ).ToElements()
          .Where( x => x is FamilyInstance familyInstance && ( familyInstance.MEPModel?.ConnectorManager?.Connectors?.Size ?? 0 ) > 0 || true ) ;
        elementIntersect.AddRange(elementIntersectSolids) ;
      }
      else {
      }

      return null ;
    }

    private static Solid? CreateSolidFromCurve( Document document, Curve bodyDirection )
    {
      try {
        var offset = 3000d.MillimetersToRevitUnits() ;
        var tolerance = 50d.MillimetersToRevitUnits() ;
        var levels = document.GetAllElements<Level>().OrderBy( x => x.Elevation ).EnumerateAll() ;
        var curveLoop = CurveLoop.CreateViaThicken( bodyDirection, tolerance, document.ActiveView.ViewDirection ) ;
        return GeometryCreationUtilities.CreateExtrusionGeometry( new List<CurveLoop>() { CurveLoop.CreateViaTransform( curveLoop, Transform.CreateTranslation( -XYZ.BasisZ * ( document.ActiveView.GenLevel.Elevation - levels.First().Elevation - offset ) ) ) }, document.ActiveView.ViewDirection, 2 * offset + levels.Last().Elevation - levels.First().Elevation ) ;
      }
      catch {
        return null ;
      }
    }
  }
}