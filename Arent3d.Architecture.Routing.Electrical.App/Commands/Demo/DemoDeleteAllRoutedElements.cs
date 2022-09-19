using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.Electrical.App.ViewModels ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Demo
{
  [DisplayName( "ルーティング要素\nのみ削除" )]
  [Transaction( TransactionMode.Manual )]
  public class DemoDeleteAllRoutedElements : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      return document.Transaction( "Delete All Routed Elements", _ =>
      {
        var deletingCategories = new HashSet<BuiltInCategory> { BuiltInCategory.OST_Conduit, BuiltInCategory.OST_ConduitFitting, } ;

        var elementsToDelete = document.GetAllElementsOfRoute<Element>().Where( e => deletingCategories.Contains( e.GetBuiltInCategory() ) ).Select( e => e.Id ).ToList() ;
        document.Delete( elementsToDelete ) ;

        DeleteNotationAndRack( document ) ;
        DeleteLocationConduit( document ) ;

        return Result.Succeeded ;
      } ) ;
    }

    private void DeleteNotationAndRack( Document document )
    {
      var rackNotationStorable = document.GetAllStorables<RackNotationStorable>().FirstOrDefault() ?? document.GetRackNotationStorable() ;
      foreach ( var notationModelData in rackNotationStorable.RackNotationModelData ) {
        if ( document.GetElement( notationModelData.RackId ) is { } rack ) {
          document.Delete( rack.Id ) ;
          notationModelData.RackId = string.Empty ;

          var cableTrayFittings = document.GetAllInstances<FamilyInstance>().Where( x => x.Category.Id.IntegerValue == (int) BuiltInCategory.OST_CableTrayFitting ).ToList() ;
          if ( cableTrayFittings.Any() )
            document.Delete( cableTrayFittings.Select( x => x.Id ).ToList() ) ;
        }
        
        if ( document.GetElement( notationModelData.NotationId ) is { } textNote ) {
          document.Delete( textNote.Id ) ;
          notationModelData.NotationId = string.Empty ;
        }

        if ( document.GetElement( notationModelData.EndLineLeaderId ) is { } endLine ) {
          document.Delete( endLine.Id ) ;
          notationModelData.EndLineLeaderId = string.Empty ;
        }

        foreach ( var otherLineId in notationModelData.OtherLineIds ) {
          if ( document.GetElement( otherLineId ) is { } otherLine )
            document.Delete( otherLine.Id ) ;
        }

        notationModelData.OtherLineIds = new List<string>() ;
      }

      rackNotationStorable.Save() ;
    }

    private void DeleteLocationConduit( Document document )
    {
      var familyNames = ChangeWireSymbolUsingDetailItemViewModel.WireSymbolOptions.Values ;
      var detailItems = document.GetAllInstances<FamilyInstance>().Where( x => familyNames.Any( y => y == x.Symbol.Family.Name ) ).ToList() ;
      if ( detailItems.Any() )
        document.Delete( detailItems.Select( x => x.Id ).ToList() ) ;

      var curveELements = document.GetAllInstances<CurveElement>().Where( x => x.LineStyle.Name == "LeakageZone" ).ToList() ;
      if ( ! curveELements.Any() )
        return ;

      document.Delete( curveELements.Select( x => x.Id ).ToList() ) ;
    }
  }
}