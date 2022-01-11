using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Mechanical ;
using MathLib ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  // 高砂向け. 頂いているデータを使っているため、他では使わないこと.
  internal class TTEUtil
  {
    private static readonly List<(double diameter, double airFlow)> DiameterToAirFlow = new()
    {
      ( 150, 195 ),
      ( 200, 420 ),
      ( 250, 765 ),
      ( 300, 1240 ),
      ( 350, 1870 ),
      ( 400, 2670 ),
      ( 450, 3650 ),
      ( 500, 4820 ),
      ( 550, 6200 ),
      ( 600, 7800 ),
      ( 650, 9600 ),
      ( 700, 11700 ),
    } ;

    public static double ConvertAirflowToDiameterForTTE( double airFlow )
    {
      // TODO : 仮対応、風量が11700m3/h以上の場合はルート径が700にします。
      foreach ( var relation in DiameterToAirFlow ) {
        if ( airFlow <= relation.airFlow ) return relation.diameter ;
      }

      return DiameterToAirFlow.Last().diameter ;
    }

    public static IList<Element> GetAllSpaces( Document document )
    {
      ElementCategoryFilter filter = new(BuiltInCategory.OST_MEPSpaces) ;
      FilteredElementCollector collector = new(document) ;
      IList<Element> spaces = collector.WherePasses( filter ).WhereElementIsNotElementType().ToElements() ;
      return spaces ;
    }

    public static double? GetAirFlowOfSpace( Document document, Vector3d pointInSpace )
    {
      // TODO Spaceではなく, VAVから取得するようにする
      var spaces = GetAllSpaces( document ).OfType<Space>().ToArray() ;
      var targetSpace = spaces.FirstOrDefault( space => space.get_BoundingBox( document.ActiveView ).ToBox3d().Contains( pointInSpace, 0.0 ) ) ;

#if REVIT2019 || REVIT2020
      return targetSpace == null
        ? null
        : UnitUtils.ConvertFromInternalUnits( targetSpace.DesignSupplyAirflow, Autodesk.Revit.DB.DisplayUnitType.DUT_CUBIC_METERS_PER_HOUR ) ;
#else
      return targetSpace == null ? null : UnitUtils.ConvertFromInternalUnits( targetSpace.DesignSupplyAirflow, UnitTypeId.CubicMetersPerHour ) ;
#endif
    }

    public static double ConvertDesignSupplyAirflowFromInternalUnits( double designSupplyAirflowInternalUnits )
    {
#if REVIT2019 || REVIT2020
      return UnitUtils.ConvertFromInternalUnits( designSupplyAirflowInternalUnits, Autodesk.Revit.DB.DisplayUnitType.DUT_CUBIC_METERS_PER_HOUR ) ;
#else
      return UnitUtils.ConvertFromInternalUnits( designSupplyAirflowInternalUnits, UnitTypeId.CubicMetersPerHour ) ;
#endif
    }

    public static double ConvertDesignSupplyAirflowToInternalUnits( double designSupplyAirflow )
    {
#if REVIT2019 || REVIT2020
      return UnitUtils.ConvertToInternalUnits( designSupplyAirflow, Autodesk.Revit.DB.DisplayUnitType.DUT_CUBIC_METERS_PER_HOUR ) ;
#else
      return UnitUtils.ConvertToInternalUnits( designSupplyAirflow, UnitTypeId.CubicMetersPerHour ) ;
#endif
    }

    public static bool IsValidBranchNumber( int branchNumber )
    {
      return branchNumber >= 0 ;
    }

    public static bool IsValidAhuNumber( int ahuNumber )
    {
      return ahuNumber > 0 ;
    }

    public static int? GetAhuNumberByPickedConnector( Connector rootConnector )
    {
      var ahuNumber = 0 ;
      const int limit = 30 ;
      List<Element> visitedElement = new List<Element>() ;
      List<Connector> nextConnectors = new List<Connector>() { rootConnector } ;

      for ( var depth = 0 ; depth < limit ; depth++ ) {
        var currentConnectors = nextConnectors.Select( c => c ).ToList() ;
        nextConnectors.Clear() ;
        foreach ( var currentConnector in currentConnectors ) {
          if ( visitedElement.Any( e => e.Id == currentConnector.Owner.Id ) ) continue ;
          visitedElement.Add( currentConnector.Owner ) ;
          currentConnector.Owner.TryGetProperty( AHUNumberParameter.AHUNumber, out ahuNumber ) ;
          if ( IsValidAhuNumber( ahuNumber ) ) return ahuNumber ;
          var otherConnectors = currentConnector.Owner.GetConnectors().Where( connector => connector.Id != currentConnector.Id ).ToArray() ;
          foreach ( var otherConnector in otherConnectors ) {
            var connectors = otherConnector.GetConnectedConnectors().ToArray() ;
            nextConnectors.AddRange( connectors ) ;
          }
        }
      }

      return IsValidAhuNumber( ahuNumber ) ? ahuNumber : null ;
    }

    public static bool HasValidBranchNumber( Element space )
    {
      var branchNumber = space.GetSpaceBranchNumber() ;
      return IsValidBranchNumber( branchNumber ) ;
    }

    public static bool HasSpecifiedAhuNumber( Element space, int ahuNumber )
    {
      if ( ! space.TryGetProperty( AHUNumberParameter.AHUNumber, out int ahuNumberOfSpace ) ) return false ;
      return ahuNumberOfSpace == ahuNumber ;
    }

    public static string GetNameBase( Element? systemType, Element curveType )
    {
      return systemType?.Name ?? curveType.Category.Name ;
    }

    public static int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      var pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match( k ) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }

    public static bool IsInSpace( BoundingBoxXYZ spaceBox, XYZ vavPosition )
    {
      return spaceBox.ToBox3d().Contains( vavPosition.To3dPoint(), 0.0 ) ;
    }

    // Get the angle between two vectors
    public static double GetAngleBetweenVector( Vector2d rootVec, Vector2d otherVector )
    {
      // return the angle (in radian)
      return Math.Acos( Vector2d.Dot( rootVec, otherVector ) / ( rootVec.magnitude * otherVector.magnitude ) ) ;
    }
  }
}