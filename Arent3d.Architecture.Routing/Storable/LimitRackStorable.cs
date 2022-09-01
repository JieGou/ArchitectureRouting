using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "3dbebaed-2dfc-4ce4-b39b-00108cd5ec45" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class LimitRackStorable : StorableBase
  {
    public const string LimitRackStorableName = "Limit Rack Model" ;
    private const string LimitRackStorableField = "LimitRackModel" ;

    public override string Name => LimitRackStorableName ;

    public IList<LimitRackModel> LimitRackModels { get ; private set ; } = new List<LimitRackModel>() ;

    public LimitRackStorable( DataStorage owner ) : base( owner, false )
    {
    }


    public LimitRackStorable( Document document ) : base( document, false )
    {
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      LimitRackModels = reader.GetArray<LimitRackModel>( LimitRackStorableField ).ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( LimitRackStorableField, LimitRackModels ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<LimitRackModel>( LimitRackStorableField ) ;
    }
  }
}