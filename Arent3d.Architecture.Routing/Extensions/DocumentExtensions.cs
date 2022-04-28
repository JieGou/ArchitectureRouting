using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.StorableConverter ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.Exceptions ;

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
    public static RegistrationOfBoardDataStorable GetRegistrationOfBoardDataStorable(
      this Document document )
    {
      try {
        return RegistrationOfBoardDataStorableCache.Get( DocumentKey.Get( document ) )
          .FindOrCreate( RegistrationOfBoardDataStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new RegistrationOfBoardDataStorable( document ) ;
      }
    }
    
    /// <summary>
    /// Get eco default setting from DB
    /// </summary>
    public static EcoSettingStorable GetEcoSettingStorable( this Document document )
    {
      try {
        return EcoSettingStorableCache.Get( DocumentKey.Get( document ) )
          .FindOrCreate( EcoSettingStorable.StorableName ) ;
      }
      catch ( InvalidOperationException ) {
        return new EcoSettingStorable( document ) ;
      }
    }
  }
}
