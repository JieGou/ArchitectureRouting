using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "56ab99bd-96bd-412a-bbc3-882bb05a60cf" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class LimitRackStorable : StorableBase
  {
    public const string StorableName = "Limit Rack Model" ;
    private const string LimitRackModelField = "LimitRackModel" ;
    public override string Name => StorableName ;

    public List<LimitRackModel> LimitRackModelData { get ; private set ; } = new List<LimitRackModel>() ;

    #region Constructor

    public LimitRackStorable( DataStorage owner ) : base( owner, false)
    {
    }

    public LimitRackStorable( Document document ) : base( document, false )
    {
    }

    #endregion Constructor
    
    protected override void LoadAllFields( FieldReader reader )
    {
      LimitRackModelData = reader.GetArray<LimitRackModel>( LimitRackModelField ).ToList() ;

    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( LimitRackModelField, LimitRackModelData ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<LimitRackModel>( LimitRackModelField ) ;
    }
  }
}