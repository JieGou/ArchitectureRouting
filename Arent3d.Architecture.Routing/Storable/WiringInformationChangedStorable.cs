using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "730c0961-e381-41e6-81a5-78c66b747398" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class WiringInformationChangedStorable : StorableBase
  {
    public const string StorableName = "WiringInformationChangedStorable" ;
    private const string AllWiringInformationChangedField = "AllWiringInformationChanged" ;

    public List<WiringInformationChangedModel> WiringInformationChangedData { get ; set ; }
     

    public WiringInformationChangedStorable( DataStorage owner ) : base( owner, false )
    {
      WiringInformationChangedData = new List<WiringInformationChangedModel>() ; 
    }

    public WiringInformationChangedStorable( Document document ) : base( document, false )
    {
      WiringInformationChangedData = new List<WiringInformationChangedModel>() ; 
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      WiringInformationChangedData = reader.GetArray<WiringInformationChangedModel>( AllWiringInformationChangedField ).ToList() ;
     
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( AllWiringInformationChangedField, WiringInformationChangedData ) ; 
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<WiringInformationChangedModel>( AllWiringInformationChangedField ) ;
     
    }

    public override string Name => StorableName ;
  }
}