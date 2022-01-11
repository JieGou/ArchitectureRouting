using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using System.Collections.Generic ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.UI.Selection ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.AutoRoutingAnemostatCommand", DefaultString = "Auto Routing\nAnemostat" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class AutoRoutingAnemostatCommand : RoutingCommandBase<Element>
  {
    protected override string GetTransactionNameKey()
    {
      return "TransactionName.Commands.Routing.AutoRoutingAnemostat" ;
    }

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view )
    {
      return AppCommandSettings.CreateRoutingExecutor( document, view ) ;
    }

    protected override OperationResult<Element> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var fasu = SelectFasu( uiDocument ) ;
      return new OperationResult<Element>( fasu ) ;
    }

    private static Element SelectFasu( UIDocument uiDocument )
    {
      var doc = uiDocument.Document ;

      // Todo get fasu only
      var ductAccessoryFilter = new DuctAccessoryPickFilter() ;

      while ( true ) {
        var pickedObject = uiDocument.Selection.PickObject( ObjectType.Element, ductAccessoryFilter, "UiDocument.Selection.PickObject.Fasu".GetAppStringByKeyOrDefault( "Pick the FASU of a auto route Anemostat." ) ) ;
        var element = doc.GetElement( pickedObject.ElementId ) ;
        if ( null == element ) continue ;
        return element ;
      }
    }

    private class DuctAccessoryPickFilter : ISelectionFilter
    {
      public bool AllowElement( Element e )
      {
        return ( e.Category.Id.IntegerValue.Equals( (int) BuiltInCategory.OST_DuctAccessory ) ) ;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return false ;
      }
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, Element fasu )
    {
      document.Regenerate() ; // Apply Arent-RoundDuct-Diameter
      RouteGenerator.CorrectEnvelopes( document ) ;
      var anemostatRouter = new AutoRoutingAnemostat( document, fasu ) ;
      return anemostatRouter.Execute().EnumerateAll() ;
    }
  }
}