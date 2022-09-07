using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storages.Attributes ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
  [Schema( "3749C0D6-4CCB-403F-9CDE-13CD0E4C767B", nameof( HandholeInfoModel ) )]
  public class HandholeInfoModel : IDataModel
  {
    public HandholeInfoModel()
    {
      HandholeInfoData = new List<HandholeInfoItemModel>() ;
    }

    [Field( Documentation = "Handhole Info Data" )]
    public List<HandholeInfoItemModel> HandholeInfoData { get ; set ; } = new() ;
  }

  [Schema( "DFBB9DEE-F2C5-4538-91DD-B6BFAE60567C", nameof( HandholeInfoItemModel ) )]
  public class HandholeInfoItemModel : IDataModel
  {
    public HandholeInfoItemModel()
    {
      HandholeUniqueId = string.Empty ;
      TextNoteUniqueId = string.Empty ;
    }

    public HandholeInfoItemModel( string? handholeUniqueId, string? textNoteUniqueId )
    {
      HandholeUniqueId = handholeUniqueId ?? string.Empty ;
      TextNoteUniqueId = textNoteUniqueId ?? string.Empty ;
    }

    [Field( Documentation = "Handhole UniqueId" )]
    public string HandholeUniqueId { get ; set ; }
    
    [Field( Documentation = "Text Note UniqueId" )]
    public string TextNoteUniqueId { get ; set ; }
  }
}