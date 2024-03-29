﻿using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Shaft
{
  public class CreateCylindricalShaftCommandBase : IExternalCommand
  {
    private const double RotateAngle = Math.PI / 3 ;
    public const string SubCategoryForSymbolName = "SubCategoryForSymbol";
    public const string SubCategoryForDirectionCylindricalShaftName = "SubCategoryForDirectionCylindricalShaft" ;
    public const string SubCategoryForCylindricalShaftName = "SubCategoryForCylindricalShaft" ;
    
    private static double DefaultRadius => 60d.MillimetersToRevitUnits() ;
    private static double DefaultCableTrayScale => 4d ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var selection = commandData.Application.ActiveUIDocument.Selection ;

      try {
        if ( document.ActiveView.ViewType != ViewType.FloorPlan ) {
          message = "Only created in floor plan view!" ;
          return Result.Cancelled ;
        }

        var shaftOpeningStore = document.GetShaftOpeningStorable() ;
        var shaftOpeningModels = new List<ShaftOpeningModel>() ;
        XYZ? centerPoint ;
        if ( selection.GetElementIds().Count > 0 ) {
          var shaftOpening = selection.GetElementIds().Select( x => document.GetElement( x ) ).OfType<Opening>().FirstOrDefault() ;
          var cableTrayInstance = selection.GetElementIds().Select( x => document.GetElement( x ) ).OfType<FamilyInstance>().FirstOrDefault( x => x.Symbol is { } fs && fs.FamilyName == ElectricalRoutingFamilyType.ShaftHSymbol.GetFamilyName() ) ;
          var curveElement = document.GetAllInstances<CurveElement>( document.ActiveView )
            .FirstOrDefault( x => x.LineStyle.Name is SubCategoryForSymbolName or SubCategoryForDirectionCylindricalShaftName or SubCategoryForCylindricalShaftName 
                         && selection.GetElementIds().Contains( x.Id ) ) ;
          if ( shaftOpening == null && cableTrayInstance == null && curveElement == null ) return Result.Succeeded ;
          shaftOpeningModels = GetExistedShaftModels( shaftOpeningStore, shaftOpening?.UniqueId ?? string.Empty, cableTrayInstance?.UniqueId ?? string.Empty, curveElement?.UniqueId ?? string.Empty ) ;
          if ( ! shaftOpeningModels.Any() ) return Result.Succeeded ;
          centerPoint = GetShaftOriginPoint( document, shaftOpeningModels ) ;
          if ( centerPoint == null ) return Result.Succeeded ;
        }
        else {
          centerPoint = selection.PickPoint( ObjectSnapTypes.Midpoints | ObjectSnapTypes.Centers, "Pick a point." ) ;
        }

        var shaftSettingViewModel = new ShaftSettingViewModel( document ) ;
        if ( shaftOpeningModels.Any() )
          shaftSettingViewModel.SetShaftModels( shaftOpeningModels ) ;
        var dialog = new ShaftSettingDialog( shaftSettingViewModel ) ;
        if ( false == dialog.ShowDialog() )
          return Result.Succeeded ;

        var shaftModels = dialog.ShaftSettingViewModel.Shafts.Where( s => s.IsShafted ).ToList() ;
        if ( ! shaftModels.Any() && ! shaftOpeningModels.Any() ) {
          message = "Please, select level in the dialog!" ;
          return Result.Cancelled ;
        }

        using var trans = new Transaction( document, "Create Arent Shaft" ) ;
        trans.Start() ;

        if ( shaftModels.Any() ) {
          var shaftProfile = new CurveArray() ;
          double.TryParse( dialog.ShaftSettingViewModel.Size, out var widthCableTray ) ;
          var radius = widthCableTray.MillimetersToRevitUnits() / 2 ;
          var cylinderCurve = Arc.Create( centerPoint, radius, 0, 2 * Math.PI, XYZ.BasisX, XYZ.BasisY ) ;
          shaftProfile.Append( cylinderCurve ) ;
          var shaftModelGroup = GroupShaftModels( shaftModels ) ;
          var shaftIndex = shaftOpeningModels.Any() ? shaftOpeningModels.First().ShaftIndex : shaftOpeningStore.ShaftOpeningModels.Count ;

          foreach ( var shafts in shaftModelGroup ) {
            var levels = shafts.Select( s => s.FromLevel ).ToList() ;
            levels.Add( shafts.Last().ToLevel ) ;
            var opening = document.Create.NewOpening( levels.First(), levels.Last(), shaftProfile ) ;
            document.Regenerate() ;

            List<Element> cableTraySymbols = new() ;
            List<string> cableTraySymbolIds = new() ;
            foreach ( var shaftModel in shafts ) {
              if ( ! shaftModel.IsRacked ) continue ;
              var cableTrayLength = shaftModel.ToLevel.Elevation - shaftModel.FromLevel.Elevation ;
              var cableTraySymbol = CreateCableTraySymbol( document, centerPoint.X, centerPoint.Y, cableTrayLength, shaftModel.FromLevel ) ;
              cableTraySymbols.Add( cableTraySymbol ) ;
              cableTraySymbolIds.Add( cableTraySymbol.UniqueId ) ;
            }

            var detailUniqueIds = new List<string>() ;
            var (styleForBodyDirection, styleForOuterShape, styleForSymbol) = GetLineStyles( document ) ;

            var oldCableTrays = GetOldCableTrays( document, shaftOpeningModels ) ;
            oldCableTrays.AddRange( cableTraySymbols ) ;
            var viewPlans = document.GetAllElements<ViewPlan>().Where( x => ! x.IsTemplate && x.ViewType == ViewType.FloorPlan && levels.Any( y => y.Id == x.GenLevel.Id ) ).OrderBy( x => x.GenLevel.Elevation ).EnumerateAll() ;
            foreach ( var viewPlan in viewPlans ) {
              var cableTraySymbolId = cableTraySymbols.FirstOrDefault( x => x.LevelId == viewPlan.GenLevel.Id )?.UniqueId ?? string.Empty ;
              var detailCurves = CreateSymbolForShaftOpeningOnViewPlan( opening, viewPlan, styleForSymbol, styleForBodyDirection, styleForOuterShape, widthCableTray, cableTraySymbolId, oldCableTrays ) ;
              detailUniqueIds.AddRange( detailCurves ) ;
              if ( ! string.IsNullOrEmpty( cableTraySymbolId ) ) cableTraySymbols.RemoveAll( e => e.UniqueId == cableTraySymbolId ) ;
            }

            shaftOpeningStore.ShaftOpeningModels.Add( new ShaftOpeningModel( shaftIndex, opening.UniqueId, cableTraySymbolIds, detailUniqueIds, widthCableTray ) ) ;
          }
        }

        if ( shaftOpeningModels.Any() ) {
          RemoveOldShaftOpeningsAndCableTrays( document, shaftOpeningStore, shaftOpeningModels ) ;
        }
        
        shaftOpeningStore.Save() ;
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

    private static IEnumerable<List<ShaftModel>> GroupShaftModels( IReadOnlyList<ShaftModel> shaftModels )
    {
      var shaftModelGroups = new List<List<ShaftModel>> { new() { shaftModels.First() } } ;
      for ( var i = 1 ; i < shaftModels.Count ; i++ ) {
        if ( shaftModels[ i - 1 ].ToLevel.Id == shaftModels[ i ].FromLevel.Id ) {
          shaftModelGroups.Last().Add( shaftModels[ i ] ) ;
        }
        else {
          shaftModelGroups.Add( new List<ShaftModel> { shaftModels[ i ] } ) ;
        }
      }

      return shaftModelGroups ;
    }

    private static List<ShaftOpeningModel> GetExistedShaftModels( ShaftOpeningStorable shaftOpeningStore, string shaftOpeningId, string cableTrayId, string curveElementId )
    {
      var shaftOpeningModel = shaftOpeningStore.ShaftOpeningModels
        .FirstOrDefault( s => ( ! string.IsNullOrEmpty( shaftOpeningId ) && s.ShaftOpeningUniqueId == shaftOpeningId ) 
                              || ( ! string.IsNullOrEmpty( cableTrayId ) && s.CableTrayUniqueIds.Contains( cableTrayId ) )
                              || ( ! string.IsNullOrEmpty( curveElementId ) && s.DetailUniqueIds.Contains( curveElementId ) ) ) ;
      if ( shaftOpeningModel == null ) return new List<ShaftOpeningModel>() ;
      var shaftModels = shaftOpeningStore.ShaftOpeningModels.Where( s => s.ShaftIndex == shaftOpeningModel.ShaftIndex ).ToList() ;
      return shaftModels ;
    }

    private static XYZ? GetShaftOriginPoint( Document document, IEnumerable<ShaftOpeningModel> shaftOpeningModels )
    {
      var shaftOpeningId = shaftOpeningModels.First().ShaftOpeningUniqueId ;
      var shaftOpening = document.GetElementById<Opening>( shaftOpeningId ) ;
      return shaftOpening?.GetShaftPosition() ;
    }

    private static void RemoveOldShaftOpeningsAndCableTrays( Document document, ShaftOpeningStorable shaftOpeningStore, ICollection<ShaftOpeningModel> oldShaftOpeningModels )
    {
      List<ElementId> oldElementIds = new() ;
      foreach ( var oldShaftOpeningModel in oldShaftOpeningModels ) {
        var shaftOpeningId = document.GetElement( oldShaftOpeningModel.ShaftOpeningUniqueId ).Id ;
        var detailCurveIds = oldShaftOpeningModel.DetailUniqueIds.Where( x => document.GetElement( x ) != null ).Select( document.GetElement ).Select( x => x.Id ).ToList() ;
        var cableTrayIds = oldShaftOpeningModel.CableTrayUniqueIds.Where( x => document.GetElement( x ) != null ).Select( document.GetElement ).Select( x => x.Id ).ToList() ;
        if ( shaftOpeningId != null ) oldElementIds.Add( shaftOpeningId ) ;
        oldElementIds.AddRange( detailCurveIds ) ;
        oldElementIds.AddRange( cableTrayIds ) ;
      }

      document.Delete( oldElementIds ) ;
      shaftOpeningStore.ShaftOpeningModels.RemoveAll( oldShaftOpeningModels.Contains ) ;
    }

    public static List<Element> GetOldCableTrays( Document document, IEnumerable<ShaftOpeningModel> oldShaftOpeningModels )
    {
      List<Element> oldCableTrayIds = new() ;
      foreach ( var oldShaftOpeningModel in oldShaftOpeningModels ) {
        var cableTrayIds = oldShaftOpeningModel.CableTrayUniqueIds.Where( x => document.GetElement( x ) != null ).Select( document.GetElement ).ToList() ;
        oldCableTrayIds.AddRange( cableTrayIds ) ;
      }

      return oldCableTrayIds ;
    }
    
    public static (Element StyleForBodyDirection, Element StyleForOuterShape, Element StyleForSymbol) GetLineStyles(Document document)
    {
      var styleForBodyDirection = GetLineStyle( document, SubCategoryForDirectionCylindricalShaftName, new Color( 255, 0, 255 ), 1 ).GetGraphicsStyle( GraphicsStyleType.Projection ) ;
      var styleForOuterShape = GetLineStyle( document, SubCategoryForCylindricalShaftName, new Color( 0, 250, 0 ), 2 ).GetGraphicsStyle( GraphicsStyleType.Projection ) ;
      var styleForSymbol = GetLineStyle( document, SubCategoryForSymbolName, new Color( 0, 0, 0 ), 2 ).GetGraphicsStyle( GraphicsStyleType.Projection ) ;
      return ( styleForBodyDirection, styleForOuterShape, styleForSymbol ) ;
    }

    public static IEnumerable<string> CreateSymbolForShaftOpeningOnViewPlan(Opening opening, ViewPlan viewPlan, Element styleForSymbol, Element styleForBodyDirection, Element styleForOuterShape, double size, string cableTrayUniqueId, List<Element>? oldCableTrays = null )
    {
      var ratio = viewPlan.Scale / 100d ;
      var scaleRadius = DefaultRadius * ratio ;
      var offSetWidth = size / 100 ;
      var cabTrayWidth = size.MillimetersToRevitUnits() ;

      var detailUniqueIds = new List<string>() ;
      var arc = opening.BoundaryCurves.OfType<Arc>().SingleOrDefault() ;
      if(null == arc)
        return detailUniqueIds; 

      var baseLevel = (Level) opening.Document.GetElement( opening.get_Parameter( BuiltInParameter.WALL_BASE_CONSTRAINT ).AsElementId() ) ;
      var baseElevation = baseLevel.Elevation + opening.get_Parameter( BuiltInParameter.WALL_BASE_OFFSET ).AsDouble() ;
      var topElevation = baseElevation + opening.get_Parameter( BuiltInParameter.WALL_USER_HEIGHT_PARAM ).AsDouble() ;
      
      var viewPlans = opening.Document.GetAllElements<ViewPlan>()
        .Where( x => ! x.IsTemplate && x.ViewType == ViewType.FloorPlan && x.GenLevel.Elevation >= baseElevation && x.GenLevel.Elevation <= topElevation ).OrderBy( x => x.GenLevel.Elevation ).EnumerateAll() ;
      
      var lengthDirection = 12000d.MillimetersToRevitUnits()*ratio ;
      var transformRotation = Transform.CreateRotationAtPoint( opening.Document.ActiveView.ViewDirection, RotateAngle, arc.Center ) ;
      var bodyDirections = new List<Curve>
      {
        Line.CreateBound( Transform.CreateTranslation( XYZ.BasisX * scaleRadius ).OfPoint( arc.Center ), Transform.CreateTranslation( XYZ.BasisX * lengthDirection * 0.5 ).OfPoint( arc.Center ) ).CreateTransformed( transformRotation ), 
        Line.CreateBound( Transform.CreateTranslation( -XYZ.BasisX * scaleRadius ).OfPoint( arc.Center ), Transform.CreateTranslation( -XYZ.BasisX * lengthDirection * 0.5 ).OfPoint( arc.Center ) ).CreateTransformed( transformRotation )
      } ;

      var transformTranslation = Transform.CreateTranslation( XYZ.BasisZ * ( viewPlan.GenLevel.Elevation - arc.Center.Z ) ) ;

      IEnumerable<Curve> curvesBody ;
      if ( viewPlans.FindIndex( x => x.Id == viewPlan.Id ) == 0 ) {
        CreateSymbol( viewPlan, bodyDirections[ 0 ].GetEndPoint( 1 ), RotateAngle - Math.PI * 0.5, ratio, styleForSymbol )
          .ForEach(x => detailUniqueIds.Add(x.UniqueId));
        curvesBody = GeometryHelper.GetCurvesAfterIntersection( viewPlan, new List<Curve> { bodyDirections[ 0 ].CreateTransformed( transformTranslation ) }, null, oldCableTrays ) ;
      }
      else if ( viewPlans.FindIndex( x => x.Id == viewPlan.Id ) == viewPlans.Count - 1 ) {
        CreateSymbol( viewPlan, bodyDirections[ 1 ].GetEndPoint( 1 ), Math.PI * 0.5 + RotateAngle, ratio, styleForSymbol )
          .ForEach(x => detailUniqueIds.Add(x.UniqueId));
        curvesBody = GeometryHelper.GetCurvesAfterIntersection( viewPlan, new List<Curve> { bodyDirections[ 1 ].CreateTransformed( transformTranslation ) }, null, oldCableTrays ) ;
      }
      else {
        CreateSymbol( viewPlan, bodyDirections[ 0 ].GetEndPoint( 1 ), RotateAngle - Math.PI * 0.5, ratio, styleForSymbol )
          .ForEach(x => detailUniqueIds.Add(x.UniqueId));
        CreateSymbol( viewPlan, bodyDirections[ 1 ].GetEndPoint( 1 ), Math.PI * 0.5 + RotateAngle, ratio, styleForSymbol )
          .ForEach(x => detailUniqueIds.Add(x.UniqueId));
        curvesBody = GeometryHelper.GetCurvesAfterIntersection( viewPlan, bodyDirections.Select( x => x.CreateTransformed( transformTranslation ) ).ToList(), null, oldCableTrays ) ;
      }

      SequenceExtensions.ForEach( curvesBody.Select( x => CreateDetailLine( viewPlan, styleForBodyDirection, x ) ), x => detailUniqueIds.Add(x.UniqueId)) ;

      var circle = Arc.Create( new XYZ( arc.Center.X, arc.Center.Y, viewPlan.GenLevel.Elevation ), scaleRadius, 0, 2 * Math.PI, XYZ.BasisX, XYZ.BasisY ) ;
      detailUniqueIds.Add( CreateDetailLine( viewPlan, styleForOuterShape, circle ).UniqueId ) ;

      if ( string.IsNullOrEmpty( cableTrayUniqueId ) ) return detailUniqueIds ;
      var cableTrayElement = opening.Document.GetElement( cableTrayUniqueId ) ;
      if ( cableTrayElement == null ) return detailUniqueIds ;
      cableTrayElement.ParametersMap.get_Item( "トレイ幅" ).Set( cabTrayWidth ) ;
      cableTrayElement.ParametersMap.get_Item( "ラックの倍率" ).Set( DefaultCableTrayScale * ratio / offSetWidth ) ;

      return detailUniqueIds ;
    }
    
    private static Element CreateCableTraySymbol( Document document, double originX, double originY, double length, Level level )
    {
      var routingSymbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.ShaftHSymbol ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      var position = new XYZ( originX, originY, 0 ) ;
      var cableTrayInstance = routingSymbol.Instantiate( position, level, StructuralType.NonStructural ) ;
      ElementTransformUtils.RotateElement( document, cableTrayInstance.Id, Line.CreateBound( position, position + XYZ.BasisZ ), Math.PI / 2 ) ;
      position = ( cableTrayInstance.Location as LocationPoint )!.Point ;
      ElementTransformUtils.RotateElement( document, cableTrayInstance.Id, Line.CreateBound( position, position + XYZ.BasisX ), Math.PI / 2 ) ;
      cableTrayInstance.ParametersMap.get_Item( "トレイ長さ" ).Set( length ) ;
      return cableTrayInstance ;
    }

    private static List<DetailCurve> CreateSymbol(View viewPlan, XYZ point, double angle, double ratio, Element lineStyle)
    {
      var transform = Transform.CreateRotationAtPoint( XYZ.BasisZ, angle, point ) ;
      var lineOne = Line.CreateBound( point, Transform.CreateTranslation( XYZ.BasisY * 1500d.MillimetersToRevitUnits() * ratio ).OfPoint( point ) ) ;
      var lineTwo = Line.CreateBound( lineOne.GetEndPoint(1), Transform.CreateTranslation( XYZ.BasisX.Negate() * Math.Tan( 5 * Math.PI / 180 ) * lineOne.Length ).OfPoint( point ) ) ;
      var lineThree = Line.CreateBound( lineTwo.GetEndPoint( 1 ), Transform.CreateTranslation( Transform.CreateRotation( XYZ.BasisZ, 30 * Math.PI / 180 ).OfVector( XYZ.BasisX ) * 500d.MillimetersToRevitUnits() * ratio ).OfPoint( lineTwo.GetEndPoint( 1 ) ) ) ;

      var curves = new List<Curve> { lineOne.CreateTransformed(transform), lineTwo.CreateTransformed(transform), lineThree.CreateTransformed(transform) } ;
      var detailCurves = new List<DetailCurve>() ;
      foreach ( var curve in curves ) {
        var detailCurve = CreateDetailLine( viewPlan, lineStyle, curve ) ;
        detailCurves.Add( detailCurve ) ;
      }

      return detailCurves ;
    }

    private static DetailCurve CreateDetailLine( View viewPlan, Element lineStyle, Curve curve )
    {
      var detailLineOne = viewPlan.Document.Create.NewDetailCurve( viewPlan, curve ) ;
      detailLineOne.LineStyle = lineStyle ;
      return detailLineOne ;
    }

    private static Category GetLineStyle( Document document, string subCategoryName, Color color, int lineWeight )
    {
      var categories = document.Settings.Categories ;
      var category = document.Settings.Categories.get_Item( BuiltInCategory.OST_Lines ) ;
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

    public static void DeleteAllShaftOpening(Document document )
    {
      var shaftOpeningStore = document.GetShaftOpeningStorable() ;
      if ( ! shaftOpeningStore.ShaftOpeningModels.Any() )
        return ;

      foreach ( var shaftOpeningModel in shaftOpeningStore.ShaftOpeningModels ) {
        if ( document.GetElement( shaftOpeningModel.ShaftOpeningUniqueId ) is { } opening )
          document.Delete( opening.Id ) ;

        var detailCurves = shaftOpeningModel.DetailUniqueIds.Select( document.GetElement ).OfType<DetailCurve>().EnumerateAll() ;
        if(!detailCurves.Any())
          continue;

        document.Delete( detailCurves.Select( x => x.Id ).ToList() ) ;
      }

      if ( null != shaftOpeningStore.OwnerElement )
        document.Delete( shaftOpeningStore.OwnerElement.Id ) ;
    }
  }
}