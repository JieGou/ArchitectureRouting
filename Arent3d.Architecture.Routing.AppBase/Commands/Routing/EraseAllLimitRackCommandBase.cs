using System ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class EraseAllLimitRackCommandBase : EraseLimitRackCommandBase
  {
    protected override (IEnumerable<string> rackIds, IEnumerable<string>? detailCurverIds, IEnumerable<LimitRackModel>? selectedLimitRackModels) GetLimitRackIds( UIDocument ui,
      Document doc, LimitRackStorable limitRackStorable )
    {
      var cableTrays = doc.GetAllFamilyInstances( ElectricalRoutingFamilyType.CableTray ) ;
      var cableTrayFittings = doc.GetAllFamilyInstances( ElectricalRoutingFamilyType.CableTrayFitting ) ;
      var allLimitRack = new List<string>() ;
      foreach ( var cableTray in cableTrays ) {
        var comment = cableTray.ParametersMap.get_Item( "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( doc, "Rack Type" ) ).AsString() ;
        if ( comment == NewRackCommandBase.RackTypes[ 1 ] )
          allLimitRack.Add( cableTray.UniqueId ) ;
      }

      foreach ( var cableTrayFitting in cableTrayFittings ) {
        var comment = cableTrayFitting.ParametersMap.get_Item( "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( doc, "Rack Type" ) ).AsString() ;
        if ( comment == NewRackCommandBase.RackTypes[ 1 ] )
          allLimitRack.Add( cableTrayFitting.UniqueId ) ;
      }

      return new ValueTuple<IEnumerable<string>, IEnumerable<string>?, IEnumerable<LimitRackModel>?>( allLimitRack, null, null ) ;

    }
    
    protected override void RemoveLimitRackModels( LimitRackStorable limitRackStorable, IEnumerable<LimitRackModel>? selectedLimitRackModels )
    {
      limitRackStorable.LimitRackModelData.Clear() ;
      limitRackStorable.Save() ;
    }
    
  }
}