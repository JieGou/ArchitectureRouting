namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// An object which implements <see cref="IMappedObject{TRevitObject}"/> is managed by a <see cref="ObjectMapper{TMapper,TRevitObject,TRoutingObject}"/>.
  /// </summary>
  /// <typeparam name="TRevitObject"></typeparam>
  public interface IMappedObject<TRevitObject> where TRevitObject : class
  {
    /// <summary>
    /// Returns a Revit object which this object is based.
    /// </summary>
    TRevitObject BaseObject { get ; }
  }
}