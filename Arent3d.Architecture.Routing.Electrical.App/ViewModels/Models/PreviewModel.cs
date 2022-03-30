using System.Windows.Media.Imaging ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.Electrical.App.ViewModels.Models
{
  public class PreviewModel : NotifyPropertyChanged
  {
    private BitmapSource? _thumbnail ;

    public BitmapSource? Thumbnail
    {
      get => _thumbnail ;
      set
      {
        _thumbnail = value ;
        OnPropertyChanged();
      }
    }

    private string? _imageName ;

    public string FileName
    {
      get { return _imageName ??= string.Empty ; }
      set
      {
        _imageName = value ;
        OnPropertyChanged();
      }
    }

    private string? _path ;

    public string Path
    {
      get { return _path ??= string.Empty ; }
      set
      {
        _path = value ;
        OnPropertyChanged();
      }
    }

    private bool? _isSelected ;

    public bool IsSelected
    {
      get { return _isSelected ??= false ; }
      set
      {
        _isSelected = value ;
        OnPropertyChanged();
      }
    }
  }
}