using System ;

namespace Arent3d.Architecture.Routing.Utils
{
    public static class RouteUtil
    {
        private const char SignJoinRouteName = '_' ;

        public static string GetMainRouteName( string? routeName )
        {
            if ( string.IsNullOrEmpty( routeName ) )
                throw new ArgumentNullException( nameof( routeName ) ) ;

            var array = routeName!.Split( SignJoinRouteName ) ;
            if ( array.Length < 2 )
                throw new FormatException( nameof( routeName ) ) ;

            return string.Join( $"{SignJoinRouteName}", array[ 0 ], array[ 1 ] ) ;
        }
    }
}