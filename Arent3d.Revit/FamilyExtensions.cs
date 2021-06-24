using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  internal static class FamilyDictionary<TFamilyTypeEnum> where TFamilyTypeEnum : struct, Enum
  {
    public static IReadOnlyDictionary<TFamilyTypeEnum, string> AllFamilyNames { get ; } = NameOnRevitAttribute.ToDictionary<TFamilyTypeEnum>() ;
    public static IReadOnlyDictionary<TFamilyTypeEnum, BuiltInCategory> AllBuiltInCategories { get ; } = FamilyCategoryAttribute.ToDictionary<TFamilyTypeEnum>() ;
    public static IReadOnlyDictionary<string, TFamilyTypeEnum> ReverseFamilyNames { get ; } = NameOnRevitAttribute.ToReverseDictionary<TFamilyTypeEnum>() ;
  }

  public static class FamilyExtensions
  {
    public static bool AllFamiliesAreLoaded<TFamilyTypeEnum>( this Document document ) where TFamilyTypeEnum : struct, Enum
    {
      return FamilyDictionary<TFamilyTypeEnum>.AllFamilyNames.All( pair => null != FindFamilySymbolByName( document, pair.Key, pair.Value ) ) ;
    }

    public static void MakeCertainAllFamilies<TFamilyTypeEnum>( this Document document, Func<string, string> familyNameToPath ) where TFamilyTypeEnum : struct, Enum
    {
      foreach ( var (_, familyName) in FamilyDictionary<TFamilyTypeEnum>.AllFamilyNames.Where( pair => null == FindFamilySymbolByName( document, pair.Key, pair.Value ) ).EnumerateAll() ) {
        LoadFamilySymbol( document, familyNameToPath( familyName ) ) ;
      }
    }

    public static FamilySymbol? GetFamilySymbol<TFamilyTypeEnum>( this Document document, TFamilyTypeEnum familyType ) where TFamilyTypeEnum : struct, Enum
    {
      if ( FamilyDictionary<TFamilyTypeEnum>.AllFamilyNames.TryGetValue( familyType, out var familyName ) ) {
        return FindFamilySymbolByName( document, familyType, familyName ) ;
      }

      return null ;
    }

    public static IEnumerable<FamilyInstance> GetAllFamilyInstances<TFamilyTypeEnum>( this Document document, TFamilyTypeEnum familyType ) where TFamilyTypeEnum : struct, Enum
    {
      var familySymbol = document.GetFamilySymbol( familyType ) ;
      if ( null == familySymbol ) return Array.Empty<FamilyInstance>() ;

      return document.GetAllFamilyInstances( familySymbol ) ;
    }

    public static bool IsFamilyInstance<TFamilyTypeEnum>( this FamilyInstance familyInstance ) where TFamilyTypeEnum : struct, Enum
    {
      var familyName = familyInstance.Symbol.FamilyName ;
      return ( null != familyName ) && FamilyDictionary<TFamilyTypeEnum>.ReverseFamilyNames.ContainsKey( familyName ) ;
    }

    public static bool IsFamilyInstanceOf<TFamilyTypeEnum>( this FamilyInstance familyInstance, TFamilyTypeEnum familyType ) where TFamilyTypeEnum : struct, Enum
    {
      var familyName = familyInstance.Symbol.FamilyName ;
      return ( null != familyName ) && IsFamilyType( familyName, familyType ) ;
    }

    public static bool IsFamilyInstanceOfAny<TFamilyTypeEnum>( this FamilyInstance familyInstance, params TFamilyTypeEnum[] familyTypes ) where TFamilyTypeEnum : struct, Enum
    {
      var familyName = familyInstance.Symbol.FamilyName ;
      return ( null != familyName ) && IsAnyFamilyType( familyName, familyTypes ) ;
    }

    public static bool IsFamilyInstanceExcept<TFamilyTypeEnum>( this FamilyInstance familyInstance, TFamilyTypeEnum familyType ) where TFamilyTypeEnum : struct, Enum
    {
      var familyName = familyInstance.Symbol.FamilyName ;
      return ( null != familyName ) && ( false == IsFamilyType( familyName, familyType ) ) ;
    }
    public static bool IsFamilyInstanceExcept<TFamilyTypeEnum>( this FamilyInstance familyInstance, params TFamilyTypeEnum[] familyTypes ) where TFamilyTypeEnum : struct, Enum
    {
      var familyName = familyInstance.Symbol.FamilyName ;
      return ( null != familyName ) && ( false == IsAnyFamilyType( familyName, familyTypes ) ) ;
    }

    private static FamilySymbol? FindFamilySymbolByName<TFamilyTypeEnum>( Document document, TFamilyTypeEnum familyType, string familyName ) where TFamilyTypeEnum : struct, Enum
    {
      if ( false == FamilyDictionary<TFamilyTypeEnum>.AllBuiltInCategories.TryGetValue( familyType, out var builtInCategory ) ) return null ;
      return document.GetAllElements<FamilySymbol>().OfCategory( builtInCategory ).FirstOrDefault( e => e.FamilyName == familyName ) ;
    }

    private static bool LoadFamilySymbol( Document document, string familyPath )
    {
      if ( ! File.Exists( familyPath ) ) return false ;

      return document.LoadFamily( familyPath, out _ ) ;
    }

    private static bool IsFamilyType<TFamilyTypeEnum>( string familyName, TFamilyTypeEnum familyType ) where TFamilyTypeEnum : struct, Enum
    {
      return FamilyDictionary<TFamilyTypeEnum>.ReverseFamilyNames.TryGetValue( familyName, out var ft ) && Equals( ft, familyType ) ;
    }
    private static bool IsAnyFamilyType<TFamilyTypeEnum>( string familyName, TFamilyTypeEnum[] familyTypes ) where TFamilyTypeEnum : struct, Enum
    {
      return FamilyDictionary<TFamilyTypeEnum>.ReverseFamilyNames.TryGetValue( familyName, out var ft ) && familyTypes.Contains( ft ) ;
    }
    
  }
}