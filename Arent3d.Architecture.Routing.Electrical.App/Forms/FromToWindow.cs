using System.Collections.ObjectModel ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.PostCommands ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.UI ;
using Arent3d.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Forms
{
  public class FromToWindow : AppBase.Forms.FromToWindowBase
  {
    public FromToWindow( UIDocument uiDoc, ObservableCollection<FromToItems> fromToItemsList ) : base( uiDoc, fromToItemsList )
    {
      Title = "Dialog.Forms.FromToWindow.Title".GetAppStringByKeyOrDefault( Title ) ;
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