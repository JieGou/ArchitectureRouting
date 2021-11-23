using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "14e49565-23e8-48f2-9873-653f285b6901" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class PickUpStorable : StorableBase
  {
    public const string StorableName = "Pick-Up Model" ;
    private const string AllPickUpModelField = "AllPickUpModel" ;
    private const string AirConditioningPipingField = "AirConditioningPipingModel" ;
    private const string SatellitePlumbingField = "SatellitePlumbingModel" ;
    private const string DuctField = "ConduitModel" ;
    private const string ElectricityField = "ElectricityModel" ;
    private const string OtherField = "OtherModel" ;
    public List<PickUpModel> AllPickUpModelData { get ; set ; }
    public List<PickUpModel> AirConditioningPipingData { get ; set ; }
    public List<PickUpModel> SatellitePlumbingData { get ; set ; }
    public List<PickUpModel> DuctData { get ; set ; }
    public List<PickUpModel> ElectricityData { get ; set ; }
    public List<PickUpModel> OtherData { get ; set ; }

    public PickUpStorable( DataStorage owner ) : base( owner, false )
    {
      AllPickUpModelData = new List<PickUpModel>() ;
      AirConditioningPipingData = new List<PickUpModel>() ;
      SatellitePlumbingData = new List<PickUpModel>() ;
      DuctData = new List<PickUpModel>() ;
      ElectricityData = new List<PickUpModel>() ;
      OtherData = new List<PickUpModel>() ;
    }

    public PickUpStorable( Document document ) : base( document, false )
    {
      AllPickUpModelData = new List<PickUpModel>() ;
      AirConditioningPipingData = new List<PickUpModel>() ;
      SatellitePlumbingData = new List<PickUpModel>() ;
      DuctData = new List<PickUpModel>() ;
      ElectricityData = new List<PickUpModel>() ;
      OtherData = new List<PickUpModel>() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      AllPickUpModelData = reader.GetArray<PickUpModel>( AllPickUpModelField ).ToList() ;
      AirConditioningPipingData = reader.GetArray<PickUpModel>( AirConditioningPipingField ).ToList() ;
      SatellitePlumbingData = reader.GetArray<PickUpModel>( SatellitePlumbingField ).ToList() ;
      DuctData = reader.GetArray<PickUpModel>( DuctField ).ToList() ;
      ElectricityData = reader.GetArray<PickUpModel>( ElectricityField ).ToList() ;
      OtherData = reader.GetArray<PickUpModel>( OtherField ).ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( AllPickUpModelField, AllPickUpModelData ) ;
      writer.SetArray( AirConditioningPipingField, AirConditioningPipingData ) ;
      writer.SetArray( SatellitePlumbingField, SatellitePlumbingData ) ;
      writer.SetArray( DuctField, DuctData ) ;
      writer.SetArray( ElectricityField, ElectricityData ) ;
      writer.SetArray( OtherField, OtherData ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<PickUpModel>( AllPickUpModelField ) ;
      generator.SetArray<PickUpModel>( AirConditioningPipingField ) ;
      generator.SetArray<PickUpModel>( SatellitePlumbingField ) ;
      generator.SetArray<PickUpModel>( DuctField ) ;
      generator.SetArray<PickUpModel>( ElectricityField ) ;
      generator.SetArray<PickUpModel>( OtherField ) ;
    }

    public override string Name => StorableName ;
  }
}