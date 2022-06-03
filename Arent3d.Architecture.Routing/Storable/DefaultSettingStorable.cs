﻿using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "75f2c2a1-3fe3-45cf-8ea7-bcf083750bfb" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class DefaultSettingStorable : StorableBase
  {
    public const string StorableName = "Default Setting Model" ;
    private const string EcoValueField = "EcoValue" ;
    private const string GradeValueField = "GradeValue" ;
    private const string ImportDwgMappingModelsField = "ImportDwgMappingModels" ;

    public EcoSettingModel EcoSettingData { get ; private set ; }
    
    public GradeSettingModel GradeSettingData { get ; private set ; }
    
    public List<ImportDwgMappingModel> ImportDwgMappingData { get ; set ; }

    public DefaultSettingStorable( DataStorage owner ) : base( owner, false )
    {
      EcoSettingData = new EcoSettingModel() ;
      GradeSettingData = new GradeSettingModel() ;
      ImportDwgMappingData = new List<ImportDwgMappingModel>() ;
    }

    public DefaultSettingStorable( Document document ) : base( document, false )
    {
      EcoSettingData = new EcoSettingModel() ;
      GradeSettingData = new GradeSettingModel() ;
      ImportDwgMappingData = new List<ImportDwgMappingModel>() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      var dataSaved = reader.GetSingle<EcoSettingModel>( EcoValueField ) ;
      EcoSettingData = dataSaved ;
      GradeSettingData = reader.GetSingle<GradeSettingModel>( GradeValueField ) ;
      ImportDwgMappingData = reader.GetArray<ImportDwgMappingModel>( ImportDwgMappingModelsField ).ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetSingle( EcoValueField, EcoSettingData ) ;
      writer.SetSingle( GradeValueField, GradeSettingData ) ;
      writer.SetArray( ImportDwgMappingModelsField, ImportDwgMappingData ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetSingle<EcoSettingModel>( EcoValueField ) ;
      generator.SetSingle<GradeSettingModel>( GradeValueField ) ;
      generator.SetArray<ImportDwgMappingModel>( ImportDwgMappingModelsField ) ;
    }

    public override string Name => StorableName ;
  }
}