using System ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages.Attributes
{
    /// <summary>
    /// Only properties with field attributes are stored in storage
    /// </summary>
    [AttributeUsage( AttributeTargets.Property )]
    public class FieldAttribute : Attribute
    {
        public FieldAttribute()
        {
            Documentation = string.Empty ;
            SpecTypeId = string.Empty ;
            UnitTypeId = string.Empty ;
        }

        public string Documentation { get ; set ; }
        public string SpecTypeId { get ; set ; }
        public string UnitTypeId { get ; set ; }
    }
}