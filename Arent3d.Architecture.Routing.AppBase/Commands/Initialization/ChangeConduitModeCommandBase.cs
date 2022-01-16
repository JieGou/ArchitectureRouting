using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Threading ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Commands.Base ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Enums ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using ProgressBar = Arent3d.Revit.UI.Forms.ProgressBar ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class ChangeConduitModeCommandBase: ConduitCommandBase, IExternalCommand
  {
    protected ElectricalMode Mode ;
    private UIDocument UiDocument { get ; set ; } = null! ;
    
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      UiDocument = commandData.Application.ActiveUIDocument ;
      Document document = UiDocument.Document ;
      MessageBox.Show( "Dialog.Electrical.SelectElement.Message".GetAppStringByKeyOrDefault( "Please select a range." ), "Dialog.Electrical.SelectElement.Title".GetAppStringByKeyOrDefault( "Message" ), MessageBoxButtons.OK ) ;
      var selectedElements = UiDocument.Selection.PickElementsByRectangle( ConduitWithStartEndSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).Where( p => p is FamilyInstance or Conduit ) ;
      var conduitList = selectedElements.ToList() ;
      if ( ! conduitList.Any() ) {
        message = "No Conduits are selected." ;
      }
      var listApplyConduit = GetConduitRelated(document, conduitList) ;
      using var transaction = new Transaction( document ) ;
      transaction.Start( "Set conduits mode" ) ;
      SetModeForConduit( listApplyConduit.ToList(), Mode ) ;
      transaction.Commit() ;
      MessageBox.Show(
        string.IsNullOrEmpty( message )
          ? "Dialog.Electrical.SetElementProperty.Success".GetAppStringByKeyOrDefault( "Success" )
          : message,
        "Dialog.Electrical.SetElementProperty.Title".GetAppStringByKeyOrDefault( "Construction item addition result" ),
        MessageBoxButtons.OK ) ;
      return Result.Succeeded ;
    }
    
    private static void SetModeForConduit( List<Element> elements, ElectricalMode mode )
    {
      foreach ( var conduit in elements ) {
        conduit.SetProperty( RoutingFamilyLinkedParameter.Mode, mode.ToString() ) ;
      }
    }
  }
}