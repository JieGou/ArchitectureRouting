using System ;

#nullable disable

namespace Arent3d.Architecture.Routing
{
  public class ValueChangedEventArgs<T> : EventArgs
  {
    public T OldValue { get ; }
    public T NewValue { get ; }
    
    public ValueChangedEventArgs( T oldValue, T newValue )
    {
      OldValue = oldValue ;
      NewValue = newValue ;
    }
    
  }
}

#nullable restore