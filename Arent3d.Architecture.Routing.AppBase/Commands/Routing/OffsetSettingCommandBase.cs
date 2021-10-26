using System ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class OffsetSettingCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      //Call Open UI dialog
      var property = ShowDialog( document ) ;
      if ( true != property?.DialogResult ) return Result.Succeeded ;
      var value = property.OffsetNumeric.Value ;
      try {
        // get all envelop
        var envelops = document.GetAllFamilyInstances( RoutingFamilyType.Envelope ) ;
        foreach ( var envelop in envelops ) {
        }


        return Result.Succeeded ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    /// <summary>
    ///   Show dialog Offset Setting
    /// </summary>
    private static OffsetSetting ShowDialog( Document document )
    {
      var sv = new OffsetSetting( document ) ;
      sv.ShowDialog() ;
      return sv ;
    }
  }
}