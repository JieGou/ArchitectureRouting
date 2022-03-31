using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Shaft
{
  [Transaction( TransactionMode.Manual )]
  public class CreateSignCylindricalShaftCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var selection = commandData.Application.ActiveUIDocument.Selection ;

      var dialog = new GetLevel( document ) ;
      if ( false == dialog.ShowDialog() )
        return Result.Succeeded ;

      try {
        var cylindricalShaftReference = selection.PickObject( ObjectType.Element, SelectionFilter.GetElementFilter( x =>
        {
          if ( x is not Opening opening )
            return false ;

          var curves = opening.BoundaryCurves.OfType<Curve>().EnumerateAll() ;
          if ( curves.Count != 1 )
            return false ;

          return curves.First() is Arc ;
        } ), "Pick a cylindrical shaft!" ) ;
        var cylindricalShaft = null != cylindricalShaftReference ? document.GetElement( cylindricalShaftReference ) as Opening : null ;
        if ( null == cylindricalShaft )
          return Result.Failed ;

        var baseLevel = document.GetElement( cylindricalShaft.get_Parameter( BuiltInParameter.WALL_BASE_CONSTRAINT ).AsElementId() ) as Level ;
        var baseElevation = cylindricalShaft.get_Parameter( BuiltInParameter.WALL_BASE_OFFSET ).AsDouble() + baseLevel!.Elevation ;
        
        var topLevelId = cylindricalShaft.get_Parameter( BuiltInParameter.WALL_HEIGHT_TYPE ).AsElementId() ;
        var topElevation = baseElevation;
        if ( topLevelId == ElementId.InvalidElementId  ) {
          topElevation += cylindricalShaft.get_Parameter( BuiltInParameter.WALL_USER_HEIGHT_PARAM ).AsDouble() ;
        }
        else {
          topElevation = ( document.GetElement( topLevelId ) as Level )!.Elevation ;
          topElevation += cylindricalShaft.get_Parameter( BuiltInParameter.WALL_TOP_OFFSET ).AsDouble() ;
        }
        
        var centerPoint = cylindricalShaft.BoundaryCurves.OfType<Arc>().First().Center ;
        var levels = dialog.GetSelectedLevels().Select(x => document.GetElement(x.Id) as Level).Where(x => x!.Elevation >= baseElevation && x.Elevation <= topElevation) ;
        var viewPlans = document.GetAllElements<ViewPlan>()
          .Where( x => ! x.IsTemplate && x.ViewType == ViewType.FloorPlan && levels.Any( y => y!.Id == x.GenLevel.Id ) ) ;

        using var transaction = new Transaction( document ) ;
        transaction.Start( "Electrical.App.Commands.Shaft.CreateSignCylindricalShaftCommand".GetAppStringByKeyOrDefault( "Create Sign Cylindrical Shaft" ) ) ;

        foreach ( var viewPlan in viewPlans ) {
          CreateSymbolCenter( viewPlan, centerPoint ) ;
        }

        transaction.Commit() ;

        return Result.Succeeded ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    private static void CreateSymbolCenter( View viewPlan, XYZ centerPoint )
    {
      var lineStyle = GetLineStyle( viewPlan.Document, "SubCategoryForCylindricalShaft", new Color( 0, 250, 0 ), 2 )
        .GetGraphicsStyle( GraphicsStyleType.Projection ) ;
      var lineOne = Line.CreateBound( Transform.CreateTranslation( XYZ.BasisX * 200d.MillimetersToRevitUnits() ).OfPoint( centerPoint ),
        Transform.CreateTranslation( -XYZ.BasisX * 200d.MillimetersToRevitUnits() ).OfPoint( centerPoint ) ) ;
      CreateDetailLine( viewPlan, lineStyle, lineOne ) ;
      var lineTwo = Line.CreateBound( Transform.CreateTranslation( XYZ.BasisY * 60d.MillimetersToRevitUnits() ).OfPoint( lineOne.GetEndPoint( 1 ) ),
        Transform.CreateTranslation( -XYZ.BasisY * 60d.MillimetersToRevitUnits() ).OfPoint( lineOne.GetEndPoint( 1 ) ) ) ;
      CreateDetailLine( viewPlan, lineStyle, lineTwo ) ;
      var lineThree = Line.CreateBound( Transform.CreateTranslation( XYZ.BasisY * 60d.MillimetersToRevitUnits() ).OfPoint( lineOne.GetEndPoint( 0 ) ),
        Transform.CreateTranslation( -XYZ.BasisY * 60d.MillimetersToRevitUnits() ).OfPoint( lineOne.GetEndPoint( 0 ) ) ) ;
      CreateDetailLine( viewPlan, lineStyle, lineThree ) ;
    }

    private static void CreateDetailLine( View viewPlan, Element lineStyle, Curve curve )
    {
      var detailLineOne = viewPlan.Document.Create.NewDetailCurve( viewPlan, curve ) ;
      detailLineOne.LineStyle = lineStyle ;
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