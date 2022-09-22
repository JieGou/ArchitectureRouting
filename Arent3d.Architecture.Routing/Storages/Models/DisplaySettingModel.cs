using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storages.Attributes ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
  [Schema( "F4B42EC4-6BD5-4684-B928-ECE3251F5568", nameof( DisplaySettingModel ) )]
  public class DisplaySettingModel : IDataModel
  {
    public DisplaySettingModel()
    {
      IsWiringVisible = true ;
      IsDetailSymbolVisible = true ;
      IsPullBoxVisible = true ;
      IsScheduleVisible = true ;
      IsLegendVisible = true ;
      HiddenLegendElementIds = new List<string>() ;
    }

    public DisplaySettingModel( bool? isWiringVisible, bool? isDetailSymbolVisible, bool? isPullBoxVisible, bool? isScheduleVisible, bool? isLegendVisible, List<string>? hiddenLegendElementIds )
    {
      IsWiringVisible = isWiringVisible ?? true ;
      IsDetailSymbolVisible = isDetailSymbolVisible ?? true ;
      IsPullBoxVisible = isPullBoxVisible ?? true ;
      IsScheduleVisible = isScheduleVisible ?? true ;
      IsLegendVisible = isLegendVisible ?? true ;
      HiddenLegendElementIds = hiddenLegendElementIds ?? new List<string>() ;
    }

    [Field( Documentation = "Is Wiring Visible" )]
    public bool IsWiringVisible { get ; set ; }

    [Field( Documentation = "Is Detail Symbol Visible" )]
    public bool IsDetailSymbolVisible { get ; set ; }

    [Field( Documentation = "Is Pull Box Visible" )]
    public bool IsPullBoxVisible { get ; set ; }

    [Field( Documentation = "Is Schedule Visible" )]
    public bool IsScheduleVisible { get ; set ; }
    
    [Field( Documentation = "Is Legend Visible" )]
    public bool IsLegendVisible { get ; set ; }
    
    [Field( Documentation = "Hidden Legend Element Ids" )]
    public List<string> HiddenLegendElementIds { get ; set ; }

    public DisplaySettingModel Clone() => new(IsWiringVisible, IsDetailSymbolVisible, IsPullBoxVisible, IsScheduleVisible, IsLegendVisible, HiddenLegendElementIds) ;
  }
}