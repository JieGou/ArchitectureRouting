using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Shaft
{
  public class CreateCylindricalShaftCommandBase : IExternalCommand
  {
    private const double RotateAngle = Math.PI / 3 ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var selection = commandData.Application.ActiveUIDocument.Selection ;

      try {
        if ( document.ActiveView.ViewType != ViewType.FloorPlan ) {
          message = "Only created in floor plan view!" ;
          return Result.Cancelled ;
        }

        var centerPoint = selection.PickPoint( ObjectSnapTypes.Midpoints | ObjectSnapTypes.Centers, "Pick a point." ) ;

        var dialog = new GetLevel( document ) ;
        if ( false == dialog.ShowDialog() )
          return Result.Succeeded ;

        var levels = dialog.GetSelectedLevels().Select( x => document.GetElement( x.Id ) ).OfType<Level>().OrderBy( x => x.Elevation ).EnumerateAll() ;
        if ( ! levels.Any() ) {
          message = "Please, select level in the dialog!" ;
          return Result.Cancelled ;
        }

        using var trans = new Transaction( document, "Create Arent Shaft" ) ;
        trans.Start() ;

        var shaftProfile = new CurveArray() ;
        var radius = 60d.MillimetersToRevitUnits() ;
        var cylinderCurve = Arc.Create( centerPoint, radius, 0, 2 * Math.PI, XYZ.BasisX, XYZ.BasisY ) ;
        shaftProfile.Append( cylinderCurve ) ;
        document.Create.NewOpening( levels.First(), levels.Last(), shaftProfile ) ;

        var symbolDirection = document.GetFamilySymbols( ElectricalRoutingFamilyType.SymbolDirectionCylindricalShaft ).FirstOrDefault() ?? throw new InvalidOperationException() ;
        if ( ! symbolDirection.IsActive ) symbolDirection.Activate() ;

        var lengthDirection = 12000d.MillimetersToRevitUnits() ;
        var transformRotation = Transform.CreateRotationAtPoint( document.ActiveView.ViewDirection, RotateAngle, centerPoint ) ;
        var bodyDirections = new List<Curve> { Line.CreateBound( Transform.CreateTranslation( XYZ.BasisX * radius ).OfPoint( centerPoint ), Transform.CreateTranslation( XYZ.BasisX * lengthDirection * 0.5 ).OfPoint( centerPoint ) ).CreateTransformed( transformRotation ), Line.CreateBound( Transform.CreateTranslation( -XYZ.BasisX * radius ).OfPoint( centerPoint ), Transform.CreateTranslation( -XYZ.BasisX * lengthDirection * 0.5 ).OfPoint( centerPoint ) ).CreateTransformed( transformRotation ) } ;

        var subCategoryForBodyDirection = GetLineStyle( document, "SubCategoryForDirectionCylindricalShaft", new Color( 255, 0, 255 ), 1 ).GetGraphicsStyle( GraphicsStyleType.Projection ) ;
        var subCategoryForOuterShape = GetLineStyle( document, "SubCategoryForCylindricalShaft", new Color( 0, 250, 0 ), 2 ).GetGraphicsStyle( GraphicsStyleType.Projection ) ;

        var viewPlans = document.GetAllElements<ViewPlan>().Where( x => ! x.IsTemplate && x.ViewType == ViewType.FloorPlan && levels.Any( y => y.Id == x.GenLevel.Id ) ).OrderBy( x => x.GenLevel.Elevation ).EnumerateAll() ;
        foreach ( var viewPlan in viewPlans ) {
          var transformTranslation = Transform.CreateTranslation( XYZ.BasisZ * ( viewPlan.GenLevel.Elevation - centerPoint.Z ) ) ;

          IEnumerable<Curve> curvesBody ;
          if ( viewPlans.IndexOf( viewPlan ) == 0 ) {
            PlaceInstance( viewPlan, symbolDirection, bodyDirections[ 0 ], RotateAngle - Math.PI * 0.5 ) ;
            curvesBody = GetCurvesIntersectElement( viewPlan, transformTranslation.OfPoint( centerPoint ), new List<Curve> { bodyDirections[ 0 ].CreateTransformed( transformTranslation ) } ) ;
          }
          else if ( viewPlans.IndexOf( viewPlan ) == viewPlans.Count - 1 ) {
            PlaceInstance( viewPlan, symbolDirection, bodyDirections[ 1 ], Math.PI * 0.5 + RotateAngle ) ;
            curvesBody = GetCurvesIntersectElement( viewPlan, transformTranslation.OfPoint( centerPoint ), new List<Curve> { bodyDirections[ 1 ].CreateTransformed( transformTranslation ) } ) ;
          }
          else {
            PlaceInstance( viewPlan, symbolDirection, bodyDirections[ 0 ], RotateAngle - Math.PI * 0.5 ) ;
            PlaceInstance( viewPlan, symbolDirection, bodyDirections[ 1 ], Math.PI * 0.5 + RotateAngle ) ;
            curvesBody = GetCurvesIntersectElement( viewPlan, transformTranslation.OfPoint( centerPoint ), bodyDirections.Select( x => x.CreateTransformed( transformTranslation ) ).ToList() ) ;
          }

          curvesBody.ForEach( x => CreateDetailLine( viewPlan, subCategoryForBodyDirection, x ) ) ;

          var circle = Arc.Create( new XYZ( centerPoint.X, centerPoint.Y, viewPlan.GenLevel.Elevation ), radius, 0, 2 * Math.PI, XYZ.BasisX, XYZ.BasisY ) ;
          CreateDetailLine( viewPlan, subCategoryForOuterShape, circle ) ;
        }

        trans.Commit() ;

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

    private static void CreateDetailLine( View viewPlan, Element lineStyle, Curve curve )
    {
      var detailLineOne = viewPlan.Document.Create.NewDetailCurve( viewPlan, curve ) ;
      detailLineOne.LineStyle = lineStyle ;
    }

    private static void PlaceInstance( View viewPlan, FamilySymbol symbolDirection, Curve halfBodyDirection, double angleRotate )
    {
      var pointBase = halfBodyDirection.GetEndPoint( 1 ) ;
      var instance = viewPlan.Document.Create.NewFamilyInstance( pointBase, symbolDirection, viewPlan ) ;
      var axis = Line.CreateBound( pointBase, Transform.CreateTranslation( XYZ.BasisZ ).OfPoint( pointBase ) ) ;
      ElementTransformUtils.RotateElement( viewPlan.Document, instance.Id, axis, angleRotate ) ;
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

    private static IEnumerable<Curve> GetCurvesIntersectElement( View viewPlan, XYZ centerPoint, IList<Curve> bodyDirections )
    {
      var classFilter2Ds = new ElementMulticlassFilter( new List<Type> { typeof( Wire ), typeof( TextNote ) } ) ;
      var classFilter3Ds = new ElementMulticlassFilter( new List<Type> { typeof( CableTray ), typeof( Conduit ), typeof( FamilyInstance ) } ) ;
      var outlineCurve = GetOutlineFromCurve( viewPlan.Document, centerPoint.Z, bodyDirections.Select( x => x.Tessellate() ).SelectMany( x => x ).ToList() ) ;
      var boxFilter = new BoundingBoxIntersectsFilter( outlineCurve ) ;

      var element2Ds = new FilteredElementCollector( viewPlan.Document, viewPlan.Id ).WherePasses( classFilter2Ds ).ToElements() ;
      var element3Ds = new FilteredElementCollector( viewPlan.Document, viewPlan.Id ).WherePasses( new LogicalAndFilter( classFilter3Ds, boxFilter ) ).ToElements().Where( x => x is FamilyInstance familyInstance && ( familyInstance.MEPModel?.ConnectorManager?.Connectors?.Size ?? 0 ) > 0 || true ) ;

      var elevation = viewPlan.GenLevel.Elevation ;
      var viewDirection = viewPlan.ViewDirection ;
      var curveIntersects = new List<Curve>( bodyDirections ) ;

      foreach ( var element2D in element2Ds ) {
        var boxElement2D = element2D.get_BoundingBox( viewPlan ) ;
        var outlineElement2D = new Outline( new XYZ( boxElement2D.Min.X, boxElement2D.Min.Y, elevation ), new XYZ( boxElement2D.Max.X, boxElement2D.Max.Y, elevation ) ) ;
        if ( ! outlineCurve.Intersects( outlineElement2D, 0d ) ) continue ;

        CurveLoop? curveLoopOrigin = null ;
        CurveLoop? curveLoopOffset = null ;
        Line? locationElement = null ;
        switch ( element2D ) {
          case TextNote textNote :
            var outline = GeometryHelper.GetOutlineTextNote( textNote ) ;
            curveLoopOffset = CurveLoop.CreateViaTransform( outline, Transform.CreateTranslation( viewDirection * ( elevation - textNote.Coord.Z ) ) ) ;
            break ;
          case Wire wire :
            if ( wire.Location is not LocationCurve { Curve: Line line } )
              continue ;

            if ( line.Length <= viewPlan.Document.Application.ShortCurveTolerance )
              continue ;

            locationElement = line.Clone() as Line ;
            curveLoopOffset = CurveLoop.CreateViaThicken( locationElement, 400d.MillimetersToRevitUnits(), viewDirection ) ;
            break ;
        }

        if ( null != curveLoopOffset && curveIntersects.Any() )
          curveIntersects = GetCurvesIntersectSolid( viewPlan, curveLoopOrigin, curveLoopOffset, locationElement, curveIntersects ) ;
      }

      foreach ( var element3D in element3Ds ) {
        CurveLoop? curveLoopOrigin = null ;
        CurveLoop? curveLoopOffset = null ;
        Line? locationElement = null ;

        if ( element3D is FamilyInstance familyInstance ) {
          curveLoopOrigin = GeometryHelper.GetBoundaryBoundingBox( familyInstance.get_BoundingBox( null ), elevation ) ;
          curveLoopOffset = CurveLoop.CreateViaOffset( curveLoopOrigin, 200d.MillimetersToRevitUnits(), viewDirection ) ;
        }
        else {
          if ( element3D.Location is not LocationCurve { Curve: Line line } )
            continue ;

          if ( line.Length <= viewPlan.Document.Application.ShortCurveTolerance )
            continue ;

          var endPointOne = new XYZ( line.GetEndPoint( 0 ).X, line.GetEndPoint( 0 ).Y, elevation ) ;
          var endPointTwo = new XYZ( line.GetEndPoint( 1 ).X, line.GetEndPoint( 1 ).Y, elevation ) ;
          if ( endPointOne.DistanceTo( endPointTwo ) <= viewPlan.Document.Application.ShortCurveTolerance )
            continue ;
          locationElement = Line.CreateBound( endPointOne, endPointTwo ) ;

          switch ( element3D ) {
            case CableTray cableTray :
              curveLoopOffset = CurveLoop.CreateViaThicken( locationElement, 600d.MillimetersToRevitUnits() + cableTray.Width, viewDirection ) ;
              break ;
            case Conduit conduit :
            {
              var outSizePara = conduit.get_Parameter( BuiltInParameter.RBS_CONDUIT_OUTER_DIAM_PARAM ) ;
              if ( null == outSizePara )
                continue ;
              curveLoopOffset = CurveLoop.CreateViaThicken( locationElement, 400d.MillimetersToRevitUnits() + outSizePara.AsDouble(), viewDirection ) ;
              break ;
            }
          }
        }

        if ( null != curveLoopOffset && curveIntersects.Any() )
          curveIntersects = GetCurvesIntersectSolid( viewPlan, curveLoopOrigin, curveLoopOffset, locationElement, curveIntersects ) ;
      }

      return curveIntersects ;
    }

    private static Outline GetOutlineFromCurve( Document document, double pickPointZ, IList<XYZ> points )
    {
      var rangeExtend = 3000d.MillimetersToRevitUnits() ;
      var minZ = pickPointZ - rangeExtend ;
      var maxZ = pickPointZ + rangeExtend ;

      var levels = document.GetAllElements<Level>().OrderBy( x => x.Elevation ).EnumerateAll() ;
      if ( levels.Any() ) {
        minZ = levels.First().Elevation - rangeExtend ;
        maxZ = levels.Last().Elevation + rangeExtend ;
      }

      var minPoint = new XYZ( points.MinBy( x => x.X )!.X, points.MinBy( x => x.Y )!.Y, minZ ) ;
      var maxPoint = new XYZ( points.MaxBy( x => x.X )!.X, points.MaxBy( x => x.Y )!.Y, maxZ ) ;
      return new Outline( minPoint, maxPoint ) ;
    }

    private static List<Curve> GetCurvesIntersectSolid( View viewPlan, CurveLoop? curveLoopOrigin, CurveLoop curveLoopOffset, Curve? locationElement, List<Curve> curves )
    {
      var curvesResult = new List<Curve>() ;
      try {
        if ( null != curveLoopOrigin ) {
          var solidOrigin = CreateSolid( viewPlan, curveLoopOrigin ) ;
          var curvesTemp = GetCurvesIntersectSolid( solidOrigin, curves, viewPlan.Document.Application.ShortCurveTolerance ) ;
          if ( Math.Abs( curves.Select( x => x.Length ).Sum() - curvesTemp.Select( x => x.Length ).Sum() ) < GeometryHelper.Tolerance )
            return curves ;
        }

        if ( null != locationElement ) {
          var pointOne = new XYZ( locationElement.GetEndPoint( 0 ).X, locationElement.GetEndPoint( 0 ).Y, viewPlan.GenLevel.Elevation ) ;
          var pointTwo = new XYZ( locationElement.GetEndPoint( 1 ).X, locationElement.GetEndPoint( 1 ).Y, viewPlan.GenLevel.Elevation ) ;
          if ( pointOne.DistanceTo( pointTwo ) < GeometryHelper.Tolerance )
            return curves ;

          var newLocationElement = Line.CreateBound( pointOne, pointTwo ) ;
          if ( ! GeometryHelper.IsCurveIntersectCurves( newLocationElement, curves ) )
            return curves ;
        }

        var solidScale = CreateSolid( viewPlan, curveLoopOffset ) ;
        curvesResult = GetCurvesIntersectSolid( solidScale, curves, viewPlan.Document.Application.ShortCurveTolerance ) ;
      }
      catch {
        // ignore
      }

      return curvesResult ;
    }

    private static List<Curve> GetCurvesIntersectSolid( Solid? solid, IList<Curve> curves, double shortCurveTolerance )
    {
      var curveResult = new List<Curve>() ;
      if ( null == solid || ! curves.Any() )
        return curveResult ;

      var option = new SolidCurveIntersectionOptions { ResultType = SolidCurveIntersectionMode.CurveSegmentsOutside } ;
      foreach ( var curve in curves ) {
        var result = solid.IntersectWithCurve( curve, option ) ;
        if ( null == result ) continue ;

        for ( var i = 0 ; i < result.SegmentCount ; i++ ) {
          if ( result.GetCurveSegment( i ).Length > shortCurveTolerance ) {
            curveResult.Add( result.GetCurveSegment( i ) ) ;
          }
        }
      }

      return curveResult ;
    }

    private static Solid CreateSolid( View viewPlan, CurveLoop curveLoop )
    {
      var tolerance = 100d.MillimetersToRevitUnits() ;
      curveLoop = CurveLoop.CreateViaTransform( curveLoop, Transform.CreateTranslation( viewPlan.ViewDirection.Negate() * tolerance ) ) ;
      var solid = GeometryCreationUtilities.CreateExtrusionGeometry( new List<CurveLoop> { curveLoop }, viewPlan.ViewDirection, 2 * tolerance ) ;
      return solid ;
    }
  }
}