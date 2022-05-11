using System.Runtime.InteropServices ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "9e2420bf-556c-441d-a39c-99d2539ef0be" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class LocationTypeStorable : StorableBase
  {
    public const string StorableName = "Change Location Type" ;
    
    private const string LocationTypeField = "LocationType" ;
    
    public string LocationType { get ; set ; }
    

    private LocationTypeStorable( DataStorage owner ) : base( owner, false )
    {
      LocationType = string.Empty ;
    }

    public LocationTypeStorable( Document document ) : base( document, false )
    {
      LocationType = string.Empty ;
    }

    public override string Name => StorableName ;


    protected override void LoadAllFields( FieldReader reader )
    {
      LocationType = reader.GetSingle<string>( LocationTypeField ) ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetSingle( LocationTypeField, LocationType ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetSingle<string>( LocationTypeField ) ;
    }
  }
}