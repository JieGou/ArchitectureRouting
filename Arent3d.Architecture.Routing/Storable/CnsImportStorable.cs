using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "A52AB9F8-1EB5-4BEF-9B7E-DC8CA228C12D" )]
  [StorableVisibility( AppInfo.VendorId )]
  public sealed class CnsImportStorable : StorableBase
  {
    public const string StorableName = "Cns Import" ;
    private const string CnsImportField = "CnsImport" ;
    public List<CnsImportModel> CnsImportData { get ; private set ; }
    
    /// <summary>
    /// for loading from storage.
    /// </summary>
    /// <param name="owner">Owner element.</param>
    private CnsImportStorable( DataStorage owner ) : base( owner, false )
    {
      CnsImportData = new List<CnsImportModel>();
    }

    /// <summary>
    /// Called by RouteCache.
    /// </summary>
    /// <param name="document"></param>
    public CnsImportStorable( Document document ) : base( document, false )
    {
      CnsImportData = new List<CnsImportModel>();
    }

    public override string Name => StorableName ;

    protected override void LoadAllFields( FieldReader reader )
    {
      var dataSaved = reader.GetArray<CnsImportModel>( CnsImportField ) ;
      CnsImportData = dataSaved.ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetSingle( CnsImportField, CnsImportData ) ;
    }

    protected override void SetupAllFields(FieldGenerator generator)
    {
      generator.SetArray<CnsImportModel>( CnsImportField ) ;
    }
  }
  

}