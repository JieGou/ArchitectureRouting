using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "7d5caf37-8a4f-4a93-98ae-d4eecc1927c9" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class TextNotePickUpModelStorable : StorableBase
  {
    public const string StorableName = "Text Note Pick Up" ;
    private const string TextNotePickUpField = "TextNotePickUp" ;
    private const string IsPickUpNumberSettingField = "IsPickUpNumberSetting" ;
    public List<TextNotePickUpModel> TextNotePickUpData { get ; set ; }
    public bool IsPickUpNumberSetting { get ; set ; }

    public TextNotePickUpModelStorable( DataStorage owner ) : base( owner, false )
    {
      TextNotePickUpData = new List<TextNotePickUpModel>() ;
      IsPickUpNumberSetting = false ;
    }

    public TextNotePickUpModelStorable( Document document ) : base( document, false )
    {
      TextNotePickUpData = new List<TextNotePickUpModel>() ;
      IsPickUpNumberSetting = false ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      TextNotePickUpData = reader.GetArray<TextNotePickUpModel>( TextNotePickUpField ).ToList() ;
      IsPickUpNumberSetting = reader.GetSingle<bool>( IsPickUpNumberSettingField ) ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( TextNotePickUpField, TextNotePickUpData ) ;
      writer.SetSingle( IsPickUpNumberSettingField, IsPickUpNumberSetting ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<TextNotePickUpModel>( TextNotePickUpField ) ;
      generator.SetSingle<bool>( IsPickUpNumberSettingField ) ;
    }

    public override string Name => StorableName ;
  }
}