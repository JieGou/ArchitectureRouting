namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class ImportDwgMappingModel
  {
    public string Id { get ; set ; }
    public string FullFilePath { get ; set ; }
    public string FileName { get ; set ; }
    public string FloorName { get ; set ; }
    public double FloorHeight { get ; set ; }
    public int Scale { get ; set ; }
    
    public ImportDwgMappingModel( string? id, string? fullFilePath, string? fileName, string? floorName, double? floorHeight, int? scale )
    {
      Id = id ?? string.Empty ;
      FullFilePath = fullFilePath ?? string.Empty ;
      FileName = fileName ?? string.Empty ;
      FloorName = floorName ?? string.Empty ;
      FloorHeight = floorHeight ?? 0 ;
      Scale = scale ?? 100 ;
    }
  }
}