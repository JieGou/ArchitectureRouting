using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Arent3d.Architecture
{
  class InvalidCommandLineFormatException : Exception
  {
    public InvalidCommandLineFormatException()
    {
    }

    public InvalidCommandLineFormatException( string message ) : base( message )
    {
    }

    public InvalidCommandLineFormatException( string message, Exception innerException ) : base( message, innerException )
    {
    }

    protected InvalidCommandLineFormatException( SerializationInfo info, StreamingContext context ) : base( info, context )
    {
    }

    public override void GetObjectData( SerializationInfo info, StreamingContext context )
    {
      base.GetObjectData( info, context );
    }
  }
}
