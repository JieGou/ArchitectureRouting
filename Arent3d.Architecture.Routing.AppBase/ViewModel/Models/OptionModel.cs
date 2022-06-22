using Arent3d.Architecture.Routing.Storable.Model ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel.Models
{
  public class OptionModel : NotifyPropertyChanged
  {
    private string? _name ;
    public string Name
    {
      get => _name ??= string.Empty ;
      set
      {
        _name = value ;
        OnPropertyChanged();
      }
    }

    private bool _isChecked ;
    public bool IsChecked
    {
      get => _isChecked ;
      set
      {
        _isChecked = value ;
        OnPropertyChanged();
      }
    }
  }
}