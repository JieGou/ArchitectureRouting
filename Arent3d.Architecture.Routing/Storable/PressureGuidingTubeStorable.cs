using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "ab91aae7-0176-4b0c-8699-913c795cfd29" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class PressureGuidingTubeStorable : StorableBase
  {
    public const string StorableName = "Pressure Guiding Tube Model" ;
    private const string PressureGuidingTubeModelField = "PressureGuidingTubeModel" ;
    public PressureGuidingTubeModel PressureGuidingTubeModelData { get ; set ; }

    public PressureGuidingTubeStorable( DataStorage owner ) : base( owner, false )
    {
      PressureGuidingTubeModelData = new PressureGuidingTubeModel() ;
    }

    public PressureGuidingTubeStorable( Document document ) : base( document, false )
    {
      PressureGuidingTubeModelData = new PressureGuidingTubeModel() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      PressureGuidingTubeModelData = reader.GetSingle<PressureGuidingTubeModel>( PressureGuidingTubeModelField );
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetSingle( PressureGuidingTubeModelField, PressureGuidingTubeModelData ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetSingle<PressureGuidingTubeModel>( PressureGuidingTubeModelField ) ;
    }

    public override string Name => StorableName ;
  }
}