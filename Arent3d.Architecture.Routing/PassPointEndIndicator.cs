using System ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public readonly struct PassPointEndIndicator : IEquatable<PassPointEndIndicator>, IEndPointIndicator
  {
    public static readonly PassPointEndIndicator InvalidConnectorIndicator = new PassPointEndIndicator( 0, PassPointEndSide.Forward ) ;

    public int ElementId { get ; }
    public PassPointEndSide SideType { get ; }

    public bool IsInvalid => ( ElementId == 0 ) ;

    public PassPointEndIndicator( int elementId, PassPointEndSide sideType )
    {
      ElementId = elementId ;
      SideType = sideType ;
    }

    public FamilyInstance? GetPassPointElement( Document document )
    {
      return document.FindPassPointElement( ElementId ) ;
    }

    public EndPoint? GetEndPoint( Document document, SubRoute subRoute, bool isFrom )
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

    public static ConnectorIndicator Parse( string str )
    {
      return EndPointIndicator.ParseConnectorIndicator( str ) ;
    }
  }
}