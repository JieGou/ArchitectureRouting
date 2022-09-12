using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storages.Attributes ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
  [Schema( "11A646C8-8AF3-4E6C-921E-9CAB42675875", nameof( DisplaySettingByGradeModel ) )]
  public class DisplaySettingByGradeModel : IDataModel
  {
    public DisplaySettingByGradeModel()
    {
      DisplaySettingByGradeData = new List<DisplaySettingByGradeItemModel>() ;
      GradeDisplayMode = string.Empty ;
    }

    public DisplaySettingByGradeModel( List<DisplaySettingByGradeItemModel>? displaySettingByGradeData, string? gradeDisplayMode )
    {
      DisplaySettingByGradeData = displaySettingByGradeData ?? new List<DisplaySettingByGradeItemModel>() ;
      GradeDisplayMode = gradeDisplayMode ?? string.Empty ;
    }

    [Field( Documentation = "Display Setting By Grade Data" )]
    public List<DisplaySettingByGradeItemModel> DisplaySettingByGradeData { get ; set ; }
    
    [Field( Documentation = "Grade Display Mode" )]
    public string GradeDisplayMode { get ; set ; }
  }

  [Schema( "EF7E901A-DEB4-4FD5-AB39-91FCB3D0AE38", nameof( DisplaySettingByGradeItemModel ) )]
  public class DisplaySettingByGradeItemModel : IDataModel
  {
    public DisplaySettingByGradeItemModel()
    {
      GradeMode = string.Empty ;
      Wiring = new DisplaySettingByGradeItemDetailsModel() ;
      DetailSymbol = new DisplaySettingByGradeItemDetailsModel() ;
      PullBox = new DisplaySettingByGradeItemDetailsModel() ;
      Legend = new DisplaySettingByGradeItemDetailsModel() ;
    }

    public DisplaySettingByGradeItemModel( string? gradeMode, DisplaySettingByGradeItemDetailsModel? wiring,
      DisplaySettingByGradeItemDetailsModel? detailSymbol, DisplaySettingByGradeItemDetailsModel? pullBox,
      DisplaySettingByGradeItemDetailsModel? legend )
    {
      GradeMode = gradeMode ?? string.Empty ;
      Wiring = wiring ?? new DisplaySettingByGradeItemDetailsModel() ;
      DetailSymbol = detailSymbol ?? new DisplaySettingByGradeItemDetailsModel() ;
      PullBox = pullBox ?? new DisplaySettingByGradeItemDetailsModel() ;
      Legend = legend ?? new DisplaySettingByGradeItemDetailsModel() ;
    }

    [Field( Documentation = "Grade Mode" )]
    public string GradeMode { get ; set ; }

    [Field( Documentation = "Wiring" )]
    public DisplaySettingByGradeItemDetailsModel Wiring { get ; set ; }

    [Field( Documentation = "Detail Symbol" )]
    public DisplaySettingByGradeItemDetailsModel DetailSymbol { get ; set ; }

    [Field( Documentation = "Pull Box" )]
    public DisplaySettingByGradeItemDetailsModel PullBox { get ; set ; }

    [Field( Documentation = "Legend" )]
    public DisplaySettingByGradeItemDetailsModel Legend { get ; set ; }
  }

  [Schema( "7330D52B-EF2B-4D58-9E99-878F30C4858C", nameof( DisplaySettingByGradeItemDetailsModel ) )]
  public class DisplaySettingByGradeItemDetailsModel : IDataModel
  {
    public DisplaySettingByGradeItemDetailsModel()
    {
      IsEnabled = false ;
      IsVisible = true ;
      HiddenElementIds = new List<string>() ;
    }

    public DisplaySettingByGradeItemDetailsModel( bool? isEnabled, bool? isVisible, List<string>? hiddenElementIds = null )
    {
      IsEnabled = isEnabled ?? false ;
      IsVisible = isVisible ?? true ;
      HiddenElementIds = hiddenElementIds ?? new List<string>() ;
    }

    public DisplaySettingByGradeItemDetailsModel( bool? isEnabled, List<string>? hiddenElementIds = null )
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