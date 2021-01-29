using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.ApplicationServices ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public enum RoutingFamilyType
  {
    [NameOnRevit( "Routing Rack Guide" )]
    RackGuide,

    [NameOnRevit( "" )]
    PassPoint,
  }

  public static class RoutingFamilyExtensions
  {
    private static readonly IReadOnlyDictionary<RoutingFamilyType, string> AllFamilyNames = NameOnRevitAttribute.ToDictionary<RoutingFamilyType>() ;

    public static bool AllRoutingFamiliesAreLoaded( this Document document )
    {
      return AllFamilyNames.Values.All( familyName => null != FindFamilyElementByName( document, familyName ) ) ;
    }

    public static void MakeCertainAllRoutingFamilies( this Document document )
    {
      foreach ( var familyName in AllFamilyNames.Values.Where( familyName => null == FindFamilyElementByName( document, familyName ) ).EnumerateAll() ) {
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
        return FindFamilyElementByName( document, familyName ) ;
      }

      return null ;
    }

    private static FamilySymbol? FindFamilyElementByName( Document document, string familyName )
    {
      return document.GetFamilySymbol( BuiltInCategory.OST_GenericModel, familyName ) ;
    }

    private static bool LoadFamilySymbol( Document document, string familyName )
    {
      var familyPath = AssetManager.GetFamilyPath( familyName ) ;
      if ( ! File.Exists( familyPath ) ) return false ;

      return document.LoadFamily( familyPath, out _ ) ;
    }
  }
}