using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Arent3d.Architecture.Routing.Storable.Model;
using Arent3d.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace Arent3d.Architecture.Routing.Storable
{
    [Guid( "b614f593-b3d9-4472-b5a0-7a069f8005c6" )]
    [StorableVisibility( AppInfo.VendorId )]
    
    public class RegistrationOfBoardDataStorable : StorableBase
    {
        public const string StorableName = "Registration Of Board Data Model" ;
        private const string RegistrationOfBoardDataModelField = "RegistrationOfBoardDataModel" ;

        public List<RegistrationOfBoardDataModel> RegistrationOfBoardData { get ; set ; }

        public RegistrationOfBoardDataStorable(DataStorage owner) : base(owner, false)
        {
            RegistrationOfBoardData = new List<RegistrationOfBoardDataModel>() ;
        }

        public RegistrationOfBoardDataStorable(Document document) : base(document, false)
        {
            RegistrationOfBoardData = new List<RegistrationOfBoardDataModel>() ;
        }

        protected override void LoadAllFields(FieldReader reader)
        {
            RegistrationOfBoardData = reader.GetArray<RegistrationOfBoardDataModel>( RegistrationOfBoardDataModelField ).ToList() ;
        }

        protected override void SaveAllFields(FieldWriter writer)
        {
            writer.SetArray( RegistrationOfBoardDataModelField, RegistrationOfBoardData ) ;
        }

        protected override void SetupAllFields(FieldGenerator generator)
        {
            generator.SetArray<RegistrationOfBoardDataModel>( RegistrationOfBoardDataModelField ) ;
        }

        public override string Name => StorableName;
    }
}