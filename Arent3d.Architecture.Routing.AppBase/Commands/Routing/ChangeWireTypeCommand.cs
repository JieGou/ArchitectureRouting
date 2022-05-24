using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Extensions ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Utils ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using MoreLinq.Extensions ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public static class ChangeWireTypeCommand
  {
    private const string ConduitIdParameter = "Conduit Id" ;
    public static readonly Dictionary<string, string> WireSymbolOptions = new()
    {
      { "漏水帯（布）", "LeakageZoneCloth" }, 
      { "漏水帯（発色）", "LeakageZoneColoring" }, 
      { "漏水帯（塩ビ）", "LeakageZonePvc" }
    } ;

    private static void ChangeLocationType( Document document, List<Element> elements, string wireType )
    {
      using var transactionGroup = new TransactionGroup( document ) ;
      transactionGroup.Start( "Change Type" ) ;

      var (lines, curves) = GetLocationConduits( document, elements ) ;

      var familySymbol = document.GetAllTypes<FamilySymbol>( x => x.Name == wireType ).FirstOrDefault() ;
      if ( null == familySymbol )
        return ;

      using var transaction = new Transaction( document ) ;
      transaction.Start( "Change Location Type" ) ;

      if ( ! familySymbol.IsActive )
        familySymbol.Activate() ;

      var conduitAndDetailCurveStorable = document.GetConduitAndDetailCurveStorable() ;
      var color = new Color( 255, 215, 0 ) ;
      var lineStyle = GetLineStyle( document, color ) ;
      OverrideGraphicSettings ogs = new() ;
      ogs.SetProjectionLineColor( color ) ;
      ForEachExtension.ForEach( curves, x =>
      {
        var detailCurve = document.Create.NewDetailCurve( document.ActiveView, x.Key ) ;
        detailCurve.LineStyle = lineStyle.GetGraphicsStyle( GraphicsStyleType.Projection ) ;
        conduitAndDetailCurveStorable.ConduitAndDetailCurveData.Add( new ConduitAndDetailCurveModel( x.Value, detailCurve.UniqueId, wireType ) ) ;
      } ) ;
      ForEachExtension.ForEach( lines, x =>
      {
        var line = document.Create.NewFamilyInstance( x.Key, familySymbol, document.ActiveView ) ;
        document.ActiveView.SetElementOverrides( line.Id, ogs ) ;
        if ( line.HasParameter( ConduitIdParameter ) ) line.ParametersMap.get_Item( ConduitIdParameter ).Set( x.Value ) ;
      } ) ;
      
      conduitAndDetailCurveStorable.Save() ;

      transaction.Commit() ;
      
      using var trans = new Transaction( document ) ;
      trans.Start( "Hidden Element" ) ;
      document.ActiveView.HideElements( elements.Select( x => x.Id ).ToList() ) ;

      var dropCategory = Category.GetCategory( document, BuiltInCategory.OST_ConduitDrop ) ;
      if ( null != dropCategory )
        document.ActiveView.SetCategoryHidden( dropCategory.Id, true ) ;

      trans.Commit() ;

      RefreshView( document ) ;

      transactionGroup.Assimilate() ;
    }

    private static Category GetLineStyle( Document doc, Color color )
    {
      var categories = doc.Settings.Categories ;
      var subCategoryName = "JBoxWireLineStyle" ;
      Category category = doc.Settings.Categories.get_Item( BuiltInCategory.OST_GenericAnnotation ) ;
      Category subCategory ;
      if ( ! category.SubCategories.Contains( subCategoryName ) ) {
        subCategory = categories.NewSubcategory( category, subCategoryName ) ;
        subCategory.LineColor = color ;
      }
      else
        subCategory = category.SubCategories.get_Item( subCategoryName ) ;

      return subCategory ;
    }

    public static (Dictionary<Line, string> lineConduits, Dictionary<Curve, string> curveHorizontal) GetLocationConduits( Document document, List<Element> elements )
    {
      var conduits = elements.OfType<Conduit>().ToList() ;
      var curveConduits = GetCurveFromElements( document, conduits ) ;

      var conduitFittings = elements.OfType<FamilyInstance>().ToList() ;
      var fittingHorizontals = conduitFittings.Where( x => Math.Abs( x.GetTransform().OfVector( XYZ.BasisZ ).Z - 1 ) < GeometryUtil.Tolerance ).ToList() ;
      var fittingVerticals = conduitFittings.Where( x => Math.Abs( x.GetTransform().OfVector( XYZ.BasisZ ).Z ) < GeometryUtil.Tolerance ).ToList() ;

      var lineConduits = curveConduits.Where( c => c.Key is Line ).ToDictionary( d => ( d.Key as Line ) !, d => d.Value ) ;
      var lineVerticalFittings = GetLineVerticalFittings( fittingVerticals ) ;

      foreach ( var lineVerticalFitting in lineVerticalFittings ) {
        if ( ! lineConduits.ContainsKey( lineVerticalFitting.Key ) ) {
          lineConduits.Add( lineVerticalFitting.Key, lineVerticalFitting.Value ) ;
        }
      }

      var lines = ConnectLines( document, lineConduits ) ;
      var curves = GetCurveHorizontalFittings( document, fittingHorizontals ) ;
      return ( lines, curves ) ;
    }

    private static Func<FamilyInstance, XYZ> GetCenterPoint =>
      familyInsatance =>
      {
        var connectors = familyInsatance.MEPModel.ConnectorManager.Connectors.OfType<Connector>().ToList() ;
        return 0.5 * ( connectors[ 0 ].Origin + connectors[ 1 ].Origin ) ;
      } ;

    private static Dictionary<Curve, string> GetCurveHorizontalFittings( Document document, IEnumerable<FamilyInstance> fittingHorizontals )
    {
      var comparer = new XyzComparer() ;
      fittingHorizontals = fittingHorizontals.Where( x => x.MEPModel.ConnectorManager.Connectors.Size == 2 ) ;
      fittingHorizontals = fittingHorizontals.DistinctBy( x => GetCenterPoint( x ), comparer ) ;
      return GetCurveFromElements( document, fittingHorizontals ) ;
    }

    private static Dictionary<Line, string> GetLineVerticalFittings( IEnumerable<FamilyInstance> fittingVerticals )
    {
      var comparer = new XyzComparer() ;
      var connectorsOfConduit = fittingVerticals.DistinctBy( x => ( (LocationPoint) x.Location ).Point, comparer ).ToDictionary( g => g.UniqueId, g => g.MEPModel.ConnectorManager.Connectors.OfType<Connector>().ToList() ) ;

      var lines = new Dictionary<Line, string>() ;
      foreach ( var connectors in connectorsOfConduit ) {
        var connector = connectors.Value ;
        if ( connector.Count != 2 )
          continue ;

        var maxZ = connector[ 0 ].Origin.Z > connector[ 1 ].Origin.Z ? connector[ 0 ].Origin.Z : connector[ 1 ].Origin.Z ;
        lines.Add( Line.CreateBound( new XYZ( connector[ 0 ].Origin.X, connector[ 0 ].Origin.Y, maxZ ), new XYZ( connector[ 1 ].Origin.X, connector[ 1 ].Origin.Y, maxZ ) ), connectors.Key ) ;
      }

      return lines ;
    }

    private static Dictionary<Line, string> ConnectLines( Document document, Dictionary<Line, string> linesDic )
    {
      var lineConnects = new Dictionary<Line, string>() ;
      var lines = linesDic.Keys.ToList() ;
      while ( lines.Any() ) {
        var line = lines[ 0 ] ;
        var conduitId = linesDic[ line ] ?? string.Empty ;
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

        lineConnects.Add( line, conduitId ) ;
      }

      var lineOnPlanes = new Dictionary<Line, string>() ;
      var elevation = document.ActiveView.GenLevel.Elevation ;
      foreach ( var lineConnect in lineConnects ) {
        var firstPoint = lineConnect.Key.GetEndPoint( 0 ) ;
        var secondPoint = lineConnect.Key.GetEndPoint( 1 ) ;
        lineOnPlanes.Add( Line.CreateBound( new XYZ( firstPoint.X, firstPoint.Y, elevation ), new XYZ( secondPoint.X, secondPoint.Y, elevation ) ), lineConnect.Value ) ;
      }

      return lineOnPlanes ;
    }

    private static Dictionary<Curve, string> GetCurveFromElements( Document document, IEnumerable<Element> elements )
    {
      using var transaction = new Transaction( document ) ;
      transaction.Start( "Get Geometry" ) ;

      var detailLevel = document.ActiveView.DetailLevel ;
      document.ActiveView.DetailLevel = ViewDetailLevel.Coarse ;

      var curves = new Dictionary<Curve, string>() ;
      var options = new Options { View = document.ActiveView } ;

      foreach ( var element in elements ) {
        if ( element.get_Geometry( options ) is { } geometryElement )
          RecursiveCurves( geometryElement, element.UniqueId, ref curves ) ;
      }

      document.ActiveView.DetailLevel = detailLevel ;
      transaction.Commit() ;

      return curves ;
    }

    private static void RecursiveCurves( GeometryElement geometryElement, string elementId, ref Dictionary<Curve, string> curves )
    {
      foreach ( var geometry in geometryElement ) {
        switch ( geometry ) {
          case GeometryInstance geometryInstance :
          {
            if ( geometryInstance.GetInstanceGeometry() is { } subGeometryElement )
              RecursiveCurves( subGeometryElement, elementId, ref curves ) ;
            break ;
          }
          case Curve curve :
            curves.Add( curve.Clone(), elementId ) ;
            break ;
        }
      }
    }

    public static void RefreshView( Document document )
    {
      if ( document.ActiveView.DetailLevel != ViewDetailLevel.Fine )
        return ;

      using var transaction = new Transaction( document ) ;

      transaction.Start( "Detail Level Coarse" ) ;
      document.ActiveView.DetailLevel = ViewDetailLevel.Coarse ;
      transaction.Commit() ;

      transaction.Start( "Detail Level Fine" ) ;
      document.ActiveView.DetailLevel = ViewDetailLevel.Fine ;
      transaction.Commit() ;
    }

    public static string RemoveDetailLines( Document document, HashSet<string> conduitIds )
    {
      var conduitAndDetailCurveStorable = document.GetConduitAndDetailCurveStorable() ;
      var conduitAndDetailCurves = conduitAndDetailCurveStorable.ConduitAndDetailCurveData.Where( c => conduitIds.Contains( c.ConduitId ) ).ToList() ;
      var oldLines = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_DetailComponents ).Where( e => e is FamilyInstance f && f.HasParameter( ConduitIdParameter ) && conduitIds.Contains( f.ParametersMap.get_Item( ConduitIdParameter ).AsString() ) ).ToList() ;
      if ( ! conduitAndDetailCurves.Any() && ! oldLines.Any() ) return string.Empty ;
      var familyInstanceName = conduitAndDetailCurves.First().WireType ;
      try {
        using var transaction = new Transaction( document ) ;
        transaction.Start( "Remove Detail Lines" ) ;
        foreach ( var conduitAndDetailCurve in conduitAndDetailCurves ) {
          var element = document.GetElement( conduitAndDetailCurve.DetailCurveId ) ;
          if ( element != null ) document.Delete( element.UniqueId ) ;
        }
        
        foreach ( var oldLine in oldLines ) {
          document.Delete( oldLine.UniqueId ) ;
        }
        
        conduitAndDetailCurveStorable.ConduitAndDetailCurveData = conduitAndDetailCurveStorable.ConduitAndDetailCurveData.Where( c => ! conduitIds.Contains( c.ConduitId ) ).ToList() ;
        conduitAndDetailCurveStorable.Save() ;
        transaction.Commit() ;
        return familyInstanceName ;
      }
      catch {
        return familyInstanceName ;
      }
    }

    public static void ChangeWireType( Document document, HashSet<string> reReRouteNames, string wireTypeName )
    {
      using var transaction = new Transaction( document ) ;
      transaction.Start( "Change color and bend radius of conduits." ) ;
      var arentFamilyType = document.GetFamilySymbols( ElectricalRoutingFamilyType.ArentConduitFittingType ).FirstOrDefault() ;
      OverrideGraphicSettings ogs = new() ;
      ogs.SetProjectionLineColor( new Color( 255, 215, 0 ) ) ;
      var newConduitsOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => reReRouteNames.Contains( c.GetRouteName() ! ) ).ToList() ;
      foreach ( var conduit in newConduitsOfRoute ) {
        //Change conduit color to yellow RGB(255,215,0)
        document.ActiveView.SetElementOverrides( conduit.Id, ogs ) ;
        //Change conduit fitting bend radius = 1 mm
        if ( conduit is not FamilyInstance conduitFitting || arentFamilyType == null ) continue ;
        conduitFitting.Symbol = arentFamilyType ;
      }
      transaction.Commit() ;
      
      ChangeLocationType( document, newConduitsOfRoute, wireTypeName ) ;
    }
  }
}