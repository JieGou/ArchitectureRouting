using System ;
using System.IO ;
using System.Runtime.Serialization.Formatters.Binary ;

namespace Arent3d.Architecture.Routing.Utils
{
  public static class ObjectExtension
  {
    public static T DeepCopy<T>(this T obj )
    {
      if ( ! typeof( T ).IsSerializable ) {
        throw new Exception( "The source object must be serializable" ) ;
      }

      if ( ReferenceEquals( obj, null ) ) {
        throw new Exception( "The source object must not be null" ) ;
      }

      using var memoryStream = new MemoryStream() ;
      var formatter = new BinaryFormatter() ;

      formatter.Serialize( memoryStream, obj ) ;

      memoryStream.Seek( 0, SeekOrigin.Begin ) ;

      var result = (T)formatter.Deserialize( memoryStream ) ;

      memoryStream.Close() ;

      return result ;
    }
  }
}