using System ;
using System.ComponentModel ;
using System.Diagnostics ;
using System.Threading ;
using System.Windows ;
using System.Windows.Threading ;

namespace Arent3d.Revit.UI.Forms
{
  public partial class ProgressBar : Window
  {
    private readonly CancellationTokenSource? _cancellationTokenSource ;
    private readonly ProgressData _progressData ;

    public event CancelEventHandler? CancelButtonClick ;

    public static readonly DependencyProperty ProgressValueProperty = DependencyProperty.Register( "ProgressValue", typeof( double ), typeof( ProgressBar ), new PropertyMetadata( 0d ) ) ;

    public double ProgressValue
    {
      get => (double) GetValue( ProgressValueProperty ) ;
      private set
      {
        if ( Dispatcher.CheckAccess() ) {
          SetValue( ProgressValueProperty, value ) ;
        }
        else {
          Dispatcher.BeginInvoke( (Action) ( () =>
          {
            SetValue( ProgressValueProperty, value ) ;
          } ) ) ;
        }
      }
    }

    private ProgressBar( CancellationTokenSource? cancellationTokenSource )
    {
      InitializeComponent() ;

      _cancellationTokenSource = cancellationTokenSource ;
      _progressData = new ProgressData() ;
      _progressData.Progress += ProgressData_Progress ;
    }

    private void ProgressData_Progress( object sender, ProgressEventArgs e )
    {
      if ( e.IsFinished ) {
        if ( Dispatcher.CheckAccess() ) {
          this.Close() ;
        }
        else {
          Dispatcher.BeginInvoke( (Action) Close ) ;
        }
      }
      else {
        this.ProgressValue = e.CurrentValue ;
      }
    }

    public static ProgressData Show( CancellationTokenSource? cancellationTokenSource )
    {
      var window = new ProgressBar( cancellationTokenSource ) ;
      window.Show() ;

      return window._progressData ;
    }

    public static ProgressData ShowWithNewThread( CancellationTokenSource? cancellationTokenSource )
    {
      ProgressBar? window = null ;
      var condition = new CountdownEvent( 1 ) ;

      var t = new Thread( _ =>
      {
        window = new ProgressBar( cancellationTokenSource ) ;
        window.Closed += ( _, __ ) =>
        {
          Dispatcher.CurrentDispatcher.BeginInvokeShutdown( DispatcherPriority.SystemIdle ) ;
          Dispatcher.Run() ;
        } ;
        condition.Signal() ;

        window.ShowDialog() ;
      } ) ;
      t.SetApartmentState( ApartmentState.STA ) ;
      t.Start() ;

      condition.Wait() ;

      return window!._progressData ;
    }

    private void CancelButton_OnClick( object sender, RoutedEventArgs e )
    {
      if ( null != CancelButtonClick ) {
        var cancelEventArgs = new CancelEventArgs() ;
        CancelButtonClick.Invoke( this, cancelEventArgs ) ;
        if ( cancelEventArgs.Cancel ) return ;  // Cancel cancellations.
      }

      _cancellationTokenSource?.Cancel() ;
      Close() ;
    }

    private void Window_OnUnloaded( object sender, RoutedEventArgs e )
    {
      _progressData.Finish() ;
      _cancellationTokenSource?.Dispose() ;
    }
  }
}