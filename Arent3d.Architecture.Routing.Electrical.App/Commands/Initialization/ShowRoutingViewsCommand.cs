using System ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Initialization.ShowRoutingViewsCommand", DefaultString = "Plans" )]
  [Image( "resources/Plans.png", ImageType = ImageType.Large )]
  public class ShowRoutingViewsCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;

      var dialog = new GetLevel( document ) ;
      if ( false == dialog.ShowDialog() ) return Result.Succeeded ;

      try {
        return document.Transaction( "TransactionName.Commands.Initialization.CreateRoutingViews".GetAppStringByKeyOrDefault( "Create Routing Views" ), _ =>
        {
          document.CreateRoutingView( dialog.GetSelectedLevels() ) ;
          return Result.Succeeded ;
        } ) ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }
  }
}