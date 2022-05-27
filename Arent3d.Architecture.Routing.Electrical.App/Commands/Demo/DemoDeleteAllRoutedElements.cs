using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Extensions ;
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
      return document.Transaction( "Delete All Routed Elements", t =>
      {
        var deletingCategories = new HashSet<BuiltInCategory>
        {
          BuiltInCategory.OST_Conduit,
          BuiltInCategory.OST_ConduitFitting,
        } ;

        var elementsToDelete = document.GetAllElementsOfRoute<Element>().Where( e => deletingCategories.Contains( e.GetBuiltInCategory() ) ).Select( e => e.Id ).ToList() ;
        document.Delete( elementsToDelete ) ;

        DeleteBoundaryRack( document ) ;
        
        return Result.Succeeded ;
      } ) ;
    }

    private void DeleteBoundaryRack(Document document)
    {
      var curveELements = document.GetAllInstances<CurveElement>().Where( x => x.LineStyle.Name == EraseAllLimitRackCommandBase.BoundaryCableTrayLineStyleName ).ToList() ;
      if(!curveELements.Any())
        return;

      document.Delete( curveELements.Select( x => x.Id ).ToList() ) ;
    }
  }
}