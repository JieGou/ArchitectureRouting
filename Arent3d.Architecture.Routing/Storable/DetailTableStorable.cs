using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "5bf89aba-effd-4262-9482-08d6dab1f6dc" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class DetailTableStorable : StorableBase
  {
    public const string StorableName = "Detail Table Model" ;
    private const string DetailTableModelField = "DetailTableModel" ;
    public List<DetailTableModel> DetailTableModelData { get ; set ; }
    
    public DetailTableStorable( DataStorage owner ) : base( owner, false )
    {
      DetailTableModelData = new List<DetailTableModel>() ;
    }

    public DetailTableStorable( Document document ) : base( document, false )
    {
      DetailTableModelData = new List<DetailTableModel>() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      DetailTableModelData = reader.GetArray<DetailTableModel>( DetailTableModelField ).ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( DetailTableModelField, DetailTableModelData ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<DetailTableModel>( DetailTableModelField ) ;
    }

    public override string Name => StorableName ;
  }
}