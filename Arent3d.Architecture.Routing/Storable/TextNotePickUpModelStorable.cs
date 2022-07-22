using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid("28ac1781-9bfc-4728-b974-e9345ebb8b0c")]
  [StorableVisibility( AppInfo.VendorId )]
  public class TextNotePickUpModelStorable : StorableBase
  {
    public const string StorableName = "Text Note Pick Up" ;
    private const string TextNotePickUpField = "TextNotePickUp" ;
    private const string PickUpNumberSettingField = "PickUpNumberSetting" ;
    public List<TextNotePickUpModel> TextNotePickUpData { get ; set ; }
    public List<PickUpNumberSettingModel> PickUpNumberSettingData { get ; set ; }

    public TextNotePickUpModelStorable( DataStorage owner ) : base( owner, false )
    {
      TextNotePickUpData = new List<TextNotePickUpModel>() ;
      PickUpNumberSettingData = new List<PickUpNumberSettingModel>() ;
    }

    public TextNotePickUpModelStorable( Document document ) : base( document, false )
    {
      TextNotePickUpData = new List<TextNotePickUpModel>() ;
      PickUpNumberSettingData = new List<PickUpNumberSettingModel>() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      TextNotePickUpData = reader.GetArray<TextNotePickUpModel>( TextNotePickUpField ).ToList() ;
      PickUpNumberSettingData = reader.GetArray<PickUpNumberSettingModel>( PickUpNumberSettingField ).ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( TextNotePickUpField, TextNotePickUpData ) ;
      writer.SetArray( PickUpNumberSettingField, PickUpNumberSettingData ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<TextNotePickUpModel>( TextNotePickUpField ) ;
      generator.SetArray<PickUpNumberSettingModel>( PickUpNumberSettingField ) ;
    }

    public override string Name => StorableName ;
  }
}