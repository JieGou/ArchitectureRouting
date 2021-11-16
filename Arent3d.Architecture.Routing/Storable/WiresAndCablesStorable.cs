using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "2abee280-4a54-4256-945f-ca5fc4f57ab3" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class WiresAndCablesStorable : StorableBase
  {
    public const string StorableName = "Wires And Cables Model" ;
    private const string WiresAndCablesModelField = "WiresAndCablesModel" ;

    public List<WiresAndCablesModel> WiresAndCablesModelData { get ; set ; }
    
    public WiresAndCablesStorable( DataStorage owner ) : base( owner, false )
    {
      WiresAndCablesModelData = new List<WiresAndCablesModel>() ;
    }

    public WiresAndCablesStorable( Document document ) : base( document, false )
    {
      WiresAndCablesModelData = new List<WiresAndCablesModel>() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      WiresAndCablesModelData = reader.GetArray<WiresAndCablesModel>( WiresAndCablesModelField ).ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( WiresAndCablesModelField, WiresAndCablesModelData ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<WiresAndCablesModel>( WiresAndCablesModelField ) ;
    }

    public override string Name => StorableName ;
  }
}