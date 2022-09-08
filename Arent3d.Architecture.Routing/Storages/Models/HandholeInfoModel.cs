using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storages.Attributes ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
  [Schema( "D2BB9B40-65BD-4DF1-AEB5-5441FE12B6B2", nameof( HandholeInfoModel ) )]
  public class HandholeInfoModel : IDataModel
  {
    public HandholeInfoModel()
    {
      HandholeInfoData = new List<HandholeInfoItemModel>() ;
    }

    [Field( Documentation = "Handhole Info Data" )]
    public List<HandholeInfoItemModel> HandholeInfoData { get ; set ; } = new() ;
  }

  [Schema( "F20DB3DC-9A89-4F9C-8C8F-1EBEAB027866", nameof( HandholeInfoItemModel ) )]
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