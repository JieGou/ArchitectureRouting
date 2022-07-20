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
    private const string PickUpNumberSettingOfLevelsField = "PickUpNumberSettingOfLevels" ;
    public List<TextNotePickUpModel> TextNotePickUpData { get ; set ; }
    public List<PickUpNumberSettingModel> PickUpNumberSettingOfLevels { get ; set ; }

    public TextNotePickUpModelStorable( DataStorage owner ) : base( owner, false )
    {
      TextNotePickUpData = new List<TextNotePickUpModel>() ;
      PickUpNumberSettingOfLevels = new List<PickUpNumberSettingModel>() ;
    }

    public TextNotePickUpModelStorable( Document document ) : base( document, false )
    {
      TextNotePickUpData = new List<TextNotePickUpModel>() ;
      PickUpNumberSettingOfLevels = new List<PickUpNumberSettingModel>() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      TextNotePickUpData = reader.GetArray<TextNotePickUpModel>( TextNotePickUpField ).ToList() ;
      PickUpNumberSettingOfLevels = reader.GetArray<PickUpNumberSettingModel>( PickUpNumberSettingOfLevelsField ).ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( TextNotePickUpField, TextNotePickUpData ) ;
      writer.SetArray( PickUpNumberSettingOfLevelsField, PickUpNumberSettingOfLevels ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<TextNotePickUpModel>( TextNotePickUpField ) ;
      generator.SetArray<PickUpNumberSettingModel>( PickUpNumberSettingOfLevelsField ) ;
    }

    public override string Name => StorableName ;
  }
}