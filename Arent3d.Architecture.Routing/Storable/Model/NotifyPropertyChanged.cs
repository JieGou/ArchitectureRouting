using System.ComponentModel ;
using System.Runtime.CompilerServices ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class NotifyPropertyChanged : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler? PropertyChanged ;

    protected void OnPropertyChanged( [CallerMemberName] string? propertyName = null )
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