using System ;
using System.Collections.Generic ;
using System.IO ;
using Arent3d.Utility ;
using Arent3d.Revit.Csv ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class FileRoutingCommandBase : RoutingCommandBase<IEnumerable<RouteRecord>>
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.RoutingFromFile" ;

    protected override OperationResult<IEnumerable<RouteRecord>> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var csvFileName = OpenFromToCsv() ;
      if ( null == csvFileName ) return OperationResult<IEnumerable<RouteRecord>>.Cancelled ;

      return new OperationResult<IEnumerable<RouteRecord>>( ReadRouteRecordsFromFile( csvFileName ) ) ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, IEnumerable<RouteRecord> fileRecords )
    {
      return fileRecords.ToSegmentsWithName( document ).EnumerateAll() ;
    }

    private static IEnumerable<RouteRecord> ReadRouteRecordsFromFile( string csvFileName )
    {
      using var reader = new StreamReader( csvFileName, true ) ;
      // Cannot use return directly, because `reader` will be closed in that case.
      foreach ( var item in reader.ReadCsvFile<RouteRecord>() ) {
        yield return item ;
      }
    }

    private static string? OpenFromToCsv()
    {
      using var dlg = new FileOpenDialog( $"{"Dialog.Commands.Routing.FromTo.FileName".GetAppStringByKeyOrDefault( null )} (*.csv)|*.csv" ) { Title = "Dialog.Commands.Routing.FromTo.Title.Import".GetAppStringByKeyOrDefault( null ) } ;

      if ( ItemSelectionDialogResult.Confirmed != dlg.Show() ) return null ;

      return ModelPathUtils.ConvertModelPathToUserVisiblePath( dlg.GetSelectedModelPath() ) ;
    }
  }
}