using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "c09d4b3c-19ad-402e-8b49-059b7b20377b" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class RegisterSymbolStorable : StorableBase
  {
    public const string StorableName = "Register Symbol" ;
    private const string BrowseFolderPathField = "BrowseFolderPath" ;
    private const string FolderSelectedPathField = "FolderSelectedPath" ;
    
    public string BrowseFolderPath { get ; set ; }
    public string FolderSelectedPath { get ; set ; }

    private RegisterSymbolStorable( DataStorage owner ) : base( owner, false )
    {
      BrowseFolderPath = string.Empty ;
      FolderSelectedPath = string.Empty ;
    }

    public RegisterSymbolStorable( Document document ) : base( document, false )
    {
      BrowseFolderPath = string.Empty ;
      FolderSelectedPath = string.Empty ;
    }

    public override string Name => StorableName ;


    protected override void LoadAllFields( FieldReader reader )
    {
      BrowseFolderPath = reader.GetSingle<string>( BrowseFolderPathField ) ;
      FolderSelectedPath = reader.GetSingle<string>( FolderSelectedPathField ) ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetSingle( BrowseFolderPathField, BrowseFolderPath ) ;
      writer.SetSingle( FolderSelectedPathField, FolderSelectedPath ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetSingle<string>( BrowseFolderPathField ) ;
      generator.SetSingle<string>( FolderSelectedPathField ) ;
    }
  }
}