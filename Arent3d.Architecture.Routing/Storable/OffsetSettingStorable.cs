using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "b20a515e-8f67-450e-b312-c5f0ee87b474" )]
  [StorableVisibility( AppInfo.VendorId )]
  public sealed class OffsetSettingStorable : StorableBase, IEquatable<OffsetSettingStorable>
  {
    public const string StorableName = "Offset Setting" ;
    
    private const string OffsetSettingField = "OffsetSetting" ;

    public Dictionary<int, OffsetSettingModel> OffsetSettingsData { get ; private set ; }

    /// <summary>
    /// for loading from storage.
    /// </summary>
    /// <param name="owner">Owner element.</param>
    private OffsetSettingStorable( DataStorage owner ) : base( owner, false )
    {
      OffsetSettingsData = new Dictionary<int, OffsetSettingModel>() ;
    }

    /// <summary>
    /// Called by RouteCache.
    /// </summary>
    /// <param name="document"></param>
    public OffsetSettingStorable( Document document ) : base( document, false )
    {
      OffsetSettingsData = new Dictionary<int, OffsetSettingModel>() ;
    }

    public override string Name => StorableName ;

    protected override void LoadAllFields( FieldReader reader )
    {
      OffsetSettingsData[0] = reader.GetArray<OffsetSettingModel>( OffsetSettingField ).FirstOrDefault() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      OffsetSettingsData[0] = new OffsetSettingModel( 0 ) ;
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