using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "161e47ea-ef08-4d5f-82f9-3a571f592ce4" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class DemoStorable : StorableBase
  {
    public const string StorableName = "Demo Storable" ;
    private const string UniqueIdDetailCurveField = "UniqueIdDetailCurveField" ;
    public List<string> UniqueIdDetailCurveData { get ; set ; }

    public DemoStorable( DataStorage owner ) : base( owner, false )
    {
      UniqueIdDetailCurveData = new List<string>() ;
    }

    public DemoStorable( Document document ) : base( document, false )
    {
      UniqueIdDetailCurveData = new List<string>() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      UniqueIdDetailCurveData = reader.GetArray<string>( UniqueIdDetailCurveField ).ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( UniqueIdDetailCurveField, UniqueIdDetailCurveData ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<string>( UniqueIdDetailCurveField ) ;
    }

    public override string Name => StorableName ;
  }
}