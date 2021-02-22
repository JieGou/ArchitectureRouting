using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public enum RoutingParameter
  {
    // RoutingSharedParameters
    [ParameterGuid( "42a113b5-364a-4918-a423-6590c47b828f" ), NameOnRevit( "Route Name" )]
    RouteName,
    [ParameterGuid( "4620ee8d-7c76-4798-bfdc-87491ff8b355" ), NameOnRevit( "SubRoute Index" )]
    SubRouteIndex,
    [ParameterGuid( "b113fb98-a9eb-4f8e-a6a2-c4632922eb1b" ), NameOnRevit( "Route From-side Connector Ids" )]
    RoutedElementFromSideConnectorIds,
    [ParameterGuid( "6b594a61-ebec-4bc9-bbfb-e5abda7372cb" ), NameOnRevit( "Route To-side Connector Ids" )]
    RoutedElementToSideConnectorIds,
    [ParameterGuid( "5e822fe8-274e-41e0-b197-27a75bd52500" ), NameOnRevit( "Nearest From-side End Points" )]
    NearestFromSideEndPoints,
    [ParameterGuid( "7a6ec320-3c94-489a-89d2-dfb783ae8ae1" ), NameOnRevit( "Nearest To-side End Points" )]
    NearestToSideEndPoints,

    // PassPointSharedParameters
    [ParameterGuid( "b975f161-499f-4cc6-8e11-0d7ddf25b1f4" ), NameOnRevit( "PassPoint From-side Element Id" )]
    PassPointNextToFromSideConnectorIds,
    [ParameterGuid( "7af4819d-3aec-4235-9f81-e6d3d0ca9ca2" ), NameOnRevit( "PassPoint To-side Element Id" )]
    PassPointNextToToSideConnectorIds,
  }

  public static class RoutingPropertyExtensions
  {
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
      BuiltInCategory.OST_Sprinklers,
    } ;
    private static readonly BuiltInCategory[] PassPointBuiltInCategorySet =
    {
      BuiltInCategory.OST_MechanicalEquipment,
    } ;

    public static bool AllRoutingParametersAreRegistered( this Document document )
    {
      return document.AllParametersAreRegistered<RoutingParameter>() ;
    }

    public static void MakeCertainAllRoutingParameters( this Document document )
    {
      document.LoadAllAllParametersFromFile( RoutingBuiltInCategorySet, AssetManager.GetRoutingSharedParameterPath() ) ;
      document.LoadAllAllParametersFromFile( PassPointBuiltInCategorySet, AssetManager.GetPassPointSharedParameterPath() ) ;
    }
  }
}