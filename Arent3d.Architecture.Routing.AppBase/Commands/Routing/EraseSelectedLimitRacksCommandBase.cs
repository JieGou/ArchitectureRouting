using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class EraseSelectedLimitRacksCommandBase : EraseLimitRackCommandBase
  {
    protected override (IEnumerable<string>? allCableTrayIds, IEnumerable<string>? limitRackDetailIds,LimitRackModel? selectedLimitRackModel) 
      GetLimitRackIds( UIDocument ui, Document doc,LimitRackStorable limitRackStorable )
    {
      var selectedLimitRackDetailCurves = ui.Selection
        .PickElementsByRectangle( DetailtLimitRackSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ) ;

      if ( ! selectedLimitRackDetailCurves.Any() ) return new (null,null,null);

      var selectedLimitRackModel = GetSelectedLimitRackModel( limitRackStorable,selectedLimitRackDetailCurves ) ;

      return selectedLimitRackModel is null ? (null,null,null) : new ValueTuple<IEnumerable<string>?,IEnumerable<string>?,LimitRackModel?>(selectedLimitRackModel.LimitRackIds.Concat( selectedLimitRackModel.LitmitRackFittingIds ),selectedLimitRackModel.LimitRackDetailIds,selectedLimitRackModel) ;
    }

    private static LimitRackModel? GetSelectedLimitRackModel(LimitRackStorable limitRackStorable, IEnumerable<Element> selectedLimitRackDetailCurves )
    {
      return limitRackStorable.LimitRackModelData.FirstOrDefault( limitRackModel => limitRackModel.LimitRackDetailIds.Any( limitRackDetailId => selectedLimitRackDetailCurves.Any( x => x.UniqueId == limitRackDetailId ) ) ) ;
    }
  }
}