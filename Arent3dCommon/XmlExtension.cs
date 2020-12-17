using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

#nullable enable

namespace Arent3d
{
  public static class XmlExtension
  {
    public static IDisposable WriteElement( this XmlWriter writer, string name )
    {
      writer.WriteStartElement( name );
      return new OnDispose( () => writer.WriteEndElement(), $"{nameof( XmlExtension )}.{nameof( WriteElement )}( {name} )" );
    }

    private class OnDispose : IDisposable
    {
      private readonly Action _action;
      private readonly string _tag;
      private bool _disposed = false;

      public OnDispose( Action action, string tag )
      {
        _action = action;
        _tag = tag;
      }

      public void Dispose()
      {
        if ( _disposed ) return;
        _disposed = true;

        GC.SuppressFinalize( this );

        _action();
      }

      ~OnDispose()
      {
        throw new InvalidOperationException( _tag + " is not disposed explicitly!" );
      }
    }
  }
}
