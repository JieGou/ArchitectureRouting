using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit.EntityFields ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Revit
{
  public class FieldReader
  {
    private readonly Element _element ;
    private readonly Entity _entity ;
    
    internal FieldReader( Element element, Entity entity )
    {
      _element = element ;
      _entity = entity ;
    }

    public TFieldType GetSingle<TFieldType>( string name )
    {
      var converter = typeof( TFieldType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TFieldType ).FullName} is not acceptable." ) ;

      return (TFieldType) converter.NativeToCustom( _element, _entity.GetNativeValue( name, converter.GetNativeType() ) ) ;
    }

    public IEnumerable<TFieldType> GetArray<TFieldType>( string name )
    {
      var converter = typeof( TFieldType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TFieldType ).FullName} is not acceptable." ) ;

      foreach ( var value in _entity.GetNativeArray( name, converter.GetNativeType() ) ) {
        yield return (TFieldType) converter.NativeToCustom( _element, value ) ;
      }
    }
  }
}