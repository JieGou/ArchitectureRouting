using System.Text.RegularExpressions ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class PullBoxModel
  {
    private const string NumberPattern = @"\d+" ;
    public string Buzaicd { get ; }
    public string Kikaku { get ; }
    public int Width { get ; private set ; }
    public int Height { get ; private set ; }
    public int Depth { get ; private set ; }

    public string? Name { get ; private set ; }

    public PullBoxModel( HiroiMasterModel hiroiMasterModel )
    {
      Buzaicd = hiroiMasterModel.Buzaicd ;
      Kikaku = hiroiMasterModel.Kikaku ;
      InitPullBoxSizeFromString( Kikaku ) ;
    }

    private void InitPullBoxSizeFromString( string kikaku )
    {
      if ( string.IsNullOrEmpty( kikaku ) ) return ;
      var kikakuStrings = kikaku.Split( 'x' ) ;
      if ( kikakuStrings.Length != 3 ) return ;
      Width = TryConvertStringToInt( kikakuStrings[ 0 ] ) ;
      Depth = TryConvertStringToInt( kikakuStrings[ 1 ] ) ;
      Height = TryConvertStringToInt( kikakuStrings[ 2 ] ) ;
      var subName = GetPullBoxName( Width, Height ) ;
      Name = string.IsNullOrEmpty( subName )? kikaku: $"{kikaku} ({subName})" ;
    }

    private static int TryConvertStringToInt( string value )
    {
      var regex = new Regex( NumberPattern ) ;
      var match = regex.Match( value ) ;

      if ( ! match.Success ) return 0 ;
      return int.TryParse( match.Value, out var result ) ? result : 0 ;
    }

    private static string GetPullBoxName( int width, int height ) =>
      ( width, height ) switch
      {
        (<= 150, <= 100) => PullBoxSizeNameConstance.PB1,
        (<= 200, <= 200) => PullBoxSizeNameConstance.PB2,
        (<= 300, <= 300) => PullBoxSizeNameConstance.PB3,
        (<= 400, <= 300) => PullBoxSizeNameConstance.PB4,
        (<= 500, <= 400) => PullBoxSizeNameConstance.PB5,
        (<= 600, <= 400) => PullBoxSizeNameConstance.PB6,
        (<= 800, <= 400) => PullBoxSizeNameConstance.PB8,
        (<= 1000, <= 400) => PullBoxSizeNameConstance.PB10,
        _ => string.Empty
      } ;

    private static class PullBoxSizeNameConstance
    {
      public const string PB1 = nameof( PB1 ) ;
      public const string PB2 = nameof( PB2 ) ;
      public const string PB3 = nameof( PB3 ) ;
      public const string PB4 = nameof( PB4 ) ;
      public const string PB5 = nameof( PB5 ) ;
      public const string PB6 = nameof( PB6 ) ;
      public const string PB8 = nameof( PB8 ) ;
      public const string PB10 = nameof( PB10 ) ;
    }
    
  }
}