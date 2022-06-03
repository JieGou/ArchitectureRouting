namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class CsvFileModel
  {
    public string CsvName { get ; set ; } 
    
    public string CsvFilePath { get ; set ; }
    
    public string CsvFileName { get ; set ; } 

    public CsvFileModel( string? csvName, string? csvFilePath, string? csvFileName )
    {
      CsvName = csvName??string.Empty ;
      CsvFilePath = csvFilePath??string.Empty ;
      CsvFileName = csvFileName??string.Empty ;
    }
  }
}