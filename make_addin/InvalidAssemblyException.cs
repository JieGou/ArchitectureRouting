using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Arent3d.Architecture
{
  class InvalidAssemblyException : Exception
  {
    public string AssemblyPath { get; }

    public InvalidAssemblyException( string assemblyPath )
    {
      AssemblyPath = assemblyPath;
    }

    public InvalidAssemblyException( string assemblyPath, string message ) : base( message )
    {
      AssemblyPath = assemblyPath;
    }

    public InvalidAssemblyException( string assemblyPath, string message, Exception innerException ) : base( message, innerException )
    {
      AssemblyPath = assemblyPath;
    }

    protected InvalidAssemblyException( SerializationInfo info, StreamingContext context ) : base( info, context )
    {
      AssemblyPath = info.GetString( nameof( AssemblyPath ) );
    }

    public override void GetObjectData( SerializationInfo info, StreamingContext context )
    {
      base.GetObjectData( info, context );

      info.AddValue( nameof( AssemblyPath ), AssemblyPath );
    }
  }
}
