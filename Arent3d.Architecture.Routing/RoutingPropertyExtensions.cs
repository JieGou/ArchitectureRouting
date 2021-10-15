using System ;
using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public enum RoutingParameter
  {
    // RoutingSharedParameters
    [ParameterGuid( "42a113b5-364a-4918-a423-6590c47b828f" ), NameOnRevit( "Route Name" )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( RoutingPropertyExtensions ), nameof( RoutingPropertyExtensions.RoutingBuiltInCategorySet ) )]
    RouteName,

    [ParameterGuid( "4620ee8d-7c76-4798-bfdc-87491ff8b355" ), NameOnRevit( "SubRoute Index" )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( RoutingPropertyExtensions ), nameof( RoutingPropertyExtensions.RoutingBuiltInCategorySet ) )]
    SubRouteIndex,

    [ParameterGuid( "b113fb98-a9eb-4f8e-a6a2-c4632922eb1b" ), NameOnRevit( "Route From-side Connector Ids" )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( RoutingPropertyExtensions ), nameof( RoutingPropertyExtensions.RoutingBuiltInCategorySet ) )]
    RoutedElementFromSideConnectorIds,

    [ParameterGuid( "6b594a61-ebec-4bc9-bbfb-e5abda7372cb" ), NameOnRevit( "Route To-side Connector Ids" )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( RoutingPropertyExtensions ), nameof( RoutingPropertyExtensions.RoutingBuiltInCategorySet ) )]
    RoutedElementToSideConnectorIds,

    [ParameterGuid( "5e822fe8-274e-41e0-b197-27a75bd52500" ), NameOnRevit( "Nearest From-side End Points" )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( RoutingPropertyExtensions ), nameof( RoutingPropertyExtensions.RoutingBuiltInCategorySet ) )]
    NearestFromSideEndPoints,

    [ParameterGuid( "7a6ec320-3c94-489a-89d2-dfb783ae8ae1" ), NameOnRevit( "Nearest To-side End Points" )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( RoutingPropertyExtensions ), nameof( RoutingPropertyExtensions.RoutingBuiltInCategorySet ) )]
    NearestToSideEndPoints,

    [ParameterGuid( "6cf2fece-a396-43e8-bede-4a2a75de5511" ), NameOnRevit( "Related Pass Point Id" )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( RoutingPropertyExtensions ), nameof( RoutingPropertyExtensions.PassPointBuiltInCategorySet ) )]
    RelatedPassPointId,

    [ParameterGuid( "0e79cbf5-ac77-4fd2-be12-7969f5204a28" ), NameOnRevit( "Related Terminate Point Id" )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( RoutingPropertyExtensions ), nameof( RoutingPropertyExtensions.PassPointBuiltInCategorySet ) )]
    RelatedTerminatePointId,
    
    [ParameterGuid( "ba87dfa3-c1f7-4b44-b07d-00333404bda8" ), NameOnRevit( "Representative Route Name" )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( RoutingPropertyExtensions ), nameof( RoutingPropertyExtensions.RoutingBuiltInCategorySet ) )]
    RepresentativeRouteName,
    
    [ParameterGuid( "9e825887-84fe-474d-ac2e-c683f7376647" ), NameOnRevit( "Representative SubRoute Index" )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( RoutingPropertyExtensions ), nameof( RoutingPropertyExtensions.RoutingBuiltInCategorySet ) )]
    RepresentativeSubRouteIndex,
  }

  public enum PassPointParameter
  {
    [ParameterGuid( "b975f161-499f-4cc6-8e11-0d7ddf25b1f4" ), NameOnRevit( "PassPoint From-side Element Id" )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( RoutingPropertyExtensions ), nameof( RoutingPropertyExtensions.PassPointBuiltInCategorySet ) )]
    PassPointNextToFromSideConnectorIds,

    [ParameterGuid( "7af4819d-3aec-4235-9f81-e6d3d0ca9ca2" ), NameOnRevit( "PassPoint To-side Element Id" )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( RoutingPropertyExtensions ), nameof( RoutingPropertyExtensions.PassPointBuiltInCategorySet ) )]
    PassPointNextToToSideConnectorIds,
  }

  public enum RoutingFamilyLinkedParameter
  {
    [ParameterGuid( "3285f3e8-1838-4eba-a676-1a2af4708e7a" ), NameOnRevit( "Route Connector Relation Ids" )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( RoutingPropertyExtensions ), nameof( RoutingPropertyExtensions.RoutingElementBuiltInCategorySet ) )]
    RouteConnectorRelationIds,
  }
  public enum ConnectorParameter
  {
    [ParameterGuid( "442b05ee-df38-4595-93c9-e2d7cfa227e9" ), NameOnRevit( "Connector Type" )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( RoutingPropertyExtensions ), nameof( RoutingPropertyExtensions.ConnectorBuiltInCategorySet ) )]
    ConnectorIds,
  }
  public static class RoutingPropertyExtensions
  {
    internal static readonly BuiltInCategory[] PipeRoutingBuiltInCategorySet =
    {
      BuiltInCategory.OST_FlexPipeCurves,
      BuiltInCategory.OST_PipeFitting,
      BuiltInCategory.OST_PipeCurves,
    } ;

    internal static readonly BuiltInCategory[] DuctRoutingBuiltInCategorySet =
    {
      BuiltInCategory.OST_DuctTerminal,
      BuiltInCategory.OST_DuctFitting,
      BuiltInCategory.OST_DuctCurves,
      BuiltInCategory.OST_PlaceHolderDucts,
      BuiltInCategory.OST_FlexDuctCurves,
    } ;

    internal static readonly BuiltInCategory[] OtherMechanicalBuiltInCategorySet =
    {
      BuiltInCategory.OST_DuctAccessory,
      BuiltInCategory.OST_DuctSystem,
      BuiltInCategory.OST_MechanicalEquipment,
      BuiltInCategory.OST_PipeAccessory,
      //BuiltInCategory.OST_PipeSegments, // cannot use parameters for OST_PipeSegments category!
      BuiltInCategory.OST_PlumbingFixtures,
      BuiltInCategory.OST_Sprinklers,
    } ;

    internal static readonly BuiltInCategory[] ElectricalRoutingBuiltInCategorySet =
    {
      BuiltInCategory.OST_Conduit,
      BuiltInCategory.OST_ConduitFitting,
      BuiltInCategory.OST_ConduitRun,
      BuiltInCategory.OST_CableTray,
    } ;

    internal static readonly BuiltInCategory[] OtherElectricalBuiltInCategorySet =
    {
      BuiltInCategory.OST_ElectricalEquipment,
      BuiltInCategory.OST_ElectricalFixtures,
    } ;

    internal static readonly BuiltInCategory[] CommonRoutingRoutingBuiltInCategorySet =
    {
      BuiltInCategory.OST_GenericModel,
    } ;

    internal static readonly BuiltInCategory[] RoutingBuiltInCategorySet = CombineArrays(
      PipeRoutingBuiltInCategorySet,
      DuctRoutingBuiltInCategorySet,
      OtherMechanicalBuiltInCategorySet,
      ElectricalRoutingBuiltInCategorySet,
      OtherElectricalBuiltInCategorySet,
      CommonRoutingRoutingBuiltInCategorySet
    ) ;

    internal static readonly BuiltInCategory[] RoutingCollisionBuiltInCategorySet = CombineArrays(
      RoutingBuiltInCategorySet,
      new[]
      {
        BuiltInCategory.OST_StructuralColumns,
        BuiltInCategory.OST_StructuralFoundation,
        BuiltInCategory.OST_StructuralFraming,
        BuiltInCategory.OST_StructuralTruss,
      }
    ) ;
    

    private static T[] CombineArrays<T>( params T[][] arrays )
    {
      var totalLength = arrays.Sum( array => array.Length ) ;
      var result = new T[ totalLength ] ;
      var index = 0 ;
      foreach ( var array in arrays ) {
        Array.Copy( array, 0, result, index, array.Length ) ;
        index += array.Length ;
      }
      return result ;
    }

    internal static readonly BuiltInCategory[] PassPointBuiltInCategorySet =
    {
      BuiltInCategory.OST_MechanicalEquipment,
    } ;

    internal static readonly BuiltInCategory[] RoutingElementBuiltInCategorySet = 
    {
      BuiltInCategory.OST_MechanicalEquipment,
      BuiltInCategory.OST_GenericModel,
    } ;
    
    internal static readonly BuiltInCategory[] ConnectorBuiltInCategorySet =
    {
      BuiltInCategory.OST_ElectricalFixtures,
    } ;
    
    public static bool AllRoutingParametersAreRegistered( this Document document )
    {
      return document.AllParametersAreRegistered<RoutingParameter>() && document.AllParametersAreRegistered<RoutingFamilyLinkedParameter>() ;
    }

    public static void MakeCertainAllRoutingParameters( this Document document )
    {
      document.LoadAllAllParametersFromFile<RoutingParameter>( AssetManager.GetRoutingSharedParameterPath() ) ;
      document.LoadAllAllParametersFromFile<PassPointParameter>( AssetManager.GetPassPointSharedParameterPath() ) ;
      document.LoadAllAllParametersFromFile<RoutingFamilyLinkedParameter>( AssetManager.GetRoutingElementSharedParameterPath() );
      document.LoadAllAllParametersFromFile<ConnectorParameter>( AssetManager.GetConnectorSharedParameterPath() ) ;
    }
  }
}