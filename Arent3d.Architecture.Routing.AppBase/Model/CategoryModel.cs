using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;

namespace Arent3d.Architecture.Routing.AppBase.Model
{
  public class CategoryModel : NotifyPropertyChanged
  {
    private string? _name ;

    public string Name
    {
      get { return _name ??= string.Empty ; }
      set
      {
        _name = value ;
        OnPropertyChanged();
      }
    }

    private string? _parentName ;

    public string ParentName
    {
      get { return _parentName ??= string.Empty ; }
      set
      {
        _parentName = value ;
        OnPropertyChanged();
      }
    }

    private bool? _isExpanded ;

    public bool IsExpanded
    {
      get { return _isExpanded ??= false ; }
      set
      {
        _isExpanded = value ;
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
    
    public List<CategoryModel> SubCategories { get ; set ; } = new() ;
  }
}