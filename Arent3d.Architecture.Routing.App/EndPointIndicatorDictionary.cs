using System ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.RouteEnd ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.App
{
  internal class EndPointIndicatorDictionaryForImport
  {
    private readonly Document _document ;
    private readonly Dictionary<string, IEndPointIndicator> _dic = new() ;

    public EndPointIndicatorDictionaryForImport( Document document )
    {
      _document = document ;
    }

    public IEndPointIndicator? GetIndicator( string routeName, string key, IEndPointIndicator? indicator )
    {
      if ( string.IsNullOrEmpty( key ) ) {
        return indicator ;
      }

      if ( _dic.TryGetValue( key, out var ind ) ) {
        // use previous indicator.
        return ind ;
      }

      if ( null == indicator ) return null ;

      indicator = GetCalculatedIndicator( routeName, indicator ) ;
      _dic.Add( key, indicator ) ;

      return indicator ;
    }

    private IEndPointIndicator GetCalculatedIndicator( string routeName, IEndPointIndicator indicator )
    {
      if ( indicator is not CoordinateIndicator ind ) return indicator ;

      // convert to PassPointEndIndicator
      IEndPointIndicator AddPassPoint()
      {
        var familyInstance = _document.AddPassPoint( routeName, ind.Origin, ind.Direction, -1 ) ;
        return new PassPointEndIndicator( familyInstance.Id.IntegerValue ) ;
      }

      if ( null == ThreadDispatcher.UiDispatcher ) {
        return AddPassPoint() ;
      }
      else {
        return ThreadDispatcher.UiDispatcher.Invoke( AddPassPoint ) ;
      }
    }
  }

  internal class EndPointIndicatorDictionaryForExport : IEndPointIndicatorVisitor<(string Key, IEndPointIndicator Indicator)>
  {
    private int _index = 0 ;
    private readonly Document _document ;
    private readonly Dictionary<string, IEndPointIndicator> _dic = new() ;
    private readonly Dictionary<IEndPointIndicator, string> _sameNameDic = new() ;

    public EndPointIndicatorDictionaryForExport( Document document )
    {
      _document = document ;
    }

    public (string Key, IEndPointIndicator Indicator) GetIndicator( IEndPointIndicator indicator )
    {
      var (key, ind) = ToExportingIndicator( indicator ) ;
      if ( string.IsNullOrEmpty( key ) ) {
        return ( string.Empty, ind ) ;
      }

      if ( _dic.TryGetValue( key, out var lastIndicator ) ) {
        return ( key, lastIndicator ) ;
      }

      _dic.Add( key, ind ) ;
      return ( key, ind ) ;
    }

    private (string Key, IEndPointIndicator Indicator) ToExportingIndicator( IEndPointIndicator indicator )
    {
      return indicator.Accept( this ) ;
    }

    public (string Key, IEndPointIndicator Indicator) Visit( ConnectorIndicator indicator )
    {
      return ( string.Empty, indicator ) ;
    }

    public (string Key, IEndPointIndicator Indicator) Visit( CoordinateIndicator indicator )
    {
      return ( string.Empty, indicator ) ;
    }

    public (string Key, IEndPointIndicator Indicator) Visit( PassPointEndIndicator indicator )
    {
      var elm = _document.GetElementById<FamilyInstance>( indicator.ElementId ) ;
      if ( null == elm ) return ( string.Empty, indicator ) ;

      if ( false == _sameNameDic.TryGetValue( indicator, out var key ) ) {
        key = $"pp#{++_index}" ;
        _sameNameDic.Add( indicator, key ) ;
      }

      var transform = elm.GetTotalTransform() ;
      return ( key, new CoordinateIndicator( transform.Origin, transform.BasisX ) ) ;
    }

    public (string Key, IEndPointIndicator Indicator) Visit( PassPointBranchEndIndicator indicator )
    {
      var elm = _document.GetElementById<FamilyInstance>( indicator.ElementId ) ;
      if ( null == elm ) return ( string.Empty, indicator ) ;

      var keyIndicator = new PassPointEndIndicator( indicator.ElementId ) ;
      if ( false == _sameNameDic.TryGetValue( keyIndicator, out var key ) ) {
        key = $"pp#{++_index}" ;
        _sameNameDic.Add( keyIndicator, key ) ;
      }

      ++_index ;
      return ( key, indicator ) ;
    }

    public (string Key, IEndPointIndicator Indicator) Visit( RouteIndicator indicator )
    {
      return (string.Empty, indicator ) ;
    }
  }
}