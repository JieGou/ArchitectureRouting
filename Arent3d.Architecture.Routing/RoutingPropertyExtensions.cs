using Arent3d.Revit ;
using Autodesk.Revit.DB ;

#if DEBUG
using ParameterVisibility = Arent3d.Revit.ParameterVisibilityForDebug ;
#else
using ParameterVisibility = Arent3d.Revit.ParameterVisibility ;
#endif

namespace Arent3d.Architecture.Routing
{
  [ParameterGroupName( "com.arent3d Routing" )]
  public enum RoutingParameter
  {
    // RoutingSharedParameters
    [Parameter( "42a113b5-364a-4918-a423-6590c47b828f", "Route Name", DataType.Text, ParameterVisibility.ReadOnlyOnHasValue )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.RoutingElements ) )]
    RouteName,

    [Parameter( "4620ee8d-7c76-4798-bfdc-87491ff8b355", "SubRoute Index", DataType.Integer, ParameterVisibility.ReadOnlyOnHasValue )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.RoutingElements ) )]
    SubRouteIndex,

    [Parameter( "58fd42f8-df12-41f3-9d7b-3dd4f1bffb41", "Branch Route Names", DataType.Text, ParameterVisibility.ReadOnlyOnHasValue )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.RoutingElements ) )]
    BranchRouteNames,

    [Parameter( "b113fb98-a9eb-4f8e-a6a2-c4632922eb1b", "Route From-side Connector Ids", DataType.Text, ParameterVisibility.Hidden )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.RoutingElements ) )]
    RoutedElementFromSideConnectorIds,

    [Parameter( "6b594a61-ebec-4bc9-bbfb-e5abda7372cb", "Route To-side Connector Ids", DataType.Text, ParameterVisibility.Hidden )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.RoutingElements ) )]
    RoutedElementToSideConnectorIds,

    [Parameter( "5e822fe8-274e-41e0-b197-27a75bd52500", "Nearest From-side End Points", DataType.Text, ParameterVisibility.Hidden )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.RoutingElements ) )]
    NearestFromSideEndPoints,

    [Parameter( "7a6ec320-3c94-489a-89d2-dfb783ae8ae1", "Nearest To-side End Points", DataType.Text, ParameterVisibility.Hidden )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.RoutingElements ) )]
    NearestToSideEndPoints,

    [Parameter( "6cf2fece-a396-43e8-bede-4a2a75de5511", "Related Pass Point Id", DataType.Text, ParameterVisibility.Hidden )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.PassPoints ) )]
    RelatedPassPointUniqueId,

    [Parameter( "0e79cbf5-ac77-4fd2-be12-7969f5204a28", "Related Terminate Point Id", DataType.Text, ParameterVisibility.Hidden )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.PassPoints ) )]
    RelatedTerminatePointUniqueId,

    [Parameter( "ba87dfa3-c1f7-4b44-b07d-00333404bda8", "Representative Route Name", DataType.Text, ParameterVisibility.Hidden )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.RoutingElements ) )]
    RepresentativeRouteName,

    [Parameter( "9e825887-84fe-474d-ac2e-c683f7376647", "Representative SubRoute Index", DataType.Integer, ParameterVisibility.Hidden )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.RoutingElements ) )]
    RepresentativeSubRouteIndex,
    
    [Parameter( "1957C74A-9BDA-4850-959A-796DF6BF43A9", "Obstacle Name", DataType.Text, ParameterVisibility.Hidden )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.CommonRoutingElement ) )]
    ObstacleName,
  }

  [ParameterGroupName( "com.arent3d Routing" )]
  public enum PassPointParameter
  {
    [Parameter( "b975f161-499f-4cc6-8e11-0d7ddf25b1f4", "PassPoint From-side Element Id", DataType.Text, ParameterVisibility.Hidden )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.PassPoints ) )]
    PassPointNextToFromSideConnectorUniqueIds,

    [Parameter( "7af4819d-3aec-4235-9f81-e6d3d0ca9ca2", "PassPoint To-side Element Id", DataType.Text, ParameterVisibility.Hidden )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.PassPoints ) )]
    PassPointNextToToSideConnectorUniqueIds,

    [Parameter( "c766d041-3867-4e55-a2bc-0272d8eb3013", "Related Connector Id", DataType.Text, ParameterVisibility.Hidden )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.PassPoints ) )]
    RelatedConnectorUniqueId,

    [Parameter( "86b66529-f4f6-4392-b80b-5f2dc71e9564", "Related From Connector Id", DataType.Text, ParameterVisibility.Hidden )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.PassPoints ) )]
    RelatedFromConnectorUniqueId,
  }

  [ParameterGroupName( "com.arent3d Routing" )]
  public enum RoutingFamilyLinkedParameter
  {
    [Parameter( "3285f3e8-1838-4eba-a676-1a2af4708e7a", "Route Connector Relation Ids", DataType.Integer, ParameterVisibility.Hidden )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.ElementsUsedForUI ) )]
    RouteConnectorRelationId,
  }

  [ParameterGroupName( "com.arent3d Routing" )]
  public enum ElectricalRoutingElementParameter
  {
    [Parameter( "442b05ee-df38-4595-93c9-e2d7cfa227e9", "Connector Type", DataType.Text, ParameterVisibility.Editable, DescriptionKey = "Select Connector Type" )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.OtherElectricalElements ) )]
    ConnectorType,

    [Parameter( "7632d393-dade-437a-96a7-c4d508383012", "Rack Type", DataType.Text, ParameterVisibility.Editable, DescriptionKey = "Rack Type" )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.RackTypeElements ) )]
    RackType,

    [Parameter( "f208f9ab-b763-4b2a-afc9-0b2a22936dab", "Parent Envelope Id", DataType.Text, ParameterVisibility.Hidden )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.CommonRoutingElement ) )]
    ParentEnvelopeId,

    [Parameter( "f339149b-704c-403c-a97c-335646773992", "To-Side Connector Id", DataType.Text, ParameterVisibility.Hidden )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.CableTrays ) )]
    ToSideConnectorId,

    [Parameter( "57332190-02d7-4f25-a60d-b33a459f9fb7", "From-Side Connector Id", DataType.Text, ParameterVisibility.Hidden )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.CableTrays ) )]
    FromSideConnectorId,

    [Parameter( "f71cbd72-3fe4-47cb-b777-36d6511d42ed", "CeeD Code", DataType.Text, ParameterVisibility.ReadOnlyOnHasValue )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.OtherElectricalElements ) )]
    CeedCode,

    [Parameter( "f054f110-68e7-4bce-9d17-688557d410da", "Construction Item", DataType.Text, ParameterVisibility.ReadOnlyOnHasValue )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.ConstructionItems ) )]
    ConstructionItem,

    [Parameter( "302a0b15-ee8b-44a2-98b2-c5eb105a3579", "IsEcoMode", DataType.Text, ParameterVisibility.ReadOnlyOnHasValue )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.ConnectorsAndConduits ) )]
    IsEcoMode,
    
    [Parameter( "674f8e8e-b923-4bfc-9a3b-399adaa783ee", "Room Condition", DataType.Text, ParameterVisibility.Editable, DescriptionKey = "Select Room Condition" )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_TEXT, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.CommonRoutingElement ) )]
    RoomCondition,
    
    [Parameter( "fcacd40a-3bf7-495f-8d0c-6efeafa45775", "Symbol Content", DataType.Text, ParameterVisibility.ReadOnlyOnHasValue, DescriptionKey = "Symbol Content")]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.OtherElectricalElements ) )]
    SymbolContent,
    
    [Parameter( "6a4206f1-156b-449e-b9b6-a124dc56dfd5", "数量", DataType.Text, ParameterVisibility.Editable, DescriptionKey = "数量")]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.OtherElectricalElements ) )]
    Quantity,
    
    [Parameter( "05a6c3f6-ce5b-4bcb-a683-4087b928fe6d", "Text", DataType.Text, ParameterVisibility.Editable, DescriptionKey = "Text")]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.PG_IDENTITY_DATA, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.CommonRoutingElement ) )]
    Text,

    #region Schedules

    [Parameter( "ff5d3b79-2b6c-48cf-9627-e09c4d64a91d", "Schedule Header Row Count", DataType.Integer, ParameterVisibility.Editable )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.INVALID, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.ScheduleElements ) )]
    ScheduleHeaderRowCount,

    [Parameter( "4472b8cf-167c-4ad9-8bb1-99a96decc3d1", "IsSplit", DataType.Integer, ParameterVisibility.Editable )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.INVALID, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.ScheduleElements ) )]
    IsSplit,

    [Parameter( "d7ec6828-0b7a-4768-a1f8-3199d0c9d54b", "Split Index", DataType.Integer, ParameterVisibility.Editable )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.INVALID, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.ScheduleElements ) )]
    SplitIndex,

    [Parameter( "24dcc21e-807b-4562-9e65-a8289a6599d4", "Parent Schedule Id", DataType.Integer, ParameterVisibility.Editable )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.INVALID, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.ScheduleElements ) )]
    ParentScheduleId,
    
    [Parameter( "9dcb1465-2f98-4542-9bdd-550694d97a48", "Schedule Base Name", DataType.Text, ParameterVisibility.Editable )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.INVALID, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.ScheduleElements ) )]
    ScheduleBaseName,

    [Parameter( "63c05fcb-fba0-4502-903a-4f713c0e6cb8", "Image Cell Map", DataType.Text, ParameterVisibility.Editable )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.INVALID, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.ScheduleElements ) )]
    ImageCellMap,

    [Parameter( "268a73c8-acca-491e-87b4-0b1a0a627fc4", "Split Level", DataType.Integer, ParameterVisibility.Editable )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.INVALID, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.ScheduleElements ) )]
    SplitLevel,

    #endregion
    
  }

  [ParameterGroupName( "com.arent3d Routing" )]
  public enum BranchNumberParameter
  {
    [Parameter( "01c73735-4b79-4729-91af-3dede453c482", "BranchNumber", DataType.Integer, ParameterVisibility.Editable )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.INVALID, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.SpaceElements ) )]
    BranchNumber
  }

  [ParameterGroupName( "com.arent3d Routing" )]
  public enum AHUNumberParameter
  {
    [Parameter( "e1522ec9-2d76-4c32-addc-f9f27d3aa8ea", "AHUNumber", DataType.Integer, ParameterVisibility.Editable )]
    [BuiltInCategories( ExternalParameterType.Instance, BuiltInParameterGroup.INVALID, typeof( BuiltInCategorySets ), nameof( BuiltInCategorySets.AHUNumberElements ) )]
    AHUNumber
  }
  
  public static class RoutingPropertyExtensions
  {
    public static bool AllRoutingParametersAreRegistered( this Document document )
    {
      return document.AllParametersAreRegistered<RoutingParameter>() && document.AllParametersAreRegistered<RoutingFamilyLinkedParameter>() ;
    }

    public static bool AllElectricalRoutingParametersAreRegistered( this Document document )
    {
      return document.AllParametersAreRegistered<ElectricalRoutingElementParameter>() ;
    }

    public static bool AllMechanicalRoutingParametersAreRegistered( this Document document )
    {
      return document.AllParametersAreRegistered<BranchNumberParameter>() && document.AllParametersAreRegistered<AHUNumberParameter>() ;
    }

    public static void MakeCertainAllRoutingParameters( this Document document )
    {
      document.LoadAllParameters<RoutingParameter>() ;
      document.LoadAllParameters<PassPointParameter>() ;
      document.LoadAllParameters<RoutingFamilyLinkedParameter>() ;
    }

    public static void MakeElectricalRoutingElementParameters( this Document document )
    {
      document.LoadAllParameters<ElectricalRoutingElementParameter>() ;
    }

    public static void MakeMechanicalRoutingElementParameters( this Document document )
    {
      document.LoadAllParameters<BranchNumberParameter>() ;
      document.LoadAllParameters<AHUNumberParameter>() ;
    }

    public static void UnloadAllRoutingParameters( this Document document )
    {
      document.UnloadAllParameters<RoutingParameter>() ;
      document.UnloadAllParameters<PassPointParameter>() ;
      document.UnloadAllParameters<RoutingFamilyLinkedParameter>() ;
      document.UnloadAllParameters<BranchNumberParameter>() ;
      document.UnloadAllParameters<AHUNumberParameter>() ;
      document.UnloadAllParameters<ElectricalRoutingElementParameter>() ;
    }
  }
}