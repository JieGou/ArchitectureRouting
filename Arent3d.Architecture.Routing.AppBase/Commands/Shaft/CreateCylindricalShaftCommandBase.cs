using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Utility ;
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

        var scaleSetup = document.GetSetupPrintStorable().Scale ;
        var ratio = scaleSetup / 100d ;
        
        var shaftProfile = new CurveArray() ;
        var radius = 60d.MillimetersToRevitUnits()*ratio ;
        var cylinderCurve = Arc.Create( centerPoint, radius, 0, 2 * Math.PI, XYZ.BasisX, XYZ.BasisY ) ;
        shaftProfile.Append( cylinderCurve ) ;
        document.Create.NewOpening( levels.First(), levels.Last(), shaftProfile ) ;

        var symbolDirection = document.GetFamilySymbols( ElectricalRoutingFamilyType.SymbolDirectionCylindricalShaft ).FirstOrDefault() ?? throw new InvalidOperationException() ;
        if ( ! symbolDirection.IsActive ) symbolDirection.Activate() ;

        var lengthDirection = 12000d.MillimetersToRevitUnits()*ratio ;
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
            curvesBody = GeometryHelper.GetCurvesAfterIntersection( viewPlan, new List<Curve> { bodyDirections[ 0 ].CreateTransformed( transformTranslation ) } ) ;
          }
          else if ( viewPlans.IndexOf( viewPlan ) == viewPlans.Count - 1 ) {
            PlaceInstance( viewPlan, symbolDirection, bodyDirections[ 1 ], Math.PI * 0.5 + RotateAngle ) ;
            curvesBody = GeometryHelper.GetCurvesAfterIntersection( viewPlan, new List<Curve> { bodyDirections[ 1 ].CreateTransformed( transformTranslation ) } ) ;
          }
          else {
            PlaceInstance( viewPlan, symbolDirection, bodyDirections[ 0 ], RotateAngle - Math.PI * 0.5 ) ;
            PlaceInstance( viewPlan, symbolDirection, bodyDirections[ 1 ], Math.PI * 0.5 + RotateAngle ) ;
            curvesBody = GeometryHelper.GetCurvesAfterIntersection( viewPlan, bodyDirections.Select( x => x.CreateTransformed( transformTranslation ) ).ToList() ) ;
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
  }
}