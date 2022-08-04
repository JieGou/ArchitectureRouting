namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class CategoryModel
  {
    public string Name { get ; }
    public string ParentName { get ; }
    public bool IsExpanded { get ; }
    public bool IsSelected { get ; }

    public CategoryModel( string? name, string? parentName, bool? isExpanded, bool? isSelected )
    {
      Name = name ?? string.Empty ;
      ParentName = parentName ?? string.Empty ;
      IsExpanded = isExpanded ?? false ;
      IsSelected = isSelected ?? false ;
    }
  }
}