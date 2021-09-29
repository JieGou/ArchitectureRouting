using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.DB.Plumbing ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public class RoutePropertyTypeList
  {
    //For experimental state
    private static readonly bool UseExperimentalFeatures = true ;

    public IList<MEPSystemType>? SystemTypes { get ; }
    public IList<Opening>? Shafts { get ; }
    public IList<MEPCurveType> CurveTypes { get ; }
    public IList<string>? StandardTypes { get ; }

    internal RoutePropertyTypeList( IReadOnlyCollection<SubRoute> subRoutes )
    {
      if ( 0 == subRoutes.Count ) throw new ArgumentException() ;

      var firstSubRoute = subRoutes.First() ;
      var document = firstSubRoute.Route.Document ;

      var systemClassification = firstSubRoute.Route.GetSystemClassificationInfo() ;
      if ( systemClassification.HasSystemType() ) {
        SystemTypes = document.GetSystemTypes( systemClassification ).OrderBy( s => s.Name ).ToList() ;
        Shafts = null ;
      }
      else {
        SystemTypes = null ;
        Shafts = document.GetAllElements<Opening>().ToList() ;
      }

      CurveTypes = GetCompatibleCurveTypes( document, firstSubRoute.GetMEPCurveType().GetType() ) ;
    }

    public RoutePropertyTypeList( Document document, AddInType addInType )
    {
      ( SystemTypes, CurveTypes, StandardTypes, Shafts ) = addInType switch
      {
        AddInType.Electrical => GetElectricalTypeLists( document ),
        AddInType.Mechanical => GetMechanicalTypeLists( document ),
        _ => throw new ArgumentOutOfRangeException( nameof( addInType ), addInType, null )
      } ;
    }

    private static (IList<MEPSystemType>? SystemTypes, IList<MEPCurveType> CurveTypes, IList<string>? StandardTypes, IList<Opening>? Shafts) GetMechanicalTypeLists( Document document )
    {
      var systemTypes = document.GetAllElements<MEPSystemType>().Where( type => type is MechanicalSystemType or PipingSystemType ).OrderBy( s => s.Name ).ToList() ;
      var curveTypes = document.GetAllElements<MEPCurveType>().Where( type => type is DuctType or PipeType ).OrderBy( s => s.Name ).ToList() ;
      return ( systemTypes, curveTypes, null, null ) ;
    }

    private static (IList<MEPSystemType>? SystemTypes, IList<MEPCurveType> CurveTypes, IList<string>? StandardTypes, IList<Opening>? Shafts) GetElectricalTypeLists( Document document )
    {
      var curveTypes = document.GetAllElements<ConduitType>().OrderBy( c => c.Name ).OfType<MEPCurveType>().ToList() ;
      var standardTypes = document.GetStandardTypes().ToList() ;
      var shafts = document.GetAllElements<Opening>().ToList() ;
      return ( null, curveTypes, standardTypes, shafts ) ;
    }

    public RoutePropertyTypeList( Document document, MEPSystemClassificationInfo classificationInfo )
    {
      if ( classificationInfo.HasSystemType() ) {
        SystemTypes = document.GetSystemTypes( classificationInfo ).OrderBy( s => s.Name ).ToList() ;
        CurveTypes = GetCompatibleCurveTypes( document, classificationInfo.GetCurveTypeClass() ) ;
      }
      else {
        CurveTypes = document.GetAllElements<ConduitType>().OrderBy( c => c.Name ).OfType<MEPCurveType>().ToList() ;
        StandardTypes = document.GetStandardTypes().ToList() ;
        Shafts = document.GetAllElements<Opening>().ToList() ;
      }
    }

    private static IList<MEPCurveType> GetCompatibleCurveTypes( Document document, Type? mepCurveTypeClass )
    {
      var curveTypes = document.GetCurveTypes( mepCurveTypeClass ) ;
      if ( UseExperimentalFeatures ) {
        curveTypes = curveTypes.Where( c => c.Shape == ConnectorProfileType.Round ) ;
      }

      return curveTypes.OrderBy( s => s.Name ).ToList() ;
    }
  }
  
}