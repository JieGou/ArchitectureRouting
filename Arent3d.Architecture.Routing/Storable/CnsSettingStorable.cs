using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "1B299E62-B71D-4B4E-A3D3-3052FCB12197" )]
  [StorableVisibility( AppInfo.VendorId )]
  public sealed class CnsSettingStorable : StorableBase, IEquatable<CnsSettingStorable>
  {
    public const string CnsStorableName = "Cns Setting" ;
    private const string CnsSettingField = "CnsSetting" ;
    private const string CnsFilePathField = "CnsFilePath" ;
    public ObservableCollection<CnsSettingModel> CnsSettingData { get ; set ; }
    public string CnsFilePath { get ; set ; }

    public enum UpdateItemType
    {
      None, // デフォルト：工事項目を設定しない
      Conduit, //配線に工事項目を設定する
      Connector,
      Rack,
      Range,
      All
    }

    public int SelectedIndex { get ; set ; }
    public UpdateItemType ElementType { get ; set ; }

    /// <summary>
    /// for loading from storage.
    /// </summary>
    /// <param name="owner">Owner element.</param>
    private CnsSettingStorable( DataStorage owner ) : base( owner, false )
    {
      CnsSettingData = new ObservableCollection<CnsSettingModel>() ;
      SelectedIndex = 0 ;
      ElementType = UpdateItemType.None ;
      CnsFilePath = string.Empty ;
    }

    /// <summary>
    /// Called by RouteCache.
    /// </summary>
    /// <param name="document"></param>
    public CnsSettingStorable( Document document ) : base( document, false )
    {
      CnsSettingData = new ObservableCollection<CnsSettingModel>() ;
      SelectedIndex = 0 ;
      ElementType = UpdateItemType.None ;
      CnsFilePath = string.Empty ;
    }

    public override string Name => CnsStorableName ;

    protected override void LoadAllFields( FieldReader reader )
    {
      var dataSaved = reader.GetArray<CnsSettingModel>( CnsSettingField ) ;
      CnsSettingData = new ObservableCollection<CnsSettingModel>( dataSaved ) ;
      CnsFilePath = reader.GetSingle<string>( CnsFilePathField ) ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( CnsSettingField, CnsSettingData ) ;
      writer.SetSingle( CnsFilePathField, CnsFilePath ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<CnsSettingModel>( CnsSettingField ) ;
      generator.SetSingle<string>( CnsFilePathField ) ;
    }


    public bool Equals( CnsSettingStorable? other )
    {
      if ( other == null ) return false ;
      return CnsSettingData.SequenceEqual( other.CnsSettingData, new CnsSettingStorableComparer() ) ;
    }
  }

  public class CnsSettingStorableComparer : IEqualityComparer<CnsSettingModel>
  {
    public bool Equals( CnsSettingModel x, CnsSettingModel y )
    {
      return x.Equals( y ) ;
    }

    public int GetHashCode( CnsSettingModel obj )
    {
      return obj.GetHashCode() ;
    }
  }
}
