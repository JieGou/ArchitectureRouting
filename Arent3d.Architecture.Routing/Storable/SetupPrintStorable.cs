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

    private const string RatioField = "Ratio" ;
    public double Ratio { get ; set ; }

    public SetupPrintStorable( DataStorage owner ) : base( owner, false )
    {
      TitleBlockTypeId = ElementId.InvalidElementId.IntegerValue ;
      Scale = 100 ;
      Ratio = 1d ;
    }

    public SetupPrintStorable( Document document ) : base( document, false )
    {
      TitleBlockTypeId = ElementId.InvalidElementId.IntegerValue ;
      Scale = 100 ;
      Ratio = 1d ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      TitleBlockTypeId = reader.GetSingle<int>( TitleBlockTypeIdField ) ;
      Scale = reader.GetSingle<int>( ScaleField ) ;
      Ratio = reader.GetSingle<double>( RatioField, UnitTypeId.Millimeters ) ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetSingle(TitleBlockTypeIdField, TitleBlockTypeId);
      writer.SetSingle( ScaleField, Scale ) ;
      writer.SetSingle( RatioField, Ratio, UnitTypeId.Millimeters ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetSingle<int>(TitleBlockTypeIdField);
      generator.SetSingle<int>(ScaleField);
      generator.SetSingle<double>(RatioField, SpecTypes.Length);
    }

    public override string Name => StorableName ;
  }
}