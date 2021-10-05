using Arent3d.Architecture.Routing.Storable ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.Exceptions ;

namespace Arent3d.Architecture.Routing.Extensions
{
  public static class DocumentExtensions
  {
    /// <summary>
    /// Get Height settings data from snoop DB. <br />
    /// If there is no data, it is returned default settings
    /// </summary>
    /// <param name="document">current document of Revit</param>
    /// <returns>Height settings data was stored in snoop DB</returns>
    public static HeightSettingStorable GetHeightSettingStorable( this Document document )
    {
      try {
        return HeightSettingStorableCache.Get( document ).FindOrCreate( HeightSettingStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new HeightSettingStorable( document ) ;
      }
    }
  }
}