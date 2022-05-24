using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "161e47ea-ef08-4d5f-82f9-3a571f592ce3" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class BorderTextNoteStorable : StorableBase
  {
    public const string StorableName = "Border Text Note" ;
    private const string BorderTextNoteField = "BorderTextNote" ;
    public List<BorderTextNoteModel> BorderTextNoteData { get ; set ; }

    public BorderTextNoteStorable( DataStorage owner ) : base( owner, false )
    {
      BorderTextNoteData = new List<BorderTextNoteModel>() ;
    }

    public BorderTextNoteStorable( Document document ) : base( document, false )
    {
      BorderTextNoteData = new List<BorderTextNoteModel>() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      BorderTextNoteData = reader.GetArray<BorderTextNoteModel>( BorderTextNoteField ).ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( BorderTextNoteField, BorderTextNoteData ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<BorderTextNoteModel>( BorderTextNoteField ) ;
    }

    public override string Name => StorableName ;
  }
}