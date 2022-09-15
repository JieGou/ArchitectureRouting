using Arent3d.Architecture.Routing.Storages.Attributes ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
  [Schema( "1443AD84-3C95-4656-9812-9A3EBFC5636B", nameof( DisplaySettingModel ) )]
  public class DisplaySettingModel : IDataModel
  {
    public DisplaySettingModel()
    {
      IsWiringVisible = true ;
      IsDetailSymbolVisible = true ;
      IsPullBoxVisible = true ;
      IsScheduleVisible = true ;
    }

    public DisplaySettingModel( bool? isWiringVisible, bool? isDetailSymbolVisible, bool? isPullBoxVisible, bool? isScheduleVisible )
    {
      IsWiringVisible = isWiringVisible ?? true ;
      IsDetailSymbolVisible = isDetailSymbolVisible ?? true ;
      IsPullBoxVisible = isPullBoxVisible ?? true ;
      IsScheduleVisible = isScheduleVisible ?? true ;
    }

    [Field( Documentation = "Is Wiring Visible" )]
    public bool IsWiringVisible { get ; set ; }

    [Field( Documentation = "Is Detail Symbol Visible" )]
    public bool IsDetailSymbolVisible { get ; set ; }

    [Field( Documentation = "Is Pull Box Visible" )]
    public bool IsPullBoxVisible { get ; set ; }

    [Field( Documentation = "Is Schedule Visible" )]
    public bool IsScheduleVisible { get ; set ; }

    public DisplaySettingModel Clone() => new(IsWiringVisible, IsDetailSymbolVisible, IsPullBoxVisible, IsScheduleVisible) ;
  }
}