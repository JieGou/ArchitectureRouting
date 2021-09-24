using System.Linq ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
namespace Arent3d.Architecture.Routing.Extensions
{
  public static class DocumentExtensions
  {
    /// <summary>
    /// Get Height settings data from snoop DB. <br />
    /// If there is no data, it is returned default settings
    /// </summary>
    /// <param name="document">current document of Revit</param>
    /// <returns>Height settings data was storaged in snoop DB</returns>
    public static HeightSettingStorable GetHeightSettingStorable( this Document document )
    {
      return document.GetAllStorables<HeightSettingStorable>()
                     .AsEnumerable()
                     .DefaultIfEmpty( new HeightSettingStorable( document ) )
                     .First() ;
    }

  }
}