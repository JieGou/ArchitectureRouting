using System ;
using System.ComponentModel ;
using System.Runtime.CompilerServices ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class CnsSettingModel : NotifyPropertyChanged, ICloneable
  {
    private bool _isDefaultItemChecked ;

    public bool IsDefaultItemChecked
    {
      get => _isDefaultItemChecked ;
      set
      {
        _isDefaultItemChecked = value ;
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
    
    public bool IsHighLighted { get ; set ; }

    public CnsSettingModel( int sequence, string categoryName, bool isDefaultItemChecked = false )
    {
      Sequence = sequence ;
      CategoryName = categoryName ;
      Position = sequence ;
      IsDefaultItemChecked = isDefaultItemChecked ;
    }

    public bool Equals( CnsSettingModel other )
    {
      return Sequence == other.Sequence && CategoryName == other.CategoryName ;
    }
 
    public object Clone()
    {
      return this.MemberwiseClone() ;
    }
  }
}