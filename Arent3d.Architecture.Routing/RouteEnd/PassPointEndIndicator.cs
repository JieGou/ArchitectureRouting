using System ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.RouteEnd
{
  public class PassPointEndIndicator : IEquatable<PassPointEndIndicator>, IEndPointIndicator
  {
    public (Route? Route, SubRoute? SubRoute) ParentBranch( Document document ) => ( null, null ) ;  // PassPointEndIndicator has no parent branch (provisional).

    public int ElementId { get ; }
    public bool IsOneSided => false ;

    public PassPointEndIndicator( int elementId )
    {
      ElementId = elementId ;
    }

    private FamilyInstance? GetPassPointElement( Document document )
    {
      return document.FindPassPointElement( ElementId ) ;
    }

    public EndPointBase? GetEndPoint( Document document, SubRoute subRoute )
    {
      var familyInstance = GetPassPointElement( document ) ;
      if ( null == familyInstance ) return null ;

      return new PassPointEndPoint( subRoute.Route, familyInstance, subRoute.GetReferenceConnector() ) ;
    }

    public double? GetEndPointDiameter( Document document ) => null ;

    public bool IsValid( Document document, bool isFrom )
    {
      return ( null != GetPassPointElement( document ) ) ;
    }

    public void Accept( IEndPointIndicatorVisitor visitor )
    {
      visitor.Visit( this ) ;
    }

    public T Accept<T>( IEndPointIndicatorVisitor<T> visitor )
    {
      return visitor.Visit( this ) ;
    }

    public bool Equals( PassPointEndIndicator other )
    {
      return ElementId == other.ElementId ;
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
      return ElementId ;
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