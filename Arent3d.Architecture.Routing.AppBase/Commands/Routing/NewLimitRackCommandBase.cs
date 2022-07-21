using System ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.DB.Electrical ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Utility ;
using Autodesk.Revit.ApplicationServices ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class NewLimitRackCommandBase : IExternalCommand
  {
    /// <summary>
    /// Max Distance Tolerance when find Connector Closest
    /// </summary>
    private readonly double maxDistanceTolerance = ( 20.0 ).MillimetersToRevitUnits() ;

    private readonly int minNumberOfMultiplicity = 5 ;
    private readonly double minLengthOfConduit = ( 3.0 ).MetersToRevitUnits() ;
    private readonly double cableTrayDefaultBendRadius = ( 16.0 ).MillimetersToRevitUnits() ;

    private readonly double[] cableTrayWidthMapping = { 200.0, 300.0, 400.0, 500.0, 600.0, 800.0, 1000.0, 1200.0 } ;

    private Dictionary<ElementId, List<Connector>> elbowsToCreate = new Dictionary<ElementId, List<Connector>>() ;

    private Dictionary<string, double> routeLengthCache = new Dictionary<string, double>() ;

    private Dictionary<string, Dictionary<int, double>> routeMaxWidthCache = new Dictionary<string, Dictionary<int, double>>() ;

    private static readonly double WidthCableTrayDefault2D = 300d.MillimetersToRevitUnits() ;

    protected abstract AddInType GetAddInType() ;
    protected abstract bool IsCircle { get ; }

    private const string TransactionKey = "TransactionName.Commands.Rack.CreateLimitCableRack" ;
    private readonly string _transactioName = TransactionKey.GetAppStringByKeyOrDefault( "Create Limit Cable" ) ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      UIApplication uiApp = commandData.Application ;
      Application app = uiApp.Application ;
      var limitRackStorable = document.GetAllStorables<LimitRackStorable>().FirstOrDefault() ?? document.GetLimitRackStorable() ;
      var allLimitRackCaches = new List<(string routeName,IList<string> rackIds,IList<string> rackDetailCurveIds)>() ;
      
      try {
        var result = document.Transaction( _transactioName, _ =>
        {
          var racks = new List<FamilyInstance>() ;
          var fittings = new List<FamilyInstance>() ;
          var elements = document.CollectAllMultipliedRoutingElements( minNumberOfMultiplicity ).ToList() ;
          foreach ( var element in elements ) {
            var (mepCurve, subRoute) = element ;
            if ( RouteLength( subRoute.Route.RouteName, elements, document ) >= minLengthOfConduit ) {
              var conduit = ( mepCurve as Conduit )! ;
              var cableRackWidth = CalcCableRackMaxWidth( element, elements, document ) ;

              CreateCableRackForConduit( uiDocument, conduit, cableRackWidth, racks, allLimitRackCaches ) ;
            }
          }

          foreach ( var elbow in elbowsToCreate ) {
            CreateElbow( uiDocument, elbow.Key, elbow.Value, fittings, allLimitRackCaches ) ;
          }
          
          document.Regenerate() ;

          var newRacks = ConnectedRacks( document, racks, fittings, allLimitRackCaches ) ;

          //insert notation for racks
          NewRackCommandBase.CreateNotationForRack( document, app, newRacks ) ;

          return Result.Succeeded ;
        } ) ;

        return result ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    private IEnumerable<FamilyInstance> ConnectedRacks( Document document, List<FamilyInstance> cableTrays, List<FamilyInstance> fittings,  ICollection<(string routeName,IList<string> rackIds,IList<string> rackDetailCurveIds)> allRackIds )
    {
      var tolerance = 10d.MillimetersToRevitUnits() ;
      cableTrays = cableTrays.Where( MEPModelOnPlan ).ToList() ;
      fittings = fittings.Where( MEPModelOnPlan ).ToList() ;
      var cableTrayWidth = WidthCableTrayDefault2D ;

      if ( ! cableTrays.Any() )
        return fittings ;

      var groupCableTrays = GroupRacks( cableTrays ) ;

      var newCableTrays = new List<FamilyInstance>() ;
      var infoCableTrays = new List<(Line LocationLine, double Width)>() ;
      foreach ( var groupCableTray in groupCableTrays ) {
        var locationTempt = GetMaxLength( document, groupCableTray.Select( GetConnector ).SelectMany( x => x ).Select( x => x.Origin ).ToList() ) ;
        if ( null == locationTempt )
          continue ;

        var locationAfterIntersect = IntersectFitting( locationTempt, fittings, tolerance ) ;
        var cableTray = groupCableTray[ 0 ] ;
        newCableTrays.Add( cableTray ) ;
        cableTray.LookupParameter( "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( document, "トレイ長さ" ) ).Set( locationAfterIntersect.Length ) ;
        cableTrayWidth = cableTray.LookupParameter( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ) ).AsDouble() ;
        infoCableTrays.Add( (locationAfterIntersect, cableTrayWidth ) ) ;
        var locationCableTray = ( cableTray.Location as LocationPoint )!.Point ;
        var pointNearest = locationAfterIntersect.GetEndPoint( 0 ).DistanceTo( locationCableTray ) < locationAfterIntersect.GetEndPoint( 1 ).DistanceTo( locationCableTray ) ? locationAfterIntersect.GetEndPoint( 0 ) : locationAfterIntersect.GetEndPoint( 1 ) ;
        ElementTransformUtils.MoveElement( document, cableTray.Id, new XYZ( pointNearest.X, pointNearest.Y, locationCableTray.Z ) - locationCableTray ) ;
        
        var rackIdToRemove = groupCableTray.Select( x => x.UniqueId ).EnumerateAll() ;
        RemoveRackIdWhenCombineRacksToOneInCaches( allRackIds.EnumerateAll(),rackIdToRemove);
        groupCableTray.RemoveAt( 0 ) ;
        document.Delete( rackIdToRemove.ToList() ) ;
      }

      if ( ! IsCircle ) {
        var inforCableTrays = ExtendCurves( document, infoCableTrays, fittings ) ;
        var curveLoops = GroupCurves( inforCableTrays ).Select( x => CurveLoop.CreateViaThicken( x.CurveLoop, cableTrayWidth, XYZ.BasisZ ) ) ;
        var lineStyle = GetLineStyle( document, EraseLimitRackCommandBase.BoundaryCableTrayLineStyleName, new Color( 255, 0, 255 ), 5 ).GetGraphicsStyle( GraphicsStyleType.Projection ) ;
        CreateDetailLines( document, curveLoops, lineStyle ) ;
      }
      else {
        var curves = new List<Curve>() ;
        curves.AddRange( infoCableTrays.Select( x => x.LocationLine ) ) ;
        var fittingLocations = GeometryHelper.GetCurveFromElements( document.ActiveView, fittings ) ;
        curves.AddRange( fittingLocations.Select( x => x.Key ) ) ;
        var mergeCurves = MergeCurves( curves ) ;

        var curveLoops = mergeCurves.Select( x => CurveLoop.Create( x.ToList() ) ).Select( x => CurveLoop.CreateViaThicken( x, cableTrayWidth, XYZ.BasisZ ) ) ;
        var lineStyle = GetLineStyle( document, EraseLimitRackCommandBase.BoundaryCableTrayLineStyleName, new Color( 255, 0, 255 ), 5 ).GetGraphicsStyle( GraphicsStyleType.Projection ) ;
        CreateDetailLines( document, curveLoops, lineStyle) ;
      }
      return newCableTrays ;
    }

    /// <summary>
    /// Remove rack id in mapping collection when ConnectedRacks
    /// </summary>
    /// <param name="allRackIds"></param>
    /// <param name="rackIds"></param>
    private static void RemoveRackIdWhenCombineRacksToOneInCaches( IReadOnlyCollection<(string routeName, IList<string> rackIds, IList<string> rackDetailCurveIds)> allRackIds, IReadOnlyCollection<string> rackIds )
    {
      var rackIdCount = rackIds.Count() ;
      
      for ( var i = 0; i < rackIdCount; i++ ) {
        var rackId = rackIds.ElementAt( i ) ;
        (string routeName, IList<string> rackIds,IList<string> rackDetailCurveIds )? rackMap = allRackIds.FirstOrDefault( rm => rm.rackIds.Any( r => r == rackId ) ) ;
        rackMap?.rackIds.Remove(rackId);
      }
    }
    
    private List<List<Curve>> MergeCurves( List<Curve> curves )
    {
      var curvesGroups = new List<List<Curve>>() ;
      var mergeCurves = new List<Curve>() ;

      var cloneCurves = curves.ToList() ;
      while ( cloneCurves.Count > 0 ) {
        var count = cloneCurves.Count ;

        for ( var i = cloneCurves.Count - 1 ; i >= 0 ; i-- ) {
          if ( AddCurve( cloneCurves[ i ], ref mergeCurves ) ) {
            cloneCurves.RemoveAt( i ) ;
          }
        }

        if ( count == cloneCurves.Count ) {
          curvesGroups.Add( mergeCurves ) ;
          mergeCurves = new List<Curve>() ;
        }

        if ( cloneCurves.Count == 0 ) {
          curvesGroups.Add( mergeCurves ) ;
        }
      }

      return curvesGroups ;
    }

    private bool AddCurve( Curve curve, ref List<Curve> curves )
    {
      if ( curves.Count == 0 ) {
        curves.Add( curve ) ;
        return true ;
      }

      var lc = curves.Last() ;

      if ( lc.GetEndPoint( 1 ).IsAlmostEqualTo( curve.GetEndPoint( 0 ), GeometryHelper.Tolerance ) ) {
        if ( lc is Line lf && curve is Line ls ) {
          var l = Line.CreateBound( lf.GetEndPoint( 0 ), ls.GetEndPoint( 1 ) ) ;
          curves.RemoveAt( curves.Count - 1 ) ;
          curves.Add( l ) ;
        }
        else {
          curves.Add( curve ) ;
        }

        return true ;
      }

      if ( lc.GetEndPoint( 1 ).IsAlmostEqualTo( curve.GetEndPoint( 1 ), GeometryHelper.Tolerance ) ) {
        if ( lc is Line lf && curve is Line ls ) {
          var l = Line.CreateBound( lf.GetEndPoint( 0 ), ls.GetEndPoint( 0 ) ) ;
          curves.RemoveAt( curves.Count - 1 ) ;
          curves.Add( l ) ;
        }
        else {
          curves.Add( curve.CreateReversed() ) ;
        }

        return true ;
      }

      var fc = curves.First() ;

      if ( fc.GetEndPoint( 0 ).IsAlmostEqualTo( curve.GetEndPoint( 0 ), GeometryHelper.Tolerance ) ) {
        if ( fc is Line lf && curve is Line ls ) {
          var l = Line.CreateBound( ls.GetEndPoint( 1 ), lf.GetEndPoint( 1 ) ) ;
          curves.RemoveAt( 0 ) ;
          curves.Insert( 0, l ) ;
        }
        else {
          curves.Insert( 0, curve.CreateReversed() ) ;
        }

        return true ;
      }

      if ( fc.GetEndPoint( 0 ).IsAlmostEqualTo( curve.GetEndPoint( 1 ), GeometryHelper.Tolerance ) ) {
        if ( fc is Line lf && curve is Line ls ) {
          var l = Line.CreateBound( ls.GetEndPoint( 0 ), lf.GetEndPoint( 1 ) ) ;
          curves.RemoveAt( 0 ) ;
          curves.Insert( 0, l ) ;
        }
        else {
          curves.Insert( 0, curve ) ;
        }

        return true ;
      }

      return false ;
    }

    private static void CreateDetailLines( Document document, IEnumerable<CurveLoop> curveLoops, Element lineStyle)
    {
      foreach ( var curveLoop in curveLoops ) {
        foreach ( var curve in curveLoop ) {
          var detaiLine = document.Create.NewDetailCurve( document.ActiveView, curve ) ;
          detaiLine.LineStyle = lineStyle ;
        }
      }

      var category = Category.GetCategory( document, BuiltInCategory.OST_CableTrayFitting ) ;
      document.ActiveView.SetCategoryHidden( category.Id, true ) ;
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

    public static IEnumerable<(CurveLoop CurveLoop, double Width)> GroupCurves( IEnumerable<(Line LocationLine, double Width)> inforCableTrays )
    {
      var cloneCurves = inforCableTrays.ToList() ;
      var curveLoops = new List<(CurveLoop CurveLoop, double Width)>() ;
      // Algorithm to group interconnected curves
      while ( cloneCurves.Count > 0 ) {
        var groupCurves = new List<(Line LocationLine, double Width)> { cloneCurves[ 0 ] } ;
        cloneCurves.RemoveAt( 0 ) ;
        if ( cloneCurves.Count == 0 )
          curveLoops.Add( ( CreateCurveLoop( groupCurves.Select( x => x.LocationLine ) ), groupCurves[ 0 ].Width ) ) ;

        int count ;
        // Algorithm to connect curves in the order specified by CurveLoop
        do {
          count = groupCurves.Count ;
          for ( var i = cloneCurves.Count - 1 ; i >= 0 ; i-- ) {
            if ( groupCurves.Count == 1 ) {
              if ( groupCurves[ 0 ].LocationLine.GetEndPoint( 0 ).DistanceTo( cloneCurves[ i ].LocationLine.GetEndPoint( 0 ) ) < GeometryUtil.Tolerance ) {
                groupCurves = new List<(Line LocationLine, double Width)> { ( ( groupCurves[ 0 ].LocationLine.CreateReversed() as Line )!, groupCurves[ 0 ].Width ), cloneCurves[ i ] } ;
                cloneCurves.RemoveAt( i ) ;
              }
              else if ( groupCurves[ 0 ].LocationLine.GetEndPoint( 0 ).DistanceTo( cloneCurves[ i ].LocationLine.GetEndPoint( 1 ) ) < GeometryUtil.Tolerance ) {
                groupCurves = new List<(Line LocationLine, double Width)> { cloneCurves[ i ], groupCurves[ 0 ] } ;
                cloneCurves.RemoveAt( i ) ;
              }
              else if ( groupCurves[ 0 ].LocationLine.GetEndPoint( 1 ).DistanceTo( cloneCurves[ i ].LocationLine.GetEndPoint( 0 ) ) < GeometryUtil.Tolerance ) {
                groupCurves = new List<(Line LocationLine, double Width)> { groupCurves[ 0 ], cloneCurves[ i ] } ;
                cloneCurves.RemoveAt( i ) ;
              }
              else if ( groupCurves[ 0 ].LocationLine.GetEndPoint( 1 ).DistanceTo( cloneCurves[ i ].LocationLine.GetEndPoint( 1 ) ) < GeometryUtil.Tolerance ) {
                groupCurves = new List<(Line LocationLine, double Width)> { groupCurves[ 0 ], ( ( cloneCurves[ i ].LocationLine.CreateReversed() as Line )!, cloneCurves[ i ].Width ) } ;
                cloneCurves.RemoveAt( i ) ;
              }
            }
            else {
              if ( groupCurves.Last().LocationLine.GetEndPoint( 1 ).DistanceTo( cloneCurves[ i ].LocationLine.GetEndPoint( 0 ) ) < GeometryUtil.Tolerance ) {
                groupCurves.Add( cloneCurves[ i ] ) ;
                cloneCurves.RemoveAt( i ) ;
              }
              else if ( groupCurves.Last().LocationLine.GetEndPoint( 1 ).DistanceTo( cloneCurves[ i ].LocationLine.GetEndPoint( 1 ) ) < GeometryUtil.Tolerance ) {
                groupCurves.Add( ( ( cloneCurves[ i ].LocationLine.CreateReversed() as Line )!, cloneCurves[ i ].Width ) ) ;
                cloneCurves.RemoveAt( i ) ;
              }
              else if ( groupCurves.First().LocationLine.GetEndPoint( 0 ).DistanceTo( cloneCurves[ i ].LocationLine.GetEndPoint( 0 ) ) < GeometryUtil.Tolerance ) {
                groupCurves.Insert( 0, ( ( cloneCurves[ i ].LocationLine.CreateReversed() as Line )!, cloneCurves[ i ].Width ) ) ;
                cloneCurves.RemoveAt( i ) ;
              }
              else if ( groupCurves.First().LocationLine.GetEndPoint( 0 ).DistanceTo( cloneCurves[ i ].LocationLine.GetEndPoint( 1 ) ) < GeometryUtil.Tolerance ) {
                groupCurves.Insert( 0, cloneCurves[ i ] ) ;
                cloneCurves.RemoveAt( i ) ;
              }
            }
          }
        } while ( count != groupCurves.Count ) ;


        curveLoops.Add( ( CreateCurveLoop( groupCurves.Select( x => x.LocationLine ) ), groupCurves[ 0 ].Width ) ) ;
      }

      return curveLoops ;
    }

    private static CurveLoop CreateCurveLoop( IEnumerable<Line> curves )
    {
      var curveLoop = new CurveLoop() ;
      foreach ( var curve in curves ) {
        curveLoop.Append( curve ) ;
      }

      return curveLoop ;
    }

    private static List<(Line LocationLine, double Width)> ExtendCurves( Document document, List<( Line LocationLine, double Width)> infoCableTrays, List<FamilyInstance> fittings )
    {
      var newInfoCableTrays = new List<(Line LocationLine, double Width)>() ;
      foreach ( var infoCableTray in infoCableTrays ) {
        var points = new List<XYZ> { infoCableTray.LocationLine.GetEndPoint( 0 ), infoCableTray.LocationLine.GetEndPoint( 1 ) } ;
        var newPoints = new List<XYZ>() ;
        foreach ( var fitting in fittings ) {
          var elbow = GetLengthElbow( fitting ) ;
          if ( elbow.Length == 0 )
            continue ;

          if ( Math.Abs( points[ 0 ].DistanceTo( new XYZ( elbow.Point.X, elbow.Point.Y, points[ 0 ].Z ) ) - elbow.Length ) < GeometryUtil.Tolerance || Math.Abs( points[ 1 ].DistanceTo( new XYZ( elbow.Point.X, elbow.Point.Y, points[ 1 ].Z ) ) - elbow.Length ) < GeometryUtil.Tolerance ) {
            newPoints.Add( new XYZ( elbow.Point.X, elbow.Point.Y, points[ 0 ].Z ) ) ;
          }
        }

        points.AddRange( newPoints ) ;
        var locationLine = GetMaxLength( document, points ) ;
        if ( null == locationLine )
          continue ;

        newInfoCableTrays.Add( ( locationLine, infoCableTray.Width ) ) ;
      }

      return newInfoCableTrays ;
    }

    private static (double Length, XYZ Point) GetLengthElbow( FamilyInstance fitting )
    {
      var point = ( fitting.Location as LocationPoint )!.Point ;
      var connectors = GetConnector( fitting ) ;
      return connectors.Count == 0 ? ( 0, point ) : ( point.DistanceTo( connectors[ 0 ].Origin ), point ) ;
    }

    private static List<List<FamilyInstance>> GroupRacks( IList<FamilyInstance> cableTrays )
    {
      var groupRacks = new List<List<FamilyInstance>>() ;
      var cloneCableTrays = cableTrays.ToList() ;
      while ( cloneCableTrays.Any() ) {
        var rack = cloneCableTrays[ 0 ] ;
        cloneCableTrays.RemoveAt( 0 ) ;
        var subRacks = new List<FamilyInstance> { rack } ;

        if ( ! cloneCableTrays.Any() ) {
          groupRacks.Add( subRacks ) ;
        }
        else {
          int count ;
          do {
            count = subRacks.Count ;
            var flag = false ;

            for ( var i = 0 ; i < cloneCableTrays.Count ; i++ ) {
              foreach ( var con in GetConnector( cloneCableTrays[ i ] ) ) {
                if ( GetConnector( subRacks.Last() ).Any( c => con.Origin.DistanceTo( c.Origin ) < GeometryUtil.Tolerance ) ) {
                  subRacks.Add( cloneCableTrays[ i ] ) ;
                  cloneCableTrays.RemoveAt( i ) ;
                  flag = true ;
                }

                if ( flag )
                  break ;
              }

              if ( flag )
                break ;
            }
          } while ( count != subRacks.Count ) ;

          groupRacks.Add( subRacks ) ;
        }
      }

      return groupRacks ;
    }

    private static Line IntersectFitting( Line locationCableTray, IEnumerable<FamilyInstance> fittings, double torance )
    {
      var pointOnLines = fittings.Select( x => GetConnector( x ).Select( y => y.Origin ) ).SelectMany( x => x ).Where( x =>
      {
        var result = locationCableTray.Project( x ) ;
        if ( null == result )
          return false ;

        return result.Distance < torance ;
      } ).ToList() ;

      if ( pointOnLines.Count is > 2 or 0 )
        return locationCableTray ;

      var z = locationCableTray.Origin.Z ;
      if ( pointOnLines.Count == 1 ) {
        return locationCableTray.GetEndPoint( 0 ).DistanceTo( pointOnLines[ 0 ] ) > locationCableTray.GetEndPoint( 1 ).DistanceTo( pointOnLines[ 0 ] ) ? Line.CreateBound( locationCableTray.GetEndPoint( 0 ), new XYZ( pointOnLines[ 0 ].X, pointOnLines[ 0 ].Y, z ) ) : Line.CreateBound( locationCableTray.GetEndPoint( 1 ), new XYZ( pointOnLines[ 0 ].X, pointOnLines[ 0 ].Y, z ) ) ;
      }

      return Line.CreateBound( new XYZ( pointOnLines[ 0 ].X, pointOnLines[ 0 ].Y, z ), new XYZ( pointOnLines[ 1 ].X, pointOnLines[ 1 ].Y, z ) ) ;
    }

    private static Line? GetMaxLength( Document document, IList<XYZ> points )
    {
      if ( ! points.Any() )
        return null ;

      var lines = new List<Line>() ;
      for ( var i = 0 ; i < points.Count - 1 ; i++ ) {
        for ( var j = i + 1 ; j < points.Count ; j++ ) {
          if ( points[ i ].DistanceTo( points[ j ] ) > document.Application.ShortCurveTolerance ) {
            lines.Add( Line.CreateBound( points[ i ], points[ j ] ) ) ;
          }
        }
      }

      return lines.MaxBy( x => x.Length ) ;
    }

    private static bool MEPModelOnPlan( FamilyInstance familyInstance )
    {
      var connectors = GetConnector( familyInstance ) ;
      if ( connectors.Count != 2 )
        return false ;

      return Math.Abs( connectors[ 0 ].Origin.Z - connectors[ 1 ].Origin.Z ) < GeometryHelper.Tolerance ;
    }

    private static List<Connector> GetConnector( FamilyInstance familyInstance )
    {
      var connectorSet = familyInstance.MEPModel?.ConnectorManager?.Connectors ;
      return null == connectorSet ? new List<Connector>() : connectorSet.OfType<Connector>().ToList() ;
    }

    private static void SetParameter( FamilyInstance instance, string parameterName, double value )
    {
      instance.ParametersMap.get_Item( parameterName )?.Set( value ) ;
    }

    /// <summary>
    /// Creat cable rack for Conduit
    /// </summary>
    /// <param name="uiDocument"></param>
    /// <param name="conduit"></param>
    /// <param name="cableRackWidth"></param>
    /// <param name="racks"></param>
    /// <param name="allLimitRackCaches"></param>
    private void CreateCableRackForConduit( UIDocument uiDocument, Conduit conduit, double cableRackWidth, List<FamilyInstance> racks, ICollection<(string routeName, IList<string> rackIds, IList<string> rackDetailCurveIds)> allLimitRackCaches )
    {
      if ( conduit != null ) {
        var document = uiDocument.Document ;

        using var transaction = new SubTransaction( document ) ;
        try {
          transaction.Start() ;
          var location = ( conduit.Location as LocationCurve )! ;
          var line = ( location.Curve as Line )! ;

          var instance = NewRackCommandBase.CreateRackForStraightConduit( uiDocument, conduit, cableRackWidth ) ;

          // check cable tray exists
          if ( NewRackCommandBase.ExistsCableTray( document, instance ) ) {
            transaction.RollBack() ;
            return ;
          }
          
          UpdateMapRacksToRouteName( allLimitRackCaches, instance,conduit ) ;

          racks.Add( instance ) ;

          if ( 1.0 != line.Direction.Z && -1.0 != line.Direction.Z ) {
            var elbows = conduit.GetConnectors().SelectMany( c => c.GetConnectedConnectors() ).OfEnd().Select( c => c.Owner ).OfType<FamilyInstance>() ;
            foreach ( var elbow in elbows ) {
              if ( elbowsToCreate.ContainsKey( elbow.Id ) ) {
                elbowsToCreate[ elbow.Id ].Add( NewRackCommandBase.GetConnectorClosestTo( instance.GetConnectors().ToList(), ( elbow.Location as LocationPoint )!.Point )! ) ;
              }
              else {
                elbowsToCreate.Add( elbow.Id, new List<Connector>() { NewRackCommandBase.GetConnectorClosestTo( instance.GetConnectors().ToList(), ( elbow.Location as LocationPoint )!.Point )! } ) ;
              }
            }
          }

          transaction.Commit() ;
        }
        catch {
          transaction.RollBack() ;
        }
      }
    }

    /// <summary>
    /// Mapping new rack instance to a route name
    /// </summary>
    /// <param name="allLimitRackCaches"></param>
    /// <param name="rack"></param>
    /// /// <param name="routeElement"></param>
    private static void UpdateMapRacksToRouteName(ICollection<(string routeName, IList<string> rackIds, IList<string> rackDetailCurveIds)> allLimitRackCaches, Element rack, Element routeElement)
    {
      var routeName = routeElement.GetRouteName()!;
      
      if ( string.IsNullOrEmpty( routeName ) ) return ;
      
      (string routeName,IList<string> rackIds, IList<string> rackDetailCurveIds)? routeNameAndRacks = allLimitRackCaches.FirstOrDefault( rm => rm.routeName == routeName ) ;
      
      if ( routeNameAndRacks.HasValue ) {
        allLimitRackCaches.Add( ( routeName, new List<string>() { rack.UniqueId }, new List<string>() ) ) ;
      }
      else {
        routeNameAndRacks.Value.rackIds.Add( rack.UniqueId ) ;
      }
    }

    /// <summary>
    /// Create elbow for 2 cable rack
    /// </summary>
    /// <param name="uiDocument"></param>
    /// <param name="elementId"></param>
    /// <param name="connectors"></param>
    /// <param name="racks"></param>
    /// /// <param name="allLimitRackCaches"></param>
    private void CreateElbow( UIDocument uiDocument, ElementId elementId, List<Connector> connectors, List<FamilyInstance> racks, ICollection<(string routeName, IList<string> rackIds, IList<string> rackDetailCurveIds)> allLimitRackCaches )
    {
      var document = uiDocument.Document ;
      using var transaction = new SubTransaction( document ) ;
      try {
        transaction.Start() ;
        var conduit = document.GetElementById<FamilyInstance>( elementId )! ;

        if ( 1.0 == conduit.FacingOrientation.Z || -1.0 == conduit.FacingOrientation.Z || -1.0 == conduit.HandOrientation.Z || 1.0 == conduit.HandOrientation.Z ) {
          return ;
        }

        var location = ( conduit.Location as LocationPoint )! ;
        var instance = NewRackCommandBase.CreateRackForFittingConduit( uiDocument, conduit, location, cableTrayDefaultBendRadius ) ;
        
        // check cable tray exists
        if ( NewRackCommandBase.ExistsCableTray( document, instance ) ) {
          transaction.RollBack() ;
          return ;
        }

        var firstCableRack = connectors.First().Owner ;
        // get cable rack width
        var firstCableRackWidth = firstCableRack.ParametersMap.get_Item( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ) ).AsDouble() ; // TODO may be must change when FamilyType change

        var secondCableRack = connectors.Last().Owner ;
        // get cable rack width
        var secondCableRackWidth = secondCableRack.ParametersMap.get_Item( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ) ).AsDouble() ; // TODO may be must change when FamilyType change

        // set cable rack length
        SetParameter( instance, "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ), firstCableRackWidth >= secondCableRackWidth ? firstCableRackWidth : secondCableRackWidth ) ; // TODO may be must change when FamilyType change

        foreach ( var connector in instance.GetConnectors() ) {
          var otherConnectors = connectors.FindAll( x => ! x.IsConnected && x.Owner.Id != connector.Owner.Id ) ;
          if ( null != otherConnectors ) {
            var connectTo = NewRackCommandBase.GetConnectorClosestTo( otherConnectors, connector.Origin, maxDistanceTolerance ) ;
            if ( connectTo != null ) {
              connector.ConnectTo( connectTo ) ;
            }
          }
        }
        
        UpdateMapRacksToRouteName( allLimitRackCaches,instance,conduit );

        racks.Add( instance ) ;

        transaction.Commit() ;
      }
      catch {
        transaction.RollBack() ;
      }
    }

    /// <summary>
    /// Calculate cable rack width base on sum diameter of route
    /// </summary>
    /// <param name="document"></param>
    /// <param name="subRoute"></param>
    /// <returns></returns>
    private double CalcCableRackWidth( Document document, SubRoute subRoute )
    {
      var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var sumDiameter = subRoute.GetSubRouteGroup().Sum( s => routes.GetSubRoute( s )?.GetDiameter().RevitUnitsToMillimeters() + 10 ) + 120 ;
      var cableTraywidth = 0.6 * sumDiameter ;
      foreach ( var width in cableTrayWidthMapping ) {
        if ( cableTraywidth <= width ) {
          cableTraywidth = width ;
          return cableTraywidth!.Value ;
        }
      }

      return cableTraywidth!.Value ;
    }

    /// <summary>
    /// Calculate cable rack max width
    /// </summary>
    /// <param name="element"></param>
    /// <param name="elements"></param>
    /// <param name="document"></param>
    /// <returns></returns>
    private double CalcCableRackMaxWidth( (MEPCurve, SubRoute) element, IEnumerable<(MEPCurve, SubRoute)> elements, Document document )
    {
      var routeName = element.Item2.Route.RouteName ;
      var routeElements = elements.Where( x => x.Item2.Route.RouteName == routeName ) ;
      var maxWidth = 0.0 ;
      if ( routeMaxWidthCache.ContainsKey( routeName ) ) {
        var elbowsConnected = element.Item1.GetConnectors().SelectMany( c => c.GetConnectedConnectors() ).OfEnd().Select( c => c.Owner ).OfType<FamilyInstance>() ;
        var straightsConnected = element.Item1.GetConnectors().SelectMany( c => c.GetConnectedConnectors() ).OfEnd().Select( c => c.Owner ).OfType<Conduit>() ;
        if ( elbowsConnected.Any() && straightsConnected.Any() && null != element.Item2.PreviousSubRoute && straightsConnected.First().GetSubRouteIndex()!.Value == element.Item2.PreviousSubRoute!.SubRouteIndex ) {
          var key = routeMaxWidthCache[ routeName ].Keys.Where( x => x <= element.Item2.PreviousSubRoute!.SubRouteIndex ).Max() ;
          return routeMaxWidthCache[ routeName ][ key ] ;
        }
        else if ( elbowsConnected.Any() && ( null == element.Item2.PreviousSubRoute || ( null != element.Item2.PreviousSubRoute && straightsConnected.Any() && straightsConnected.First().GetSubRouteIndex()!.Value != element.Item2.PreviousSubRoute!.SubRouteIndex ) ) && ! routeMaxWidthCache[ routeName ].ContainsKey( element.Item2.SubRouteIndex ) ) {
          maxWidth = CalcCableRackWidth( document, element.Item2 ) ;
          routeMaxWidthCache[ routeName ].Add( element.Item2.SubRouteIndex, maxWidth ) ;
          return maxWidth ;
        }
        else {
          var key = routeMaxWidthCache[ routeName ].Keys.Where( x => x <= element.Item2.SubRouteIndex ).Max() ;
          return routeMaxWidthCache[ routeName ][ key ] ;
        }
      }
      else {
        foreach ( var (mepCurve, subRoute) in routeElements ) {
          var cableTraywidth = CalcCableRackWidth( document, subRoute ) ;
          if ( cableTraywidth > maxWidth ) {
            maxWidth = cableTraywidth ;
          }
        }

        Dictionary<int, double> routeWidths = new Dictionary<int, double>() ;
        routeWidths.Add( element.Item2.SubRouteIndex, maxWidth ) ;
        routeMaxWidthCache.Add( routeName, routeWidths ) ;
        return maxWidth ;
      }
    }

    /// <summary>
    /// Calculate cable rack length
    /// </summary>
    /// <param name="routeName"></param>
    /// <param name="elements"></param>
    /// <param name="document"></param>
    /// <returns></returns>
    private double RouteLength( string routeName, IEnumerable<(MEPCurve, SubRoute)> elements, Document document )
    {
      if ( routeLengthCache.ContainsKey( routeName ) ) {
        return routeLengthCache[ routeName ] ;
      }

      var routeLength = elements.Where( x => x.Item2.Route.RouteName == routeName ).Sum( x => ( x.Item1 as Conduit )!.ParametersMap.get_Item( "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( document, "Length" ) ).AsDouble() ) ;

      routeLengthCache.Add( routeName, routeLength ) ;

      return routeLength ;
    }
  }
}