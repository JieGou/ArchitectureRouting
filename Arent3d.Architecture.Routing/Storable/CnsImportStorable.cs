using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "A52AB9F8-1EB5-4BEF-9B7E-DC8CA228C12D" )]
  [StorableVisibility( AppInfo.VendorId )]
  public sealed class CnsImportStorable : StorableBase, IEquatable<CnsImportStorable>
  {
    public const string StorableName = "Cns Import" ;
    private const string CnsImportField = "CnsImport" ;
    public ObservableCollection<CnsImportModel> CnsImportData { get ; set ; }
    
    /// <summary>
    /// for loading from storage.
    /// </summary>
    /// <param name="owner">Owner element.</param>
    private CnsImportStorable( DataStorage owner ) : base( owner, false )
    {
      CnsImportData = new ObservableCollection<CnsImportModel>();
    }

    /// <summary>
    /// Called by RouteCache.
    /// </summary>
    /// <param name="document"></param>
    public CnsImportStorable( Document document ) : base( document, false )
    {
      CnsImportData = new ObservableCollection<CnsImportModel>();
    }

    public override string Name => StorableName ;

    protected override void LoadAllFields( FieldReader reader )
    {
      var dataSaved = reader.GetArray<CnsImportModel>( CnsImportField ) ;
      CnsImportData = new ObservableCollection<CnsImportModel>(dataSaved);
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( CnsImportField, CnsImportData ) ;
    }

    protected override void SetupAllFields(FieldGenerator generator)
    {
      generator.SetArray<CnsImportModel>( CnsImportField ) ;
    }

    
    public bool Equals(CnsImportStorable? other)
    {
      if ( other == null ) return false ;
      return CnsImportData.SequenceEqual( other.CnsImportData, new CnsImportStorableComparer() ) ;
    }
  }
  public class CnsImportStorableComparer : IEqualityComparer<CnsImportModel>
  {
    public bool Equals( CnsImportModel x, CnsImportModel y )
    {
      return x.Equals( y ) ;
    }

    public int GetHashCode( CnsImportModel obj )
    {
      return obj.GetHashCode() ;
    }
  }

}
