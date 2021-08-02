using System.Collections.ObjectModel ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;


namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.ShowFrom_ToWindowCommand", DefaultString = "From-To\nWindow" )]
  [Image( "resources/From-ToWindow.png" )]
  public class ShowFrom_ToWindowCommand : ShowFrom_ToWindowCommandBase
  {
    protected override AddInType GetAddInType()
    {
      return AddInType.Electrical ;
    }

    protected override FromToWindow CreateFromToWindow( UIDocument uiDocument, ObservableCollection<FromToWindow.FromToItems> fromToItemsList )
    {
      return new Forms.FromToWindow( uiDocument, fromToItemsList ) ;
    }
  }
}