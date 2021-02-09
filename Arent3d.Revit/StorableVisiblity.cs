using System ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Revit
{
  [Flags]
  public enum StorableVisibilityMode
  {
    Read = 1,
    Write = 2,
    ReadWrite = Read | Write,
  }

  [AttributeUsage( AttributeTargets.Class, AllowMultiple = true )]
  public class StorableVisibilityAttribute : Attribute
  {
    /// <summary>
    /// Determines whether this visibility attribute is applied for read, for write, or for both of read/write.
    /// </summary>
    public StorableVisibilityMode Mode { get ; set ; } = StorableVisibilityMode.ReadWrite ;

    public bool ForRead => ( 0 != (int) ( Mode & StorableVisibilityMode.Read ) ) ;
    public bool ForWrite => ( 0 != (int) ( Mode & StorableVisibilityMode.Write ) ) ;

    public AccessLevel AccessLevel { get ; }
    public string? VendorId { get ; }
    public Guid ApplicationGuid { get ; } = Guid.Empty ;

    /// <summary>
    /// Storable visibility is <see cref="Autodesk.Revit.DB.ExtensibleStorage.AccessLevel.Public"/>.
    /// </summary>
    public StorableVisibilityAttribute()
    {
      AccessLevel = AccessLevel.Public ;
    }

    /// <summary>
    /// Storable visibility is <see cref="Autodesk.Revit.DB.ExtensibleStorage.AccessLevel.Vendor"/>.
    /// </summary>
    /// <param name="vendorId">Vendor ID</param>
    public StorableVisibilityAttribute( string vendorId )
    {
      AccessLevel = AccessLevel.Vendor ;
      VendorId = vendorId ;
    }

    /// <summary>
    /// Storable visibility is <see cref="Autodesk.Revit.DB.ExtensibleStorage.AccessLevel.Application"/>.
    /// </summary>
    /// <param name="vendorId">Vendor ID</param>
    /// <param name="guid">Application GUID</param>
    public StorableVisibilityAttribute( string vendorId, string guid )
    {
      AccessLevel = AccessLevel.Application ;
      VendorId = vendorId ;
      ApplicationGuid = new Guid( guid ) ;
    }
  }
}