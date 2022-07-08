using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class EraseSelectedLimitRacksCommandBase : EraseLimitRackCommandBase
  {
    protected override (IEnumerable<string> rackIds, IEnumerable<string>? detailCurverIds, IEnumerable<LimitRackModel>? selectedLimitRackModels) GetLimitRackIds( UIDocument ui, Document doc, LimitRackStorable limitRackStorable )
    {
      var selectedConduitAndConduitFittings = ui.Selection
        .PickElementsByRectangle( ConduitAndConduitFittingSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).ToList() ;

      var selectedLimitRackModels =
        (GetSelectedLimitRackModel( limitRackStorable, selectedConduitAndConduitFittings ) ?? Array.Empty<LimitRackModel>()).ToList() ;
      var rackIds = new HashSet<string>() ;
      selectedLimitRackModels.ForEach( model =>  rackIds.AddRange( model.RackIds ) ) ;
      var detailLineIds = selectedConduitAndConduitFittings.Where( detailLine => detailLine is DetailLine ).Select( detailLine => detailLine.UniqueId )
        .ToList() ;

      return new ValueTuple<IEnumerable<string>, IEnumerable<string>, IEnumerable<LimitRackModel>>( rackIds,
        detailLineIds, selectedLimitRackModels ) ;
    }
    
    protected override void RemoveLimitRackModels( LimitRackStorable limitRackStorable, IEnumerable<LimitRackModel>? selectedLimitRackModels )
    {
      selectedLimitRackModels?.ForEach( x => limitRackStorable.LimitRackModelData.Remove( x ) ) ;
      limitRackStorable.Save() ;
    }

    private static IEnumerable<LimitRackModel>? GetSelectedLimitRackModel( LimitRackStorable? limitRackStorable,
      IEnumerable<Element> selectedConduitAndConduitFittings )
    {
      var allSelectedRoute = selectedConduitAndConduitFittings.Select( x => x.GetRouteName() ).Distinct() ;

      var selectedRoute = allSelectedRoute as string?[] ?? allSelectedRoute.ToArray() ;
      if (!selectedRoute.Any()) yield break;
      
      if ( limitRackStorable == null || ! limitRackStorable.LimitRackModelData.Any() ) {
        yield break ;
      }

      var allSelectedLimitRackModel =
        limitRackStorable.LimitRackModelData.Where( lmd => selectedRoute.Any( route => route == lmd.RouteName ) ) ;

      var selectedLimitRackModel = allSelectedLimitRackModel as LimitRackModel[] ?? allSelectedLimitRackModel.ToArray() ;
      if (!selectedLimitRackModel.Any()) yield break;
      
      foreach ( var limitRackModel in selectedLimitRackModel ) {
        yield return limitRackModel ;
      }
    }
  }
}