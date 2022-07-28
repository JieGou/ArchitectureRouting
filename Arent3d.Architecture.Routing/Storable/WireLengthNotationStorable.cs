using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid("28ac1781-9bfc-4728-b974-e9345ebb8b0c")]
  [StorableVisibility( AppInfo.VendorId )]
  public class WireLengthNotationStorable : StorableBase
  {
    public const string StorableName = "Text Note Of Pick Up" ;
    private const string WireLengthNotationField = "WireLengthNotation" ;
    private const string PickUpNumberSettingField = "PickUpNumberSetting" ;
    public List<WireLengthNotationModel> WireLengthNotationData { get ; set ; }
    public Dictionary<int, PickUpNumberSettingModel> PickUpNumberSettingData { get ; set ; }
    public IReadOnlyList<Level> Levels { get ; }

    /// <summary>
    /// for loading from storage.
    /// </summary>
    /// <param name="owner">Owner element.</param>
    public WireLengthNotationStorable( DataStorage owner ) : base( owner, false )
    {
      Levels = GetAllLevels( owner.Document ) ;
      WireLengthNotationData = new List<WireLengthNotationModel>() ;
      PickUpNumberSettingData = new Dictionary<int, PickUpNumberSettingModel>() ;
    }

    public WireLengthNotationStorable( Document document ) : base( document, false )
    {
      Levels = GetAllLevels( document ) ;
      WireLengthNotationData = new List<WireLengthNotationModel>() ;
      PickUpNumberSettingData = Levels.ToDictionary( x => x.Id.IntegerValue, x => new PickUpNumberSettingModel( x ) ) ;
    }
    
    private static IReadOnlyList<Level> GetAllLevels( Document document )
    {
      var levels = document.GetAllElements<Level>().ToList() ;
      levels.Sort( ( a, b ) => a.Elevation.CompareTo( b.Elevation ) ) ;
      return levels ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      WireLengthNotationData = reader.GetArray<WireLengthNotationModel>( WireLengthNotationField ).ToList() ;
      var pickUpNumberSettingDataSaved = reader.GetArray<PickUpNumberSettingModel>( PickUpNumberSettingField ).ToDictionary( x => x.LevelId ) ;
      PickUpNumberSettingData = Levels.ToDictionary( x => x.Id.IntegerValue, x => pickUpNumberSettingDataSaved.GetOrDefault( x.Id.IntegerValue, () => new PickUpNumberSettingModel( x ) ) ) ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( WireLengthNotationField, WireLengthNotationData ) ;
      PickUpNumberSettingData = Levels.ToDictionary( x => x.Id.IntegerValue, x => PickUpNumberSettingData.GetOrDefault( x.Id.IntegerValue, () => new PickUpNumberSettingModel( x ) ) ) ;
      writer.SetArray( PickUpNumberSettingField, PickUpNumberSettingData.Values.ToList() ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<WireLengthNotationModel>( WireLengthNotationField ) ;
      generator.SetArray<PickUpNumberSettingModel>( PickUpNumberSettingField ) ;
    }

    public override string Name => StorableName ;
  }
}