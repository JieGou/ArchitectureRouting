using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "83A448F4-E120-44E0-A220-F2D3F11B6A09" )]
  [StorableVisibility( AppInfo.VendorId )]
  public sealed class HeightSettingStorable : StorableBase, IEquatable<HeightSettingStorable>
  {
    private const string HeightSettingField = "HeightSetting" ;

    public Dictionary<int, HeightSettingModel> HeightSettingsData { get ; private set ; }
    public List<Level> Levels { get ; private set ; }

    /// <summary>
    /// Get Height settings data by Level object
    /// </summary>
    /// <param name="level"></param>
    public HeightSettingModel this[ Level level ] => HeightSettingsData.GetOrDefault( level.GetValidId().IntegerValue, () => new HeightSettingModel( level ) ) ;

    /// <summary>
    /// Get Height settings data by level Id.
    /// </summary>
    /// <param name="levelId"></param>
    public HeightSettingModel this[ int levelId ]
    {
      get
      {
        var levelIndex = Levels.FindIndex( x => x.GetValidId().IntegerValue == levelId ) ;
        if ( levelIndex < 0 ) throw new KeyNotFoundException() ;
        return this[ Levels[ levelIndex ] ] ;
      }
    }

    /// <summary>
    /// Get Height settings data by level Id.
    /// </summary>
    /// <param name="levelId"></param>
    public HeightSettingModel this[ ElementId levelId ] => this[ levelId.IntegerValue ] ;


    /// <summary>
    /// for loading from storage.
    /// </summary>
    /// <param name="owner">Owner element.</param>
    private HeightSettingStorable( DataStorage owner ) : base( owner, false )
    {
      Levels = new FilteredElementCollector( owner.Document ).OfClass( typeof( Level ) )
                                                             .AsEnumerable()
                                                             .OfType<Level>()
                                                             .ToList() ;
      HeightSettingsData = Levels.ToDictionary( x => x.Id.IntegerValue, x => new HeightSettingModel( x ) ) ;
    }

    public double GetAbsoluteHeight( ElementId levelId, FixedHeightType fixedHeightType, double fixedHeightHeight )
    {
      return this[ levelId ].Elevation.MillimetersToRevitUnits() + fixedHeightHeight ;
    }

    /// <summary>
    /// Called by RouteCache.
    /// </summary>
    /// <param name="document"></param>
    public HeightSettingStorable( Document document ) : base( document, false )
    {
      Levels = new FilteredElementCollector( document ).OfClass( typeof( Level ) )
                                                       .AsEnumerable()
                                                       .OfType<Level>()
                                                       .ToList() ;
      HeightSettingsData = Levels.ToDictionary( x => x.Id.IntegerValue, x => new HeightSettingModel( x ) ) ;
    }

    public override string Name => "Height Setting" ;

    protected override void LoadAllFields( FieldReader reader )
    {
      var dataSaved = reader.GetArray<HeightSettingModel>( HeightSettingField )
                            .ToDictionary( x => x.LevelId, x => x ) ;

      HeightSettingsData = Levels.ToDictionary( x => x.Id.IntegerValue, x => dataSaved.GetOrDefault( x.Id.IntegerValue, () => new HeightSettingModel( x ) ) ) ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      HeightSettingsData = Levels.ToDictionary( x => x.Id.IntegerValue, x => HeightSettingsData.GetOrDefault( x.Id.IntegerValue, () => new HeightSettingModel( x ) ) ) ;

      writer.SetArray( HeightSettingField, HeightSettingsData.Values.ToList() ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<HeightSettingModel>( HeightSettingField ) ;
    }

    public bool Equals( HeightSettingStorable? other )
    {
      if ( other == null ) return false ;
      return HeightSettingsData.Values.SequenceEqual( other.HeightSettingsData.Values, new HeightSettingStorableComparer() ) ;
    }
  }

  public class HeightSettingStorableComparer : IEqualityComparer<HeightSettingModel>
  {
    public bool Equals( HeightSettingModel x, HeightSettingModel y )
    {
      return x.Equals( y ) ;
    }

    public int GetHashCode( HeightSettingModel obj )
    {
      return obj.GetHashCode() ;
    }
  }

}