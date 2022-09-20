using Arent3d.Architecture.Routing.Storages.Attributes ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
    [Schema("4ED94D31-DAB4-423E-B314-42D4F431D8AE", nameof( RegisterSymbolModel ))]
    public class RegisterSymbolModel : IDataModel
    {
        [Field(Documentation = "Browse Folder Path")]
        public string BrowseFolderPath { get ; set ; } = string.Empty ;
        
        [Field(Documentation = "Folder Selected Path")]
        public string FolderSelectedPath { get ; set ; } = string.Empty ;
    }
}