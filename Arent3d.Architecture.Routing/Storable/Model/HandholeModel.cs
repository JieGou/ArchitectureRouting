namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class HandholeModel : PullBoxModel
  {
    public override string DefaultPullBoxLabel => "HH" ;

    public HandholeModel( HiroiMasterModel hiroiMasterModel ) : base( hiroiMasterModel )
    {
    }

    protected override string GetPullBoxName( int width, int height ) =>
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