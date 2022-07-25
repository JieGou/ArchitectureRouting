using System ;
using System.Collections.Generic ;
using System.Linq ;
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
  [DisplayNameKey( "Electrical.App.Commands.Routing.CreateDummyConduitsIn3DViewCommand", DefaultString = "Show\nConduits in 3D" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class CreateDummyConduitsIn3DViewCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var app = commandData.Application ;
      var uiDocument = app.ActiveUIDocument ;
      var document = uiDocument.Document ;

      try {
        View? newView = null ;
        var isCreateDummyConduits = ShowConduitsIn3DUtil.RemoveDummyConduits( document ) ;
        if ( isCreateDummyConduits ) {
          var allConduits = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( c => ! string.IsNullOrEmpty( c.GetRouteName() ) && c.GetRouteName() != c.GetRepresentativeRouteName() ).ToList() ;
          if ( allConduits.Any() ) {
            ShowConduitsIn3DUtil.UpdateIsEnableButton( document, false ) ;
            Dictionary<Element, string> newConduits = new() ;
            List<ElementId> conduitsHideIn3DView = new() ;

            using var transaction = new Transaction( document, ( "TransactionName.Commands.Routing.CreateDummyConduitsIn3DViewCommand".GetAppStringByKeyOrDefault( "Create Dummy Conduits In 3D View" ) ) ) ;
            transaction.Start() ;
            var failureOptions = transaction.GetFailureHandlingOptions() ;
            failureOptions.SetFailuresPreprocessor( new ShowConduitsIn3DUtil.FailurePreprocessor() ) ;
            transaction.SetFailureHandlingOptions( failureOptions ) ;
            if ( document.ActiveView is ViewPlan activeView ) {
              newView = ShowConduitsIn3DUtil.Create3DView( document, activeView ) ;
            }
            
            var routeDic = GenerateConduits( document, allConduits, newConduits, conduitsHideIn3DView ) ;
            var removedConduitIds = GenerateConduitFittings( uiDocument, routeDic, newConduits, conduitsHideIn3DView ) ;
            transaction.Commit( failureOptions ) ;

            ShowConduitsIn3DUtil.RemoveAndHideUnusedConduits( document, removedConduitIds, newConduits, conduitsHideIn3DView ) ;
          }
        }
        
        if ( newView != null ) {
          uiDocument.ActiveView = newView ;
        }

        return Result.Succeeded ;
      }
      catch ( Exception exception ) {
        ShowConduitsIn3DUtil.UpdateIsEnableButton( document, true ) ;
        message = exception.Message ;
        return Result.Failed ;
      }
    }

    private List<ShowConduitsIn3DUtil.RouteInfo> GenerateConduits( Document document, ICollection<Element> allConduits, Dictionary<Element, string> newConduits, List<ElementId> conduitsHideIn3DView )
    {
      var routeDic = new List<ShowConduitsIn3DUtil.RouteInfo>() ;

      var allRouteName = allConduits.Select( c => c.GetRouteName() ! ).ToHashSet() ;
      var allConduitFittings = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_ConduitFitting ).Where( c => allRouteName.Contains( c.GetRouteName() ! ) ).ToList() ;

      Dictionary<string, List<Element>> conduitGroupByMainRoute = new() ;
      foreach ( var conduit in allConduits ) {
        var representativeRouteName = ( conduit.GetRepresentativeRouteName() ) ! ;
        var routeNameArray = representativeRouteName.Split( '_' ) ;
        var mainRouteName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
        if ( conduitGroupByMainRoute.ContainsKey( mainRouteName ) ) {
          conduitGroupByMainRoute[mainRouteName].Add( conduit ) ;
        }
        else {
          conduitGroupByMainRoute.Add( mainRouteName, new List<Element> { conduit } ) ;
        }
      }
      
      foreach ( var ( mainRouteName, conduits ) in conduitGroupByMainRoute ) {
        var sortConduits = conduits.OrderByDescending( c => c.GetRouteName() ).ToList() ;
        var conduitDirections = ShowConduitsIn3DUtil.GetConduitDirections( allConduits, mainRouteName ) ;
        var conduitToleranceDic = ShowConduitsIn3DUtil.SetOffsetForConduit( document, sortConduits, conduitsHideIn3DView ) ;
        foreach ( var conduit in sortConduits ) {
          var routeName = ( conduit.GetRouteName() ) ! ;
          var routeNameArray = routeName.Split( '_' ) ;
          var mainName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
          var levelId = conduit.GetLevelId() ;
          var conduitLocation = ( conduit.Location as LocationCurve ) ! ;
          var conduitLine = ( conduitLocation.Curve as Line ) ! ;
          var startPoint = conduitLine.GetEndPoint( 0 ) ;
          var endPoint = conduitLine.GetEndPoint( 1 ) ;
          var direction = conduitLine.Direction ;
          double offset ;
          MEPCurveType? conduitType ;
          var isMoveConduit = ! ShowConduitsIn3DUtil.CheckConduitOfBranchRoute( conduit ) ;
          if ( ! string.IsNullOrEmpty( routeName ) && ! routeDic.Exists( r => r.RouteName == routeName! ) ) {
            offset = conduitToleranceDic[mainName] ;
            var ( branchConduitWithSamePassPointDirection, passPointDirection ) = ShowConduitsIn3DUtil.GetBranchConduitWithSamePassPointDirection( document, mainName ) ;
            isMoveConduit = isMoveConduit || branchConduitWithSamePassPointDirection.SingleOrDefault( c => c.UniqueId == conduit.UniqueId ) != null ;
            var conduitTypeName = conduit.Name ;
            conduitType = document.GetAllElements<MEPCurveType>().FirstOrDefault( c => c.Name == conduitTypeName ) ;
            var routeInfo = new ShowConduitsIn3DUtil.RouteInfo( routeName!, conduitType!, offset, conduitDirections, branchConduitWithSamePassPointDirection, passPointDirection ) ;
            routeDic.Add( routeInfo ) ;
          }
          else {
            var routeInfo = routeDic.First( r => r.RouteName == routeName! ) ;
            offset = routeInfo.Offset ;
            var branchConduitWithSamePassPointDirection = routeInfo.ConduitsOfBranch ;
            isMoveConduit = isMoveConduit || branchConduitWithSamePassPointDirection.SingleOrDefault( c => c.UniqueId == conduit.UniqueId ) != null ;
            conduitType = routeInfo.CurveType ;
          }

          CreateConduit( document, conduitType!, allConduitFittings, newConduits, conduitDirections, conduit, routeName!, direction, startPoint, endPoint, offset, levelId, isMoveConduit ) ;
        }
      }

      return routeDic ;
    }

    private void CreateConduit( Document document, MEPCurveType conduitType, ICollection<FamilyInstance> allConduitFittings, Dictionary<Element, string> newConduits, ICollection<XYZ> conduitDirections, Element conduit, string routeName, XYZ direction, XYZ startPoint, XYZ endPoint, double offset, ElementId levelId, bool isMoveConduit )
    {
      if ( isMoveConduit ) {
        if ( direction.X is 1 or -1 ) {
          var otherDirection = conduitDirections.FirstOrDefault( d => d.X is not 1 && d.X is not -1 ) ;
          if ( otherDirection != null ) {
            startPoint = new XYZ( startPoint.X, startPoint.Y - direction.X * otherDirection.Y * offset, startPoint.Z ) ;
            endPoint = new XYZ( endPoint.X, endPoint.Y - direction.X * otherDirection.Y * offset, endPoint.Z ) ;
          }
          else {
            startPoint = new XYZ( startPoint.X, startPoint.Y + direction.X * offset, startPoint.Z ) ;
            endPoint = new XYZ( endPoint.X, endPoint.Y + direction.X * offset, endPoint.Z ) ;
          }
        }
        else if ( direction.Y is 1 or -1 ) {
          var isBranchConduit = ShowConduitsIn3DUtil.CheckConduitOfBranchRoute( conduit ) ;
          if ( isBranchConduit ) {
            startPoint = new XYZ( startPoint.X - direction.Y * offset, startPoint.Y, startPoint.Z ) ;
            endPoint = new XYZ( endPoint.X - direction.Y * offset, endPoint.Y, endPoint.Z ) ;
          }
          else {
            startPoint = new XYZ( direction.Y is 1 ? startPoint.X - offset : startPoint.X + offset, startPoint.Y, startPoint.Z ) ;
            endPoint = new XYZ( direction.Y is 1 ? endPoint.X - offset : endPoint.X + offset, endPoint.Y, endPoint.Z ) ;
          }
        }
        else {
          var (x, y, _) = ShowConduitsIn3DUtil.GetConduitFittingDirection( allConduitFittings, startPoint, direction, routeName ) ;
          if ( x is 1 or -1 ) {
            var otherDirection = conduitDirections.FirstOrDefault( d => d.X is not 1 && d.X is not -1 ) ;
            if ( otherDirection != null ) {
              startPoint = new XYZ( startPoint.X, startPoint.Y - x * otherDirection.Y * offset, startPoint.Z ) ;
              endPoint = new XYZ( endPoint.X, endPoint.Y - x * otherDirection.Y * offset, endPoint.Z ) ;
            }
            else {
              startPoint = new XYZ( startPoint.X, startPoint.Y + x * offset, startPoint.Z ) ;
              endPoint = new XYZ( endPoint.X, endPoint.Y + x * offset, endPoint.Z ) ;
            }
          }
          else {
            var isBranchConduit = ShowConduitsIn3DUtil.CheckConduitOfBranchRoute( conduit ) ;
            if ( isBranchConduit ) {
              startPoint = new XYZ( startPoint.X - y * offset, startPoint.Y, startPoint.Z ) ;
              endPoint = new XYZ( endPoint.X - y * offset, endPoint.Y, endPoint.Z ) ;
            }
            else {
              startPoint = new XYZ( y is 1 ? startPoint.X - offset : startPoint.X + offset, startPoint.Y, startPoint.Z ) ;
              endPoint = new XYZ( y is 1 ? endPoint.X - offset : endPoint.X + offset, endPoint.Y, endPoint.Z ) ;
            }
          }
        }
      }

      var newConduit = Conduit.Create( document, conduitType.Id, startPoint, endPoint, levelId ) ;
      newConduits.Add( newConduit, ShowConduitsIn3DUtil.DummyName + "_" + routeName ) ;
      if ( newConduit.HasParameter( RoutingParameter.RouteName ) ) {
        newConduit.SetProperty( RoutingParameter.RouteName, ShowConduitsIn3DUtil.DummyName + "_" + routeName ) ;
      }

      var diameter = ( conduit as Conduit )!.Diameter ;
      if ( newConduit.HasParameter( BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM ) ) {
        newConduit.SetProperty( BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM, diameter ) ;
      }
    }

    private List<ElementId> GenerateConduitFittings( UIDocument uiDocument, List<ShowConduitsIn3DUtil.RouteInfo> routeInfos, Dictionary<Element, string> newConduits, List<ElementId> conduitsHideIn3DView )
    {
      List<ElementId> removedConduitIds = new() ;
      var document = uiDocument.Document ;
      foreach ( var routeInfo in routeInfos ) {
        var offset = routeInfo.Offset ;
        var routeName = routeInfo.RouteName ;
        var conduitFittings = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_ConduitFitting ).Where( c => c.GetRouteName() == routeName ).ToList() ;
        foreach ( var conduitFitting in conduitFittings ) {
          var isBranchConduitFitting = ShowConduitsIn3DUtil.CheckConduitOfBranchRoute( conduitFitting ) ;
          if ( isBranchConduitFitting ) {
            conduitsHideIn3DView.Add( conduitFitting.Id ) ;
          }
          var level = uiDocument.Document.GetAllElements<Level>().FirstOrDefault( l => l.Id == conduitFitting.GetLevelId() ) ;
          var elevation = level!.Elevation ;
          var location = ( conduitFitting.Location as LocationPoint ) ! ;
          var origin = location.Point ;
          var handOrientation = conduitFitting.HandOrientation ;
          var facingOrientation = conduitFitting.FacingOrientation ;
          var (fromConduitInfo, toConduitInfo) = ShowConduitsIn3DUtil.GetFromAndToConduitsOfCConduitFitting( newConduits, ShowConduitsIn3DUtil.DummyName + "_" + routeName, handOrientation, facingOrientation, origin ) ;
          var isConduitWithSamePassPointDirection = routeInfo.ConduitsOfBranch.FirstOrDefault( c => c.UniqueId == conduitFitting.UniqueId ) != null ;
          var passPointDirection = routeInfo.PassPointDirection ;
          double x, y ;
          if ( handOrientation.X is 1 or -1 ) {
            if ( facingOrientation.Y is 1 or -1 ) {
              if ( isBranchConduitFitting ) {
                if ( isConduitWithSamePassPointDirection ) {
                  if ( handOrientation.X is -1 && facingOrientation.Y is -1 ) {
                    x = passPointDirection.Y is 1 or -1 ? ( toConduitInfo != null && toConduitInfo!.IsOppositeDirection ? origin.X - offset : origin.X + offset ) : origin.X ;
                    y = passPointDirection.X is 1 or -1 ? ( toConduitInfo != null && toConduitInfo!.IsOppositeDirection ? origin.Y + offset : origin.Y - offset ) : origin.Y ;
                  }
                  else if ( handOrientation.X is 1 && facingOrientation.Y is 1 ) {
                    x = passPointDirection.Y is 1 or -1 ? ( toConduitInfo != null && toConduitInfo!.IsOppositeDirection ? origin.X + offset : origin.X - offset ) : origin.X ;
                    y = passPointDirection.X is 1 or -1 ? ( toConduitInfo != null && toConduitInfo!.IsOppositeDirection ? origin.Y - offset : origin.Y + offset ) : origin.Y ;
                  }
                  else {
                    x = passPointDirection.Y is 1 or -1 ? ( origin.X - handOrientation.X * facingOrientation.Y * offset ) : origin.X ;
                    y = passPointDirection.X is 1 or -1 ? ( origin.Y + handOrientation.X * offset ) : origin.Y ;
                  }
                }
                else {
                  x = origin.X ;
                  y = origin.Y ;
                }
              }
              else {
                if ( toConduitInfo != null && toConduitInfo!.IsOppositeDirection && handOrientation.X is -1 && facingOrientation.Y is -1 ) {
                  x = origin.X - offset ;
                  y = origin.Y + offset ;
                }
                else {
                  x = toConduitInfo != null && toConduitInfo!.IsOppositeDirection ? origin.X + handOrientation.X * facingOrientation.Y * offset : origin.X - handOrientation.X * offset ;
                  y = toConduitInfo != null && toConduitInfo!.IsOppositeDirection ? origin.Y - handOrientation.X * facingOrientation.Y * offset : origin.Y + facingOrientation.Y * offset ;
                }
              }
            }
            else {
              if ( ! isConduitWithSamePassPointDirection && isBranchConduitFitting ) {
                x = origin.X ;
                y = origin.Y ;
              }
              else {
                x = origin.X ;
                y = origin.Y + handOrientation.X * offset ;
              }
            }
          }
          else if ( handOrientation.Y is 1 or -1 ) {
            if ( facingOrientation.X is 1 or -1 ) {
              if ( isBranchConduitFitting ) {
                if ( isConduitWithSamePassPointDirection ) {
                  x = passPointDirection.Y is 1 or -1 ? ( toConduitInfo != null && toConduitInfo!.IsOppositeDirection ? origin.X - facingOrientation.X * offset : origin.X + facingOrientation.X * offset ) : origin.X  ;
                  y = passPointDirection.X is 1 or -1 ? ( toConduitInfo != null && toConduitInfo!.IsOppositeDirection ? origin.Y + handOrientation.Y * offset : origin.Y - handOrientation.Y * offset ) : origin.Y ;
                }
                else {
                  x = origin.X ;
                  y = origin.Y ;
                }
              }
              else {
                if ( toConduitInfo is { IsOppositeDirection: true } ) {
                  x = origin.X + handOrientation.Y * offset ;
                  y = origin.Y + handOrientation.Y * offset ;
                }
                else {
                  x = handOrientation.Y is 1 ? origin.X - offset : origin.X + offset ;
                  y = handOrientation.Y is 1 ? origin.Y - offset : origin.Y + offset ;
                }
              }
            }
            else {
              if ( ! isConduitWithSamePassPointDirection && isBranchConduitFitting ) {
                x = origin.X ;
                y = origin.Y ;
              }
              else {
                x = handOrientation.Y is 1 ? origin.X - offset : origin.X + offset ;
                y = origin.Y ;
              }
            }
          }
          else {
            if ( ! isConduitWithSamePassPointDirection && isBranchConduitFitting ) {
              x = origin.X ;
              y = origin.Y ;
            }
            else {
              if ( facingOrientation.X is 1 or -1 ) {
                x = origin.X ;
                y = origin.Y + facingOrientation.X * offset ;
              }
              else {
                x = origin.X - facingOrientation.Y * offset ;
                y = origin.Y ;
              }
            }
          }
          var z = origin.Z - elevation ;

          var symbol = conduitFitting.Symbol ;
          var instance = symbol.Instantiate( new XYZ( x, y, z), level!, StructuralType.NonStructural ) ;
          var radius = conduitFitting.ParametersMap.get_Item( "呼び半径" ).AsDouble() ;
          instance.ParametersMap.get_Item( "呼び半径" )?.Set( radius ) ;
          if ( ! conduitFitting.ParametersMap.get_Item( "Revit.Property.Builtin.BendRadius".GetDocumentStringByKeyOrDefault( document, "Bend Radius" ) ).IsReadOnly ) {
            var bendRadius = conduitFitting.ParametersMap.get_Item( "Revit.Property.Builtin.BendRadius".GetDocumentStringByKeyOrDefault( document, "Bend Radius" ) ).AsDouble() ;
            instance.ParametersMap.get_Item( "Revit.Property.Builtin.BendRadius".GetDocumentStringByKeyOrDefault( document, "Bend Radius" ) )?.Set( bendRadius ) ;
          }
          newConduits.Add( instance, ShowConduitsIn3DUtil.DummyName + "_" + routeName ) ;

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
              if ( facingOrientation.Z is -1 ) {
                ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( x, y, origin.Z), new XYZ( x, y, origin.Z) + XYZ.BasisX ), Math.PI ) ;
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
              if ( facingOrientation.Z is -1 ) {
                ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( x, y, origin.Z), new XYZ( x, y, origin.Z) + XYZ.BasisY ), Math.PI ) ;
              }
            }
          }

          if ( ( handOrientation.X is 1 && facingOrientation.Y is 1 or -1 ) || ( handOrientation.Y is 1 or -1 && facingOrientation.X is -1 ) ) {
            UpdateConduitLenght( document, routeInfo.CurveType, newConduits, ShowConduitsIn3DUtil.DummyName + "_" + routeName, offset, routeInfo.Directions, ref removedConduitIds, fromConduitInfo, toConduitInfo, isConduitWithSamePassPointDirection ) ;
          }
          else if ( ( handOrientation.X is -1 && facingOrientation.Y is 1 or -1 ) || ( handOrientation.Y is 1 or -1 && facingOrientation.X is 1 ) ) {
            UpdateConduitLenght( document, routeInfo.CurveType, newConduits, ShowConduitsIn3DUtil.DummyName + "_" + routeName, offset, routeInfo.Directions, ref removedConduitIds, fromConduitInfo, toConduitInfo, isConduitWithSamePassPointDirection ) ;
          }
          
          if ( false == instance.TryGetProperty( RoutingParameter.RouteName, out string? _ ) ) continue ;
          instance.SetProperty( RoutingParameter.RouteName, ShowConduitsIn3DUtil.DummyName + "_" + routeName ) ;
        }
      }

      return removedConduitIds ;
    }

    private void UpdateConduitLenght( Document document, MEPCurveType arentConduitType, Dictionary<Element, string> newConduits, string routeName, double length, List<XYZ> directions, ref List<ElementId> removedConduitIds, ShowConduitsIn3DUtil.ConduitInfo? fromConduitInfo, ShowConduitsIn3DUtil.ConduitInfo? toConduitInfo, bool isConduitWithSamePassPointDirection )
    {
      var minTolerance = ( 2.54 ).MillimetersToRevitUnits() ;
      if ( fromConduitInfo != null && ! isConduitWithSamePassPointDirection ) {
        var (directionX, directionY, _) = fromConduitInfo.Direction ;
        var (endPointX, endPointY, endPointZ) = fromConduitInfo.EndPoint ;
        XYZ? fromEndPoint = null ;
        if ( directionX is 1 or -1 ) {
          var x = fromConduitInfo.IsOppositeDirection ? endPointX + directionX * length : endPointX - directionX * length ;
          fromEndPoint = new XYZ( x, endPointY, endPointZ ) ;
        }
        else if ( directionY is 1 or -1 ) {
          double y ;
          var otherDirection = directions.FirstOrDefault( d => d.Y is not 1 && d.Y is not -1 ) ;
          if ( otherDirection != null ) {
            y = endPointY - directionY * otherDirection.X * length ;
          }
          else {
            y = fromConduitInfo.IsOppositeDirection ? endPointY + directionY * length : endPointY - directionY * length ;
          }
          fromEndPoint = new XYZ( endPointX, y, endPointZ ) ;
        }

        newConduits.Remove( fromConduitInfo.Conduit ) ;
        removedConduitIds.Add( fromConduitInfo.Conduit.Id ) ;

        if ( fromConduitInfo.StartPoint.DistanceTo( fromEndPoint ) > minTolerance ) {
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
      }

      if ( toConduitInfo == null ) return ;
      {
        var (directionX, directionY, _) = toConduitInfo.Direction ;
        var (startPointX, startPointY, startPointZ) = toConduitInfo.StartPoint ;
        XYZ? toStartPoint = null ;
        if ( directionX is 1 or -1 ) {
          var x = toConduitInfo.IsOppositeDirection ? startPointX - directionX * length : startPointX + directionX * length ;
          toStartPoint = new XYZ( x, startPointY, startPointZ ) ;
        }
        else if ( directionY is 1 or -1 ) {
          double y ;
          var otherDirection = directions.FirstOrDefault( d => d.Y is not 1 && d.Y is not -1 ) ;
          if ( otherDirection != null ) {
            y = startPointY - directionY * otherDirection.X * length ;
          }
          else {
            y = toConduitInfo.IsOppositeDirection ? startPointY - directionY * length : startPointY + directionY * length ;
          }

          toStartPoint = new XYZ( startPointX, y, startPointZ ) ;
        }

        newConduits.Remove( toConduitInfo.Conduit ) ;
        removedConduitIds.Add( toConduitInfo.Conduit.Id ) ;

        if ( ! ( toConduitInfo.EndPoint.DistanceTo( toStartPoint ) > minTolerance ) ) return ;
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
  }
}