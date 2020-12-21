using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arent3d.Architecture
{
  static class ErrorMessages
  {
    public static string CommandLineNoInputFileIsSpecified { get; } = "-o: No input file is specified before -o option.";
    public static string CommandLineNoOutputFileIsSpecified { get; } = "-o: No output file is specified.";

    public static string InputAssemblyNotFoundFormat { get; } = "Assembly \"{0}\" is not found.";
    public static string InputAssemblyWasBadFormat { get; } = "Assembly \"{0}\" cannot be loaded.";
    public static string InputAssemblySecurityErrorFormat { get; } = "Assembly \"{0}\" cannot be loaded for security reason.";

    public static string InputAssemblyIsNotRevitAddinAssembly { get; } = "Assembly \"{0}\" has no `Arent3d.Revit.RevitAddinVendorAttribute'. `make_addin' cannot process it.";
  }
}
