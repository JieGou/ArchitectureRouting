using System ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storages.Attributes
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
        
        /// <summary>
        /// Floating-point types (float, double, XYZ and UV) required SpecTypeId
        /// </summary>
        public string SpecTypeId { get ; set ; }
        
        /// <summary>
        /// Floating-point types (float, double, XYZ and UV) required UnitTypeId
        /// </summary>
        public string UnitTypeId { get ; set ; }
    }
}