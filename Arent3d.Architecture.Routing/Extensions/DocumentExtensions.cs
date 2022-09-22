using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using InvalidOperationException = Autodesk.Revit.Exceptions.InvalidOperationException ;

namespace Arent3d.Architecture.Routing.Extensions
{
  public static class DocumentExtensions
  {

    #region Filters

    public static List<T> GetAllInstances<T>(this Document document) where T : Element
    {
      var filter = new FilteredElementCollector( document ) ;
      return filter.OfClass(typeof(T)).OfType<T>().ToList();
    }
    
    public static List<T> GetAllInstances<T>(this Document document, Func<T, bool> func ) where T : Element
    {
      var filter = new FilteredElementCollector( document ) ;
      return filter.OfClass(typeof(T)).OfType<T>().Where(func).ToList();
    }
    
    public static List<T> GetAllInstances<T>(this Document document, View view) where T : Element
    {
      var filter = new FilteredElementCollector( document, view.Id ) ;
      return filter.OfClass(typeof(T)).OfType<T>().ToList();
    }
    
    public static List<T> GetAllTypes<T>(this Document document) where T : ElementType
    {
      var filter = new FilteredElementCollector( document ) ;
      return filter.OfClass(typeof(T)).WhereElementIsElementType().OfType<T>().ToList();
    }
    
    public static List<T> GetAllTypes<T>(this Document document, Func<T, bool> func ) where T : ElementType
    {
      var filter = new FilteredElementCollector( document ) ;
      return filter.OfClass( typeof( T ) ).WhereElementIsElementType().OfType<T>().Where(func).ToList();
    }

    #endregion
    
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
    /// Get CNS Setting data from snoop DB.
    /// </summary>
    public static CnsSettingStorable GetCnsSettingStorable( this Document document )
    {
      try {
        return CnsSettingStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( CnsSettingStorable.CnsStorableName ) ;
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
    /// Get limit rack data from snoop DB.
    /// </summary>
    public static LimitRackStorable GetLimitRackStorable( this Document document )
    {
      try {
        return LimitRackStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( LimitRackStorable.LimitRackStorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new LimitRackStorable( document ) ;
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

    public static string GetDefaultConstructionItem( this Document document )
    {
      const string defaultConstructionItem = "未設定" ; // 工事項目設定ではデフォルトが設定されていない場合、デフォルトの工事項目を「未設定」とする
      try {
        var cnsSettingStorable = GetCnsSettingStorable( document ) ;
        var defaultCnsSettingModel = cnsSettingStorable.CnsSettingData.FirstOrDefault( x => x.IsDefaultItemChecked ) ;
        return defaultCnsSettingModel != null ? defaultCnsSettingModel.CategoryName : defaultConstructionItem ;
      }
      catch ( Exception ) {
        return defaultConstructionItem ;
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
    
    /// <summary>
    /// Get TextNotePickUpModel data from snoop DB.
    /// </summary>
    public static WireLengthNotationStorable GetWireLengthNotationStorable( this Document document )
    {
      try {
        return WireLengthNotationModelStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( WireLengthNotationStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new WireLengthNotationStorable( document ) ;
      }
    }
    
    /// <summary>
    /// Get ShaftOpeningModel data from snoop DB.
    /// </summary>
    public static ShaftOpeningStorable GetShaftOpeningStorable( this Document document )
    {
      try {
        return ShaftOpeningStorableCache.Get( DocumentKey.Get( document ) ).FindOrCreate( ShaftOpeningStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new ShaftOpeningStorable( document ) ;
      }
    }
  }
}
