using Arent3d.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Arent3d.Architecture.Routing.Extensions;
using Arent3d.Architecture.Routing.Storable.Model;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid("83A448F4-E120-44E0-A220-F2D3F11B6A09")]
  [StorableVisibility(AppInfo.VendorId)]
  public sealed class HeightSettingStorable : StorableBase, IEquatable<HeightSettingStorable>
  {
    private const string HEIGHT_SETTING_FIELD = "HeightSetting";

    public Dictionary<int, HeightSettingModel> HeightSettingsData { get; set; }
    public List<Level> Levels { get; set; }

    public HeightSettingModel this[Level level] => HeightSettingsData.GetOrDefault(level.Id.IntegerValue, new HeightSettingModel(level));


    /// <summary>
    /// for loading from storage.
    /// </summary>
    /// <param name="owner">Owner element.</param>
    private HeightSettingStorable( DataStorage owner ) : base(owner, false)
    {
      Levels = new FilteredElementCollector(owner.Document).OfClass(typeof(Level))
                                                           .AsEnumerable()
                                                           .OfType<Level>()
                                                           .ToList();
      HeightSettingsData = Levels.ToDictionary(x => x.Id.IntegerValue, x => new HeightSettingModel(x));
    }

    /// <summary>
    /// Called by RouteCache.
    /// </summary>
    /// <param name="document"></param>
    public HeightSettingStorable( Document document ) : base(document, false)
    {
      Levels = new FilteredElementCollector(document).OfClass(typeof(Level))
                                                     .AsEnumerable()
                                                     .OfType<Level>()
                                                     .ToList();
      HeightSettingsData = Levels.ToDictionary(x => x.Id.IntegerValue, x => new HeightSettingModel(x));
    }

    public override string Name => "Height Setting";

    protected override void LoadAllFields( FieldReader reader )
    {
      var dataSaved = reader.GetArray<HeightSettingModel>(HEIGHT_SETTING_FIELD)
                           .ToDictionary(x => x.LevelId, x => x);

      HeightSettingsData = Levels.ToDictionary(x => x.Id.IntegerValue, x => dataSaved.GetOrDefault(x.Id.IntegerValue, new HeightSettingModel(x)));
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      HeightSettingsData = Levels.ToDictionary(x => x.Id.IntegerValue, x => HeightSettingsData.GetOrDefault(x.Id.IntegerValue, new HeightSettingModel(x)));

      writer.SetArray(HEIGHT_SETTING_FIELD, HeightSettingsData.Values.ToList());
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<HeightSettingModel>(HEIGHT_SETTING_FIELD);
    }

    public bool Equals( HeightSettingStorable other )
    {
      if (other == null || other.HeightSettingsData == null) return false;

      return Enumerable.SequenceEqual(HeightSettingsData.Values, other.HeightSettingsData.Values, new HeightSettingStorableComparer());
    }
  }

  public class HeightSettingStorableComparer : IEqualityComparer<HeightSettingModel>
  {
    public bool Equals( HeightSettingModel x, HeightSettingModel y )
    {
      return x.Equals(y);
    }

    public int GetHashCode( HeightSettingModel obj )
    {
      return obj.GetHashCode();
    }
  }

}
