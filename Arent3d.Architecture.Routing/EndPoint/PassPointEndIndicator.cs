using System ;
using Arent3d.Routing ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.EndPoint
{
  public class PassPointEndIndicator : IEquatable<PassPointEndIndicator>, IEndPointIndicator
  {
    public Route? ParentBranch( Document document ) => null ;  // PassPointEndIndicator has no parent branch (provisional).

    public int ElementId { get ; }
    public PassPointEndSide SideType { get ; }

    public PassPointEndIndicator( int elementId, PassPointEndSide sideType )
    {
      ElementId = elementId ;
      SideType = sideType ;
    }

    private FamilyInstance? GetPassPointElement( Document document )
    {
      return document.FindPassPointElement( ElementId ) ;
    }

    public EndPointBase? GetAutoRoutingEndPoint( Document document, SubRoute subRoute, bool isFrom )
    {
      if ( ( SideType == PassPointEndSide.Forward ) != isFrom ) throw new InvalidOperationException() ;

      var familyInstance = GetPassPointElement( document ) ;
      if ( null == familyInstance ) return null ;

      return new PassPointEndPoint( subRoute.Route, familyInstance, SideType, subRoute.GetReferenceConnector() ) ;
    }

    public bool Equals( PassPointEndIndicator other )
    {
      return ElementId == other.ElementId && SideType == other.SideType ;
    }

    public bool Equals( IEndPointIndicator indicator )
    {
      return indicator is PassPointEndIndicator other && Equals( other ) ;
    }

    public override bool Equals( object? obj )
    {
      return obj is PassPointEndIndicator other && Equals( other ) ;
    }

    public override int GetHashCode()
    {
      unchecked {
        return ( ElementId * 2 ) ^ (int) SideType ;
      }
    }

    public static bool operator ==( PassPointEndIndicator left, PassPointEndIndicator right )
    {
      return left.Equals( right ) ;
    }

    public static bool operator !=( PassPointEndIndicator left, PassPointEndIndicator right )
    {
      return ! left.Equals( right ) ;
    }

    public override string ToString()
    {
      return EndPointIndicator.ToString( this ) ;
    }

    public static PassPointEndIndicator? Parse( string str )
    {
      return EndPointIndicator.ParsePassPointEndIndicator( str ) ;
    }
  }
}