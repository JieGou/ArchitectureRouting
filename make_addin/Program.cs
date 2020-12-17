using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Arent3d.Architecture
{
  class Program
  {
    static void Main( string[] args )
    {
      if ( 0 == args.Length ) {
        Console.WriteLine( @"usage: make_addin (<assembly_path> [-o <output_path>])...

    <assembly_path>: Paths of .NET assembly with `Arent3d.Revit.RevitAddinVendorAttribute'. Command surveys it and find `Arent3d.Revit.RevitAddinAttribute's and builds into one addin files.
    -o <output_path>: Path of output addin file. When no -o option is passed, addin file is built into the same directory and is named `<assembly name>.addin'. This must be passed for each <assembly_path>, but when multiple <assembly_path> has same output addin path, all is merged into one addin file." );
        return;
      }

      try {
        var collections = new BuilderPathCollection();
        collections.SetCommandLineArgs( args );

        foreach ( var builder in AddonBuilder.CreateBuilders( collections ) ) {
          builder.Build();
        }
      }
      catch ( Exception e ) {
        Console.Error.WriteLine( e.Message );
      }
    }
  }
}
