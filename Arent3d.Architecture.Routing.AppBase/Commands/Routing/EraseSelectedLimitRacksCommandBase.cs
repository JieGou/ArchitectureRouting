using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
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

      var selectedLimitRackModelsByAnyRacks = limitRackStorable.LimitRackModels
        .Where( lmb => lmb.RackIds
          .Any( rackId => selectedLimitRackRefElements
            .Any( r => r.UniqueId == rackId ) ) )
        .EnumerateAll() ;
      if ( selectedLimitRackModelsByAnyRacks.Any() ) {
        var allRacks = selectedLimitRackModelsByAnyRacks.SelectMany( lm => lm.RackIds ).Distinct().EnumerateAll() ;
        return ( allRacks, selectedLimitRackModelsByAnyRacks ) ;
      }

      var selectedLimitRackModelsByAnyDetailCurves = limitRackStorable.LimitRackModels
        .Where( lmb => lmb.RackDetailLineIds
          .Any( dtlId => selectedLimitRackRefElements
            .Any( r => r.UniqueId == dtlId ) ) )
        .ToList() ;

      var removeLimitRackModels = new List<LimitRackModel>() ;
      foreach ( var selectedLimitRackModel in selectedLimitRackModelsByAnyDetailCurves ) {
        var selectedDetailIds = selectedLimitRackRefElements.Where( lrm =>
          selectedLimitRackModel.RackDetailLineIds.Any( id => id.Equals( lrm.UniqueId ) ) ) ;
        if ( selectedDetailIds.Count() != selectedLimitRackModel.RackDetailLineIds.Count() ) {
          removeLimitRackModels.Add( selectedLimitRackModel );
        }
      }

      if ( removeLimitRackModels.Any() ) {
        MessageBox.Show( "Some limit rack can not be delete. Please select all detail line for limit rack you want to delete." ) ;
        removeLimitRackModels.ForEach( rlmd=> selectedLimitRackModelsByAnyDetailCurves. Remove( rlmd ));
      }
      
      if ( ! selectedLimitRackModelsByAnyDetailCurves.Any() ) return ( new List<string>(), new List<LimitRackModel>() ) ;
      {
        var allRacks = selectedLimitRackModelsByAnyDetailCurves.SelectMany( lm => lm.RackIds ).Distinct()
          .EnumerateAll() ;
        return ( allRacks, selectedLimitRackModelsByAnyDetailCurves ) ;
      }
    }

    protected override IEnumerable<string> GetBoundaryCableTrays( Document doc,IReadOnlyCollection<LimitRackModel> limitRackModels )
    {
      return limitRackModels.SelectMany( lm => lm.RackDetailLineIds ).Distinct().EnumerateAll() ;
    }

  }
}