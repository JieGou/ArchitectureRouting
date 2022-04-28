using System ;
using System.Windows.Input ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class RelayCommand<T> : ICommand
  {
    private bool _isExecuting ;
    private readonly Predicate<T> _canExecute ;
    private readonly Action<T> _execute ;

    public RelayCommand( Action<T> execute )
    {
      _canExecute = _ => true ;
      _execute = execute ;
    }
    
    public RelayCommand( Predicate<T> canExecute, Action<T> execute )
    {
      _canExecute = canExecute ;
      _execute = execute ;
    }

    public bool CanExecute( object parameter )
    {
      try {
        return ! _isExecuting && ( _canExecute?.Invoke( (T) parameter ) ?? true ) ;
      }
      catch {
        return true ;
      }
    }

    public void Execute( object parameter )
    {
      try {
        _isExecuting = true ;
        _execute( (T) parameter ) ;
      }
      finally {
        _isExecuting = false ;
      }
    }

    public event EventHandler CanExecuteChanged
    {
      add => CommandManager.RequerySuggested += value ;
      remove => CommandManager.RequerySuggested -= value ;
    }
  }
  
  public class RelayCommand : RelayCommand<object>
  {
    public RelayCommand( Action execute ) : base( _ => execute() )
    {
    }

    public RelayCommand( Predicate<object> canExecute, Action execute ) : base( canExecute, _ => execute() )
    {
    }
  }
}