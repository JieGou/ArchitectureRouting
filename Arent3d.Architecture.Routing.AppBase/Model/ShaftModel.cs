using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Model
{
  public class ShaftModel : NotifyPropertyChanged
  {
    private Level _fromLevel ;

    public Level FromLevel
    {
      get => _fromLevel ;
      set
      {
        _fromLevel = value ;
        OnPropertyChanged() ;
      }
    }

    private Level _toLevel ;

    public Level ToLevel
    {
      get => _toLevel ;
      set
      {
        _toLevel = value ;
        OnPropertyChanged() ;
      }
    }

    private string _betweenFloors ;

    public string BetweenFloors
    {
      get => _betweenFloors ;
      set
      {
        _betweenFloors = value ;
        OnPropertyChanged() ;
      }
    }

    private bool _isShafted ;

    public bool IsShafted
    {
      get => _isShafted ;
      set
      {
        _isShafted = value ;
        if ( ! _isShafted ) IsRacked = false ;
        OnPropertyChanged() ;
      }
    }

    private bool _isRacked ;

    public bool IsRacked
    {
      get => _isRacked ;
      set
      {
        _isRacked = value ;
        OnPropertyChanged() ;
      }
    }

    public ShaftModel( Level fromLevel, Level toLevel )
    {
      _fromLevel = fromLevel ;
      _toLevel = toLevel ;
      _betweenFloors = fromLevel.Name + "-" + toLevel.Name + "階" ;
    }
  }
}