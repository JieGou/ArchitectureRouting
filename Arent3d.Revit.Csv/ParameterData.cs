using System ;
using System.Globalization ;
using System.Linq.Expressions ;
using System.Reflection ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit.Csv
{
  public abstract class ParameterData<TParameterData> where TParameterData : ParameterData<TParameterData>
  {
    #region Quicken instance generation

    private static readonly Func<TParameterData> _new = CreateGenerator() ;

    private static Func<TParameterData> CreateGenerator()
    {
      var ctor = typeof( TParameterData ).GetConstructor( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, Array.Empty<ParameterModifier>() ) ;
      if ( null == ctor ) throw new InvalidOperationException() ;

      return Expression.Lambda<Func<TParameterData>>( Expression.New( ctor ) ).Compile() ;
    }

    private static TParameterData CreateNewParameterData() => _new() ;

    #endregion

    private static UnitDictionary? _unitDic ;

    private protected static void SetUnitInfo( UnitInfos.UnitInfo unitInfo )
    {
      _unitDic = unitInfo.CreateUnitDictionary() ;
    }

    public static TParameterData Empty { get ; } = CreateNewParameterData() ;

    public static TParameterData From( FamilyInstance familyInstance, string parameterName )
    {
      if ( false == familyInstance.ParametersMap.Contains( parameterName ) ) {
        return Empty ;
      }
      else {
        var paramData = CreateNewParameterData() ;
        paramData.SetParameter( familyInstance.ParametersMap.get_Item( parameterName ) ) ;
        return paramData ;
      }
    }

    public static TParameterData From( FamilyInstance familyInstance, BuiltInParameter builtInParameter )
    {
      var param = familyInstance.get_Parameter( builtInParameter ) ;
      if ( null == param ) {
        return Empty ;
      }
      else {
        var paramData = CreateNewParameterData() ;
        paramData.SetParameter( param ) ;
        return paramData ;
      }
    }

    public bool To( FamilyInstance familyInstance, string parameterName )
    {
      if ( false == familyInstance.ParametersMap.Contains( parameterName ) ) {
        return false ;
      }
      else {
        var param = familyInstance.ParametersMap.get_Item( parameterName ) ;
        if ( _unitDic!.Match( ValueString ) is not { } tuple ) return false ;

        if ( tuple.Unit is {} unitType ) {
          param.Set( UnitUtils.ConvertFromInternalUnits( tuple.Value, unitType ) ) ;
        }
        else {
          param.Set( tuple.Value ) ;
        }

        return true ;
      }
    }

    public bool To( FamilyInstance familyInstance, BuiltInParameter builtInParameter )
    {
      var param = familyInstance.get_Parameter( builtInParameter ) ;
      if ( null == param ) {
        return false ;
      }
      else {
        if ( _unitDic!.Match( ValueString ) is not { } tuple ) return false ;

        if ( tuple.Unit is {} unitType ) {
          param.Set( UnitUtils.ConvertToInternalUnits( tuple.Value, unitType ) ) ;
        }
        else {
          param.Set( tuple.Value ) ;
        }

        return true ;
      }
    }


    private string ValueString { get ; set ; } = string.Empty ;

    private protected ParameterData()
    {
    }

    private void SetParameter( Parameter parameter )
    {
      if ( StorageType.Double != parameter.StorageType ) throw new ArgumentException() ;

#if REVIT2019 || REVIT2020
      ValueString = parameter.AsDouble().ToString( CultureInfo.InvariantCulture ) ;
#else
      ValueString = _unitDic!.GetValueWithUnit( parameter.AsDouble(), parameter.GetUnitTypeId() ) ;
#endif
    }

    internal void SetValueString( string str )
    {
      ValueString = str ;
    }

    public override string ToString() => ValueString ;
  }
}