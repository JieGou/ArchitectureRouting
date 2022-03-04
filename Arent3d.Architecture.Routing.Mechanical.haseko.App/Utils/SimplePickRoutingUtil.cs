using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Utils
{
  internal class SimplePickRoutingUtil
  {
    public static ElementId GetLevelId( Document document, IEnumerable<IEndPoint> endPoints )
    {
      return endPoints.Select( ep => ep.GetLevelId( document ) ).FirstOrDefault( levelId => ElementId.InvalidElementId != levelId ) ?? ElementId.InvalidElementId ;
    }

    public static void SetFromHeightLevelSetting( Document document, ElementId fromLevelId, ElementId toLevelId, ref RoutePropertyTypeList routeChoiceSpec )
    {
      var heightSettingStorable = document.GetHeightSettingStorable() ;

      var nextLevelId = heightSettingStorable.GetNextLevelId( fromLevelId != ElementId.InvalidElementId ? fromLevelId : toLevelId ) ;
      if ( null == nextLevelId ) {
        routeChoiceSpec.FromHeightRangeAsNextCeilingLevel = ( 0, HeightSettingStorable.DefaultMaxLevelDistance.MillimetersToRevitUnits() ) ;
        routeChoiceSpec.FromDefaultHeightAsNextCeilingLevel = HeightSettingModel.DEFAULT_HEIGHT_OF_LEVEL.MillimetersToRevitUnits() ;
      }
      else {
        routeChoiceSpec.FromHeightRangeAsNextCeilingLevel = ( 0, heightSettingStorable.GetDistanceToNextLevel( nextLevelId ).MillimetersToRevitUnits() ) ;
        routeChoiceSpec.FromDefaultHeightAsNextCeilingLevel = heightSettingStorable[ nextLevelId ].HeightOfLevel.MillimetersToRevitUnits() ;
      }
    }
  }
}