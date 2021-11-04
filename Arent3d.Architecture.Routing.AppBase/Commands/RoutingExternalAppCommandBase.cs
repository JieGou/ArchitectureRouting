using System ;
using Arent3d.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands
{
  public abstract class RoutingExternalAppCommandBase<TUIResult> : ExternalCommandBase<TUIResult>
  {
    protected override void OnException( Exception e, TUIResult? uiResult )
    {
      CommandUtils.DebugAlertException( e ) ;
    }
  }
  public abstract class RoutingExternalAppCommandBase : ExternalCommandBase
  {
    protected override void OnException( Exception e )
    {
      CommandUtils.DebugAlertException( e ) ;
    }
  }
  public abstract class RoutingExternalAppCommandBaseWithParam<TUIResult, TParam> : ExternalCommandBaseWithParam<TUIResult, TParam>
  {
    protected override void OnException( Exception e, (TUIResult, TParam) uiResult )
    {
      CommandUtils.DebugAlertException( e ) ;
    }
  }
  public abstract class RoutingExternalAppCommandBaseWithParam<TParam> : ExternalCommandBaseWithParam<TParam>
  {
    protected override void OnException( Exception e )
    {
      CommandUtils.DebugAlertException( e ) ;
    }
  }
}