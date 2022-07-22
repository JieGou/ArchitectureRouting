using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using RibbonButton = Autodesk.Windows.RibbonButton ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  public static class ShowConduitsIn3DUtil
  {
    private const string HiddenValue = "Hidden" ;
    public const string DummyName = "Dummy" ;

    public static void UpdateIsEnableButton( Document document, bool isEnable )
    {
      var targetTabName = "Electrical.App.Routing.TabName".GetAppStringByKey() ;
      var selectionTab = UIHelper.GetRibbonTabFromName( targetTabName ) ;
      if ( selectionTab == null ) return ;

      using var transaction = new Transaction( document, "Enable buttons" ) ;
      transaction.Start() ;
      foreach ( var panel in selectionTab.Panels ) {
        if ( panel.Source.Title == "Electrical.App.Panels.Routing.Confirmation".GetAppStringByKeyOrDefault( "Confirmation" ) ) {
          foreach ( var item in panel.Source.Items ) {
            if ( ! ( item is RibbonButton ribbonButton && ribbonButton.Text == "Electrical.App.Commands.Routing.CreateDummyConduitsIn3DViewCommand".GetDocumentStringByKeyOrDefault( document, "Show\nConduits in 3D" ) ) ) {
              item.IsEnabled = isEnable ;
            }
          }
        }
        else {
          panel.IsEnabled = isEnable ;
        }
      }

      transaction.Commit() ;
    }

    public static View? Create3DView( Document document, ViewPlan activeView )
    {
      var levelId = activeView.GenLevel.Id ;
      var levelName = activeView.Name ;
      var levels = new List<(ElementId Id, string Name)> { ( levelId, levelName ) } ;
      var views = document.Create3DView( levels ) ;
      return views.Any() ? views.First() : null ;
    }

    public static void RemoveAndHideUnusedConduits( Document document, List<ElementId> removedConduitIds, Dictionary<Element, string> newConduits, List<ElementId> conduitsHideIn3DView )
    {
      using var removedTransaction = new Transaction( document, "Remove and hide unused conduits" ) ;
      removedTransaction.Start() ;

      if ( removedConduitIds.Any() ) {
        removedConduitIds = removedConduitIds.Distinct().ToList() ;
        document.Delete( removedConduitIds ) ;
      }

      HideConduitsIn2DView( document, newConduits ) ;
      HideConduitsIn3DView( document, conduitsHideIn3DView ) ;

      removedTransaction.Commit() ;
    }

    private static void HideConduitsIn2DView( Document document, Dictionary<Element, string> newConduits )
    {
      List<ViewPlan> views = new( new FilteredElementCollector( document ).OfClass( typeof( ViewPlan ) ).Cast<ViewPlan>().Where( v => v.CanBePrinted && ViewType.FloorPlan == v.ViewType ) ) ;
      var conduitsGroupByLevel = newConduits.GroupBy( c => c.Key.GetLevelId() ).Select( g => g.ToList() ) ;
      foreach ( var conduits in conduitsGroupByLevel ) {
        var levelId = conduits.First().Key.GetLevelId() ;
        var viewPlans = views.Where( v => v.GenLevel.Id == levelId ) ;
        var conduitIds = conduits.Select( c => c.Key.Id ).ToList() ;
        foreach ( var viewPlan in viewPlans ) {
          viewPlan.HideElements( conduitIds ) ;
        }
      }
    }

    private static void HideConduitsIn3DView( Document document, ICollection<ElementId> conduitIds )
    {
      document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => conduitIds.Contains( c.Id ) && c.HasParameter( BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS ) ).ForEach( c => c.TrySetProperty( BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS, HiddenValue ) ) ;
      var views = document.GetAllElements<View>().Where( v => v is View3D ) ;
      foreach ( var view in views ) {
        view.HideElements( conduitIds ) ;
      }
    }

    public static Dictionary<string, double> SetOffsetForConduit( Document document, ICollection<Element> conduits, List<ElementId> conduitsHideIn3DView )
    {
      Dictionary<string, double> conduitToleranceDic = new() ;
      Dictionary<string, XYZ> conduitDirectionDic = new() ;
      var allConduitsOfBranchRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( c => c.GetRouteName() == c.GetRepresentativeRouteName() && ( c.GetNearestEndPoints( true ).FirstOrDefault()?.Key.GetTypeName() == PassPointBranchEndPoint.Type || c.GetNearestEndPoints( true ).FirstOrDefault()?.Key.GetTypeName() == PassPointEndPoint.Type ) && c.GetNearestEndPoints( false ).FirstOrDefault()?.Key.GetTypeName() == ConnectorEndPoint.Type ).ToList() ;
      List<Element> conduitOfBranchRoutes = new() ;
      var offset = ( conduits.First() as Conduit )!.Diameter ;
      foreach ( var conduit in conduits ) {
        var routeName = conduit.GetRouteName()! ;
        var routeNameArray = routeName.Split( '_' ) ;
        var mainRouteName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
        if ( conduitDirectionDic.ContainsKey( mainRouteName ) ) continue ;
        var conduitOfBranchRoute = allConduitsOfBranchRoute.Where( c =>
        {
          if ( c.GetRouteName() is not { } rName ) return false ;
          var rNameArray = rName.Split( '_' ) ;
          var strRouteName = string.Join( "_", rNameArray.First(), rNameArray.ElementAt( 1 ) ) ;
          return strRouteName == mainRouteName ;
        } ).ToList() ;
        conduitOfBranchRoutes.AddRange( conduitOfBranchRoute ) ;
        conduitsHideIn3DView.AddRange( conduitOfBranchRoute.Select( c => c.Id ) ) ;
        var maxLength = double.MinValue ;
        var direction = new XYZ() ;
        foreach ( var otherConduit in conduitOfBranchRoute ) {
          var conduitLocation = ( otherConduit.Location as LocationCurve ) ! ;
          var conduitLine = ( conduitLocation.Curve as Line ) ! ;
          var conduitDirection = conduitLine.Direction ;
          if ( ( conduitDirection.X is 1 or -1 || conduitDirection.Y is 1 or -1 ) && conduitLine.Length > maxLength ) {
            maxLength = conduitLine.Length ;
            direction = conduitDirection ;
          }
        }

        if ( ! conduitDirectionDic.ContainsKey( mainRouteName ) )
          conduitDirectionDic.Add( mainRouteName, direction ) ;
      }

      conduits.AddRange( conduitOfBranchRoutes ) ;

      if ( ! conduitDirectionDic.Any() ) return conduitToleranceDic ;
      {
        var plusDirections = conduitDirectionDic.Where( c => c.Value.X is 1 || c.Value.Y is 1 ).ToList() ;
        var minusDirections = conduitDirectionDic.Where( c => c.Value.X is -1 || c.Value.Y is -1 ).ToList() ;
        if ( ! plusDirections.Any() || ! minusDirections.Any() ) {
          var count = conduitDirectionDic.Count / 2 ;
          for ( var i = 0 ; i <= count ; i++ ) {
            conduitToleranceDic.Add( conduitDirectionDic.ElementAt( i ).Key, -offset * ( i + 1 ) ) ;
          }

          var number = 1 ;
          for ( var i = count + 1 ; i < conduitDirectionDic.Count ; i++ ) {
            conduitToleranceDic.Add( conduitDirectionDic.ElementAt( i ).Key, offset * number ) ;
            number++ ;
          }
        }
        else {
          var number = 1 ;
          foreach ( var plusDirection in plusDirections ) {
            conduitToleranceDic.Add( plusDirection.Key, -offset * number ) ;
            number++ ;
          }

          number = 1 ;
          foreach ( var minusDirection in minusDirections ) {
            conduitToleranceDic.Add( minusDirection.Key, offset * number ) ;
            number++ ;
          }
        }
      }

      return conduitToleranceDic ;
    }

    public static List<XYZ> GetConduitDirections( ICollection<Element> conduits, string routeName )
    {
      var routeNameArray = routeName.Split( '_' ) ;
      routeName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
      var conduitsOfRoute = conduits.Where( c =>
      {
        if ( c.GetRouteName() is not { } rName ) return false ;
        var rNameArray = rName.Split( '_' ) ;
        var strRouteName = string.Join( "_", rNameArray.First(), rNameArray.ElementAt( 1 ) ) ;
        return strRouteName == routeName ;
      } ).ToList() ;
      var conduitDirection = new List<XYZ>() ;
      foreach ( var conduitLine in from conduit in conduitsOfRoute select ( conduit.Location as LocationCurve ) ! into conduitLocation select ( conduitLocation.Curve as Line ) ! ) {
        var (x, y, z) = conduitLine.Direction ;
        if ( z is 1 or -1 ) continue ;
        if ( x is 1 or -1 && ! conduitDirection.Exists( d => d.X - x == 0 ) ) {
          conduitDirection.Add( new XYZ( x, 0, 0 ) ) ;
        }
        else if ( y is 1 or -1 && ! conduitDirection.Exists( d => d.Y - y == 0 ) ) {
          conduitDirection.Add( new XYZ( 0, y, 0 ) ) ;
        }
      }

      return conduitDirection ;
    }

    public static XYZ GetConduitFittingDirection( ICollection<FamilyInstance> conduitFittings, XYZ conduitOrigin, XYZ conduitDirection, string routeName )
    {
      const double tolerance = 0.01 ;
      var routeNameArray = routeName.Split( '_' ) ;
      routeName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
      conduitFittings = conduitFittings.Where( c => c.GetRouteName() is { } rName && rName.Contains( routeName ) ).ToList() ;
      XYZ direction = new() ;
      foreach ( var conduitFitting in conduitFittings ) {
        var location = ( conduitFitting.Location as LocationPoint ) ! ;
        var (originX, originY, _) = location.Point ;
        if ( ! ( Math.Abs( originX - conduitOrigin.X ) < tolerance ) || ! ( Math.Abs( originY - conduitOrigin.Y ) < tolerance ) ) continue ;
        var handOrientation = conduitFitting.HandOrientation ;
        var facingOrientation = conduitFitting.FacingOrientation ;
        if ( Math.Abs( handOrientation.X - conduitDirection.X ) < tolerance && Math.Abs( handOrientation.Y - conduitDirection.Y ) < tolerance && Math.Abs( handOrientation.Z - conduitDirection.Z ) < tolerance ) {
          direction = facingOrientation ;
        }
        else {
          direction = handOrientation ;
        }

        break ;
      }

      return direction ;
    }

    public static bool CheckConduitOfBranchRoute( Element conduit )
    {
      var fromEndPoint = conduit.GetNearestEndPoints( true ).ToList() ;
      if ( ! fromEndPoint.Any() ) return false ;
      var fromEndPointType = fromEndPoint.First().Key.GetTypeName() ;
      return fromEndPointType == PassPointBranchEndPoint.Type ;
    }

    public static ( List<Element>, XYZ ) GetBranchConduitWithSamePassPointDirection( Document document, string routeName )
    {
      const double tolerance = 0.01 ;
      var branchConduitWithSamePassPointDirection = new List<Element>() ;
      var allConduits = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() is { } rName && rName.Contains( routeName ) && c.GetNearestEndPoints( true ).FirstOrDefault()?.Key.GetTypeName() == PassPointBranchEndPoint.Type ).ToList() ;
      if ( ! allConduits.Any() ) return ( branchConduitWithSamePassPointDirection, new XYZ() ) ;
      var passPointEndPoint = allConduits.First().GetNearestEndPoints( true ).ToList() ;
      if ( ! passPointEndPoint.Any() ) return ( branchConduitWithSamePassPointDirection, new XYZ() ) ;
      var passPointId = passPointEndPoint.First().Key.GetElementUniqueId() ;
      var passPoint = document.GetElement( passPointId ) ;
      var passPointPosition = ( passPoint.Location as LocationPoint )!.Point ;
      var passPointDirection = ( passPoint as FamilyInstance )!.HandOrientation ;
      foreach ( var conduit in allConduits ) {
        if ( conduit is Conduit ) {
          var location = ( conduit.Location as LocationCurve )! ;
          var line = ( location.Curve as Line )! ;
          var origin = line.Origin ;
          var direction = line.Direction ;
          if ( ( passPointDirection.X is 1 or -1 && direction.X is 1 or -1 && Math.Abs( passPointPosition.X - origin.X ) < tolerance ) || ( passPointDirection.Y is 1 or -1 && direction.Y is 1 or -1 && Math.Abs( passPointPosition.Y - origin.Y ) < tolerance ) ) {
            branchConduitWithSamePassPointDirection.Add( conduit ) ;
          }
        }
        else if ( conduit is FamilyInstance conduitFitting ) {
          var origin = ( conduitFitting.Location as LocationPoint )!.Point ;
          var handOrientation = conduitFitting.HandOrientation ;
          var facingOrientation = conduitFitting.FacingOrientation ;
          if ( ( ( passPointDirection.X is 1 or -1 && handOrientation.X is 1 or -1 && Math.Abs( passPointPosition.Y - origin.Y ) < tolerance ) || ( passPointDirection.Y is 1 or -1 && handOrientation.Y is 1 or -1 && Math.Abs( passPointPosition.X - origin.X ) < tolerance ) || ( passPointDirection.X is 1 or -1 && passPointDirection.X + facingOrientation.X == 0 && Math.Abs( passPointPosition.Y - origin.Y ) < tolerance ) || ( passPointDirection.Y is 1 or -1 && passPointDirection.Y + facingOrientation.Y == 0 && Math.Abs( passPointPosition.X - origin.X ) < tolerance ) ) && facingOrientation.Z is not 1 && facingOrientation.Z is not -1 ) {
            branchConduitWithSamePassPointDirection.Add( conduit ) ;
          }
        }
      }

      return ( branchConduitWithSamePassPointDirection, passPointDirection ) ;
    }

    public static bool RemoveDummyConduits( Document document )
    {
      UpdateIsEnableButton( document, true ) ;
      var allConduitIds = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => ! string.IsNullOrEmpty( c.GetRouteName() ) && c.GetRouteName()!.Contains( DummyName ) ).Select( c => c.Id ).Distinct().ToList() ;

      if ( ! allConduitIds.Any() ) return true ;

      using var transaction = new Transaction( document, "Remove dummy conduits" ) ;
      transaction.Start() ;
      document.Delete( allConduitIds ) ;

      var hiddenConduits = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.HasParameter( BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS ) && c.GetParameter( BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS )?.AsString() == HiddenValue ).ToList() ;
      hiddenConduits.ForEach( c => c.GetParameter( BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS )?.ClearProperty() ) ;
      var hiddenConduitIds = hiddenConduits.Select( c => c.Id ).ToList() ;
      var views = document.GetAllElements<View>().Where( v => v is View3D ) ;
      foreach ( var view in views ) {
        view.UnhideElements( hiddenConduitIds ) ;
      }

      transaction.Commit() ;
      return false ;
    }

    public static ( ConduitInfo?, ConduitInfo? ) GetFromAndToConduitsOfCConduitFitting( Dictionary<Element, string> newConduits, string routeName, XYZ handOrientation, XYZ facingOrientation, XYZ origin )
    {
      var conduits = newConduits.Where( c => c.Key is Conduit && c.Value == routeName ) ;
      ConduitInfo? fromConduitInfo = null ;
      ConduitInfo? toConduitInfo = null ;
      var minFromDistance = double.MaxValue ;
      var minToDistance = double.MaxValue ;
      foreach ( var conduit in conduits ) {
        var levelId = conduit.Key.GetLevelId() ;
        var location = ( conduit.Key.Location as LocationCurve )! ;
        var line = ( location.Curve as Line )! ;
        var (directionX, directionY, directionZ) = line.Direction ;
        var fromPoint = line.GetEndPoint( 0 ) ;
        var toPoint = line.GetEndPoint( 1 ) ;
        var fromDistance = fromPoint.DistanceTo( origin ) ;
        var toDistance = toPoint.DistanceTo( origin ) ;
        if ( ( handOrientation.X is 1 or -1 && Math.Abs( directionX - handOrientation.X ) == 0 ) || ( handOrientation.Y is 1 or -1 && Math.Abs( directionY - handOrientation.Y ) == 0 ) || ( handOrientation.Z is 1 or -1 && Math.Abs( directionZ - handOrientation.Z ) == 0 ) ) {
          if ( toDistance < minToDistance ) {
            minToDistance = toDistance ;
            fromConduitInfo = new ConduitInfo( conduit.Key, fromPoint, toPoint, line.Direction, levelId, false ) ;
          }
        }

        if ( ( facingOrientation.X is 1 or -1 && Math.Abs( directionX + facingOrientation.X ) == 0 ) || ( facingOrientation.Y is 1 or -1 && Math.Abs( directionY + facingOrientation.Y ) == 0 ) || ( facingOrientation.Z is 1 or -1 && Math.Abs( directionZ + facingOrientation.Z ) == 0 ) ) {
          if ( toDistance < minToDistance ) {
            minToDistance = toDistance ;
            fromConduitInfo = new ConduitInfo( conduit.Key, fromPoint, toPoint, line.Direction, levelId, true ) ;
          }
        }

        if ( ( facingOrientation.X is 1 or -1 && Math.Abs( directionX - facingOrientation.X ) == 0 ) || ( facingOrientation.Y is 1 or -1 && Math.Abs( directionY - facingOrientation.Y ) == 0 ) || ( facingOrientation.Z is 1 or -1 && Math.Abs( directionZ - facingOrientation.Z ) == 0 ) ) {
          if ( ! ( fromDistance < minFromDistance ) ) continue ;
          minFromDistance = fromDistance ;
          toConduitInfo = new ConduitInfo( conduit.Key, fromPoint, toPoint, line.Direction, levelId, false ) ;
        }

        if ( ( handOrientation.X is 1 or -1 && Math.Abs( directionX + handOrientation.X ) == 0 ) || ( handOrientation.Y is 1 or -1 && Math.Abs( directionY + handOrientation.Y ) == 0 ) || ( handOrientation.Z is 1 or -1 && Math.Abs( directionZ + handOrientation.Z ) == 0 ) ) {
          if ( ! ( fromDistance < minFromDistance ) ) continue ;
          minFromDistance = fromDistance ;
          toConduitInfo = new ConduitInfo( conduit.Key, fromPoint, toPoint, line.Direction, levelId, true ) ;
        }
      }

      return ( fromConduitInfo, toConduitInfo ) ;
    }

    public class ConduitInfo
    {
      public Element Conduit { get ; }
      public XYZ StartPoint { get ; }
      public XYZ EndPoint { get ; }
      public XYZ Direction { get ; }
      public ElementId LevelId { get ; }
      public bool IsOppositeDirection { get ; }

      public ConduitInfo( Element conduit, XYZ startPoint, XYZ endPoint, XYZ direction, ElementId levelId, bool isOppositeDirection )
      {
        Conduit = conduit ;
        StartPoint = startPoint ;
        EndPoint = endPoint ;
        Direction = direction ;
        LevelId = levelId ;
        IsOppositeDirection = isOppositeDirection ;
      }
    }

    public class RouteInfo
    {
      public string RouteName { get ; }
      public MEPCurveType CurveType { get ; }
      public double Offset { get ; }
      public List<XYZ> Directions { get ; }
      public List<Element> ConduitsOfBranch { get ; }
      public XYZ PassPointDirection { get ; }

      public RouteInfo( string routeName, MEPCurveType curveType, double offset, List<XYZ> directions, List<Element> conduitsOfBranch, XYZ passPointDirection )
      {
        RouteName = routeName ;
        CurveType = curveType ;
        Offset = offset ;
        Directions = directions ;
        ConduitsOfBranch = conduitsOfBranch ;
        PassPointDirection = passPointDirection ;
      }
    }
  }
}