using System ;
using System.Collections.Generic ;
using Autodesk.Revit.ApplicationServices ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public static class RoutingProperties
  {
    private static readonly Dictionary<Document, Dictionary<RoutingParameter, Definition>> _definitionsCache = new() ;

    public static void SetSharedParameters( Document document, IEnumerable<KeyValuePair<RoutingParameter, Definition>> definitions )
    {
      if ( _definitionsCache.TryGetValue( document, out var dic ) ) {
        dic.Clear() ;
      }
      else {
        dic = new Dictionary<RoutingParameter, Definition>() ;
        _definitionsCache.Add( document, dic ) ;
      }

      foreach ( var pair in definitions ) {
        dic.Add( pair.Key, pair.Value ) ;
      }
    }

    public static void SetProperty( this Element elm, RoutingParameter routingParam, double value )
    {
      var parameter = elm.GetParameter( routingParam ) ;

      if ( StorageType.Double == parameter.StorageType ) {
        parameter.Set( value ) ;
      }
      else {
        throw new InvalidOperationException() ;
      }
    }

    public static void SetProperty( this Element elm, RoutingParameter routingParam, int value )
    {
      var parameter = elm.GetParameter( routingParam ) ;

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
      var parameter = elm.GetParameter( routingParam ) ;

      if ( StorageType.String == parameter.StorageType ) {
        parameter.Set( value ) ;
      }
      else {
        throw new InvalidOperationException() ;
      }
    }

    public static void SetProperty( this Element elm, RoutingParameter routingParam, ElementId value )
    {
      var parameter = elm.GetParameter( routingParam ) ;

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
      var parameter = elm.GetParameter( routingParam ) ;

      return parameter.StorageType switch
      {
        StorageType.Integer => parameter.AsInteger(),
        _ => throw new InvalidOperationException(),
      } ;
    }
    public static double GetPropertyDouble( this Element elm, RoutingParameter routingParam )
    {
      var parameter = elm.GetParameter( routingParam ) ;

      return parameter.StorageType switch
      {
        StorageType.Integer => parameter.AsInteger(),
        StorageType.Double => parameter.AsDouble(),
        _ => throw new InvalidOperationException(),
      } ;
    }
    public static string GetPropertyString( this Element elm, RoutingParameter routingParam )
    {
      var parameter = elm.GetParameter( routingParam ) ;

      return parameter.StorageType switch
      {
        StorageType.String => parameter.AsString(),
        _ => throw new InvalidOperationException(),
      } ;
    }
    public static ElementId GetPropertyElementId( this Element elm, RoutingParameter routingParam )
    {
      var parameter = elm.GetParameter( routingParam ) ;

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

    public static bool HasParameter( this Element elm, RoutingParameter routingParam )
    {
      var document = elm.Document ;
      var dic = Get( document ) ;
      return ( null != dic && dic.ContainsKey( routingParam ) ) ;
    }

    private static Parameter GetParameter( this Element elm, RoutingParameter routingParam )
    {
      var document = elm.Document ;
      var dic = Get( document ) ;
      if ( null == dic || false == dic.TryGetValue( routingParam, out var definition ) ) throw new InvalidOperationException() ;

      return elm.get_Parameter( definition ) ;
    }
    
    private static IReadOnlyDictionary<RoutingParameter, Definition>? Get( Document document )
    {
      return _definitionsCache.TryGetValue( document, out var dic ) ? dic : null ;
    }
  }
}