using System ;
using System.Linq.Expressions ;
using System.Reflection ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Revit
{
  /// <summary>
  /// Fields with the Boolean, Byte, Int16, Int32, Float, Double, ElementId, GUID, String, XYZ, UV and Entity is storable.
  /// </summary>
  public abstract class StorableBase
  {
    public Document Document { get ; }

    public DataStorage? OwnerElement { get ; set ; }

    public abstract string Name { get ; }

    internal bool SubStorable { get ; }

    protected StorableBase( DataStorage owner, bool subStorable )
    {
      Document = owner.Document ;
      OwnerElement = owner ;
      SubStorable = subStorable ;
    }

    protected StorableBase( Document document, bool subStorable )
    {
      Document = document ;
      OwnerElement = null ;
      SubStorable = subStorable ;
    }

    public void Save()
    {
      if ( SubStorable ) throw new InvalidOperationException( "This storable is a sub-storable." ) ;

      OwnerElement ??= DataStorage.Create( Document ) ;
      OwnerElement.SaveStorable( this ) ;
    }

    public void Delete()
    {
      OwnerElement?.DeleteStorable( this ) ;
      OwnerElement = null ;
    }

    protected internal abstract void LoadAllFields( FieldReader reader ) ;
    protected internal abstract void SaveAllFields( FieldWriter writer ) ;
    protected internal abstract void SetupAllFields( FieldGenerator generator ) ;

    internal static TStorableBase CreateFromEntity<TStorableBase>( DataStorage ownerElement ) where TStorableBase : StorableBase
    {
      return StorableInstantiator<TStorableBase>.Instantiate( ownerElement ) ;
    }
  }

  internal static class StorableInstantiator<TStorableBase> where TStorableBase : StorableBase
  {
    private static readonly Func<DataStorage, TStorableBase> _instantiator = CreateInstantiator() ;

    public static TStorableBase Instantiate( DataStorage ownerElement )
    {
      return _instantiator( ownerElement ) ;
    }
  
    private static Func<DataStorage, TStorableBase> CreateInstantiator()
    {
      ConstructorInfo? noArgs = null ;
      ConstructorInfo? oneArg = null ;
      foreach ( var ctor in typeof( TStorableBase ).GetConstructors( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ) ) {
        var @params = ctor.GetParameters() ;
        if ( 0 == @params.Length ) {
          noArgs = ctor ;
        }
        else if ( 1 == @params.Length && @params[ 0 ].ParameterType.IsAssignableFrom( typeof( DataStorage ) ) ) {
          oneArg = ctor ;
        }
      }

      if ( null != oneArg ) {
        return CreateInstantiatorWithElement( oneArg ) ;
      }
      if ( null != noArgs ) {
        return CreateInstantiatorWithNoArgs( noArgs ) ;
      }

      throw new InvalidOperationException( $"{typeof( TStorableBase ).FullName} has no constructor with 0 args nor with one element arg." ) ;
    }

    private static Func<DataStorage, TStorableBase> CreateInstantiatorWithElement( ConstructorInfo oneArg )
    {
      var param = Expression.Parameter( typeof( DataStorage ) ) ;
      var expression = Expression.New( oneArg, Expression.Convert( param, oneArg.GetParameters()[ 0 ].ParameterType ) ) ;
      return Expression.Lambda<Func<DataStorage, TStorableBase>>( expression, param ).Compile() ;
    }

    private static Func<DataStorage, TStorableBase> CreateInstantiatorWithNoArgs( ConstructorInfo noArgs )
    {
      var param = Expression.Parameter( typeof( DataStorage ) ) ;
      return Expression.Lambda<Func<DataStorage, TStorableBase>>( Expression.New( noArgs ), param ).Compile() ;
    }
  }
}