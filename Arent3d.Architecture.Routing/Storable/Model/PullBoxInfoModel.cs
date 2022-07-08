namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class PullBoxInfoModel
  {
    public string PullBoxUniqueId { get ; set ; }
    public string TextNoteUniqueId { get ; set ; }

    public PullBoxInfoModel( string? pullBoxUniqueId, string? textNoteUniqueId )
    {
      PullBoxUniqueId = pullBoxUniqueId ?? string.Empty ;
      TextNoteUniqueId = textNoteUniqueId ?? string.Empty ;
    }
  }
}