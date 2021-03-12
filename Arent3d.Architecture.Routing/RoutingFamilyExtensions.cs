using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;

namespace Arent3d.Architecture.Routing
{
  public enum RoutingFamilyType
  {
    [NameOnRevit( "Routing Rack Guide" )]
    [FamilyCategory( BuiltInCategory.OST_GenericModel )]
    RackGuide,

    [NameOnRevit( "Routing Pass Point" )]
    [FamilyCategory( BuiltInCategory.OST_MechanicalEquipment )]
    PassPoint,
  }

  public static class RoutingFamilyExtensions
  {
    private static readonly IReadOnlyDictionary<RoutingFamilyType, string> AllFamilyNames = NameOnRevitAttribute.ToDictionary<RoutingFamilyType>() ;
    private static readonly IReadOnlyDictionary<RoutingFamilyType, BuiltInCategory> AllBuiltInCategories = FamilyCategoryAttribute.ToDictionary<RoutingFamilyType>() ;
    private static readonly IReadOnlyDictionary<string, RoutingFamilyType> ReverseFamilyNames = NameOnRevitAttribute.ToReverseDictionary<RoutingFamilyType>() ;

    public static bool AllRoutingFamiliesAreLoaded( this Document document )
    {
      return AllFamilyNames.All( pair => null != FindFamilySymbolByName( document, pair.Key, pair.Value ) ) ;
    }

    public static void MakeCertainAllRoutingFamilies( this Document document )
    {
      foreach ( var (_, familyName) in AllFamilyNames.Where( pair => null == FindFamilySymbolByName( document, pair.Key, pair.Value ) ).EnumerateAll() ) {
        LoadFamilySymbol( document, familyName ) ;
      }
    }

    /// <summary>
    /// Gets a family element for a routing family type.
    /// </summary>
    /// <param name="document">Revit document.</param>
    /// <param name="familyType">A routing family type.</param>
    /// <returns>Family. May be null if <see cref="MakeCertainAllRoutingFamilies"/> have not been called.</returns>
    public static FamilySymbol? GetFamilySymbol( this Document document, RoutingFamilyType familyType )
    {
      if ( AllFamilyNames.TryGetValue( familyType, out var familyName ) ) {
        return FindFamilySymbolByName( document, familyType, familyName ) ;
      }

      return null ;
    }

    public static IEnumerable<FamilyInstance> GetAllFamilyInstances( this Document document, RoutingFamilyType familyType )
    {
      var familySymbol = document.GetFamilySymbol( familyType ) ;
      if ( null == familySymbol ) return Array.Empty<FamilyInstance>() ;

      return document.GetAllFamilyInstances( familySymbol ) ;
    }

    private static FamilySymbol? FindFamilySymbolByName( Document document, RoutingFamilyType familyType, string familyName )
    {
      if ( false == AllBuiltInCategories.TryGetValue( familyType, out var builtInCategory ) ) return null ;
      return document.GetAllElements<FamilySymbol>().OfCategory( builtInCategory ).FirstOrDefault( e => e.FamilyName == familyName ) ;
    }

    public static FamilyInstance Instantiate( this FamilySymbol symbol, XYZ position, string levelName, StructuralType structuralType )
    {
      var level = GetLevel( symbol.Document, levelName ) ;
      if ( null == level ) throw new InvalidOperationException() ;
      return symbol.Instantiate( position, level, structuralType ) ;
    }
    public static FamilyInstance Instantiate( this FamilySymbol symbol, XYZ position, Level level, StructuralType structuralType )
    {
      var document = symbol.Document ;
      if ( false == symbol.IsActive ) symbol.Activate() ;

      return document.Create.NewFamilyInstance( position, symbol, level, structuralType ) ;
    }

    private static Level? GetLevel( Document document, string levelName )
    {
      return document.GetAllElements<Level>().FirstOrDefault( l => l.Name == levelName ) ;
    }

    private static bool LoadFamilySymbol( Document document, string familyName )
    {
      var familyPath = AssetManager.GetFamilyPath( familyName ) ;
      if ( ! File.Exists( familyPath ) ) return false ;

      return document.LoadFamily( familyPath, out _ ) ;
    }

    public static bool IsRoutingFamilyInstance( this FamilyInstance familyInstance )
    {
      var familyName = familyInstance.Symbol.FamilyName ;
      return ( null != familyName ) && ReverseFamilyNames.ContainsKey( familyName ) ;
    }

    public static bool IsRoutingFamilyInstanceOf( this FamilyInstance familyInstance, RoutingFamilyType familyType )
    {
      var familyName = familyInstance.Symbol.FamilyName ;
      return ( null != familyName ) && IsFamilyType( familyName, familyType ) ;
    }
    public static bool IsRoutingFamilyInstanceOf( this FamilyInstance familyInstance, params RoutingFamilyType[] familyTypes )
    {
      var familyName = familyInstance.Symbol.FamilyName ;
      return ( null != familyName ) && IsAnyFamilyType( familyName, familyTypes ) ;
    }
    public static bool IsRoutingFamilyInstanceExcept( this FamilyInstance familyInstance, RoutingFamilyType familyType )
    {
      var familyName = familyInstance.Symbol.FamilyName ;
      return ( null != familyName ) && ( false == IsFamilyType( familyName, familyType ) ) ;
    }
    public static bool IsRoutingFamilyInstanceExcept( this FamilyInstance familyInstance, params RoutingFamilyType[] familyTypes )
    {
      var familyName = familyInstance.Symbol.FamilyName ;
      return ( null != familyName ) && ( false == IsAnyFamilyType( familyName, familyTypes ) ) ;
    }

    private static bool IsFamilyType( string familyName, RoutingFamilyType familyType )
    {
      return ReverseFamilyNames.TryGetValue( familyName, out var ft ) && ( ft == familyType ) ;
    }
    private static bool IsAnyFamilyType( string familyName, RoutingFamilyType[] familyTypes )
    {
      return ReverseFamilyNames.TryGetValue( familyName, out var ft ) && familyTypes.Contains( ft ) ;
    }
  }
}