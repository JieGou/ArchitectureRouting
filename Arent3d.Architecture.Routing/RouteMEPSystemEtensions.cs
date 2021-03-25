using System ;
using System.Collections.Generic;
using System.ComponentModel ;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using Arent3d.Architecture.Routing;
using Arent3d.Revit;
using Autodesk.Revit.DB.Plumbing;


namespace Arent3d.Architecture.Routing
{
    public static class RouteMEPSystemEtensions
    {
        /// <summary>
        /// Get NominalDiameterList
        /// </summary>
        /// <param name="rms"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IList<double> GetNominalDiameterList(this RouteMEPSystem rms, MEPCurveType type)
        {
            IList<double> resultList = new List<double>();
            var segment = GetTargetSegment(rms, type);
            if (segment != null)
            {
                resultList = segment.GetSizes().Where(s => rms.HasAnyNominalDiameter(type, s.NominalDiameter))
                    .Select(s => s.NominalDiameter).ToList();
            }
            
            return resultList;
        }

        public static IEnumerable<RoutingPreferenceRule> GetRules(this RouteMEPSystem rms,
            RoutingPreferenceManager rpm, RoutingPreferenceRuleGroupType groupType)
        {
            var count = rpm.GetNumberOfRules(groupType);
            for (var i = 0; i < count; ++i)
            {
                yield return rpm.GetRule(groupType, i);
            }
        }

        public static IEnumerable<RoutingCriterionBase> GetCriteria(this RouteMEPSystem rms,
            RoutingPreferenceRule rule)
        {
            var count = rule.NumberOfCriteria;
            for (var i = 0; i < count; ++i)
            {
                yield return rule.GetCriterion(i);
            }
        }

        public static bool IsMatchRange(this RouteMEPSystem rms, PrimarySizeCriterion criterion,
            double nominalDiameter)
        {
            return criterion.MinimumSize <= nominalDiameter && nominalDiameter <= criterion.MaximumSize;
        }

        public static IList<MEPSystemType> GetSystemTypeList(this RouteMEPSystem rms, Document doc, Connector connector)
        {
            //IList<MEPSystem> resultList = new List<MEPSystem>();
            var systemClassification = RouteMEPSystem.GetSystemClassification( connector ) ;

            var resultList = doc.GetAllElements<MEPSystemType>()
                .Where(s => RouteMEPSystem.IsCompatibleMEPSystemType(s, systemClassification))
                .Select(s => s).ToList();
            /*foreach ( var test in doc.GetAllElements<MEPSystemType>() )
            {
                if (IsCompatibleMEPSystemType(test, systemClassification))
                {
                    Debug.Print(test.Name) ;
                }*/
            return resultList;
        }

        private static Segment? GetTargetSegment(this RouteMEPSystem rms, MEPCurveType type)
        {
            Segment? targetSegment = null;
            var document = type.Document;
            var rpm = type.RoutingPreferenceManager;
            IEnumerable<RoutingPreferenceRule> rules = GetRules(rms, rpm, RoutingPreferenceRuleGroupType.Segments);
            foreach (var rule in rules)
            {
                targetSegment = document.GetElementById<Segment>(rule.MEPPartId);
            }

            return targetSegment;
        }
        
    }
}