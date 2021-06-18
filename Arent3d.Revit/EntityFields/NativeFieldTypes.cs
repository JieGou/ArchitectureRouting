using System ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Revit.EntityFields
{
  internal static class NativeFieldTypes
  {
    public static bool IsAcceptable( Type type ) => AcceptableTypes.Contains( type ) ;
    public static bool IsAcceptableForKey( Type type ) => AcceptableTypesForKey.Contains( type ) ;

    public static IReadOnlyCollection<Type> AcceptableTypes { get ; } = new HashSet<Type>
    {
      typeof( bool ),
      typeof( byte ),
      typeof( short ),
      typeof( int ),
      typeof( float ),
      typeof( double ),
      typeof( ElementId ),
      typeof( Guid ),
      typeof( string ),
      typeof( XYZ ),
      typeof( UV ),
      typeof( Entity ),
    } ;

    public static IReadOnlyCollection<Type> AcceptableTypesForKey { get ; } = new HashSet<Type>
    {
      typeof( bool ),
      typeof( byte ),
      typeof( short ),
      typeof( int ),
      typeof( ElementId ),
      typeof( Guid ),
      typeof( string ),
    } ;
  }
}