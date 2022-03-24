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

          var lengthOfDirection = radius * 100 ;

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

          var bodyDirection = Line.CreateBound( Transform.CreateTranslation( XYZ.BasisX * lengthOfDirection ).OfPoint( firstPoint ), Transform.CreateTranslation( -XYZ.BasisX * lengthOfDirection ).OfPoint( firstPoint ) ).CreateTransformed( Transform.CreateRotationAtPoint( document.ActiveView.ViewDirection, RotateAngle, firstPoint ) ) ;

          var instanceOne = document.Create.NewFamilyInstance( bodyDirection.GetEndPoint( 0 ), symbolDirection, document.ActiveView ) ;
          var axis = Line.CreateBound( bodyDirection.GetEndPoint( 0 ), Transform.CreateTranslation( XYZ.BasisZ ).OfPoint( bodyDirection.GetEndPoint( 0 ) ) ) ;
          ElementTransformUtils.RotateElement( document, instanceOne.Id, axis, RotateAngle - Math.PI * 0.5 ) ;
          var instanceTwo = document.Create.NewFamilyInstance( bodyDirection.GetEndPoint( 1 ), symbolDirection, document.ActiveView ) ;
          axis = Line.CreateBound( bodyDirection.GetEndPoint( 1 ), Transform.CreateTranslation( XYZ.BasisZ ).OfPoint( bodyDirection.GetEndPoint( 1 ) ) ) ;
          ElementTransformUtils.RotateElement( document, instanceTwo.Id, axis, Math.PI * 0.5 + RotateAngle ) ;

          var subCategoryForBodyDirection = GetLineStyle( document, "SubCategoryForDirectionCylindricalShaft", new Color( 255, 0, 255 ), 1 ) ;
          var curvesBody = GetCurvesIntersectElement( document, bodyDirection ) ;
          foreach ( var curveBody in curvesBody ) {
            var detailLineCurveBody = document.Create.NewDetailCurve( document.ActiveView, curveBody ) ;
            detailLineCurveBody.LineStyle = subCategoryForBodyDirection.GetGraphicsStyle( GraphicsStyleType.Projection ) ;
          }

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

    private static IEnumerable<Curve> GetCurvesIntersectElement( Document document, Curve bodyDirection )
    {
      var classFilter2Ds = new ElementMulticlassFilter( new List<Type> { typeof( Wire ), typeof( TextNote ) } ) ;
      var classFilter3Ds = new ElementMulticlassFilter( new List<Type> { typeof( CableTray ), typeof( Conduit ), typeof( FamilyInstance ) } ) ;
      var outlineCurve = GetOutlineFromCurve( document, bodyDirection ) ;
      var boxFilter = new BoundingBoxIntersectsFilter( outlineCurve ) ;

      var element2Ds = new FilteredElementCollector( document, document.ActiveView.Id ).WherePasses( classFilter2Ds ).ToElements() ;
      var element3Ds = new FilteredElementCollector( document, document.ActiveView.Id ).WherePasses( new LogicalAndFilter( classFilter3Ds, boxFilter ) ).ToElements().Where( x => x is FamilyInstance familyInstance && ( familyInstance.MEPModel?.ConnectorManager?.Connectors?.Size ?? 0 ) > 0 || true ) ;

      var elevation = document.ActiveView.GenLevel.Elevation ;
      var viewDirection = document.ActiveView.ViewDirection ;
      var curveIntersects = new List<Curve> { bodyDirection } ;

      CurveLoop? curveLoop ;
      foreach ( var element2D in element2Ds ) {
        var boxElement2D = element2D.get_BoundingBox( document.ActiveView ) ;
        var outlineElement2D = new Outline( new XYZ( boxElement2D.Min.X, boxElement2D.Min.Y, elevation ), new XYZ( boxElement2D.Max.X, boxElement2D.Max.Y, elevation ) ) ;
        if ( ! outlineCurve.Intersects( outlineElement2D, 0d ) ) continue ;

        switch ( element2D ) {
          case TextNote textNote :
            curveLoop = CurveLoop.CreateViaTransform( GeometryHelper.GetOutlineTextNote( textNote ), Transform.CreateTranslation( viewDirection * elevation ) ) ;
            break ;
          case Wire wire :
          {
            if ( wire.Location is not LocationCurve { Curve: Line line } )
              continue ;
            curveLoop = CurveLoop.CreateViaThicken( line.Clone(), 400d.MillimetersToRevitUnits(), viewDirection ) ;
            break ;
          }
          default :
            curveLoop = null ;
            break ;
        }

        if ( null != curveLoop && curveIntersects.Any() )
          curveIntersects = GetCurvesIntersectSolid( document, curveLoop, curveIntersects ) ;
      }

      foreach ( var element3D in element3Ds ) {
        if ( element3D is FamilyInstance familyInstance ) {
          curveLoop = CurveLoop.CreateViaOffset( GeometryHelper.GetBoundaryBoundingBox( familyInstance.get_BoundingBox( null ), elevation ), 200d.MillimetersToRevitUnits(), viewDirection ) ;
        }
        else {
          if ( element3D.Location is not LocationCurve { Curve: Line line })
            continue ;
          
          if(line.Length <= document.Application.ShortCurveTolerance)
            continue;

          var endPointOne =  new XYZ( line.GetEndPoint( 0 ).X, line.GetEndPoint( 0 ).Y, elevation ) ;
          var endPointTwo = new XYZ( line.GetEndPoint( 1 ).X, line.GetEndPoint( 1 ).Y, elevation ) ;
          if(endPointOne.DistanceTo(endPointTwo) <= document.Application.ShortCurveTolerance)
            continue;
          var location = Line.CreateBound( endPointOne, endPointTwo ) ;

          switch ( element3D ) {
            case CableTray cableTray :
              curveLoop = CurveLoop.CreateViaThicken( location, 600d.MillimetersToRevitUnits() + cableTray.Width, viewDirection ) ;
              break ;
            case Conduit conduit :
            {
              var outSizePara = conduit.get_Parameter( BuiltInParameter.RBS_CONDUIT_OUTER_DIAM_PARAM ) ;
              if ( null == outSizePara )
                continue ;
              curveLoop = CurveLoop.CreateViaThicken( location, 200d.MillimetersToRevitUnits() + outSizePara.AsDouble(), viewDirection ) ;
              break ;
            }
            default :
              curveLoop = null ;
              break ;
          }
        }

        if ( null != curveLoop && curveIntersects.Any() )
          curveIntersects = GetCurvesIntersectSolid( document, curveLoop, curveIntersects ) ;
      }

      return curveIntersects ;
    }

    private static Outline GetOutlineFromCurve( Document document, Curve bodyDirection )
    {
      var endPointOne = bodyDirection.GetEndPoint( 0 ) ;
      var endPointTwo = bodyDirection.GetEndPoint( 1 ) ;
      var rangeExtend = 3000d.MillimetersToRevitUnits() ;
      var minZ = bodyDirection.Evaluate( 0.5, true ).Z - rangeExtend ;
      var maxZ = bodyDirection.Evaluate( 0.5, true ).Z + rangeExtend ;
      var levels = document.GetAllElements<Level>().OrderBy( x => x.Elevation ).EnumerateAll() ;
      if ( levels.Any() ) {
        minZ = levels.First().Elevation - rangeExtend ;
        maxZ = levels.Last().Elevation + rangeExtend ;
      }

      var minPoint = new XYZ( Math.Min( endPointOne.X, endPointTwo.X ), Math.Min( endPointOne.Y, endPointTwo.Y ), minZ ) ;
      var maxPoint = new XYZ( Math.Max( endPointOne.X, endPointTwo.X ), Math.Max( endPointOne.Y, endPointTwo.Y ), maxZ ) ;
      return new Outline( minPoint, maxPoint ) ;
    }

    private static List<Curve> GetCurvesIntersectSolid( Document document, CurveLoop curveLoop, IEnumerable<Curve> curves )
    {
      var tolerance = 100d.MillimetersToRevitUnits() ;
      var curveResult = new List<Curve>() ;
      try {
        curveLoop = CurveLoop.CreateViaTransform( curveLoop, Transform.CreateTranslation( document.ActiveView.ViewDirection.Negate() * tolerance ) ) ;
        var solid = GeometryCreationUtilities.CreateExtrusionGeometry( new List<CurveLoop> { curveLoop }, document.ActiveView.ViewDirection, 2 * tolerance ) ;

        var option = new SolidCurveIntersectionOptions { ResultType = SolidCurveIntersectionMode.CurveSegmentsOutside } ;
        foreach ( var curve in curves ) {
          var result = solid.IntersectWithCurve( curve, option ) ;
          if ( null == result ) continue ;

          for ( var i = 0 ; i < result.SegmentCount ; i++ ) {
            if ( result.GetCurveSegment( i ).Length > document.Application.ShortCurveTolerance ) {
              curveResult.Add( result.GetCurveSegment( i ) ) ;
            }
          }
        }
      }
      catch {
        // ignore
      }

      return curveResult ;
    }
  }
}