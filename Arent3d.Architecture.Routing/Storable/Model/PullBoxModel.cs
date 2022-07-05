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
    }

    private static int TryConvertStringToInt( string value )
    {
      var regex = new Regex( NumberPattern ) ;
      var match = regex.Match( value ) ;

      if ( ! match.Success ) return 0 ;
      return int.TryParse( match.Value, out var result ) ? result : 0 ;
    }
  }
}