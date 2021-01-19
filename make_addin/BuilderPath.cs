using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arent3d.Architecture
{
  class BuilderPath
  {
    public string InputPath { get; }

    private string? _outputPath = null;

    public BuilderPath( string inputPath )
    {
      InputPath = Path.GetFullPath( inputPath );
    }

    public bool IsClosed { get; private set; }

    public void Close()
    {
      if ( IsClosed ) return;

      _outputPath ??= Path.Combine( Path.GetDirectoryName( InputPath )!, Path.GetFileNameWithoutExtension( InputPath ) + ".addin" ) ;
    }

    public string OutputPath
    {
      get => _outputPath ?? throw new InvalidOperationException( nameof( BuilderPath ) + " is not closed." );
      set => _outputPath = Path.GetFullPath( value );
    }
  }
}
