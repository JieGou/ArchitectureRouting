using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "2294442b-f6ba-4568-a48d-11b557d8f975" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class CeedStorable : StorableBase
  {
    public const string StorableName = "CeeD Model" ;
    private const string CeedModelField = "CeeDModel" ;
    private const string CeedModelUsedField = "CeeDModelUsed" ;
    private const string IsShowCeedModelNumberField = "IsShowCeedModelNumber" ;
    private const string ConnectorFamilyUploadField = "ConnectorFamilyUpload" ;
    private const string IsShowOnlyUsingCodeField = "IsShowOnlyUsingCode" ;
    private const string OldCeedModelField = "OldCeedModel" ;

    public List<CeedModel> CeedModelData { get ; set ; }
    public List<CeedModel> CeedModelUsedData { get ; set ; }
    public bool IsShowCeedModelNumber { get ; set ; }
    public List<string> ConnectorFamilyUploadData { get ; set ; }
    public bool IsShowOnlyUsingCode { get ; set ; }
    
    public List<CeedModel> OldCeedModelData { get ; set ; }

    public CeedStorable( DataStorage owner ) : base( owner, false )
    {
      CeedModelData = new List<CeedModel>() ;
      CeedModelUsedData = new List<CeedModel>() ;
      ConnectorFamilyUploadData = new List<string>() ;
      OldCeedModelData = new List<CeedModel>() ;
    }

    public CeedStorable( Document document ) : base( document, false )
    {
      CeedModelData = new List<CeedModel>() ;
      CeedModelUsedData = new List<CeedModel>() ;
      ConnectorFamilyUploadData = new List<string>() ;
      OldCeedModelData = new List<CeedModel>() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      CeedModelData = reader.GetArray<CeedModel>( CeedModelField ).ToList() ;
      CeedModelUsedData = reader.GetArray<CeedModel>( CeedModelUsedField ).ToList() ;
      IsShowCeedModelNumber = reader.GetSingle<bool>( IsShowCeedModelNumberField ) ;
      ConnectorFamilyUploadData = reader.GetArray<string>( ConnectorFamilyUploadField ).ToList() ;
      IsShowOnlyUsingCode = reader.GetSingle<bool>( IsShowOnlyUsingCodeField ) ;
      OldCeedModelData = reader.GetArray<CeedModel>( OldCeedModelField ).ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( CeedModelField, CeedModelData ) ;
      writer.SetArray( CeedModelUsedField, CeedModelUsedData ) ;
      writer.SetSingle(  IsShowCeedModelNumberField, IsShowCeedModelNumber) ;
      writer.SetArray( ConnectorFamilyUploadField, ConnectorFamilyUploadData ) ;
      writer.SetSingle(  IsShowOnlyUsingCodeField, IsShowOnlyUsingCode) ;
      writer.SetArray( OldCeedModelField, OldCeedModelData ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<CeedModel>( CeedModelField ) ;
      generator.SetArray<CeedModel>( CeedModelUsedField ) ;
      generator.SetSingle<bool>( IsShowCeedModelNumberField  ) ;
      generator.SetArray<string>( ConnectorFamilyUploadField ) ;
      generator.SetSingle<bool>( IsShowOnlyUsingCodeField  ) ;
      generator.SetArray<CeedModel>( OldCeedModelField ) ;
    }

    public override string Name => StorableName ;
  }
}