using Arent3d.Architecture.Routing.Storages.Attributes ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
  [Schema( "407C6820-A094-426D-B54F-F7E5FF6841A7", nameof( DisplaySettingByGradeModel ) )]
  public class DisplaySettingByGradeModel : IDataModel
  {
    public DisplaySettingByGradeModel()
    {
      GradeMode = string.Empty ;
      Wiring = new DisplaySettingByGradeItemModel() ;
      DetailSymbol = new DisplaySettingByGradeItemModel() ;
      PullBox = new DisplaySettingByGradeItemModel() ;
    }

    public DisplaySettingByGradeModel( string? gradeMode, DisplaySettingByGradeItemModel? wiring,
      DisplaySettingByGradeItemModel? detailSymbol, DisplaySettingByGradeItemModel? pullBox )
    {
      GradeMode = gradeMode ?? string.Empty ;
      Wiring = wiring ?? new DisplaySettingByGradeItemModel() ;
      DetailSymbol = detailSymbol ?? new DisplaySettingByGradeItemModel() ;
      PullBox = pullBox ?? new DisplaySettingByGradeItemModel() ;
    }

    [Field( Documentation = "Grade Mode" )]
    public string GradeMode { get ; set ; }

    [Field( Documentation = "Wiring" )]
    public DisplaySettingByGradeItemModel Wiring { get ; set ; }

    [Field( Documentation = "Detail Symbol" )]
    public DisplaySettingByGradeItemModel DetailSymbol { get ; set ; }

    [Field( Documentation = "Pull Box" )]
    public DisplaySettingByGradeItemModel PullBox { get ; set ; }

    public DisplaySettingByGradeModel Clone() => new ( GradeMode, new DisplaySettingByGradeItemModel( Wiring.IsEnabled, Wiring.IsVisible ), new DisplaySettingByGradeItemModel( DetailSymbol.IsEnabled, DetailSymbol.IsVisible ), new DisplaySettingByGradeItemModel( PullBox.IsEnabled, PullBox.IsVisible ) ) ;
  }

  [Schema( "3D28724A-D093-47F8-AF6A-A7510C6C1667", nameof( DisplaySettingByGradeItemModel ) )]
  public class DisplaySettingByGradeItemModel : IDataModel
  {
    public DisplaySettingByGradeItemModel()
    {
      IsEnabled = false ;
      IsVisible = true ;
    }

    public DisplaySettingByGradeItemModel( bool? isEnabled, bool? isVisible )
    {
      IsEnabled = isEnabled ?? false ;
      IsVisible = isVisible ?? true ;
    }

    public DisplaySettingByGradeItemModel( bool? isEnabled )
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