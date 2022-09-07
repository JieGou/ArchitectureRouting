using System.Text.RegularExpressions ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class HandholeModel
  {
    private const string NumberPattern = @"\d+" ;
    private const string StringPattern = @"[^0-9 ]" ;
    public string Buzaicd { get ; }
    public string Kikaku { get ; }
    public string? SuffixCategoryName { get ; private set ; }
    public string? PrefixCategoryName { get ; private set ; }
    public int Width { get ; private set ; }
    public int Height { get ; private set ; }
    public int Depth { get ; private set ; }
    public string? Name { get ; private set ; }
    private readonly HiroiMasterModel _hiroiMasterModel ;

    public HandholeModel( HiroiMasterModel hiroiMasterModel )
    {
      _hiroiMasterModel = hiroiMasterModel ;
      Buzaicd = hiroiMasterModel.Buzaicd ;
      Kikaku = hiroiMasterModel.Kikaku ;
      InitHandholeSizeFromString( Kikaku ) ;
    }

    public HandholeModel( PullBoxModel pullBoxModel )
    {
      _hiroiMasterModel = new HiroiMasterModel( null, null, pullBoxModel.Buzaicd, null, pullBoxModel.Kikaku, null, null, null, null, null, null, null ) ;
      Buzaicd = pullBoxModel.Buzaicd ;
      Kikaku = pullBoxModel.Kikaku ;
      InitHandholeSizeFromString( Kikaku ) ;
    }

    public PullBoxModel ConvertToPullBoxModel()
    {
      return new PullBoxModel( _hiroiMasterModel ) ;
    }

    private void InitHandholeSizeFromString( string kikaku )
    {
      if ( string.IsNullOrEmpty( kikaku ) ) return ;
      Name = kikaku ;
      var kikakuStrings = kikaku.Split( 'x' ) ;
      if ( kikakuStrings.Length != 3 ) return ;
      Width = TryConvertStringToInt( kikakuStrings[ 0 ] ) ;
      Depth = TryConvertStringToInt( kikakuStrings[ 1 ] ) ;
      Height = TryConvertStringToInt( kikakuStrings[ 2 ] ) ;
      var subName = GetHandholeName( Width, Height ) ;
      Name = string.IsNullOrEmpty( subName ) ? kikaku : $"{kikaku} ({subName})" ;
      SuffixCategoryName = GetHandholeCategoryName( kikakuStrings[ 2 ] ) ;
      PrefixCategoryName = GetHandholeCategoryName( kikakuStrings[ 0 ] ) ;
    }

    private static int TryConvertStringToInt( string value )
    {
      var regex = new Regex( NumberPattern ) ;
      var match = regex.Match( value ) ;
      if ( ! match.Success ) return 0 ;
      return int.TryParse( match.Value, out var result ) ? result : 0 ;
    }

    private static string GetHandholeCategoryName( string value )
    {
      var regex = new Regex( StringPattern ) ;
      var match = regex.Match( value ) ;
      return match.Success ? match.Value : string.Empty ;
    }

    private static string GetHandholeName( int width, int height ) =>
      ( width, height ) switch
      {
        (150, 100) => HandholeSizeNameConstance.HH1,
        (200, 200) => HandholeSizeNameConstance.HH2,
        (300, 300) => HandholeSizeNameConstance.HH3,
        (400, 300) => HandholeSizeNameConstance.HH4,
        (500, 400) => HandholeSizeNameConstance.HH5,
        (600, 400) => HandholeSizeNameConstance.HH6,
        (800, 400) => HandholeSizeNameConstance.HH8,
        (1000, 400) => HandholeSizeNameConstance.HH10,
        _ => string.Empty
      } ;

    private static class HandholeSizeNameConstance
    {
      public const string HH1 = nameof( HH1 ) ;
      public const string HH2 = nameof( HH2 ) ;
      public const string HH3 = nameof( HH3 ) ;
      public const string HH4 = nameof( HH4 ) ;
      public const string HH5 = nameof( HH5 ) ;
      public const string HH6 = nameof( HH6 ) ;
      public const string HH8 = nameof( HH8 ) ;
      public const string HH10 = nameof( HH10 ) ;
    }
  }
}