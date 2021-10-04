using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;

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
      }
      else {
        SystemTypes = null ;
      }

      CurveTypes = GetCompatibleCurveTypes( document, firstSubRoute.GetMEPCurveType().GetType() ) ;
      Shafts = document.GetAllElements<Opening>().ToList() ;
    }

    public RoutePropertyTypeList( Document document )
    {
      SystemTypes = document.GetAllElements<MEPSystemType>().OrderBy( s => s.Name ).ToList() ;
      Shafts = document.GetAllElements<Opening>().ToList() ;
      CurveTypes = document.GetAllElements<MEPCurveType>().OrderBy( s => s.Name ).ToList() ;
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
      }
      Shafts = document.GetAllElements<Opening>().ToList() ;
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