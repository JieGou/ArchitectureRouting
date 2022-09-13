using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storages.Attributes ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
  [Schema( "EF7E901A-DEB4-4FD5-AB39-91FCB3D0AE38", nameof( DisplaySettingByGradeModel ) )]
  public class DisplaySettingByGradeModel : IDataModel
  {
    public DisplaySettingByGradeModel()
    {
      GradeMode = string.Empty ;
      Wiring = new DisplaySettingByGradeItemModel() ;
      Symbol = new DisplaySettingByGradeItemModel() ;
      PullBox = new DisplaySettingByGradeItemModel() ;
    }

    public DisplaySettingByGradeModel( string? gradeMode, DisplaySettingByGradeItemModel? wiring,
      DisplaySettingByGradeItemModel? detailSymbol, DisplaySettingByGradeItemModel? pullBox )
    {
      GradeMode = gradeMode ?? string.Empty ;
      Wiring = wiring ?? new DisplaySettingByGradeItemModel() ;
      Symbol = detailSymbol ?? new DisplaySettingByGradeItemModel() ;
      PullBox = pullBox ?? new DisplaySettingByGradeItemModel() ;
    }

    [Field( Documentation = "Grade Mode" )]
    public string GradeMode { get ; set ; }

    [Field( Documentation = "Wiring" )]
    public DisplaySettingByGradeItemModel Wiring { get ; set ; }

    [Field( Documentation = "Detail Symbol" )]
    public DisplaySettingByGradeItemModel Symbol { get ; set ; }

    [Field( Documentation = "Pull Box" )]
    public DisplaySettingByGradeItemModel PullBox { get ; set ; }

    public DisplaySettingByGradeModel Clone() => new ( GradeMode, Wiring, Symbol, PullBox ) ;
  }

  [Schema( "7330D52B-EF2B-4D58-9E99-878F30C4858C", nameof( DisplaySettingByGradeItemModel ) )]
  public class DisplaySettingByGradeItemModel : IDataModel
  {
    public DisplaySettingByGradeItemModel()
    {
      IsEnabled = false ;
      IsVisible = true ;
      HiddenElementIds = new List<string>() ;
    }

    public DisplaySettingByGradeItemModel( bool? isEnabled, bool? isVisible, List<string>? hiddenElementIds = null )
    {
      IsEnabled = isEnabled ?? false ;
      IsVisible = isVisible ?? true ;
      HiddenElementIds = hiddenElementIds ?? new List<string>() ;
    }

    public DisplaySettingByGradeItemModel( bool? isEnabled, List<string>? hiddenElementIds = null )
    {
      IsEnabled = isEnabled ?? false ;
      IsVisible = ! IsEnabled ;
      HiddenElementIds = hiddenElementIds ?? new List<string>() ;
    }

    [Field( Documentation = "IsEnabled" )]
    public bool IsEnabled { get ; set ; }

    [Field( Documentation = "IsVisible" )]
    public bool IsVisible { get ; set ; }

    [Field( Documentation = "Hidden Element Ids" )]
    public List<string> HiddenElementIds { get ; set ; }
  }
}