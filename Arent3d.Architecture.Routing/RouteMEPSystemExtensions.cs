using System ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;
using Arent3d.Revit ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.DB.Plumbing ;


namespace Arent3d.Architecture.Routing
{
  public static class RouteMEPSystemExtensions
  {
    /// <summary>
    /// Get NominalDiameterList
    /// </summary>
    /// <param name="type"></param>
    /// <param name="diameterTolerance"></param>
    /// <returns></returns>
    public static List<double> GetNominalDiameters( this MEPCurveType type, double diameterTolerance )
    {
      var resultList = new List<double>() ;
      var segment = type.GetTargetSegment() ;
      if ( segment != null ) {
        resultList = segment.GetSizes().Where( s => type.HasAnyNominalDiameter( s.NominalDiameter, diameterTolerance ) ).Select( s => s.NominalDiameter ).ToList() ;
      }
      //Get duct sizes
      else if(type.GetType() == typeof(DuctType)){
        var ductSizeSettings = DuctSizeSettings.GetDuctSizeSettings(type.Document) ;
        //Currently, only round shapes are acquired
        var roundSizes = ductSizeSettings[ DuctShape.Round ].Where(s => s.UsedInSizeLists).Select(s => s.NominalDiameter) ;
        resultList = roundSizes.ToList() ;
      }

      resultList.Sort() ;

      return resultList ;
    }

    public static bool HasAnyNominalDiameter( this MEPCurveType type, double nominalDiameter, double diameterTolerance )
    {
      var document = type.Document ;
      return type.RoutingPreferenceManager.GetRules( RoutingPreferenceRuleGroupType.Segments ).All( rule => HasAnyNominalDiameter( document, rule, nominalDiameter, diameterTolerance ) ) ;
    }

    private static bool HasAnyNominalDiameter( Document document, RoutingPreferenceRule rule, double nominalDiameter, double diameterTolerance )
    {
      if ( false == rule.GetCriteria().OfType<PrimarySizeCriterion>().All( criterion => criterion.IsMatchRange( nominalDiameter ) ) ) return false ;

      var segment = document.GetElementById<Segment>( rule.MEPPartId ) ;
      return ( null != segment ) && segment.HasAnyNominalDiameter( nominalDiameter, diameterTolerance ) ;
    }

    private static bool HasAnyNominalDiameter( this Segment segment, double nominalDiameter, double diameterTolerance )
    {
      return segment.GetSizes().Any( size => Math.Abs( size.NominalDiameter - nominalDiameter ) < diameterTolerance ) ;
    }

    public static IEnumerable<RoutingPreferenceRule> GetRules( this RoutingPreferenceManager rpm, RoutingPreferenceRuleGroupType groupType )
    {
      var count = rpm.GetNumberOfRules( groupType ) ;
      for ( var i = 0 ; i < count ; ++i ) {
        yield return rpm.GetRule( groupType, i ) ;
      }
    }

    public static IEnumerable<RoutingCriterionBase> GetCriteria( this RoutingPreferenceRule rule )
    {
      var count = rule.NumberOfCriteria ;
      for ( var i = 0 ; i < count ; ++i ) {
        yield return rule.GetCriterion( i ) ;
      }
    }

    public static bool IsCompatibleCurveType( this MEPCurveType curveType, Type targetCurveType )
    {
      return ( curveType.GetType() == targetCurveType ) ;
    }

    public static bool IsMatchRange( this PrimarySizeCriterion criterion, double nominalDiameter )
    {
      return criterion.MinimumSize <= nominalDiameter && nominalDiameter <= criterion.MaximumSize ;
    }

    /// <summary>
    /// Get Target SystemTypeList
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="systemClassificationInfo"></param>
    /// <returns></returns>
    public static IEnumerable<MEPSystemType> GetSystemTypes( this Document doc, MEPSystemClassificationInfo systemClassificationInfo )
    {
      return doc.GetAllElements<MEPSystemType>().Where( systemClassificationInfo.IsCompatibleTo ) ;
    }

    /// <summary>
    /// Get compatible curve types.
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="curveType"></param>
    /// <returns></returns>
    public static IEnumerable<MEPCurveType> GetCurveTypes( this Document doc, MEPCurveType? curveType )
    {
      if ( null == curveType ) return Enumerable.Empty<MEPCurveType>() ;

      var type = curveType.GetType() ;
      return doc.GetAllElements<MEPCurveType>().Where( s => s.IsCompatibleCurveType( type ) ).Select( s => s ) ;
    }

    private static Segment? GetTargetSegment( this MEPCurveType type )
    {
      Segment? targetSegment = null ;
      var document = type.Document ;
      IEnumerable<RoutingPreferenceRule> rules = type.RoutingPreferenceManager.GetRules( RoutingPreferenceRuleGroupType.Segments ) ;
      foreach ( var rule in rules ) {
        targetSegment = document.GetElementById<Segment>( rule.MEPPartId ) ;
      }

      return targetSegment ;
    }


    private static readonly IReadOnlyDictionary<MEPSystemClassification, Domain> _classificationToDomain = GetClassificationToDomain() ;

    private static IReadOnlyDictionary<MEPSystemClassification, Domain> GetClassificationToDomain()
    {
      var dir = new Dictionary<MEPSystemClassification, Domain>() ;

      AddAllSystemClassifications<PipeSystemType>( dir, Domain.DomainPiping ) ;
      AddAllSystemClassifications<DuctSystemType>( dir, Domain.DomainHvac ) ;
      AddAllSystemClassifications<ElectricalSystemType>( dir, Domain.DomainCableTrayConduit ) ;

      return dir ;
    }

    private static void AddAllSystemClassifications<T>( Dictionary<MEPSystemClassification, Domain> dir, Domain domain ) where T : Enum
    {
      foreach ( var classification in GetAllSystemClassifications<T>() ) {
        if ( dir.ContainsKey( classification ) ) {
          dir[ classification ] = Domain.DomainUndefined ;
        }
        else {
          dir.Add( classification, domain ) ;
        }
      }
    }

    private static IEnumerable<MEPSystemClassification> GetAllSystemClassifications<T>() where T : Enum
    {
      foreach ( var name in Enum.GetNames( typeof( T ) ) ) {
        if ( false == Enum.TryParse( name, out MEPSystemClassification result ) ) continue ;

        yield return result ;
      }
    }

    public static Domain GetDomain( this MEPSystemType mepSystemType )
    {
      if ( _classificationToDomain.TryGetValue( mepSystemType.SystemClassification, out var domain ) ) return domain ;

      return Domain.DomainUndefined ;
    }
  }
}