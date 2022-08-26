namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class CategoryModel
  {
    public string Name { get ; }
    public string ParentName { get ; }
    public bool IsExpanded { get ; }
    public bool IsSelected { get ; }
    public bool IsCeedCodeNumber { get ; }
    public bool IsExistModelNumber { get ; }

    public CategoryModel( string? name, string? parentName, bool? isExpanded, bool? isSelected, bool? isCeedCodeNumber, bool? isExistModelNumber )
    {
      Name = name ?? string.Empty ;
      ParentName = parentName ?? string.Empty ;
      IsExpanded = isExpanded ?? false ;
      IsSelected = isSelected ?? false ;
      IsCeedCodeNumber = isCeedCodeNumber ?? false ;
      IsExistModelNumber = isExistModelNumber ?? false ;
    }
  }
}