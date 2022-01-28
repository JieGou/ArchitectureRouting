using Autodesk.Revit.DB ;
using Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Autodesk.Revit.ApplicationServices ;
using Autodesk.Revit.UI ;
using System ;
using System.Linq ;
using Arent3d.Revit ;

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

          var lengthOfDirectionCylindricalShaft = radius * 5 ;

          if ( 2 * lengthOfDirectionCylindricalShaft <= document.Application.ShortCurveTolerance ) {
            message = $"Direction symbol length must be greater than {document.Application.ShortCurveTolerance.RevitUnitsToMillimeters()}mm!" ;
            return Result.Cancelled ;
          }

          var symbol = document.GetFamilySymbols( RoutingFamilyType.DirectionCylindricalShaft ).FirstOrDefault() ?? throw new InvalidOperationException() ;
          if ( ! symbol.IsActive ) symbol.Activate() ;

          if ( document.ActiveView.ViewType != ViewType.FloorPlan ) {
            message = "Only created in floor plan view!" ;
            return Result.Cancelled ;
          }

          //Place symbol family
          var instance = document.Create.NewFamilyInstance( firstPoint, symbol, document.ActiveView ) ;
          var axis = Line.CreateBound( firstPoint, Transform.CreateTranslation( XYZ.BasisZ ).OfPoint( firstPoint ) ) ;
          ElementTransformUtils.RotateElement( document, instance.Id, axis, RotateAngle ) ;

          //Set parameters
          instance.LookupParameter( "Length End One" ).Set( lengthOfDirectionCylindricalShaft ) ;
          instance.LookupParameter( "Length End Two" ).Set( lengthOfDirectionCylindricalShaft ) ;

          //Create green circle on the outer shape of the shaft
          var subCategory = GetLineStyle( document ) ;
          var allFloorPlanView = document.GetAllElements<ViewPlan>().Where( v => v.GenLevel != null ).ToList() ;
          foreach ( var viewPlan in allFloorPlanView ) {
            var heightCircle = viewPlan.GenLevel.Elevation ;
            var circleCurve = Arc.Create( new XYZ( firstPoint.X, firstPoint.Y, heightCircle ), radius, startAngle, endAngle, xAxis, yAxis ) ;
            var greenCircle = document.Create.NewDetailCurve( viewPlan, circleCurve ) ;
            greenCircle.LineStyle = subCategory.GetGraphicsStyle( GraphicsStyleType.Projection ) ;
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

    private Category GetLineStyle( Document doc )
    {
      const string subCategoryName = "SubCategoryForCylindricalShaft" ;
      var categories = doc.Settings.Categories ;
      Category category = doc.Settings.Categories.get_Item( BuiltInCategory.OST_GenericAnnotation ) ;
      Category subCategory ;
      if ( ! category.SubCategories.Contains( subCategoryName ) ) {
        subCategory = categories.NewSubcategory( category, subCategoryName ) ;
        var newColor = new Color( 0, 250, 0 ) ;
        subCategory.LineColor = newColor ;
        subCategory.SetLineWeight( 7, GraphicsStyleType.Projection ) ;
      }
      else
        subCategory = category.SubCategories.get_Item( subCategoryName ) ;

      return subCategory ;
    }
  }
}