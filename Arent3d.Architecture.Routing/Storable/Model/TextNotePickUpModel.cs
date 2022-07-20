namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class TextNotePickUpModel
  {
    public string TextNoteId { get ; set ; }
    public string Level { get ; set ; }
    
    public TextNotePickUpModel(string? textNoteId, string? level)
    {
      TextNoteId = textNoteId ?? string.Empty ;
      Level = level ?? string.Empty ;
    }
  }
}