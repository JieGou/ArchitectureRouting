namespace Arent3d.Architecture.Routing.App
{
  public static class UIHelper
  {
    /// <summary>
    /// Get LabelName From CurveType
    /// </summary>
    /// <param name="targetStrings"></param>
    /// <returns></returns>
    public static string GetTypeLabel( string targetStrings )
    {
      if ( targetStrings.EndsWith( "Type" ) ) {
        targetStrings = targetStrings.Substring( 0, targetStrings.Length - 4 ) + " Type" ;
      }

      return targetStrings ;
    }
  }
}