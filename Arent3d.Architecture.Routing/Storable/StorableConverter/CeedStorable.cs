using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [Guid( "998cd31f-ada6-4fbf-ae78-971fc071c030" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class CeedStorable : StorableBase
  {
    public const string StorableName = "CeeD Model" ;
    private const string CeeDModelField = "CeeDModel" ;
    private const string CeeDFileName = "CeeDModel" ;

    public Dictionary<string, CeedModel> CeedModelData { get ; private set ; }
    public IReadOnlyList<string> CeeDModelNumbers { get ; }

    public CeedStorable( DataStorage owner, bool subStorable ) : base( owner, subStorable )
    {
      CeeDModelNumbers = GetAllCeeDModelNumber( CeeDFileName ) ;
      CeedModelData = new Dictionary<string, CeedModel>() ;
    }

    public CeedStorable( Document document, bool subStorable ) : base( document, subStorable )
    {
      CeeDModelNumbers = GetAllCeeDModelNumber( CeeDFileName ) ;
      CeedModelData = new Dictionary<string, CeedModel>() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      var dataSaved = reader.GetArray<CeedModel>( CeeDModelField ).ToDictionary( x => x.CeeDModelNumber ) ;

      CeedModelData = CeeDModelNumbers.ToDictionary( x => x, x => dataSaved.GetOrDefault( x, () => new CeedModel( x, string.Empty, new List<string>(), string.Empty, new List<string>(), string.Empty ) ) ) ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      CeedModelData = CeeDModelNumbers.ToDictionary( x => x, x => CeedModelData.GetOrDefault( x, () => new CeedModel( x, string.Empty, new List<string>(), string.Empty, new List<string>(), string.Empty ) ) ) ;

      writer.SetArray( CeeDModelField, CeedModelData.Values.ToList() ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<CeedModel>( CeeDModelField ) ;
    }

    public override string Name => StorableName ;

    private static IReadOnlyList<string> GetAllCeeDModelNumber( string ceedFileName )
    {
      List<string> ceeDModelNumbers = new List<string>() ;
      var path = AssetManager.GetCeeDModelPath( ceedFileName ) ;
      return ceeDModelNumbers ;
    }
  }
}