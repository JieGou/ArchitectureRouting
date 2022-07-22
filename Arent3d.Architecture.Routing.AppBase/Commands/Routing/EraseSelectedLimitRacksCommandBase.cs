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
    protected override (IReadOnlyCollection<string> limitRackIds, IReadOnlyCollection<LimitRackModel> limitRackModels) GetLimitRackIds( UIDocument ui, Document doc, LimitRackStorable limitRackStorable )
    {
      if ( ! limitRackStorable.LimitRackModels.Any() ) return ( new List<string>(), new List<LimitRackModel>() ) ;
      
      var selectedLimitRackRefElements = ui.Selection
        .PickElementsByRectangle( LimitRackReferenceSelectionFilter.Instance, "Please select any rack or rack detail curve by mouse drag." ).ToList() ;
      if ( ! selectedLimitRackRefElements.Any() ) return ( new List<string>(), new List<LimitRackModel>() ) ;

      var selectedLimitRackModelsByAnyRack = limitRackStorable.LimitRackModels
        .Where( lmb => lmb.RackIds
          .Any( rackId => selectedLimitRackRefElements
            .Any( r => r.UniqueId == rackId ) ) )
        .EnumerateAll() ;
      if ( selectedLimitRackModelsByAnyRack.Any() ) {
        var allRacks = selectedLimitRackModelsByAnyRack.SelectMany( lm => lm.RackIds ).Distinct().EnumerateAll() ;
        return ( allRacks, selectedLimitRackModelsByAnyRack ) ;
      }

      var selectedLimitRackModelsByAnyDetailCurve = limitRackStorable.LimitRackModels
        .Where( lmb => lmb.RackDetailLineIds
          .Any( dtlId => selectedLimitRackRefElements
            .Any( r => r.UniqueId == dtlId ) ) )
        .EnumerateAll() ;
      if ( ! selectedLimitRackModelsByAnyDetailCurve.Any() ) return ( new List<string>(), new List<LimitRackModel>() ) ;
      {
        var allRacks = selectedLimitRackModelsByAnyDetailCurve.SelectMany( lm => lm.RackIds ).Distinct()
          .EnumerateAll() ;
        return ( allRacks, selectedLimitRackModelsByAnyDetailCurve ) ;
      }
    }

    protected override IEnumerable<string> GetBoundaryCableTrays( Document doc,IReadOnlyCollection<LimitRackModel> limitRackModels )
    {
      return limitRackModels.SelectMany( lm => lm.RackDetailLineIds ).Distinct().EnumerateAll() ;
    }

  }
}