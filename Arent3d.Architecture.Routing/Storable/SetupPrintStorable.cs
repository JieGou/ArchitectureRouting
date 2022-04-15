using System.Runtime.InteropServices ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "1b9157f7-ce27-48aa-953c-b72d2eade3e0" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class SetupPrintStorable : StorableBase
  {
    public const string StorableName = "Setup Print" ;
    
    private const string TitleBlockTypeIdField = "TitleBlockTypeId" ;
    public int TitleBlockTypeId { get ; set ; }
    
    private const string ScaleField = "Scale" ;
    public int Scale { get ; set ; }

    public SetupPrintStorable( DataStorage owner ) : base( owner, false )
    {
      TitleBlockTypeId = ElementId.InvalidElementId.IntegerValue ;
      Scale = 100 ;
    }

    public SetupPrintStorable( Document document ) : base( document, false )
    {
      TitleBlockTypeId = ElementId.InvalidElementId.IntegerValue ;
      Scale = 100 ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      TitleBlockTypeId = reader.GetSingle<int>( TitleBlockTypeIdField ) ;
      Scale = reader.GetSingle<int>( ScaleField ) ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetSingle(TitleBlockTypeIdField, TitleBlockTypeId);
      writer.SetSingle( ScaleField, Scale ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetSingle<int>(TitleBlockTypeIdField);
      generator.SetSingle<int>(ScaleField);
    }

    public override string Name => StorableName ;
  }
}