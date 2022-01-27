using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "ead6b130-7b19-49ce-a6a3-0ddfd06a36b9" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class ConnectorFamilyTypeStorable : StorableBase
  {
    public const string StorableName = "Connector Family Type Model" ;
    private const string ConnectorFamilyTypeField = "ConnectorFamilyTypeModel" ;

    public List<ConnectorFamilyTypeModel> ConnectorFamilyTypeModelData { get ; set ; }

    public ConnectorFamilyTypeStorable( DataStorage owner ) : base( owner, false )
    {
      ConnectorFamilyTypeModelData = new List<ConnectorFamilyTypeModel>() ;
    }

    public ConnectorFamilyTypeStorable( Document document ) : base( document, false )
    {
      ConnectorFamilyTypeModelData = new List<ConnectorFamilyTypeModel>() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      ConnectorFamilyTypeModelData = reader.GetArray<ConnectorFamilyTypeModel>( ConnectorFamilyTypeField ).ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( ConnectorFamilyTypeField, ConnectorFamilyTypeModelData ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<ConnectorFamilyTypeModel>( ConnectorFamilyTypeField ) ;
    }

    public override string Name => StorableName ;
  }
}