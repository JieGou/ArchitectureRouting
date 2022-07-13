using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid("6dc98d39-0080-45d8-9367-21761d6cd652")]
  [StorableVisibility( AppInfo.VendorId )]
  public class PullBoxInfoStorable : StorableBase
  {
    public const string StorableName = "Pullbox Info Model" ;
    private const string PullBoxInfoModelField = "PullBoxInfoModel" ;
    public List<PullBoxInfoModel> PullBoxInfoModelData { get ; set ; }
    
    public PullBoxInfoStorable( DataStorage owner ) : base( owner, false )
    {
      PullBoxInfoModelData = new List<PullBoxInfoModel>() ;
    }

    public PullBoxInfoStorable( Document document ) : base( document, false )
    {
      PullBoxInfoModelData = new List<PullBoxInfoModel>() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      PullBoxInfoModelData = reader.GetArray<PullBoxInfoModel>( PullBoxInfoModelField ).ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( PullBoxInfoModelField, PullBoxInfoModelData ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<PullBoxInfoModel>( PullBoxInfoModelField ) ;
    }

    public override string Name => StorableName ;
  }
}