using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "1b36f973-89fc-426a-8350-5f83244abc5f" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class CeedStorable : StorableBase
  {
    public const string StorableName = "CeeD Model" ;
    private const string CeedModelField = "CeeDModel" ;
    private const string CeedModelUsedField = "CeeDModelUsed" ;
    private const string CategoriesWithCeedCodeField = "CategoriesWithCeedCode" ;
    private const string CategoriesWithoutCeedCodeField = "CategoriesWithoutCeedCode" ;

    public List<CeedModel> CeedModelData { get ; set ; }
    public List<CeedModel> CeedModelUsedData { get ; set ; }
    private List<CategoryModel> CategoriesWithCeedCode { get ; set ; }
    private List<CategoryModel> CategoriesWithoutCeedCode { get ; set ; }

    public CeedStorable( DataStorage owner ) : base( owner, false )
    {
      CeedModelData = new List<CeedModel>() ;
      CeedModelUsedData = new List<CeedModel>() ;
      CategoriesWithCeedCode = new List<CategoryModel>() ;
      CategoriesWithoutCeedCode = new List<CategoryModel>() ;
    }

    public CeedStorable( Document document ) : base( document, false )
    {
      CeedModelData = new List<CeedModel>() ;
      CeedModelUsedData = new List<CeedModel>() ;
      CategoriesWithCeedCode = new List<CategoryModel>() ;
      CategoriesWithoutCeedCode = new List<CategoryModel>() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      CeedModelData = reader.GetArray<CeedModel>( CeedModelField ).ToList() ;
      CeedModelUsedData = reader.GetArray<CeedModel>( CeedModelUsedField ).ToList() ;
      CategoriesWithCeedCode = reader.GetArray<CategoryModel>( CategoriesWithCeedCodeField ).ToList() ;
      CategoriesWithoutCeedCode = reader.GetArray<CategoryModel>( CategoriesWithoutCeedCodeField ).ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( CeedModelField, CeedModelData ) ;
      writer.SetArray( CeedModelUsedField, CeedModelUsedData ) ;
      writer.SetArray( CategoriesWithCeedCodeField, CategoriesWithCeedCode ) ;
      writer.SetArray( CategoriesWithoutCeedCodeField, CategoriesWithoutCeedCode ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<CeedModel>( CeedModelField ) ;
      generator.SetArray<CeedModel>( CeedModelUsedField ) ;
      generator.SetArray<CategoryModel>( CategoriesWithCeedCodeField ) ;
      generator.SetArray<CategoryModel>( CategoriesWithoutCeedCodeField ) ;
    }

    public override string Name => StorableName ;
  }
}