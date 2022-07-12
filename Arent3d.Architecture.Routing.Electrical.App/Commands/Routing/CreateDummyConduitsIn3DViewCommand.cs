using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.CreateDummyConduitsIn3DViewCommand", DefaultString = "Create Dummy Conduits\nIn 3D View" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class CreateDummyConduitsIn3DViewCommand : IExternalCommand
  {
    private const string DummyName = "Dummy";
    
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var app = commandData.Application ;
      var uiDocument = app.ActiveUIDocument ;
      var document = uiDocument.Document ;

      try {
        using var transaction = new Transaction( document, ( "TransactionName.Commands.Routing.CreateDummyConduitsIn3DViewCommand".GetAppStringByKeyOrDefault( "Create Dummy Conduits In 3D View" ) ) ) ;
        transaction.Start() ;
        RemoveDummyConduits( document ) ;
        var arentConduitTypeName = "Routing.Revit.DummyConduit.ConduitTypeName".GetDocumentStringByKeyOrDefault( document, "Arent電線" ) ;
        FilteredElementCollector collector = new( document ) ;
        collector.OfClass( typeof( ConduitType ) ) ;
        var arentConduitType = document.GetAllElements<MEPCurveType>().FirstOrDefault( c => c.Name == arentConduitTypeName ) ;

        var allConduits = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( c => ! string.IsNullOrEmpty( c.GetRouteName() ) && c.GetRouteName() != c.GetRepresentativeRouteName() ).ToList() ;

        Dictionary<Element, string> newConduits = new() ;

        var routeDic = GenerateConduits( document, arentConduitType!, allConduits, newConduits ) ;

        var removedConduitIds = GenerateConduitFittings( uiDocument, arentConduitType!, routeDic, newConduits ) ;
        transaction.Commit() ;

        transaction.Start() ;
        if ( removedConduitIds.Any() ) {
          foreach ( var conduitId in removedConduitIds ) {
            try {
              document.Delete( conduitId ) ;
            }
            catch {
              //
            }
          }
        }
        
        HideConduitsIn2DView( document, newConduits ) ;
        transaction.Commit() ;

        return Result.Succeeded ;
      }
      catch ( Exception exception ) {
        message = exception.Message ;
        return Result.Failed ;
      }
    }

    private List<RouteInfo> GenerateConduits( Document document, MEPCurveType arentConduitType, ICollection<Element> allConduits, Dictionary<Element, string> newConduits )
    {
      var defaultTolerance = ( 30.0 ).MillimetersToRevitUnits() ;
      var routeDic = new List<RouteInfo>() ;

      var allRouteName = allConduits.Select( c => c.GetRouteName() ! ).ToHashSet() ;
      var allConduitFittings = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_ConduitFitting ).Where( c => allRouteName.Contains( c.GetRouteName() ! ) ).ToList() ;

      var conduitGroupByRepresentativeRouteName = allConduits.GroupBy( c => c.GetRepresentativeRouteName() ).Select( g => g.ToList() ).ToList() ;
      foreach ( var conduits in conduitGroupByRepresentativeRouteName ) {
        var toleranceDic = new List<RouteInfo>() ;
        var count = 0 ;
        var sortConduits = conduits.OrderByDescending( c => c.GetRouteName() ).ToList() ;
        var conduitDirections = GetConduitDirections( allConduits, sortConduits.First().GetRouteName()! ) ;
        foreach ( var conduit in sortConduits ) {
          var routeName = conduit.GetRouteName() ;
          var levelId = conduit.GetLevelId() ;
          var conduitLocation = ( conduit.Location as LocationCurve ) ! ;
          var conduitLine = ( conduitLocation.Curve as Line ) ! ;
          var startPoint = conduitLine.GetEndPoint( 0 ) ;
          var endPoint = conduitLine.GetEndPoint( 1 ) ;
          var direction = conduitLine.Direction ;
          double tolerance ;
          if ( ! string.IsNullOrEmpty( routeName ) && ! toleranceDic.Exists( r => r.RouteName == routeName! ) ) {
            count++ ;
            tolerance = defaultTolerance * count ;
            var routeInfo = new RouteInfo( routeName!, defaultTolerance * count, conduitDirections ) ;
            toleranceDic.Add( routeInfo ) ;
            routeDic.Add( routeInfo ) ;
          }
          else {
            tolerance = toleranceDic.First( r => r.RouteName == routeName! ).Tolerance ;
          }

          if ( direction.X is 1 or -1 ) {
            var otherDirection = conduitDirections.FirstOrDefault( d => d.X is not 1 && d.X is not -1 ) ;
            if ( otherDirection != null ) {
              startPoint = new XYZ( startPoint.X, startPoint.Y - direction.X * otherDirection.Y * tolerance, startPoint.Z ) ;
              endPoint = new XYZ( endPoint.X, endPoint.Y - direction.X * otherDirection.Y * tolerance, endPoint.Z ) ;
            }
            else {
              startPoint = new XYZ( startPoint.X, startPoint.Y + direction.X * tolerance, startPoint.Z ) ;
              endPoint = new XYZ( endPoint.X, endPoint.Y + direction.X * tolerance, endPoint.Z ) ;
            }
          }
          else if ( direction.Y is 1 or -1 ) {
            startPoint = new XYZ( startPoint.X + tolerance, startPoint.Y, startPoint.Z ) ;
            endPoint = new XYZ( endPoint.X + tolerance, endPoint.Y, endPoint.Z ) ;
          }
          else {
            var (x, _, _) = GetConduitFittingDirection( allConduitFittings, startPoint, direction, routeName! ) ;
            if ( x is 1 or -1 ) {
              var otherDirection = conduitDirections.FirstOrDefault( d => d.X is not 1 && d.X is not -1 ) ;
              if ( otherDirection != null ) {
                startPoint = new XYZ( startPoint.X, startPoint.Y - x * otherDirection.Y * tolerance, startPoint.Z ) ;
                endPoint = new XYZ( endPoint.X, endPoint.Y - x * otherDirection.Y * tolerance, endPoint.Z ) ;
              }
              else {
                startPoint = new XYZ( startPoint.X, startPoint.Y + x * tolerance, startPoint.Z ) ;
                endPoint = new XYZ( endPoint.X, endPoint.Y + x * tolerance, endPoint.Z ) ;
              }
            }
            else {
              startPoint = new XYZ( startPoint.X + tolerance, startPoint.Y, startPoint.Z ) ;
              endPoint = new XYZ( endPoint.X + tolerance, endPoint.Y, endPoint.Z ) ;
            }
          }
          
          var newConduit = Conduit.Create( document, arentConduitType.Id, startPoint, endPoint, levelId ) ;
          newConduits.Add( newConduit, DummyName + "_" + routeName! ) ;
          if ( newConduit.HasParameter( RoutingParameter.RouteName ) ) {
            newConduit.SetProperty( RoutingParameter.RouteName, DummyName + "_" + routeName! ) ;
          }
          
          var diameter = ( conduit as Conduit )!.Diameter ;
          if ( newConduit.HasParameter( BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM ) ) {
            newConduit.SetProperty( BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM, diameter ) ;
          }
        }
      }

      return routeDic ;
    }

    private List<XYZ> GetConduitDirections( ICollection<Element> conduits, string routeName )
    {
      var routeNameArray = routeName.Split( '_' ) ;
      routeName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
      var conduitsOfRoute = conduits.Where( c => c.GetRouteName() is { } rName && rName.Contains( routeName ) ).ToList() ;
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
    
    private XYZ GetConduitFittingDirection( ICollection<FamilyInstance> conduitFittings, XYZ conduitOrigin, XYZ conduitDirection, string routeName )
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

    private bool CheckConduitFittingOfBranchRoute ( Element conduitFitting )
    {
      var fromEndPoint = conduitFitting.GetNearestEndPoints( true ).ToList() ;
      if ( ! fromEndPoint.Any() ) return false ;
      var fromEndPointType = fromEndPoint.First().Key.GetTypeName() ;
      return fromEndPointType == PassPointBranchEndPoint.Type ;
    }
    
    private List<string> GenerateConduitFittings( UIDocument uiDocument, MEPCurveType arentConduitType, List<RouteInfo> routeInfos, Dictionary<Element, string> newConduits )
    {
      List<string> removedConduitIds = new() ;
      var document = uiDocument.Document ;
      foreach ( var routeInfo in routeInfos ) {
        var tolerance = routeInfo.Tolerance ;
        var routeName = routeInfo.RouteName ;
        var conduitFittings = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_ConduitFitting ).Where( c => c.GetRouteName() == routeName ).ToList() ;
        foreach ( var conduitFitting in conduitFittings ) {
          if ( CheckConduitFittingOfBranchRoute( conduitFitting ) ) continue ;
          var level = uiDocument.Document.GetAllElements<Level>().FirstOrDefault( l => l.Id == conduitFitting.GetLevelId() ) ;
          var elevation = level!.Elevation ;
          var location = ( conduitFitting.Location as LocationPoint ) ! ;
          var origin = location.Point ;
          var handOrientation = conduitFitting.HandOrientation ;
          var facingOrientation = conduitFitting.FacingOrientation ;
          double x, y ;
          if ( handOrientation.X is 1 or -1 ) {
            if ( facingOrientation.Y is 1 or -1 ) {
              x = handOrientation.X is 1 ? origin.X + facingOrientation.Y * tolerance : origin.X + tolerance ;
              y = handOrientation.X is 1 ? origin.Y - tolerance : origin.Y + facingOrientation.Y * tolerance ;
            }
            else {
              x = origin.X ;
              y = origin.Y - tolerance ;
            }
          }
          else if ( handOrientation.Y is 1 or -1 ) {
            if ( facingOrientation.X is 1 or -1 ) {
              x = handOrientation.Y is 1 ? origin.X + tolerance : origin.X + facingOrientation.X * tolerance ;
              y = handOrientation.Y is 1 ? origin.Y - facingOrientation.X * tolerance : origin.Y + tolerance ;
            }
            else {
              x = origin.X + tolerance ;
              y = origin.Y ;
            }
          }
          else {
            if ( facingOrientation.X is 1 or -1 ) {
              var otherDirection = routeInfo.Directions.FirstOrDefault( d => d.X is not 1 && d.X is not -1 ) ;
              x = origin.X ;
              y = otherDirection != null ? origin.Y - otherDirection.Y * facingOrientation.X * tolerance : origin.Y + facingOrientation.X * tolerance ;
            }
            else {
              x = origin.X + tolerance ;
              y = origin.Y ;
            }
          }
          var z = origin.Z - elevation ;
          
          var symbol = conduitFitting.Symbol ;
          var instance = symbol.Instantiate( new XYZ( x, y, z), level!, StructuralType.NonStructural ) ;
          newConduits.Add( instance, DummyName + "_" + routeName ) ;

          if ( ( handOrientation.X is 1 && facingOrientation.Y is -1 ) || ( handOrientation.Y is 1 && facingOrientation.X is -1 ) ) {
            ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( x, y, origin.Z ), new XYZ( x, y, origin.Z ) + XYZ.BasisX ), Math.PI ) ;
          }
          else if ( ( handOrientation.X is -1 && facingOrientation.Y is 1 ) || ( handOrientation.Y is -1 && facingOrientation.X is 1 ) ) {
            ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( x, y, origin.Z ), new XYZ( x, y, origin.Z ) + XYZ.BasisY ), Math.PI ) ;
          }
          else if ( ( handOrientation.X is -1 && facingOrientation.Y is -1 ) || ( handOrientation.Y is 1 && facingOrientation.X is 1 ) ) {
            ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( x, y, origin.Z ), new XYZ( x, y, origin.Z ) + XYZ.BasisY ), Math.PI ) ;
            ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( x, y, origin.Z ), new XYZ( x, y, origin.Z ) + XYZ.BasisX ), Math.PI ) ;
          }

          if ( handOrientation.Z is 1 or -1 || facingOrientation.Z is 1 or -1 ) {
            if ( ( handOrientation.Z is 1 or -1 && facingOrientation.X is 1 or -1 ) || ( facingOrientation.Z is 1 or -1 && handOrientation.X is 1 or -1 ) ) {
              ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( x, y, origin.Z), new XYZ( x, y, origin.Z) + XYZ.BasisX ), 
                ( handOrientation.Z is 1 or -1 && facingOrientation.X is 1 or -1 ? facingOrientation.X : handOrientation.X ) * Math.PI / 2 ) ;
              if ( facingOrientation.X is 1 || handOrientation.X is -1 ) {
                ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( x, y, origin.Z), new XYZ( x, y, origin.Z) + XYZ.BasisY ), Math.PI ) ;
              }
            }
            else {
              ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( x, y, origin.Z), new XYZ( x, y, origin.Z) + XYZ.BasisY ), 
                ( handOrientation.Z is 1 or -1 && facingOrientation.Y is 1 or -1 ? facingOrientation.Y : handOrientation.Y ) * Math.PI / 2 ) ;
              ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( x, y, origin.Z), new XYZ( x, y, origin.Z) + XYZ.BasisZ ), 
                ( handOrientation.Z is 1 or -1 && facingOrientation.Y is 1 or -1 ? facingOrientation.Y : handOrientation.Y ) * Math.PI ) ;
              if ( facingOrientation.Y is 1 || handOrientation.Y is -1 ) {
                ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( x, y, origin.Z), new XYZ( x, y, origin.Z) + XYZ.BasisX ), Math.PI ) ;
              }
            }
          }
          
          if ( ( handOrientation.X is 1 && facingOrientation.Y is 1 or -1 ) || ( handOrientation.Y is 1 or -1 && facingOrientation.X is -1 ) ) {
            UpdateConduitLenght( document, arentConduitType, newConduits, DummyName + "_" + routeName, tolerance, handOrientation, facingOrientation, origin, routeInfo.Directions, ref removedConduitIds ) ;
          }
          else if ( ( handOrientation.X is -1 && facingOrientation.Y is 1 or -1 ) || ( handOrientation.Y is 1 or -1 && facingOrientation.X is 1 ) ) {
            UpdateConduitLenght( document, arentConduitType, newConduits, DummyName + "_" + routeName, tolerance, handOrientation, facingOrientation, origin, routeInfo.Directions, ref removedConduitIds ) ;
          }
          
          if ( false == instance.TryGetProperty( RoutingParameter.RouteName, out string? _ ) ) continue ;
          instance.SetProperty( RoutingParameter.RouteName, DummyName + "_" + routeName ) ;
        }
      }

      return removedConduitIds ;
    }

    private void HideConduitsIn2DView( Document document, Dictionary<Element, string> newConduits )
    {
      List<ViewPlan> views = new( new FilteredElementCollector( document )
        .OfClass( typeof( ViewPlan ) ).Cast<ViewPlan>()
        .Where( v => v.CanBePrinted && ViewType.FloorPlan == v.ViewType ) ) ;
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
    
    public static void RemoveDummyConduits( Document document )
    {
      var allConduitIds = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits )
        .Where( c => ! string.IsNullOrEmpty( c.GetRouteName() ) && c.GetRouteName()!.Contains( DummyName ) )
        .Select( c => c.UniqueId ).ToList() ;

      if ( ! allConduitIds.Any() ) return ;
      try {
        document.Delete( allConduitIds ) ;
      }
      catch {
        //
      }
    }

    private void UpdateConduitLenght( Document document, MEPCurveType arentConduitType, Dictionary<Element, string> newConduits, string routeName, double lenght, XYZ handOrientation, XYZ facingOrientation, XYZ origin, List<XYZ> directions, ref List<string> removedConduitIds )
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
        if ( ( handOrientation.X is 1 or -1 && Math.Abs( directionX - handOrientation.X ) == 0 )
             || ( handOrientation.Y is 1 or -1 && Math.Abs( directionY - handOrientation.Y ) == 0 )
             || ( handOrientation.Z is 1 or -1 && Math.Abs( directionZ - handOrientation.Z ) == 0 ) ) {
          if ( toDistance < minToDistance ) {
            minToDistance = toDistance ;
            fromConduitInfo = new ConduitInfo( conduit.Key, fromPoint, toPoint, line.Direction, levelId, false ) ;
          }
        }
        
        if ( ( facingOrientation.X is 1 or -1 && Math.Abs( directionX + facingOrientation.X ) == 0 )
             || ( facingOrientation.Y is 1 or -1 && Math.Abs( directionY + facingOrientation.Y ) == 0 )
             || ( facingOrientation.Z is 1 or -1 && Math.Abs( directionZ + facingOrientation.Z ) == 0 ) ) {
          if ( toDistance < minToDistance ) {
            minToDistance = toDistance ;
            fromConduitInfo = new ConduitInfo( conduit.Key, fromPoint, toPoint, line.Direction, levelId, true ) ;
          }
        }

        if ( ( facingOrientation.X is 1 or -1 && Math.Abs( directionX - facingOrientation.X ) == 0 )
             || ( facingOrientation.Y is 1 or -1 && Math.Abs( directionY - facingOrientation.Y ) == 0 )
             || ( facingOrientation.Z is 1 or -1 && Math.Abs( directionZ - facingOrientation.Z ) == 0 ) ) {
          if ( ! ( fromDistance < minFromDistance ) ) continue ;
          minFromDistance = fromDistance ;
          toConduitInfo = new ConduitInfo( conduit.Key, fromPoint, toPoint, line.Direction, levelId, false ) ;
        }
        
        if ( ( handOrientation.X is 1 or -1 && Math.Abs( directionX + handOrientation.X ) == 0 )
             || ( handOrientation.Y is 1 or -1 && Math.Abs( directionY + handOrientation.Y ) == 0 )
             || ( handOrientation.Z is 1 or -1 && Math.Abs( directionZ + handOrientation.Z ) == 0 ) ) {
          if ( ! ( fromDistance < minFromDistance ) ) continue ;
          minFromDistance = fromDistance ;
          toConduitInfo = new ConduitInfo( conduit.Key, fromPoint, toPoint, line.Direction, levelId, true ) ;
        }
      }

      if ( fromConduitInfo != null ) {
        var (directionX, directionY, _) = fromConduitInfo.Direction ;
        var (endPointX, endPointY, endPointZ) = fromConduitInfo.EndPoint ;
        XYZ? fromEndPoint = null ;
        if ( directionX is 1 or -1 ) {
          var x = endPointX + lenght ;
          fromEndPoint = new XYZ( x, endPointY, endPointZ ) ;
        }
        else if ( directionY is 1 or -1 ) {
          double y ;
          var otherDirection = directions.FirstOrDefault( d => d.Y is not 1 && d.Y is not -1 ) ;
          if ( otherDirection != null ) {
            y = endPointY - directionY * otherDirection.X * lenght ;
          }
          else {
            y = fromConduitInfo.IsOppositeDirection ? endPointY + directionY * lenght : endPointY - directionY * lenght ;
          }
          fromEndPoint = new XYZ( endPointX, y, endPointZ ) ;
        }

        newConduits.Remove( fromConduitInfo.Conduit ) ;
        removedConduitIds.Add( fromConduitInfo.Conduit.UniqueId ) ;
        
        var newConduit = Conduit.Create( document, arentConduitType.Id, fromConduitInfo.StartPoint, fromEndPoint, fromConduitInfo.LevelId ) ;
        newConduits.Add( newConduit, routeName ) ;
        
        if ( newConduit.HasParameter( RoutingParameter.RouteName ) ) {
          newConduit.SetProperty( RoutingParameter.RouteName, routeName ) ;
        }
          
        var diameter = ( fromConduitInfo.Conduit as Conduit )!.Diameter ;
        if ( newConduit.HasParameter( BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM ) ) {
          newConduit.SetProperty( BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM, diameter ) ;
        }
      }

      if ( toConduitInfo == null ) return ;
      {
        var (directionX, directionY, _) = toConduitInfo.Direction ;
        var (startPointX, startPointY, startPointZ) = toConduitInfo.StartPoint ;
        XYZ? toStartPoint = null ;
        if ( directionX is 1 or -1 ) {
          var x = startPointX + lenght ;
          toStartPoint = new XYZ( x, startPointY, startPointZ ) ;
        }
        else if ( directionY is 1 or -1 ) {
          double y ;
          var otherDirection = directions.FirstOrDefault( d => d.Y is not 1 && d.Y is not -1 ) ;
          if ( otherDirection != null ) {
            y = startPointY - directionY * otherDirection.X * lenght ;
          }
          else {
            y = toConduitInfo.IsOppositeDirection ? startPointY - directionY * lenght : startPointY + directionY * lenght ;
          }

          toStartPoint = new XYZ( startPointX, y, startPointZ ) ;
        }

        newConduits.Remove( toConduitInfo.Conduit ) ;
        removedConduitIds.Add( toConduitInfo.Conduit.UniqueId ) ;
        
        var newConduit = Conduit.Create( document, arentConduitType.Id, toStartPoint, toConduitInfo.EndPoint, toConduitInfo.LevelId ) ;
        newConduits.Add( newConduit, routeName ) ;
        
        if ( newConduit.HasParameter( RoutingParameter.RouteName ) ) {
          newConduit.SetProperty( RoutingParameter.RouteName, routeName ) ;
        }
          
        var diameter = ( toConduitInfo.Conduit as Conduit )!.Diameter ;
        if ( newConduit.HasParameter( BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM ) ) {
          newConduit.SetProperty( BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM, diameter ) ;
        }
      }
    }
    
    private class ConduitInfo
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
    
    private class RouteInfo
    {
      public string RouteName { get ; }
      public double Tolerance { get ; }
      public List<XYZ> Directions { get ; }

      public RouteInfo( string routeName, double tolerance, List<XYZ> directions )
      {
        RouteName = routeName ;
        Tolerance = tolerance ;
        Directions = directions ;
      }
    }
  }
}