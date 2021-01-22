using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Reflection ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public enum RoutingFamilyType
  {
    RackGuide,
    PassPoint,
  }
  
  public static class RoutingFamilyExtensions
  {
    private const string RackGuideFamilyName = "Routing Rack Guide" ;

    private static readonly IReadOnlyDictionary<RoutingFamilyType, string> AllFamilyNames = new Dictionary<RoutingFamilyType, string>
    {
      { RoutingFamilyType.RackGuide, RackGuideFamilyName },
    } ;

    /// <summary>
    /// Make certain families which is used by routing application.
    /// </summary>
    /// <param name="document">Revit document.</param>
    public static void MakeCertainAllFamilies( this Document document )
    {
      var badFamilies = AllFamilyNames.Values.Where( familyName => null == FindFamilyElementByName( document, familyName ) ).EnumerateAll() ;
      if ( 0 == badFamilies.Count ) return ;

      using var tx = new Transaction( document ) ;
      tx.Start( "Load families" ) ;

      foreach ( var familyName in badFamilies ) {
        if ( false == LoadFamilySymbol( document, familyName ) ) {
          tx.RollBack() ;
          return ;
        }
      }

      tx.Commit() ;
    }

    /// <summary>
    /// Gets a family element for a routing family type.
    /// </summary>
    /// <param name="document">Revit document.</param>
    /// <param name="familyType">A routing family type.</param>
    /// <returns>Family. May be null if <see cref="MakeCertainAllFamilies"/> have not been called.</returns>
    private static FamilySymbol? GetFamilySymbol( this Document document, RoutingFamilyType familyType )
    {
      if ( AllFamilyNames.TryGetValue( familyType, out var familyName ) ) {
        return FindFamilyElementByName( document, familyName ) ;
      }

      return null ;
    }

    private static FamilySymbol? FindFamilyElementByName( Document document, string familyName )
    {
      return document.GetAllElementsInCategory<FamilySymbol>( BuiltInCategory.OST_GenericModel ).FirstOrDefault( e => e.FamilyName == familyName ) ;
    }

    private static bool LoadFamilySymbol( Document document, string familyName )
    {
      var directoryPath = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location )! ;
      var familyPath = Path.Combine( directoryPath, "Families", familyName + ".rfa" ) ;
      if ( ! File.Exists( familyPath ) ) return false ;

      return document.LoadFamily( familyPath, out _ ) ;
    }
  }
}