using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "02c5ecef-4b60-4c36-bbf0-61bc7b566034" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class EcoSettingStorable : StorableBase
  {
    public const string StorableName = "EcoSetting Model" ;
    private const string EcoValueField = "EcoValue" ;
      
    public EcoSettingModel EcoSettingData { get ; private set ; }
      
    public EcoSettingStorable( DataStorage owner ) : base( owner, false )
    {
      EcoSettingData = new EcoSettingModel() ;
    }

    public EcoSettingStorable( Document document ) : base( document, false )
    {
      EcoSettingData = new EcoSettingModel() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      var dataSaved = reader.GetSingle<EcoSettingModel>( EcoValueField ) ;
      EcoSettingData = dataSaved ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    { 
      writer.SetSingle(  EcoValueField, EcoSettingData) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    { 
      generator.SetSingle<EcoSettingModel>( EcoValueField  ) ;
    }

    public override string Name => StorableName ;
  }
}