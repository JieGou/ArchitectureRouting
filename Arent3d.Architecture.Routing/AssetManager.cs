using System.IO ;
using System.Linq ;
using System.Reflection ;

namespace Arent3d.Architecture.Routing
{
  public static class AssetManager
  {
#if REVIT2019
    private const string FamilyFolderName = @"Families\2019" ;
#elif REVIT2020
    private const string FamilyFolderName = @"Families\2020" ;
#elif REVIT2021
    private const string FamilyFolderName = @"Families\2021" ;
#elif REVIT2022
    private const string FamilyFolderName = @"Families\2022" ;
#elif REVIT2023
    private const string FamilyFolderName = @"Families\2023" ;
#endif
    private const string SettingFolderName = "SharedParameterFile" ;

    private const string RoutingSharedParameterFileName = "RoutingSharedParameters.txt" ;
    private const string PassPointSharedParameterFileName = "PassPointSharedParameters.txt" ;
    private const string RoutingElementSharedParameterFileName = "RoutingElementSharedParameters.txt" ;
    private const string MechanicalRoutingElementSharedParameterFileName = "MechanicalRoutingElementSharedParameters.txt" ;
    private const string ElectricalRoutingElementSharedParameterFileName = "ElectricalRoutingElementSharedParameters.txt" ;

    private static readonly string AssetPath = Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location )!, "Assets" ) ;

    public static string GetFamilyPath( string familyName )
    {
      return GetPath( FamilyFolderName, familyName + ".rfa" ) ;
    }

    public static string GetElectricalFamilyPath( string familyName )
    {
      return GetPath( FamilyFolderName + @"\Electrical", familyName + ".rfa" ) ;
    }

    public static string GetMechanicalFamilyPath( string familyName )
    {
      return GetPath( FamilyFolderName + @"\Mechanical", familyName + ".rfa" ) ;
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
      return GetPath( SettingFolderName, RoutingElementSharedParameterFileName ) ;
    }

    public static string GetMechanicalRoutingElementSharedParameterPath()
    {
      return GetPath( SettingFolderName, MechanicalRoutingElementSharedParameterFileName ) ;
    }

    public static string GetElectricalRoutingElementSharedParameterPath()
    {
      return GetPath( SettingFolderName, ElectricalRoutingElementSharedParameterFileName ) ;
    }

    private static string GetPath( string folderName, string fileName )
    {
      return Path.Combine( AssetPath, folderName, fileName ) ;
    }
    
    public static byte[]? ReadFileEmbededSource(string fileName )
    {
      var assembly = Assembly.GetExecutingAssembly() ;
      
      var resourceFullName = assembly.GetManifestResourceNames().FirstOrDefault(element => element.EndsWith(fileName));
      if ( string.IsNullOrEmpty( resourceFullName ) )
        return null ;

      using var stream = assembly.GetManifestResourceStream(resourceFullName);
      if ( null == stream )
        return null ;

      var fileData = new byte[stream.Length];
      stream.Read(fileData, 0, fileData.Length);

      return fileData ;
    }
  }
}