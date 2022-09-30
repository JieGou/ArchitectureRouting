using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;

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
    
    private bool? _isExistModelNumber ;

    public bool IsExistModelNumber
    {
      get { return _isExistModelNumber ??= false ; }
      set
      {
        _isExistModelNumber = value ;
        OnPropertyChanged();
      }
    }

    private bool? _isMainConstruction ;
    public bool IsMainConstruction
    {
      get { return _isMainConstruction ??= false ; }
      set
      {
        _isMainConstruction = value ;
        OnPropertyChanged();
      }
    }
    
    public List<CategoryModel> Categories { get ; set ; } = new() ;
    
    public List<CategoryModel> CeedCodeNumbers { get ; set ; } = new() ;

    public static List<Arent3d.Architecture.Routing.Storable.Model.CategoryModel> ConvertCategoryModel( IEnumerable<CategoryModel> categoryModels )
    {
      var convertCategoriesModel = new List<Storable.Model.CategoryModel>() ;
      foreach ( var category in categoryModels ) {
        var convertCategory = new Storable.Model.CategoryModel( category.Name, category.ParentName, category.IsExpanded, category.IsSelected, false, category.IsExistModelNumber, category.IsMainConstruction ) ;
        convertCategoriesModel.Add( convertCategory ) ;
        foreach ( var subCategory in category.Categories ) {
          var convertSubCategory = new Storable.Model.CategoryModel( subCategory.Name, subCategory.ParentName, subCategory.IsExpanded, subCategory.IsSelected, false, subCategory.IsExistModelNumber, subCategory.IsMainConstruction ) ;
          convertCategoriesModel.Add( convertSubCategory ) ;
          foreach ( var ceedCodeNumberCategory in subCategory.CeedCodeNumbers ) {
            var convertCeedCodeNumberCategory = new Storable.Model.CategoryModel( ceedCodeNumberCategory.Name, ceedCodeNumberCategory.ParentName, ceedCodeNumberCategory.IsExpanded, ceedCodeNumberCategory.IsSelected, true, ceedCodeNumberCategory.IsExistModelNumber, ceedCodeNumberCategory.IsMainConstruction ) ;
            convertCategoriesModel.Add( convertCeedCodeNumberCategory ) ;
          }
        }
      }

      return convertCategoriesModel ;
    }
    
    public static List<CategoryModel> ConvertCategoryModel( ICollection<Storable.Model.CategoryModel> categoryModels )
    {
      var convertCategoriesModel = new List<CategoryModel>() ;
      var parentCategories = categoryModels.Where( c => string.IsNullOrEmpty( c.ParentName ) ) ;
      var subCategories = categoryModels.Where( c => ! string.IsNullOrEmpty( c.ParentName ) && ! c.IsCeedCodeNumber ) ;
      foreach ( var category in parentCategories ) {
        var convertCategory = new CategoryModel { Name = category.Name, ParentName = category.ParentName, IsExpanded = category.IsExpanded, IsSelected = category.IsSelected, IsExistModelNumber = category.IsExistModelNumber, IsMainConstruction = category.IsMainConstruction } ;
        convertCategoriesModel.Add( convertCategory ) ;
      }
        
      foreach ( var category in subCategories ) {
        var parentCategory = convertCategoriesModel.FirstOrDefault( c => c.Name == category.ParentName ) ;
        if ( parentCategory == null ) continue ;
        var convertCategory = new CategoryModel { Name = category.Name, ParentName = category.ParentName, IsExpanded = category.IsExpanded, IsSelected = category.IsSelected, IsExistModelNumber = category.IsExistModelNumber, IsMainConstruction = category.IsMainConstruction  } ;
        var ceedCodeNumbers = categoryModels.Where( c => c.ParentName == category.Name && c.IsCeedCodeNumber ) ;
        foreach ( var ceedCodeNumberModel in ceedCodeNumbers ) {
          var convertCeedCodeNumberModel = new CategoryModel { Name = ceedCodeNumberModel.Name, ParentName = ceedCodeNumberModel.ParentName, IsExpanded = ceedCodeNumberModel.IsExpanded, IsSelected = ceedCodeNumberModel.IsSelected, IsExistModelNumber = category.IsExistModelNumber, IsMainConstruction = category.IsMainConstruction } ;
          convertCategory.CeedCodeNumbers.Add( convertCeedCodeNumberModel ) ;
        }
        parentCategory.Categories.Add( convertCategory ) ;
      }
      
      return convertCategoriesModel ;
    }

    public static List<string> GetCeedModelNumbers( IEnumerable<Storable.Model.CategoryModel> categoryModels )
    {
      var ceedCodeNumbers = categoryModels.Where( c => ! string.IsNullOrEmpty( c.ParentName ) && c.IsCeedCodeNumber ).Select( c => c.Name ).Distinct().ToList() ;
      return ceedCodeNumbers ;
    }

    public static bool IsMainConstructionCeedModelNumber( Document document, string ceedCodeNumber )
    {
      var ceedStorable = document.GetCeedStorable() ;
      var ceedCodeNumbers = ceedStorable.CategoriesWithoutCeedCode.Where( c => ! string.IsNullOrEmpty( c.ParentName ) && c.IsCeedCodeNumber && c.IsMainConstruction ).Select( c => c.Name ).Distinct().ToList() ;
      return ceedCodeNumbers.Contains( ceedCodeNumber ) ;
    }
    
    public static bool IsMainConstructionModelNumber( Document document, string modelNumber )
    {
      var ceedStorable = document.GetCeedStorable() ;
      var ceedCodeNumber = ceedStorable.CeedModelData.Where( c => c.ModelNumber == modelNumber ).Select( c => c.CeedModelNumber ).SingleOrDefault() ;
      return ceedCodeNumber is {} && IsMainConstructionCeedModelNumber( document, ceedCodeNumber ) ; 
    }
  }
}