using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "344b7b28-e3fc-479b-98dc-1f37fc73df0f" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class CeedDetailStorable : StorableBase
  {
    public const string StorableName = "Symbol Information Model" ;
    private const string AllCeedDetailModelField = "AllCeedDetailModel" ;
    public List<CeedDetailModel> AllCeedDetailModelData { get ; set ; }

    public CeedDetailStorable( DataStorage owner ) : base( owner, false )
    {
      AllCeedDetailModelData = new List<CeedDetailModel>() ;
    }

    public CeedDetailStorable( Document document ) : base( document, false )
    {
      AllCeedDetailModelData = new List<CeedDetailModel>() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      AllCeedDetailModelData = reader.GetArray<CeedDetailModel>( AllCeedDetailModelField ).ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( AllCeedDetailModelField, AllCeedDetailModelData ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<CeedDetailModel>( AllCeedDetailModelField ) ;
    }

    public override string Name => StorableName ;
  }
}