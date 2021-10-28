using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "83A448F4-E120-44E0-A220-F2D3F11B6A09" )]
  [StorableVisibility( AppInfo.VendorId )]
  public sealed class OffsetSettingStorable : StorableBase, IEquatable<OffsetSettingStorable>
  {
    public const string StorableName = "Offset Setting" ;
    private const double DefaultMaxLevelDistance = 100000 ; // max level distance
    
    private const string OffsetSettingField = "OffsetSetting" ;

    public Dictionary<int, OffsetSettingModel> OffsetSettingsData { get ; private set ; }
    public IReadOnlyList<Level> Levels { get ; }

    /// <summary>
    /// Get Height settings data by Level object
    /// </summary>
    /// <param name="level"></param>
    public OffsetSettingModel this[ Level level ] => OffsetSettingsData.GetOrDefault( level.GetValidId().IntegerValue, () => new OffsetSettingModel( level ) ) ;

    /// <summary>
    /// Get Height settings data by level Id.
    /// </summary>
    /// <param name="levelId"></param>
    public OffsetSettingModel this[ int levelId ]
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
    public OffsetSettingModel this[ ElementId levelId ] => this[ levelId.IntegerValue ] ;


    /// <summary>
    /// for loading from storage.
    /// </summary>
    /// <param name="owner">Owner element.</param>
    private OffsetSettingStorable( DataStorage owner ) : base( owner, false )
    {
      Levels = GetAllLevels( owner.Document ) ;
      OffsetSettingsData = new Dictionary<int, OffsetSettingModel>() ;
    }

    /// <summary>
    /// Called by RouteCache.
    /// </summary>
    /// <param name="document"></param>
    public OffsetSettingStorable( Document document ) : base( document, false )
    {
      Levels = GetAllLevels( document ) ;
      OffsetSettingsData = Levels.ToDictionary( x => x.Id.IntegerValue, x => new OffsetSettingModel( x ) ) ;
    }

    private static IReadOnlyList<Level> GetAllLevels( Document document )
    {
      var levels = document.GetAllElements<Level>().ToList() ;
      levels.Sort( ( a, b ) => a.Elevation.CompareTo( b.Elevation ) ) ;
      return levels ;
    }

    public override string Name => StorableName ;

    public double GetAbsoluteHeight( ElementId levelId, FixedHeightType fixedHeightType, double fixedHeightHeight )
    {
      return this[ levelId ].Elevation.MillimetersToRevitUnits() + fixedHeightHeight ;
    }

    public double GetDistanceToNextLevel( ElementId levelId )
    {
      var index = Levels.FindIndex( level => level.GetValidId() == levelId ) ;
      if ( index < 0 || Levels.Count - 1 <= index ) return DefaultMaxLevelDistance ;

      return this[ Levels[ index + 1 ] ].Elevation - this[ Levels[ index ] ].Elevation ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      var dataSaved = reader.GetArray<OffsetSettingModel>( OffsetSettingField ).ToDictionary( x => x.LevelId ) ;

      OffsetSettingsData = Levels.ToDictionary( x => x.Id.IntegerValue, x => dataSaved.GetOrDefault( x.Id.IntegerValue, () => new OffsetSettingModel( x ) ) ) ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      OffsetSettingsData = Levels.ToDictionary( x => x.Id.IntegerValue, x => OffsetSettingsData.GetOrDefault( x.Id.IntegerValue, () => new OffsetSettingModel( x ) ) ) ;

      writer.SetArray( OffsetSettingField, OffsetSettingsData.Values.ToList() ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<OffsetSettingModel>( OffsetSettingField ) ;
    }

    public bool Equals( OffsetSettingStorable? other )
    {
      if ( other == null ) return false ;
      return OffsetSettingsData.Values.OrderBy( x => x.LevelId ).SequenceEqual( other.OffsetSettingsData.Values.OrderBy( x => x.LevelId ), new OffsetSettingStorableComparer() ) ;
    }
  }

  public class OffsetSettingStorableComparer : IEqualityComparer<OffsetSettingModel>
  {
    public bool Equals( OffsetSettingModel x, OffsetSettingModel y )
    {
      return x.Equals( y ) ;
    }

    public int GetHashCode( OffsetSettingModel obj )
    {
      return obj.GetHashCode() ;
    }
  }

}