using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

#nullable enable

namespace Arent3d.Architecture
{
  class AddonBuilder
  {
    public static IReadOnlyCollection<AddonBuilder> CreateBuilders( BuilderPathCollection collections )
    {
      try {
        return collections.GetAll().Select( group => new AddonBuilder( group.Key, group ) ).EnumerateAll();
      }
      catch( InvalidAssemblyException e ) {
        throw new Exception( string.Format( ErrorMessages.InputAssemblyIsNotRevitAddinAssembly, e.AssemblyPath) );
      }
      catch ( FileNotFoundException e ) {
        throw new Exception( string.Format( ErrorMessages.InputAssemblyNotFoundFormat, e.FileName ) );
      }
      catch ( FileLoadException e ) {
        throw new Exception( string.Format( ErrorMessages.InputAssemblyWasBadFormat, e.FileName ) );
      }
      catch ( BadImageFormatException e ) {
        throw new Exception( string.Format( ErrorMessages.InputAssemblyWasBadFormat, e.FileName ) );
      }
      catch ( SecurityException e ) {
        throw new Exception( string.Format( ErrorMessages.InputAssemblySecurityErrorFormat, e.Url ) );
      }
    }

    private readonly FileInfo _outputFile;
    private readonly IReadOnlyCollection<Assembly> _assemblies;

    public AddonBuilder( FileInfo outputFile, IEnumerable<Assembly> assemblies )
    {
      _outputFile = outputFile;
      _assemblies = assemblies.EnumerateAll();
    }

    public void Build()
    {
      using var xml = XmlWriter.Create( _outputFile.FullName, new XmlWriterSettings()
      {
         Indent = true,
         CloseOutput = true,
         Encoding = AdditionalEncodings.UTF8NoBOM,
      } );

      xml.WriteStartDocument();
      using ( xml.WriteElement( "RevitAddIns" ) ) {
        foreach ( var assembly in _assemblies ) {
          WriteAssembly( xml, assembly );
        }
      }
    }

    private void WriteAssembly( XmlWriter xml, Assembly assembly )
    {
      var assemblyPath = assembly.Location;
      var vendorId = assembly.GetCustomAttribute<Revit.RevitAddinVendorAttribute>()!.VendorId;
      var vendorDescription = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? string.Empty;

      foreach ( var type in assembly.DefinedTypes ) {
        if ( type.GetCustomAttribute<Revit.RevitAddinAttribute>() is not { } attr ) continue;

        using ( xml.WriteElement( "AddIn" ) ) {
          xml.WriteAttributeString( "Type", attr.Type.ToString() );

          xml.WriteElementString( "Text", attr.Title );
          xml.WriteElementString( "FullClassName", type.FullName );
          xml.WriteElementString( "Assembly", assemblyPath );
          xml.WriteElementString( "AddInId", type.GUID.ToString() );
          xml.WriteElementString( "VendorId", vendorId );
          xml.WriteElementString( "VendorDescription", vendorDescription );
        }
      }
    }
  }
}
