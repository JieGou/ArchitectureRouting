using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Utils ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class DisplaySettingCommandBase : IExternalCommand
  {
    public const string LegendSelectionFilter = "ARENT_LEGEND" ;
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;

      var viewModel = new DisplaySettingViewModel( document ) ;
      var dialog = new DisplaySettingDialog( viewModel ) ;

      dialog.ShowDialog() ;
      return dialog.DialogResult == false ? Result.Cancelled : Result.Succeeded ;
    }
    
    public static void AddLegendToselectionFilter(Document document, ICollection<ElementId> addedElementIds)
    {
      if(!addedElementIds.Any())
        return;

      foreach ( var addedElementId in addedElementIds ) {
        if(document.GetElement(addedElementId) is not Viewport viewport || document.GetElement(viewport.ViewId) is not View { ViewType: ViewType.Legend } view )
          continue;
        
        FilterUtil.AddElementToSelectionFilter(LegendSelectionFilter, viewport);
        FilterUtil.AddElementToSelectionFilter(LegendSelectionFilter, view);
      }
    }
  }
}