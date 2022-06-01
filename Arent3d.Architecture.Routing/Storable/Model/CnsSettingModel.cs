using System ;
using System.ComponentModel ;
using System.Runtime.CompilerServices ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class CnsSettingModel : INotifyPropertyChanged, ICloneable
  {
    private bool _isChecked ;

    public bool IsChecked
    {
      get => _isChecked ;
      set
      {
        _isChecked = value ;
        OnPropertyChanged() ;
      }
    }
    
    private int _sequence ;

    public int Sequence
    {
      get => _sequence ;
      set
      {
        _sequence = value ;
        OnPropertyChanged() ;
      }
    }

    private int _position ;

    public int Position
    {
      get => _position ;
      set
      {
        _position = value ;
        OnPropertyChanged() ;
      }
    }

    public string CategoryName { get ; set ; }

    public CnsSettingModel( int sequence, string categoryName, bool isChecked = false )
    {
      _sequence = sequence ;
      CategoryName = categoryName ;
      _position = sequence ;
      _isChecked = isChecked ;
    }

    public bool Equals( CnsSettingModel other )
    {
      return Sequence == other.Sequence && CategoryName == other.CategoryName ;
    }

    public event PropertyChangedEventHandler? PropertyChanged ;

    protected virtual void OnPropertyChanged( [CallerMemberName] string propertyName = "" )
    {
      PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) ) ;
    }

    public object Clone()
    {
      return this.MemberwiseClone() ;
    }
  }
}