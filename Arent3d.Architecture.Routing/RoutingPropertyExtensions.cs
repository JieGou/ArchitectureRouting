using System ;
using System.Collections.Generic ;
using System.Linq ;
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
    private static readonly IReadOnlyDictionary<RoutingParameter, string> AllParameterNames = NameOnRevitAttribute.ToDictionary<RoutingParameter>() ;
    private static readonly IReadOnlyDictionary<RoutingParameter, Guid> AllParameterGuids = ParameterGuidAttribute.ToDictionary<RoutingParameter>() ;

    #region Setup

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

    public static bool AllParametersAreRegistered( this Document document )
    {
      var currentDefinitions = GetDefinitions( document.ParameterBindings ).Select( d => d.Name ).ToHashSet() ;
      return AllParameterNames.Values.All( currentDefinitions.Contains ) ;
    }

    private static IEnumerable<Definition> GetDefinitions( DefinitionBindingMap bindings )
    {
      using var it = bindings.ForwardIterator() ;
      while ( it.MoveNext() ) {
        yield return it.Key ;
      }
    }

    public static void MakeCertainAllRoutingParameters( this Document document )
    {
      var app = document.Application ;

      var arentCategorySet = app.Create.NewCategorySet() ;
      foreach ( var cat in RoutingBuiltInCategorySet ) {
        arentCategorySet.Insert( document.Settings.Categories.get_Item( cat ) ) ;
      }

      var instanceBinding = app.Create.NewInstanceBinding( arentCategorySet ) ;

      var bindingMap = document.ParameterBindings ;

      foreach ( var definition in SharedParameterReader.GetSharedParameters( document.Application, AssetManager.GetSharedParameterPath() ) ) {
        if ( bindingMap.Contains( definition ) ) continue ;

        bindingMap.Insert( definition, instanceBinding, BuiltInParameterGroup.PG_IDENTITY_DATA ) ;
      }
    }

    #endregion

    public static void SetProperty( this Element elm, RoutingParameter routingParam, double value )
    {
      var parameter = elm.GetParameter( routingParam ) ?? throw new InvalidOperationException() ;

      if ( StorageType.Double == parameter.StorageType ) {
        parameter.Set( value ) ;
      }
      else {
        throw new InvalidOperationException() ;
      }
    }

    public static void SetProperty( this Element elm, RoutingParameter routingParam, int value )
    {
      var parameter = elm.GetParameter( routingParam ) ?? throw new InvalidOperationException() ;

      if ( StorageType.Double == parameter.StorageType ) {
        parameter.Set( (double) value ) ;
      }
      else if ( StorageType.Integer == parameter.StorageType ) {
        parameter.Set( value ) ;
      }
      else {
        throw new InvalidOperationException() ;
      }
    }

    public static void SetProperty( this Element elm, RoutingParameter routingParam, string value )
    {
      var parameter = elm.GetParameter( routingParam ) ?? throw new InvalidOperationException() ;

      if ( StorageType.String == parameter.StorageType ) {
        parameter.Set( value ) ;
      }
      else {
        throw new InvalidOperationException() ;
      }
    }

    public static void SetProperty( this Element elm, RoutingParameter routingParam, ElementId value )
    {
      var parameter = elm.GetParameter( routingParam ) ?? throw new InvalidOperationException() ;

      if ( StorageType.ElementId == parameter.StorageType ) {
        parameter.Set( value ) ;
      }
      else {
        throw new InvalidOperationException() ;
      }
    }
    public static void SetProperty( this Element elm, RoutingParameter routingParam, Element? value )
    {
      elm.SetProperty( routingParam, ( null != value ) ? value.Id : ElementId.InvalidElementId ) ;
    }

    public static int GetPropertyInt( this Element elm, RoutingParameter routingParam )
    {
      var parameter = elm.GetParameter( routingParam ) ?? throw new InvalidOperationException() ;

      return parameter.StorageType switch
      {
        StorageType.Integer => parameter.AsInteger(),
        _ => throw new InvalidOperationException(),
      } ;
    }
    public static double GetPropertyDouble( this Element elm, RoutingParameter routingParam )
    {
      var parameter = elm.GetParameter( routingParam ) ?? throw new InvalidOperationException() ;

      return parameter.StorageType switch
      {
        StorageType.Integer => parameter.AsInteger(),
        StorageType.Double => parameter.AsDouble(),
        _ => throw new InvalidOperationException(),
      } ;
    }
    public static string GetPropertyString( this Element elm, RoutingParameter routingParam )
    {
      var parameter = elm.GetParameter( routingParam ) ?? throw new InvalidOperationException() ;

      return parameter.StorageType switch
      {
        StorageType.String => parameter.AsString(),
        _ => throw new InvalidOperationException(),
      } ;
    }
    public static ElementId GetPropertyElementId( this Element elm, RoutingParameter routingParam )
    {
      var parameter = elm.GetParameter( routingParam ) ?? throw new InvalidOperationException() ;

      return parameter.StorageType switch
      {
        StorageType.ElementId => parameter.AsElementId(),
        _ => throw new InvalidOperationException(),
      } ;
    }
    public static Element? GetPropertyElement( this Element elm, RoutingParameter routingParam )
    {
      return elm.Document.GetElement( elm.GetPropertyElementId( routingParam ) ) ;
    }

    public static bool TryGetProperty( this Element elm, RoutingParameter routingParam, out int value )
    {
      value = default ;

      var parameter = elm.GetParameter( routingParam ) ;
      if ( null == parameter ) return false ;

      switch ( parameter.StorageType ) {
        case StorageType.Integer :
          value = parameter.AsInteger() ;
          return true ;

        default :
          return false ;
      }
    }
    public static bool TryGetProperty( this Element elm, RoutingParameter routingParam, out double value )
    {
      value = default ;

      var parameter = elm.GetParameter( routingParam ) ;
      if ( null == parameter ) return false ;

      switch ( parameter.StorageType ) {
        case StorageType.Integer :
          value = parameter.AsInteger() ;
          return true ;

        case StorageType.Double :
          value = parameter.AsDouble() ;
          return true ;

        default :
          return false ;
      }
    }
    public static bool TryGetProperty( this Element elm, RoutingParameter routingParam, out string? value )
    {
      value = default ;

      var parameter = elm.GetParameter( routingParam ) ;
      if ( null == parameter ) return false ;

      switch ( parameter.StorageType ) {
        case StorageType.String :
          value = parameter.AsString() ;
          return true ;

        default :
          return false ;
      }
    }
    public static bool TryGetProperty( this Element elm, RoutingParameter routingParam, out ElementId value )
    {
      value = ElementId.InvalidElementId ;
      var parameter = elm.GetParameter( routingParam ) ;
      if ( null == parameter ) return false ;

      switch ( parameter.StorageType ) {
        case StorageType.ElementId :
          value = parameter.AsElementId() ;
          return true ;

        default :
          return false ;
      }
    }
    public static bool TryGetProperty( this Element elm, RoutingParameter routingParam, out Element? value )
    {
      value = default ;
      if ( false == elm.TryGetProperty( routingParam, out ElementId elmId ) ) return false ;

      value = elm.Document.GetElement( elmId ) ;
      return true ;
    }

    public static bool HasParameter( this Element elm, RoutingParameter routingParam )
    {
      if ( false == AllParameterGuids.TryGetValue( routingParam, out var guid ) ) return false ;
      return ( null != elm.get_Parameter( guid ) ) ;
    }

    public static bool HasParameter( this Element elm, RoutingParameter routingParam, StorageType type )
    {
      if ( false == AllParameterGuids.TryGetValue( routingParam, out var guid ) ) throw new InvalidOperationException() ;
      return ( elm.get_Parameter( guid )?.StorageType == type ) ;
    }

    private static Parameter? GetParameter( this Element elm, RoutingParameter routingParam )
    {
      if ( false == AllParameterGuids.TryGetValue( routingParam, out var guid ) ) return null ;
      return elm.get_Parameter( guid ) ;
    }

    public static string? GetParameterName( this Document document, RoutingParameter routingParam )
    {
      if ( false == AllParameterNames.TryGetValue( routingParam, out var name ) ) return null ;
      return name ;
    }
  }
}