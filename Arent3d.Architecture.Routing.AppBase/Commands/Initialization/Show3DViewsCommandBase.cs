using System ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class Show3DViewsCommandBase: IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;

      var dialog = new GetLevel( document ) ;
      if ( false == dialog.ShowDialog() ) return Result.Succeeded ;

      try {
        return document.Transaction( "Electrical.App.Commands.Initialization.Show3DViewsCommand".GetAppStringByKeyOrDefault( "階ごとの3dビュー作成" ), _ =>
        {
          document.Create3DView( dialog.GetSelectedLevels() ) ;
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