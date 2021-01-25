using System ;
using System.Threading ;
using System.Threading.Tasks ;
using System.Windows.Threading ;

namespace Arent3d.Revit
{
  public abstract class ThreadDispatcher
  {
    public static Dispatcher? UiDispatcher { get ; set ; }
    
    public static void DoEvents()
    {
      var frame = new DispatcherFrame() ;
      var callback = new DispatcherOperationCallback( _ =>
      {
        frame.Continue = false ;
        return null ;
      } ) ;

      Dispatcher.CurrentDispatcher.BeginInvoke( DispatcherPriority.Background, callback, null ) ;
      Dispatcher.PushFrame( frame ) ;
    }

    public static void WaitWithDoEvents( Task task )
    {
      while ( false == task.IsCompleted ) {
        Thread.Sleep( 1 ) ;
        DoEvents() ;
      }
    }

    public static void Dispatch( Action action )
    {
      var dispatcher = UiDispatcher ?? Dispatcher.CurrentDispatcher ;

      if ( dispatcher.CheckAccess() ) {
        action.Invoke() ;
      }
      else {
        dispatcher.Invoke( action ) ;
      }
    }
  }
}