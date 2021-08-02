using System.Collections.ObjectModel ;
using Arent3d.Architecture.Routing.Mechanical.App.Commands.PostCommands ;
using Autodesk.Revit.UI ;
using Arent3d.Revit.UI ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Forms
{
  public class FromToWindow : AppBase.Forms.FromToWindow
  {
    public FromToWindow( UIDocument uiDoc, ObservableCollection<FromToItems> fromToItemsList ) : base( uiDoc, fromToItemsList )
    {
    }

    protected override void OnImportButtonClick()
    {
      UiDocument.Application.PostCommand<FileRoutingCommand>();
    }

    protected override void OnExportButtonClick()
    {
      UiDocument.Application.PostCommand<ExportRoutingCommand>();
    }
  }
}