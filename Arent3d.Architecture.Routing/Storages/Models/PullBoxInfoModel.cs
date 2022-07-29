using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storages.Attributes ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
  [Schema( "3749C0D6-4CCB-403F-9CDE-13CD0E4C767A", nameof( PullBoxInfoModel ) )]
  public class PullBoxInfoModel : IDataModel
  {
    public PullBoxInfoModel()
    {
      PullBoxInfoData = new List<PullBoxInfoItemModel>() ;
    }

    [Field( Documentation = "Pull Box Info Data" )]
    public List<PullBoxInfoItemModel> PullBoxInfoData { get ; set ; } = new() ;
  }

  [Schema( "DFBB9DEE-F2C5-4538-91DD-B6BFAE605679", nameof( PullBoxInfoItemModel ) )]
  public class PullBoxInfoItemModel : IDataModel
  {
    public PullBoxInfoItemModel()
    {
      PullBoxUniqueId = string.Empty ;
      TextNoteUniqueId = string.Empty ;
    }

    public PullBoxInfoItemModel( string? pullBoxUniqueId, string? textNoteUniqueId )
    {
      PullBoxUniqueId = pullBoxUniqueId ?? string.Empty ;
      TextNoteUniqueId = textNoteUniqueId ?? string.Empty ;
    }

    [Field( Documentation = "Pull Box UniqueId" )]
    public string PullBoxUniqueId { get ; set ; }
    
    [Field( Documentation = "Text Note UniqueId" )]
    public string TextNoteUniqueId { get ; set ; }
  }
}