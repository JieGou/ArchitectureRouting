using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Demo
{
  [DisplayName( "Demo: Delete All Routed Elements" )]
  [Transaction( TransactionMode.Manual )]
  public class Demo_DeleteAllRoutedElements : IExternalCommand
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
        
        return Result.Succeeded ;
      } ) ;
    }
  }
}