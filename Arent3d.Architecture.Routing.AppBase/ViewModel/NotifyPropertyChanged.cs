using System.ComponentModel ;
using System.Runtime.CompilerServices ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class NotifyPropertyChanged : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler? PropertyChanged ;

    public virtual void OnPropertyChanged( [CallerMemberName] string? propertyName = null )
    {
      PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) ) ;
    }

    public static event PropertyChangedEventHandler? StaticPropertyChanged ;

    public static void StaticOnPropertyChanged( [CallerMemberName] string? propertyName = null )
    {
      StaticPropertyChanged?.Invoke( null, new PropertyChangedEventArgs( propertyName ) ) ;
    }
  }
}