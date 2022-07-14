using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Architecture.Routing.Utils ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using MoreLinq ;
using MoreLinq.Extensions ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public static class ChangeWireTypeCommand
  {
    public static string SubcategoryName => "LeakageZone" ;
    
    public static readonly Dictionary<string, string> WireSymbolOptions = new()
    {
      { "漏水帯（布）", "LeakageZoneCloth" }, 
      { "漏水帯（発色）", "LeakageZoneColoring" }, 
      { "漏水帯（塩ビ）", "LeakageZonePvc" }
    } ;

    private static void ChangeLocationType( Document document, View view, List<Element> elements, string wireType, bool isLeakRoute )
    {
      using var transactionGroup = new TransactionGroup( document ) ;
      transactionGroup.Start( "Change Type" ) ;

      var (lines, curves) = GetLocationConduits( document, view, elements ) ;

      var familySymbol = document.GetAllTypes<FamilySymbol>( x => x.Name == wireType ).FirstOrDefault() ;
      if ( null == familySymbol )
        return ;

      using var transaction = new Transaction( document ) ;
      transaction.Start( "Change Location Type" ) ;

      if ( ! familySymbol.IsActive )
        familySymbol.Activate() ;

      var dataStorage = document.FindOrCreateDataStorageForUser() ;
      var conduitAndDetailCurveModel = dataStorage.GetData<ConduitAndDetailCurveModel>() ?? new ConduitAndDetailCurveModel() ;
      var color = new Color( 255, 215, 0 ) ;
      var lineStyle = GetLineStyle( document, color ) ;
      OverrideGraphicSettings ogs = new() ;
      ogs.SetProjectionLineColor( color ) ;
      ForEachExtension.ForEach( curves, x =>
      {
        var detailCurve = document.Create.NewDetailCurve( view, x.Key ) ;
        detailCurve.LineStyle = lineStyle.GetGraphicsStyle( GraphicsStyleType.Projection ) ;
        if ( isLeakRoute ) view.SetElementOverrides( detailCurve.Id, ogs ) ;
        conduitAndDetailCurveModel.ConduitAndDetailCurveItemModels.Add( new ConduitAndDetailCurveItemModel
        {
          ConduitId = x.Value,
          DetailCurveId = detailCurve.UniqueId,
          WireType = wireType,
          IsLeakRoute = isLeakRoute
        }) ;
      } ) ;
      ForEachExtension.ForEach( lines, x =>
      {
        var line = document.Create.NewFamilyInstance( x.Key, familySymbol, view ) ;
        if ( isLeakRoute ) view.SetElementOverrides( line.Id, ogs ) ;
        conduitAndDetailCurveModel.ConduitAndDetailCurveItemModels.Add( new ConduitAndDetailCurveItemModel
        {
          ConduitId = x.Value,
          DetailCurveId = line.UniqueId,
          WireType = wireType,
          IsLeakRoute = isLeakRoute
        } ) ;
      } ) ;
      
      dataStorage.SetData(conduitAndDetailCurveModel) ;

      transaction.Commit() ;
      
      using var trans = new Transaction( document ) ;
      trans.Start( "Hidden Element" ) ;
      if ( elements.Any() ) {
        try {
          view.HideElements( elements.Select( x => x.Id ).ToList() ) ;
        }
        catch {
          //
        }
      }

      var dropCategory = Category.GetCategory( document, BuiltInCategory.OST_ConduitDrop ) ;
      if ( null != dropCategory ) {
        try {
          view.SetCategoryHidden( dropCategory.Id, true ) ;
        }
        catch {
          //
        }
      }
      
      trans.Commit() ;

      RefreshView( document, view ) ;

      transactionGroup.Assimilate() ;
    }

    private static Category GetLineStyle( Document doc, Color color )
    {
      var categories = doc.Settings.Categories ;
      Category category = doc.Settings.Categories.get_Item( BuiltInCategory.OST_Lines ) ;
      Category subCategory ;
      if ( ! category.SubCategories.Contains( SubcategoryName ) ) {
        subCategory = categories.NewSubcategory( category, SubcategoryName ) ;
        subCategory.LineColor = color ;
      }
      else {
        subCategory = category.SubCategories.get_Item( SubcategoryName ) ;
      }

      return subCategory ;
    }

    public static (Dictionary<Line, string> lineConduits, Dictionary<Curve, string> curveHorizontal) GetLocationConduits( Document document, View view, List<Element> elements )
    {
      var conduits = elements.OfType<Conduit>().ToList() ;
      var curveConduits = GeometryHelper.GetCurveFromElements( view, conduits ) ;

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

      var lines = ConnectLines( view, lineConduits ) ;
      var curves = GetCurveHorizontalFittings( view, fittingHorizontals ) ;
      return ( lines, curves ) ;
    }

    private static Func<FamilyInstance, XYZ> GetCenterPoint =>
      familyInstance =>
      {
        var connectors = familyInstance.MEPModel.ConnectorManager.Connectors.OfType<Connector>().ToList() ;
        return 0.5 * ( connectors[ 0 ].Origin + connectors[ 1 ].Origin ) ;
      } ;

    private static Dictionary<Curve, string> GetCurveHorizontalFittings( View view, IEnumerable<FamilyInstance> fittingHorizontals )
    {
      var comparer = new XyzComparer() ;
      fittingHorizontals = fittingHorizontals.Where( x => x.MEPModel.ConnectorManager.Connectors.Size == 2 ) ;
      fittingHorizontals = DistinctByExtension.DistinctBy( fittingHorizontals, x => GetCenterPoint( x ), comparer ) ;
      return GeometryHelper.GetCurveFromElements( view, fittingHorizontals ) ;
    }

    private static Dictionary<Line, string> GetLineVerticalFittings( IEnumerable<FamilyInstance> fittingVerticals )
    {
      var comparer = new XyzComparer() ;
      var connectorsOfConduit = DistinctByExtension.DistinctBy( fittingVerticals, x => ( (LocationPoint) x.Location ).Point, comparer ).ToDictionary( g => g.UniqueId, g => g.MEPModel.ConnectorManager.Connectors.OfType<Connector>().ToList() ) ;

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

    private static Dictionary<Line, string> ConnectLines( View view, Dictionary<Line, string> linesDic )
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
      var elevation = view.GenLevel.Elevation ;
      foreach ( var lineConnect in lineConnects ) {
        var firstPoint = lineConnect.Key.GetEndPoint( 0 ) ;
        var secondPoint = lineConnect.Key.GetEndPoint( 1 ) ;
        lineOnPlanes.Add( Line.CreateBound( new XYZ( firstPoint.X, firstPoint.Y, elevation ), new XYZ( secondPoint.X, secondPoint.Y, elevation ) ), lineConnect.Value ) ;
      }

      return lineOnPlanes ;
    }

    public static void RefreshView( Document document, View view )
    {
      using var transaction = new Transaction( document ) ;
      transaction.Start( "Enable Reveal Hidden" ) ;
      document.ActiveView.EnableRevealHiddenMode() ;
      transaction.Commit() ;

      transaction.Start( "Disable Reveal Hidden" ) ;
      document.ActiveView.DisableTemporaryViewMode( TemporaryViewMode.RevealHiddenElements ) ;
      transaction.Commit() ;
    }

    public static ( string, bool ) RemoveDetailLines( Document document, HashSet<string> conduitIds )
    {
      var dataStorages = document.GetAllData<ConduitAndDetailCurveModel>().Where(x => x.Data.ConduitAndDetailCurveItemModels.Any(y => conduitIds.Any(z => z == y.ConduitId))).ToList() ;
      if ( ! dataStorages.Any() ) 
        return ( string.Empty, false ) ;
      
      var familyInstanceName = dataStorages.First().Data.ConduitAndDetailCurveItemModels.First().WireType ;
      var isLeakRoute = dataStorages.First().Data.ConduitAndDetailCurveItemModels.First().IsLeakRoute ;

      using var transaction = new Transaction( document ) ;
      transaction.Start( "Remove Detail Curves" ) ;
      
      foreach ( var (owner, conduitAndDetailCurveModel) in dataStorages ) {
        try {
          var conduitAndDetailCurveItems = new List<ConduitAndDetailCurveItemModel>() ;
          foreach ( var conduitAndDetailCurveItem in conduitAndDetailCurveModel.ConduitAndDetailCurveItemModels ) {
            if ( conduitIds.Any( x => x == conduitAndDetailCurveItem.ConduitId ) ) {
              document.Delete( conduitAndDetailCurveItem.DetailCurveId ) ;
            }
            else {
              conduitAndDetailCurveItems.Add( conduitAndDetailCurveItem ) ;
            }
          }

          conduitAndDetailCurveModel.ConduitAndDetailCurveItemModels = conduitAndDetailCurveItems ;
          owner.SetData( conduitAndDetailCurveModel ) ;
        }
        catch {
          // Ignore
        }
      }
      
      transaction.Commit() ;
      
      return ( familyInstanceName, isLeakRoute ) ;
    }

    public static void ChangeWireType( Document document, HashSet<string> reReRouteNames, string wireTypeName, bool isLeakRoute )
    {
      var viewPlan = document.ActiveView ;
      using var transaction = new Transaction( document ) ;
      transaction.Start( "Change color and bend radius of conduits." ) ;
      var arentFamilyType = document.GetFamilySymbols( ElectricalRoutingFamilyType.ArentConduitFittingType ).FirstOrDefault() ;
      OverrideGraphicSettings ogs = new() ;
      ogs.SetProjectionLineColor( new Color( 255, 215, 0 ) ) ;
      
      var newConduitsOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => reReRouteNames.Contains( c.GetRouteName() ! ) ).ToList() ;
      
      var conduitOfRoute = newConduitsOfRoute.OfType<Conduit>().FirstOrDefault() ;
      if ( conduitOfRoute != null ) {
        var level = document.GetAllElements<Level>().FirstOrDefault( e => e.Id == conduitOfRoute.ReferenceLevel.Id ) ;
        if ( level != null ) {
          viewPlan = document.GetAllElements<ViewPlan>().FirstOrDefault( v => v.Name == level.Name ) ?? document.ActiveView ;
        }
      }
      
      var allView3d = document.GetAllElements<View>().Where( v => v is View3D ).ToList() ;
      foreach ( var conduit in newConduitsOfRoute ) {
        //Change conduit color to yellow RGB(255,215,0)
        viewPlan.SetElementOverrides( conduit.Id, ogs ) ;
        foreach ( var view in allView3d ) {
          try {
            view.SetElementOverrides( conduit.Id, ogs ) ;
          }
          catch {
            // Todo catch handle
          }
        }
        //Change conduit fitting bend radius = 1 mm
        if ( conduit is not FamilyInstance conduitFitting || arentFamilyType == null ) continue ;
        conduitFitting.Symbol = arentFamilyType ;
      }

      transaction.Commit() ;
      
      ChangeLocationType( document, viewPlan, newConduitsOfRoute, wireTypeName, isLeakRoute ) ;
    }
    
    public static void RemoveDetailLinesByRoutes( Document document, HashSet<string> routeNames )
    {
      var allConduitIds = MoreEnumerable.ToHashSet( document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( e => routeNames.Contains( e.GetRouteName() ! ) ).Select( e => e.UniqueId ) ) ;
      if ( allConduitIds.Any() ) RemoveDetailLines( document, allConduitIds ) ;
    }
  }
}