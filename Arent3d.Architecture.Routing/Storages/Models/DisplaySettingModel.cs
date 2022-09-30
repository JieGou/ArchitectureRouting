using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Storages.Attributes ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
  [Schema( "842FBF11-C465-41C2-B8BC-790F81A774A0", nameof( DisplaySettingModel ) )]
  public class DisplaySettingModel : NotifyPropertyChanged, IDataModel
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

    public DisplaySettingModel( bool? isWiringVisible, bool? isDetailSymbolVisible, bool? isPullBoxVisible, bool? isScheduleVisible, bool? isLegendVisible, 
      List<string>? hiddenLegendElementIds, string? gradeOption, bool isSaved )
    {
      IsWiringVisible = isWiringVisible ?? true ;
      IsDetailSymbolVisible = isDetailSymbolVisible ?? true ;
      IsPullBoxVisible = isPullBoxVisible ?? true ;
      IsScheduleVisible = isScheduleVisible ?? true ;
      IsLegendVisible = isLegendVisible ?? true ;
      HiddenLegendElementIds = hiddenLegendElementIds ?? new List<string>() ;
      GradeOption = gradeOption ?? GradeOptions[ 0 ] ;
      IsSaved = isSaved ;
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

    private string? _gradeOption ;
    [Field( Documentation = "Grade Option" )]
    public string GradeOption
    {
      get => _gradeOption ??= GradeOptions.First() ;
      set
      {
        _gradeOption = value ;
        OnPropertyChanged();
      }
    }

    public List<string> GradeOptions => new List<string>
    {
      "簡易",
      "詳細"
    } ;
    
    [Field( Documentation = "Mark Saved" )]
    public bool IsSaved { get ; set ; }

    public DisplaySettingModel Clone() => new(IsWiringVisible, IsDetailSymbolVisible, IsPullBoxVisible, IsScheduleVisible, IsLegendVisible, HiddenLegendElementIds, GradeOption, IsSaved) ;
  }
}