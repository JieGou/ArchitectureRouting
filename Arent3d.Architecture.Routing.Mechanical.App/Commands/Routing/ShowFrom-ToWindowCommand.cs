using System.Collections.ObjectModel ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.Mechanical.App.Commands.PostCommands ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.ShowFrom_ToWindowCommand", DefaultString = "From-To\nWindow" )]
  [Image( "resources/From-ToWindow.png" )]
  public class ShowFrom_ToWindowCommand : ShowFrom_ToWindowCommandBase
  {
    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;

    protected override FromToWindow CreateFromToWindow( UIDocument uiDocument, ObservableCollection<FromToWindow.FromToItems> fromToItemsList )
    {
      return new FromToWindow( FromToWindowBehaviour.Instance, uiDocument, fromToItemsList ) ;
    }

    private class FromToWindowBehaviour : IFromToWindowBehaviour
    {
      public static FromToWindowBehaviour Instance { get ; } = new FromToWindowBehaviour() ;

      private FromToWindowBehaviour()
      {
      }

      public string Title => "Dialog.Forms.FromToWindow.Title".GetAppStringByKeyOrDefault( "From-To Window" ) ;

      public void PostImportCommand( UIApplication application )
      {
        application.PostCommand<FileRoutingCommand>() ;
      }

      public void PostExportCommand( UIApplication application )
      {
        application.PostCommand<ExportRoutingCommand>() ;
      }
    }
  }
}