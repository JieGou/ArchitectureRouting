using System ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Revit
{
  /// <summary>
  /// Defines converters between Revit data structures and auto routing data structures.
  /// </summary>
  public static class MathInterconversionExtensions
  {
    /// <summary>
    /// Converts <see cref="XYZ"/> (feet) => <see cref="Vector3d"/> (meters).
    /// </summary>
    /// <param name="xyz">Revit coordination (feet).</param>
    /// <returns>Arent coordination (meters).</returns>
    public static Vector3d To3dPoint( this XYZ xyz ) => new( xyz.X.RevitUnitsToMeters(), xyz.Y.RevitUnitsToMeters(), xyz.Z.RevitUnitsToMeters() ) ;

    /// <summary>
    /// Converts <see cref="Vector3d"/> (meters) => <see cref="XYZ"/> (feet).
    /// </summary>
    /// <param name="vec">Arent coordination (meters).</param>
    /// <returns>Revit coordination (feet).</returns>
    public static XYZ ToXYZPoint( this Vector3d vec ) => new( vec.x.MetersToRevitUnits(), vec.y.MetersToRevitUnits(), vec.z.MetersToRevitUnits() ) ;

    /// <summary>
    /// Converts <see cref="XYZ"/> (normalized) => <see cref="Vector3d"/> (normalized).
    /// </summary>
    /// <param name="xyz">Revit direction (normalized).</param>
    /// <returns>Arent direction (normalized).</returns>
    public static Vector3d To3dDirection( this XYZ xyz ) => new( xyz.X, xyz.Y, xyz.Z ) ;

    /// <summary>
    /// Converts <see cref="Vector3d"/> (normalized) => <see cref="XYZ"/> (normalized).
    /// </summary>
    /// <param name="vec">Arent direction (normalized).</param>
    /// <returns>Revit direction (normalized).</returns>
    public static XYZ ToXYZDirection( this Vector3d vec ) => new( vec.x, vec.y, vec.z ) ;

    /// <summary>
    /// Converts <see cref="BoundingBoxXYZ"/> (feet) => <see cref="Box3d"/> (meters).
    /// </summary>
    /// <param name="xyz">Revit box (feet).</param>
    /// <returns>Arent box (meters).</returns>
    public static Box3d ToBox3d( this BoundingBoxXYZ xyz ) => new( xyz.Min.To3dPoint(), xyz.Max.To3dPoint() ) ;

    /// <summary>
    /// Converts <see cref="Box3d"/> (meters) => <see cref="BoundingBoxXYZ"/> (feet).
    /// </summary>
    /// <param name="box">Arent box (meters).</param>
    /// <returns>Revit box (feet).</returns>
    public static BoundingBoxXYZ ToBoxXYZ( this Box3d box ) => new BoundingBoxXYZ { Min = box.Min.ToXYZPoint(), Max = box.Max.ToXYZPoint() } ;

    /// <summary>
    /// Converts <see cref="Transform"/> (feet) => <see cref="LocalCodSys3d"/> (meters).
    /// </summary>
    /// <param name="transform">Revit coordinate system (feet).</param>
    /// <returns>Arent coordinate system (meters).</returns>
    public static LocalCodSys3d ToCodSys3d( this Transform transform )
    {
      return new( transform.Origin.To3dPoint(), transform.BasisX.To3dDirection(), transform.BasisY.To3dDirection(), transform.BasisX.To3dDirection() ) ;
    }

    /// <summary>
    /// Converts <see cref="LocalCodSys3d"/> (meters) => <see cref="Transform"/> (feet).
    /// </summary>
    /// <param name="codSys">Arent coordinate system (meters).</param>
    /// <returns>Revit coordinate system (feet).</returns>
    public static Transform ToTransform( this LocalCodSys3d codSys )
    {
      var transform = Transform.CreateTranslation( codSys.Origin.ToXYZPoint() ) ;
      transform.BasisX = codSys.DirectionX.ToXYZDirection() ;
      transform.BasisY = codSys.DirectionY.ToXYZDirection() ;
      transform.BasisZ = codSys.DirectionZ.ToXYZDirection() ;
      return transform ;
    }
  }
}