using System.IO ;
using System.Reflection ;

namespace Arent3d.Architecture.Routing
{
  public static class AssetManager
  {
    private const string FamilyFolderName = "Families" ;
    private const string SettingFolderName = "SharedParameterFile" ;

    private const string RoutingSharedParameterFileName = "RoutingSharedParameters.txt" ;
    private const string PassPointSharedParameterFileName = "PassPointSharedParameters.txt" ;
    private const string RoutingElementSharedParameterFileName = "RoutingElementSharedParameters.txt";

    private static readonly string AssetPath = Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location )!, "Assets" ) ;

    public static string GetFamilyPath( string familyName )
    {
      return GetPath( FamilyFolderName, familyName + ".rfa" ) ;
    }

    public static string GetRoutingSharedParameterPath()
    {
      return GetPath( SettingFolderName, RoutingSharedParameterFileName ) ;
    }

    public static string GetPassPointSharedParameterPath()
    {
      return GetPath( SettingFolderName, PassPointSharedParameterFileName ) ;
    }

    public static string GetRoutingElementSharedParameterPath()
    {
        return GetPath( SettingFolderName, RoutingElementSharedParameterFileName );
    }

    private static string GetPath( string folderName, string fileName )
    {
      return Path.Combine( AssetPath, folderName, fileName ) ;
    }
  }
}