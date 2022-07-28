namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class WireLengthNotationModel
  {
    public string TextNoteId { get ; set ; }
    public string Level { get ; set ; }
    
    public WireLengthNotationModel(string? textNoteId, string? level)
    {
      TextNoteId = textNoteId ?? string.Empty ;
      Level = level ?? string.Empty ;
    }
  }
}