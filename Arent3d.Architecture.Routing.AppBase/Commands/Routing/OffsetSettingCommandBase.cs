using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
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
      var cache = RouteCache.Get( document ) ;
      var hashSet = commandData.Application.ActiveUIDocument.Document.CollectRoutes( GetAddInType() ).Select( route => route.RouteName ).ToHashSet() ;

      //Call Open UI dialog
      var property = ShowDialog(document) ;
      if ( true != property?.DialogResult  ) return Result.Succeeded ;
      var value = property.OffsetNumeric.Value ;
      try {
        // get all envelop
        var envelops = document.GetAllFamilyInstances( RoutingFamilyType.Envelope ) ;
        foreach ( var envelop in envelops ) {
          //add border transparent
        }
        
        
        return Result.Succeeded ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }
    
    /// <summary>
    /// Show dialog Offset Setting
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    private OffsetSetting ShowDialog( Document document)
    {
      var sv = new OffsetSetting( document ) ;
      sv.ShowDialog() ;
      return sv ;
    }
    protected abstract AddInType GetAddInType() ;
  }
}