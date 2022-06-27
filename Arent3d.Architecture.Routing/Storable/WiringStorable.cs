using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "d8ceddf2-affd-49fe-a2fd-9b42d5133491" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class WiringStorable : StorableBase
  {
    public const string StorableName = "WiringStorable" ;
    private const string AllWiringField = "AllWiring" ;

    public List<WiringModel> WiringData { get ; set ; }
     

    public WiringStorable( DataStorage owner ) : base( owner, false )
    {
      WiringData = new List<WiringModel>() ; 
    }

    public WiringStorable( Document document ) : base( document, false )
    {
      WiringData = new List<WiringModel>() ; 
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      WiringData = reader.GetArray<WiringModel>( AllWiringField ).ToList() ;
     
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( AllWiringField, WiringData ) ; 
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<WiringModel>( AllWiringField ) ;
     
    }

    public override string Name => StorableName ;
  }
}