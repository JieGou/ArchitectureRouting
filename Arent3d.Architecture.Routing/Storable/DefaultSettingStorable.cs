using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "ef3838b9-efa9-46a5-8939-167c4d692846" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class DefaultSettingStorable : StorableBase
  {
    public const string StorableName = "Default Setting Model" ;
    private const string EcoValueField = "EcoValue" ;
    private const string GradeValueField = "GradeValue" ;
    private const string ImportDwgMappingModelsField = "ImportDwgMappingModels" ;
    private const string CsvFileModelsField = "CsvFileModels" ;

    public EcoSettingModel EcoSettingData { get ; private set ; }
    
    public GradeSettingModel GradeSettingData { get ; private set ; }
    
    public List<ImportDwgMappingModel> ImportDwgMappingData { get ; set ; }
    
    public List<CsvFileModel> CsvFileData { get ; set ; }

    public DefaultSettingStorable( DataStorage owner ) : base( owner, false )
    {
      EcoSettingData = new EcoSettingModel() ;
      GradeSettingData = new GradeSettingModel() ;
      ImportDwgMappingData = new List<ImportDwgMappingModel>() ;
      CsvFileData = new List<CsvFileModel>() ;
    }

    public DefaultSettingStorable( Document document ) : base( document, false )
    {
      EcoSettingData = new EcoSettingModel() ;
      GradeSettingData = new GradeSettingModel() ;
      ImportDwgMappingData = new List<ImportDwgMappingModel>() ;
      CsvFileData = new List<CsvFileModel>() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      var dataSaved = reader.GetSingle<EcoSettingModel>( EcoValueField ) ;
      EcoSettingData = dataSaved ;
      GradeSettingData = reader.GetSingle<GradeSettingModel>( GradeValueField ) ;
      ImportDwgMappingData = reader.GetArray<ImportDwgMappingModel>( ImportDwgMappingModelsField ).ToList() ;
      CsvFileData = reader.GetArray<CsvFileModel>( CsvFileModelsField ).ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetSingle( EcoValueField, EcoSettingData ) ;
      writer.SetSingle( GradeValueField, GradeSettingData ) ;
      writer.SetArray( ImportDwgMappingModelsField, ImportDwgMappingData ) ;
      writer.SetArray( CsvFileModelsField, CsvFileData ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetSingle<EcoSettingModel>( EcoValueField ) ;
      generator.SetSingle<GradeSettingModel>( GradeValueField ) ;
      generator.SetArray<ImportDwgMappingModel>( ImportDwgMappingModelsField ) ;
      generator.SetArray<CsvFileModel>( CsvFileModelsField ) ;
    }

    public override string Name => StorableName ;
  }
}