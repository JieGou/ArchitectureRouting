namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class TextNotePickUpModel
  {
    public string TextNoteId { get ; set ; }
    
    public TextNotePickUpModel(string? textNoteId = default)
    {
      TextNoteId = textNoteId ?? string.Empty ;
    }
  }
}