using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storages.Attributes ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
    [Schema("15D6461B-65D7-481E-B04A-BA2C93BBD817", nameof(CeedUserModel))]
    public class CeedUserModel : IDataModel
    {
        [Field(Documentation = "Is Show Ceed Model Number")]
        public bool IsShowCeedModelNumber { get ; set ; }
        
        [Field(Documentation = "Is Show Condition")]
        public bool IsShowCondition { get ; set ; }
        
        [Field(Documentation = "Connector Family Upload Data")]
        public List<string> ConnectorFamilyUploadData { get ; set ; } = new() ;
        
        [Field(Documentation = "Is Show Only Using Code")]
        public bool IsShowOnlyUsingCode { get ; set ; } = true ;
        
        [Field(Documentation = "Is Diff")]
        public bool IsDiff { get ; set ; }
    }
}