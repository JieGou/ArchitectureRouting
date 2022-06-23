using System.Collections.Generic;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using System.Linq;
using System.Windows ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Storable.Model ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class ArentRoomViewModel : NotifyPropertyChanged
  {

    #region Properties

    private List<string>? _conditions ;
    public List<string> Conditions
    {
      get => _conditions ??= new List<string>() ;
      set
      {
        _conditions = value ;
        SelectedCondition = _conditions.FirstOrDefault() ;
        OnPropertyChanged();
      }
    }

    private string? _selectedCondition ;
    public string? SelectedCondition
    {
      get => _selectedCondition ??= Conditions.FirstOrDefault() ;
      set { _selectedCondition = value ; OnPropertyChanged(); }
    }
    
    public bool IsCreate { get ; set ; }

    #endregion

    #region Commands

    public ICommand CreateCommand
    {
      get
      {
        return new RelayCommand<Window>( wd => null != wd, wd =>
        {
          IsCreate = true ;
          wd.Close();
        } ) ;
      }
    }

    #endregion

  }
}