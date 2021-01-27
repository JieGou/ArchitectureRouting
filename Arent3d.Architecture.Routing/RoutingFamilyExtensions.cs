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

  public enum RoutingParameter
  {
    [NameOnRevit( "Route Name" )]
    RouteName,
  }

  public static class RoutingFamilyExtensions
  {
    private static readonly IReadOnlyDictionary<RoutingFamilyType, string> AllFamilyNames = NameOnRevitAttribute.ToDictionary<RoutingFamilyType>() ;


    private const string RoutingPropertyGroupName = "Arent3d Routing" ;

    private static readonly IReadOnlyDictionary<RoutingParameter, string> AllParameterNames = NameOnRevitAttribute.ToDictionary<RoutingParameter>() ;


    /// <summary>
    /// Confirms whether families and parameters used for routing application are loaded.
    /// </summary>
    /// <param name="document"></param>
    /// <returns>True if all families and parameters are loaded.</returns>
    public static bool SetupIsDone( Document document )
    {
      return AllFamiliesAreLoaded( document ) || AllParametersAreRegistered( document ) ;
    }

    /// <summary>
    /// Setup all families and parameters used for routing application.
    /// </summary>
    /// <param name="document"></param>
    public static void SetupRoutingFamiliesAndParameters( this Document document )
    {
      if ( SetupIsDone( document ) ) return ;

      using var tx = new Transaction( document ) ;
      tx.Start( "Setup routing" ) ;
      try {
        MakeCertainAllFamilies( document ) ;
        MakeCertainAllParameters( document ) ;

        if ( false == SetupIsDone( document ) ) {
          throw new InvalidOperationException( "Failed to set up routing families and parameters." ) ;
        }

        tx.Commit() ;
      }
      catch ( Exception ) {
        tx.RollBack() ;
        throw ;
      }
    }

    #region Families

    private static bool AllFamiliesAreLoaded( Document document )
    {
      return AllFamilyNames.Values.All( familyName => null != FindFamilyElementByName( document, familyName ) ) ;
    }

    private static void MakeCertainAllFamilies( this Document document )
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
    /// <returns>Family. May be null if <see cref="MakeCertainAllFamilies"/> have not been called.</returns>
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

    #endregion

    #region Parameters

    private static readonly BuiltInCategory[] RoutingBuiltInCategorySet =
    {
      BuiltInCategory.OST_DuctTerminal,
      BuiltInCategory.OST_DuctAccessory,
      BuiltInCategory.OST_DuctFitting,
      BuiltInCategory.OST_DuctSystem,
      BuiltInCategory.OST_DuctCurves,
      BuiltInCategory.OST_PlaceHolderDucts,
      BuiltInCategory.OST_FlexDuctCurves,
      BuiltInCategory.OST_FlexPipeCurves,
      BuiltInCategory.OST_GenericModel,
      BuiltInCategory.OST_MechanicalEquipment,
      BuiltInCategory.OST_PipeAccessory,
      BuiltInCategory.OST_PipeFitting,
      //BuiltInCategory.OST_PipeSegments, // cannot use parameters for OST_PipeSegments category!
      BuiltInCategory.OST_PipeCurves,
      BuiltInCategory.OST_PlumbingFixtures,
    } ;


    private static bool AllParametersAreRegistered( Document document )
    {
      var currentDefinitions = GetDefinitions( document.ParameterBindings ).Select( d => d.Name ).ToHashSet() ;
      return AllParameterNames.Values.All( currentDefinitions.Contains ) ;
    }

    private static IEnumerable<Definition> GetDefinitions( DefinitionBindingMap bindings )
    {
      var it = bindings.ForwardIterator() ;
      while ( it.MoveNext() ) {
        yield return it.Key ;
      }
    }

    private static void MakeCertainAllParameters( Document document )
    {
      var app = document.Application ;

      var arentCategorySet = app.Create.NewCategorySet() ;
      foreach ( var cat in RoutingBuiltInCategorySet ) {
        arentCategorySet.Insert( document.Settings.Categories.get_Item( cat ) ) ;
      }

      var instanceBinding = app.Create.NewInstanceBinding( arentCategorySet ) ;

      var bindingMap = document.ParameterBindings ;

      using var sharedParameters = new SharedParameters( document ) ;
      foreach ( var definition in sharedParameters.ParameterDefinitions ) {
        if ( bindingMap.Contains( definition ) ) continue ;

        bindingMap.Insert( definition, instanceBinding, BuiltInParameterGroup.PG_IDENTITY_DATA ) ;
      }

      // apply into RoutingProperties
      RoutingProperties.SetSharedParameters( document, GetParameterDefinitionsByRoutingParameter( sharedParameters.ParameterDefinitions ) ) ;
    }

    private static IEnumerable<KeyValuePair<RoutingParameter, Definition>> GetParameterDefinitionsByRoutingParameter( Definitions definitions )
    {
      foreach ( var (param, name) in AllParameterNames ) {
        var definition = definitions.get_Item( name ) ;
        if ( null == definition ) continue ;

        yield return new KeyValuePair<RoutingParameter, Definition>( param, definition ) ;
      }
    }

    private class SharedParameters : IDisposable
    {
      private readonly Application _app ;
      private readonly string _orgSharedParametersFilename ;

      public Definitions ParameterDefinitions { get ; }

      public SharedParameters( Document document )
      {
        _app = document.Application ;
        var sharedParameterPath = AssetManager.GetSharedParameterPath() ;

        _orgSharedParametersFilename = _app.SharedParametersFilename ;
        _app.SharedParametersFilename = sharedParameterPath ;

        ParameterDefinitions = _app.OpenSharedParameterFile().Groups.get_Item( RoutingPropertyGroupName ).Definitions ;
      }

      public void Dispose()
      {
        GC.SuppressFinalize( this ) ;

        RevertSharedParametersFilename() ;
      }

      ~SharedParameters()
      {
        RevertSharedParametersFilename() ;
      }

      private void RevertSharedParametersFilename()
      {
        _app.SharedParametersFilename = _orgSharedParametersFilename ;
      }
    }

    #endregion
  }
}