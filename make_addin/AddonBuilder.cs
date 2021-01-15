using Autodesk.Revit.UI;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Arent3d.Utility ;

namespace Arent3d.Architecture
{
  class AddonBuilder
  {
    private static Dictionary<Guid, TypeDefinition> _processedGuids = new();

    public static IReadOnlyCollection<AddonBuilder> CreateBuilders( BuilderPathCollection collections )
    {
      try {
        return collections.GetAll().Select( group => new AddonBuilder( group.Key, group ) ).EnumerateAll();
      }
      catch ( InvalidAssemblyException e ) {
        throw new Exception( string.Format( ErrorMessages.InputAssemblyIsNotRevitAddinAssembly, e.AssemblyPath ) );
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
    private readonly IReadOnlyCollection<(string AssemblyPath, AssemblyDefinition Assembly)> _assemblies;

    public AddonBuilder( FileInfo outputFile, IEnumerable<(string AssemblyPath, AssemblyDefinition Assembly)> assemblies )
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
        foreach ( var (path, assembly) in _assemblies ) {
          WriteAssembly( xml, path, assembly );
        }
      }
    }

    private void WriteAssembly( XmlWriter xml, string assemblyPath, AssemblyDefinition assembly )
    {
      var vendorId = assembly.GetCustomAttribute<Revit.RevitAddinVendorAttribute>()!.VendorId;
      var vendorDescription = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? string.Empty;

      foreach ( var type in assembly.GetTypes() ) {
        if ( type.GetCustomAttribute<Revit.RevitAddinAttribute>() is not { } attr ) continue;
        var addinType = GetAddInType( type );
        if ( addinType == RevitAddInType.Unknown ) continue;
        if ( true == _processedGuids.TryGetValue( attr.Guid, out var last ) ) {
          throw new Exception( $"Same GUID is duplicated between `{last.FullName}' and `{type.FullName}'." );
        }
        _processedGuids.Add( attr.Guid, type ) ;

        using ( xml.WriteElement( "AddIn" ) ) {
          xml.WriteAttributeString( "Type", addinType.ToString() );

          switch ( addinType ) {
          case RevitAddInType.Command:
            xml.WriteElementString( "Text", type.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? type.Name );
            if ( type.GetCustomAttribute<DescriptionAttribute>() is { } desc ) {
              xml.WriteElementString( "LongDescription", desc.Description );
            }
            break;
          case RevitAddInType.Application:
            xml.WriteElementString( "Name", type.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? type.Name );
            break;
          }
          xml.WriteElementString( "FullClassName", type.FullName );
          xml.WriteElementString( "Assembly", assemblyPath );
          xml.WriteElementString( "AddInId", attr.Guid.ToString() );
          xml.WriteElementString( "VendorId", vendorId );
          xml.WriteElementString( "VendorDescription", vendorDescription );
        }
      }
    }

    private static RevitAddInType GetAddInType( TypeDefinition type )
    {
      foreach ( var ifType in type.Interfaces ) {
        switch ( ifType.InterfaceType.FullName ) {
        case "Autodesk.Revit.UI.IExternalCommand": return RevitAddInType.Command;
        case "Autodesk.Revit.UI.IExternalApplication": return RevitAddInType.Application;
        }
      }

      return RevitAddInType.Unknown;
    }
  }
}
