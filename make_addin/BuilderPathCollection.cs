using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Arent3d.Architecture
{
  class BuilderPathCollection
  {
    private readonly List<BuilderPath> _buildPaths= new();

    private bool _waitingOutput = false;

    public void SetCommandLineArgs( string[] args )
    {
      foreach ( var arg in args ) {
        AddCommandLineArg( arg );
      }

      if ( _waitingOutput ) {
        throw new InvalidCommandLineFormatException( ErrorMessages.CommandLineNoOutputFileIsSpecified );
      }

      CloseLastBuildPath();
    }

    private void AddCommandLineArg( string arg )
    {
      if ( arg == "-o" ) {
        if ( _waitingOutput ) {
          throw new InvalidCommandLineFormatException( ErrorMessages.CommandLineNoOutputFileIsSpecified );
        }
        _waitingOutput = true;
      }
      else if ( _waitingOutput ) {
        if ( GetLastBuildPath() is BuilderPath lastBuildPath ) {
          lastBuildPath.OutputPath = arg;
          CloseLastBuildPath();
        }
        else {
          throw new InvalidCommandLineFormatException( ErrorMessages.CommandLineNoInputFileIsSpecified );
        }
      }
      else {
        CloseLastBuildPath();
        OpenNewBuildPath( arg );
      }
    }

    public IEnumerable<IGrouping<FileInfo, Assembly>> GetAll()
    {
      return _buildPaths.Select( AssemblyAndOutputPath ).GroupBy( tuple => tuple.OutputPath, tuple => tuple.Assembly );
    }

    private static (FileInfo OutputPath, Assembly Assembly) AssemblyAndOutputPath( BuilderPath builderPath )
    {
      var assembly = Assembly.LoadFrom( builderPath.InputPath );
      if ( assembly.GetCustomAttribute<Revit.RevitAddinVendorAttribute>() is null ) {
        throw new InvalidAssemblyException( builderPath.InputPath );
      }

      return (OutputPath: new FileInfo( builderPath.OutputPath ), Assembly: assembly);
    }

    private BuilderPath? GetLastBuildPath()
    {
      if ( 0 == _buildPaths.Count ) return null;

      var builder = _buildPaths[_buildPaths.Count - 1];
      if ( builder.IsClosed ) return null;

      return builder;
    }

    private BuilderPath OpenNewBuildPath( string path )
    {
      if ( null != GetLastBuildPath() ) throw new InvalidOperationException();

      var newBuilder = new BuilderPath( path );
      _buildPaths.Add( newBuilder );
      return newBuilder;
    }

    private void CloseLastBuildPath()
    {
      GetLastBuildPath()?.Close();
    }
  }
}
