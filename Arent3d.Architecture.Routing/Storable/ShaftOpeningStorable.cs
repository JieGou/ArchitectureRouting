using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
    [Guid( "a6f83e7d-9166-4aab-ae89-7de643160b2d" )]
    [StorableVisibility( AppInfo.VendorId )]
    public class ShaftOpeningStorable : StorableBase
    {
        public const string StorableName = "Shaft Opening" ;
        private const string ShaftOpeningField = "ShaftOpenings" ;
        public List<ShaftOpeningModel> ShaftOpeningModels { get ; set ; }

        public ShaftOpeningStorable( DataStorage owner) : base( owner, false )
        {
            ShaftOpeningModels = new List<ShaftOpeningModel>() ;
        }

        public ShaftOpeningStorable( Document document) : base( document, false )
        {
            ShaftOpeningModels = new List<ShaftOpeningModel>() ;
        }

        protected override void LoadAllFields( FieldReader reader )
        {
            ShaftOpeningModels = reader.GetArray<ShaftOpeningModel>( ShaftOpeningField ).ToList() ;
        }

        protected override void SaveAllFields( FieldWriter writer )
        {
            writer.SetArray( ShaftOpeningField, ShaftOpeningModels ) ; 
        }

        protected override void SetupAllFields( FieldGenerator generator )
        {
            generator.SetArray<ShaftOpeningModel>( ShaftOpeningField ) ;
        }

        public override string Name => StorableName ;
    }
}