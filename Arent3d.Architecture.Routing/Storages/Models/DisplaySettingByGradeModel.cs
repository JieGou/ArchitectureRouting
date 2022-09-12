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
    }

    public DisplaySettingByGradeModel( List<DisplaySettingByGradeItemModel>? displaySettingByGradeData )
    {
      DisplaySettingByGradeData = displaySettingByGradeData ?? new List<DisplaySettingByGradeItemModel>() ;
    }

    [Field( Documentation = "Display Setting By Grade Data" )]
    public List<DisplaySettingByGradeItemModel> DisplaySettingByGradeData { get ; set ; }
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
      AirConditionerLegend = new DisplaySettingByGradeItemDetailsModel() ;
    }

    public DisplaySettingByGradeItemModel( string? gradeMode, DisplaySettingByGradeItemDetailsModel? wiring,
      DisplaySettingByGradeItemDetailsModel? detailSymbol, DisplaySettingByGradeItemDetailsModel? pullBox,
      DisplaySettingByGradeItemDetailsModel? airConditionerLegend )
    {
      GradeMode = gradeMode ?? string.Empty ;
      Wiring = wiring ?? new DisplaySettingByGradeItemDetailsModel() ;
      DetailSymbol = detailSymbol ?? new DisplaySettingByGradeItemDetailsModel() ;
      PullBox = pullBox ?? new DisplaySettingByGradeItemDetailsModel() ;
      AirConditionerLegend = airConditionerLegend ?? new DisplaySettingByGradeItemDetailsModel() ;
    }

    [Field( Documentation = "Grade Mode" )]
    public string GradeMode { get ; set ; }

    [Field( Documentation = "Wiring" )]
    public DisplaySettingByGradeItemDetailsModel Wiring { get ; set ; }

    [Field( Documentation = "Detail Symbol" )]
    public DisplaySettingByGradeItemDetailsModel DetailSymbol { get ; set ; }

    [Field( Documentation = "Pull Box" )]
    public DisplaySettingByGradeItemDetailsModel PullBox { get ; set ; }

    [Field( Documentation = "Air Conditioner Legend" )]
    public DisplaySettingByGradeItemDetailsModel AirConditionerLegend { get ; set ; }
  }

  [Schema( "7330D52B-EF2B-4D58-9E99-878F30C4858C", nameof( DisplaySettingByGradeItemDetailsModel ) )]
  public class DisplaySettingByGradeItemDetailsModel : IDataModel
  {
    public DisplaySettingByGradeItemDetailsModel()
    {
      IsEnabled = false ;
      IsVisible = true ;
    }

    public DisplaySettingByGradeItemDetailsModel( bool? isEnabled, bool? isVisible )
    {
      IsEnabled = isEnabled ?? false ;
      IsVisible = isVisible ?? true ;
    }

    public DisplaySettingByGradeItemDetailsModel( bool? isEnabled )
    {
      IsEnabled = isEnabled ?? false ;
      IsVisible = ! IsEnabled ;
    }

    [Field( Documentation = "IsEnabled" )]
    public bool IsEnabled { get ; set ; }

    [Field( Documentation = "IsVisible" )]
    public bool IsVisible { get ; set ; }
  }
}