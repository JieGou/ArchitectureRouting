using System ;
using System.IO ;
using System.Reflection ;

namespace Arent3d.Architecture.Routing
{
  public static class ConnectorFamilyManager
  {
#if REVIT2019
    private const string FamilyFolderName = @"ConnectorFamilies\2019" ;
#elif REVIT2020
    private const string FamilyFolderName = @"ConnectorFamilies\2020" ;
#elif REVIT2021
    private const string FamilyFolderName = @"ConnectorFamilies\2021" ;
#elif REVIT2022
    private const string FamilyFolderName = @"ConnectorFamilies\2022" ;
#endif
      private static readonly string AssetPath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ), "Assets" ) ;
      
      public static string GetFamilyPath( string familyName )
      {
          return GetPath( FamilyFolderName, familyName ) ;
      }
      
      public static string GetFolderPath( )
      {
          return Path.Combine( AssetPath, FamilyFolderName ) ;
      }
      
      private static string GetPath( string folderName, string fileName )
      {
          return Path.Combine( AssetPath, folderName, fileName ) ;
      }
  }
}