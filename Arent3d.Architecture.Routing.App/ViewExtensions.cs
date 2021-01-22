using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.App.Forms ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.App
{
  public static class ViewExtensions
  {
    private const string RoutingViewPostFix = "Routing Assist" ;
    
    public static void CreateRoutingView( this Document document, IReadOnlyCollection<(ElementId Id, string Name)> levels )
    {
      var floorPlanFamily = document.GetAllElements<ViewFamilyType>().FirstOrDefault( viewFamilyType => viewFamilyType.ViewFamily == ViewFamily.FloorPlan ) ?? throw new InvalidOperationException() ;

      using var tx = new Transaction( document ) ;
      tx.Start( "Create Views" ) ;
      foreach ( var (id, name) in levels ) {
        var view = ViewPlan.Create( document, floorPlanFamily.Id, id ) ;
        view.Name = $"{name} - {RoutingViewPostFix}" ;
        view.ViewTemplateId = new ElementId( -1 ) ;
        view.get_Parameter( BuiltInParameter.VIEW_DISCIPLINE ).Set( 4095 ) ;
      }

      tx.Commit() ;
    }
  }
}