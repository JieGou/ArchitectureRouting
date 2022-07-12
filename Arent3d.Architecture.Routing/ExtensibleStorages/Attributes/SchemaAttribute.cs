using System ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages.Attributes
{
  /// <summary>
  /// Definition of a schema
  /// </summary>
  [AttributeUsage( AttributeTargets.Class )]
  public class SchemaAttribute : Attribute
  {
    public SchemaAttribute( string guid, string schemaName )
    {
      if ( ! Guid.TryParse( guid, out var value ) )
        throw new InvalidCastException( "GUID value is not in the correct format." ) ;

      GUID = value ;
      SchemaName = schemaName ;
    }

    public Guid GUID { get ; }
    public string SchemaName { get ; }


    public Guid ApplicationGUID { get ; set ; } = Guid.Empty ;

    public string Documentation { get ; set ; } = string.Empty ;

    public AccessLevel ReadAccessLevel { get ; set ; } = AccessLevel.Vendor ;

    public AccessLevel WriteAccessLevel { get ; set ; } = AccessLevel.Vendor ;

    public string VendorId { get ; set ; } = AppInfo.VendorId ;
  }
}