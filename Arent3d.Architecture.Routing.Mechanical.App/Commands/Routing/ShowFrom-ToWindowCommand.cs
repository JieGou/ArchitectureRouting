using System.Collections.ObjectModel ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
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

    protected override FromToWindowBase CreateFromToWindow( UIDocument uiDocument, ObservableCollection<FromToWindowBase.FromToItems> fromToItemsList )
    {
      return new Forms.FromToWindow( uiDocument, fromToItemsList ) ;
    }
  }
}