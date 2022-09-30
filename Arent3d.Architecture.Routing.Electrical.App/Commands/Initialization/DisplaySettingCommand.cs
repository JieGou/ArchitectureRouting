using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Utils ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Initialization.DisplaySettingCommand", DefaultString = "Display Setting" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class DisplaySettingCommand : DisplaySettingCommandBase
  {
    public static void AddLegendToselectionFilter(Document document, ICollection<ElementId> addedElementIds)
    {
      if(!addedElementIds.Any())
        return;

      foreach ( var addedElementId in addedElementIds ) {
        if(document.GetElement(addedElementId) is not Viewport viewport || document.GetElement(viewport.SheetId) is not ViewSheet { ViewType: ViewType.Legend } )
          continue;
        
        FilterUtil.AddElementToSelectionFilter(LegendSelectionFilter, viewport);
      }
    }
  }
}