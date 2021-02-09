using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit.EntityFields ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Revit
{
  public class FieldWriter
  {
    private readonly Element _element ;
    private readonly Entity _entity ;
    
    internal FieldWriter( Element element, Entity entity )
    {
      _element = element ;
      _entity = entity ;
    }

    public void SetSingle<TFieldType>( string name, TFieldType value ) where TFieldType : notnull
    {
      var converter = typeof( TFieldType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TFieldType ).FullName} is not acceptable." ) ;

      _entity.SetNativeValue( name, converter.GetNativeType(), converter.CustomToNative( _element, value ) ) ;
    }

    public void SetArray<TFieldType>( string name, IEnumerable<TFieldType> values ) where TFieldType : notnull
    {
      var converter = typeof( TFieldType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TFieldType ).FullName} is not acceptable." ) ;

      _entity.SetNativeArray( name, converter.GetNativeType(), values.Select( value => converter.CustomToNative( _element, value ) ) ) ;
    }
  }
}