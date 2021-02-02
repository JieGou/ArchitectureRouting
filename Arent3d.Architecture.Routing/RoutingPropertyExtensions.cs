using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public enum RoutingParameter
  {
    [ParameterGuid( "42a113b5-364a-4918-a423-6590c47b828f" ), NameOnRevit( "Route Name" )]
    RouteName,
    [ParameterGuid( "B113FB98-A9EB-4F8E-A6A2-C4632922EB1B" ), NameOnRevit( "Route From-side Connector Ids" )]
    FromSideConnectorIds,
    [ParameterGuid( "6B594A61-EBEC-4BC9-BBFB-E5ABDA7372CB" ), NameOnRevit( "Route To-side Connector Ids" )]
    ToSideConnectorIds,
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
    } ;

    public static bool AllRoutingParametersAreRegistered( this Document document )
    {
      return document.AllParametersAreRegistered<RoutingParameter>() ;
    }

    public static void MakeCertainAllRoutingParameters( this Document document )
    {
      document.LoadAllAllParametersFromFile( RoutingBuiltInCategorySet, AssetManager.GetSharedParameterPath() ) ;
    }
  }
}