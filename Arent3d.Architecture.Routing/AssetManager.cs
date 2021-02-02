using System.IO ;
using System.Reflection ;

namespace Arent3d.Architecture.Routing
{
  public static class AssetManager
  {
    private const string FamilyFolderName = "Families" ;
    private const string SettingFolderName = "SharedParameterFile" ;

    private const string SettingFileName = "Arent3d.Architecture.Routing.SharedParameter.txt" ;

    private static readonly string AssetPath = Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location )!, "Assets" ) ;

    public static string GetFamilyPath( string familyName )
    {
      return GetPath( FamilyFolderName, familyName + ".rfa" ) ;
    }

    public static string GetSharedParameterPath()
    {
      return GetPath( SettingFolderName, SettingFileName ) ;
    }

    private static string GetPath( string folderName, string fileName )
    {
      return Path.Combine( AssetPath, folderName, fileName ) ;
    }
  }
}