using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "6c09b1c5-af87-4ca2-9f8e-c0f76562937c" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class ConduitAndDetailCurveStorable : StorableBase
  {
    public const string StorableName = "Conduit And Detail Curve" ;
    private const string ConduitAndDetailCurveField = "ConduitAndDetailCurve" ;
    public List<ConduitAndDetailCurveModel> ConduitAndDetailCurveData { get ; set ; }
    
    public ConduitAndDetailCurveStorable( DataStorage owner ) : base( owner, false )
    {
      ConduitAndDetailCurveData = new List<ConduitAndDetailCurveModel>() ;
    }

    public ConduitAndDetailCurveStorable( Document document ) : base( document, false )
    {
      ConduitAndDetailCurveData = new List<ConduitAndDetailCurveModel>() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      ConduitAndDetailCurveData = reader.GetArray<ConduitAndDetailCurveModel>( ConduitAndDetailCurveField ).ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( ConduitAndDetailCurveField, ConduitAndDetailCurveData ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<ConduitAndDetailCurveModel>( ConduitAndDetailCurveField ) ;
    }

    public override string Name => StorableName ;
  }
}