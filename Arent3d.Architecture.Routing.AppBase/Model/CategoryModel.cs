using System.Collections.Generic ;
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

    public static List<Arent3d.Architecture.Routing.Storable.Model.CategoryModel> ConvertCategoryModel( IEnumerable<CategoryModel> categoryModels )
    {
      var convertCategoriesModel = new List<Arent3d.Architecture.Routing.Storable.Model.CategoryModel>() ;
      foreach ( var category in categoryModels ) {
        var convertCategory = new Arent3d.Architecture.Routing.Storable.Model.CategoryModel( category.Name, category.ParentName, category.IsExpanded, category.IsSelected ) ;
        convertCategoriesModel.Add( convertCategory ) ;
        foreach ( var subCategory in category.SubCategories ) {
          var convertSubCategory = new Arent3d.Architecture.Routing.Storable.Model.CategoryModel( subCategory.Name, subCategory.ParentName, subCategory.IsExpanded, subCategory.IsSelected ) ;
          convertCategoriesModel.Add( convertSubCategory ) ;
          foreach ( var ceedCodeNumberCategory in subCategory.SubCategories ) {
            var convertCeedCodeNumberCategory = new Arent3d.Architecture.Routing.Storable.Model.CategoryModel( ceedCodeNumberCategory.Name, ceedCodeNumberCategory.ParentName, ceedCodeNumberCategory.IsExpanded, ceedCodeNumberCategory.IsSelected ) ;
            convertCategoriesModel.Add( convertCeedCodeNumberCategory ) ;
          }
        }
      }

      return convertCategoriesModel ;
    }
  }
}