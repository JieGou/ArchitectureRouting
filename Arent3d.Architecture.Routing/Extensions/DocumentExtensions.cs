using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.StorableConverter ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using InvalidOperationException = Autodesk.Revit.Exceptions.InvalidOperationException ;

namespace Arent3d.Architecture.Routing.Extensions
{
  public static class DocumentExtensions
  {
    /// <summary>
    /// Get Height settings data from snoop DB. <br />
    /// If there is no data, it is returned default settings
    /// </summary>
    /// <param name="document">current document of Revit</param>
    /// <returns>Height settings data was stored in snoop DB</returns>
    public static HeightSettingStorable GetHeightSettingStorable( this Document document )
    {
      try {
        return HeightSettingStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( HeightSettingStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new HeightSettingStorable( document ) ;
      }
    }

    /// <summary>
    /// Get Offset settings data from snoop DB.
    /// </summary>
    public static OffsetSettingStorable GetOffsetSettingStorable( this Document document )
    {
      try {
        return OffsetSettingStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( OffsetSettingStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new OffsetSettingStorable( document ) ;
      }
    }

    /// <summary>
    /// Get register symbol settings data from snoop DB.
    /// </summary>
    public static RegisterSymbolStorable GetRegisterSymbolStorable( this Document document )
    {
      try {
        return RegisterSymbolStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( RegisterSymbolStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new RegisterSymbolStorable( document ) ;
      }
    }


    /// <summary>
    /// Get location type settings data from snoop DB.
    /// </summary>
    public static LocationTypeStorable GetLocationTypeStorable( this Document document )
    {
      try {
        return LocationTypeStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( LocationTypeStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new LocationTypeStorable( document ) ;
      }
    }

    /// <summary>
    /// Get CNS Setting data from snoop DB.
    /// </summary>
    public static CnsSettingStorable GetCnsSettingStorable( this Document document )
    {
      try {
        return CnsSettingStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( CnsSettingStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new CnsSettingStorable( document ) ;
      }
    }

    /// <summary>
    /// Get Ceed Model data from snoop DB.
    /// </summary>
    public static CeedStorable GetCeedStorable( this Document document )
    {
      try {
        return CeedStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( CeedStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new CeedStorable( document ) ;
      }
    }

    /// <summary>
    /// Get csv data from snoop DB.
    /// </summary>
    public static CsvStorable GetCsvStorable( this Document document )
    {
      try {
        return CsvStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( CsvStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new CsvStorable( document ) ;
      }
    }

    /// <summary>
    /// Get pick up data from snoop DB.
    /// </summary>
    public static PickUpStorable GetPickUpStorable( this Document document )
    {
      try {
        return PickUpStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( PickUpStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new PickUpStorable( document ) ;
      }
    }

    /// <summary>
    /// Get detail symbol data from snoop DB.
    /// </summary>
    public static DetailSymbolStorable GetDetailSymbolStorable( this Document document )
    {
      try {
        return DetailSymbolStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( DetailSymbolStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new DetailSymbolStorable( document ) ;
      }
    }
    
    /// <summary>
    /// Get pull box data from snoop DB.
    /// </summary>
    public static PullBoxInfoStorable GetPullBoxInfoStorable( this Document document )
    {
      try {
        return PullBoxInfoStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( PullBoxInfoStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new PullBoxInfoStorable( document ) ;
      }
    }

    /// <summary>
    /// Get rack notation data from snoop DB.
    /// </summary>
    public static RackNotationStorable GetRackNotationStorable( this Document document )
    {
      try {
        return RackNotationStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( RackNotationStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new RackNotationStorable( document ) ;
      }
    }

    /// <summary>
    /// Get detail table data from snoop DB.
    /// </summary>
    public static DetailTableStorable GetDetailTableStorable( this Document document )
    {
      try {
        return DetailTableStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( DetailTableStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new DetailTableStorable( document ) ;
      }
    }

    /// <summary>
    /// Get text note data from snoop DB.
    /// </summary>
    public static BorderTextNoteStorable GetBorderTextNoteStorable( this Document document )
    {
      try {
        return BorderTextNoteStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( BorderTextNoteStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new BorderTextNoteStorable( document ) ;
      }
    }

    /// <summary>
    /// Get text note data from snoop DB.
    /// </summary>
    public static RegistrationOfBoardDataStorable GetRegistrationOfBoardDataStorable( this Document document )
    {
      try {
        return RegistrationOfBoardDataStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( RegistrationOfBoardDataStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new RegistrationOfBoardDataStorable( document ) ;
      }
    }

    /// <summary>
    /// Get default default setting from DB
    /// </summary>
    public static DefaultSettingStorable GetDefaultSettingStorable( this Document document )
    {
      try {
        return DefaultSettingStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( DefaultSettingStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new DefaultSettingStorable( document ) ;
      }
    }

    /// <summary>
    /// Get setup print data from snoop DB.
    /// </summary>
    public static SetupPrintStorable GetSetupPrintStorable( this Document document )
    {
      try {
        return SetupPrintStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( SetupPrintStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new SetupPrintStorable( document ) ;
      }
    }

    /// <summary>
    /// Get all symbolInformation data
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    public static SymbolInformationStorable GetSymbolInformationStorable( this Document document )
    {
      try {
        return SymbolInformationStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( SymbolInformationStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new SymbolInformationStorable( document ) ;
      }
    }

    /// <summary>
    /// Get all CeedDetail data
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    public static CeedDetailStorable GetCeedDetailStorable( this Document document )
    {
      try {
        return CeedDetailStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( CeedDetailStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new CeedDetailStorable( document ) ;
      }
    }

    /// <summary>
    /// Get ConduitAndDetailCurve data from snoop DB.
    /// </summary>
    public static ConduitAndDetailCurveStorable GetConduitAndDetailCurveStorable( this Document document )
    {
      try {
        return ConduitAndDetailCurveStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( ConduitAndDetailCurveStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new ConduitAndDetailCurveStorable( document ) ;
      }
    }

    public static string GetDefaultConstructionItem( this Document document )
    {
      try {
        var cnsSettingStorable = GetCnsSettingStorable( document ) ;
        var defaultCnsSettingModel = cnsSettingStorable.CnsSettingData.FirstOrDefault(x=>x.IsDefaultItemChecked) ;
        return defaultCnsSettingModel != null ? defaultCnsSettingModel.CategoryName : String.Empty ;
      }
      catch ( Exception ) {
        return String.Empty;
      }
    }

    /// <summary>
    /// Get PressureGuidingTubeStorable data from DB
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    public static PressureGuidingTubeStorable GetPressureGuidingTubeStorable( this Document document )
    {
      try {
        return PressureGuidingTubeStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( PressureGuidingTubeStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new PressureGuidingTubeStorable( document ) ;
      }
    }

    public static WiringInformationChangedStorable GetWiringInformationChangedStorable( this Document document )
    {
      try {
        return WiringInformationChangedStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( WiringInformationChangedStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new WiringInformationChangedStorable( document ) ;
      }
    }
    
    public static WiringStorable GetWiringStorable( this Document document )
    {
      try {
        return WiringStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( WiringStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new WiringStorable( document ) ;
      }
    }
    
    
    public static ChangePlumbingInformationStorable GetChangePlumbingInformationStorable( this Document document )
    {
      try {
        return ChangePlumbingInformationStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( ChangePlumbingInformationStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new ChangePlumbingInformationStorable( document ) ;
      }
    }
  }
}
