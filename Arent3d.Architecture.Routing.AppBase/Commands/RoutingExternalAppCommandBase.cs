using System ;
using Arent3d.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands
{
  public abstract class RoutingExternalAppCommandBase<TUIResult> : ExternalCommandBase<TUIResult>
  {
    protected override string? OnException( Exception e, TUIResult? uiResult )
    {
      CommandUtils.DebugAlertException( e ) ;
      return null ;
    }
  }
  public abstract class RoutingExternalAppCommandBase : ExternalCommandBase
  {
    protected override string? OnException( Exception e )
    {
      CommandUtils.DebugAlertException( e ) ;
      return null ;
    }
  }
  public abstract class RoutingExternalAppCommandBaseWithParam<TParam, TUIResult> : ExternalCommandBaseWithParam<TParam, TUIResult>
  {
    protected override string? OnException( TParam? param, Exception e, TUIResult? uiResult )
    {
      CommandUtils.DebugAlertException( e ) ;
      return null ;
    }
  }
  public abstract class RoutingExternalAppCommandBaseWithParam<TParam> : ExternalCommandBaseWithParam<TParam>
  {
    protected override string? OnException( TParam? param, Exception e )
    {
      CommandUtils.DebugAlertException( e ) ;
      return null ;
    }
  }
}