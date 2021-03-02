using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.DB.Plumbing ;

namespace Arent3d.Architecture.Routing
{
  public class RouteMEPSystem
  {
    private readonly double _diameterTolerance ;

    public MEPSystemType MEPSystemType { get ; }
    public MEPSystem? MEPSystem { get ; }
    public MEPCurveType CurveType { get ; }

    public RouteMEPSystem( Document document, Route route )
    {
      _diameterTolerance = document.Application.VertexTolerance ;
      
      var allConnectors = route.GetAllConnectors( document ).EnumerateAll() ;
      MEPSystemType = GetSystemType( document, allConnectors ) ;

      //MEPSystem = CreateMEPSystem( document, connector, allConnectors ) ;
      MEPSystem = null ;

      CurveType = GetMEPCurveType( document, allConnectors, MEPSystemType ) ;
    }

    public double Get90ElbowSize( double diameter )
    {
      return diameter * 1.5 ; // provisional
    }

    public double Get45ElbowSize( double diameter )
    {
      return diameter * 1.5 ; // provisional
    }

    public double GetTeeHeaderLength( double headerDiameter, double branchDiameter )
    {
      if ( JunctionType.Tee == CurveType.PreferredJunctionType ) {
        if ( headerDiameter < branchDiameter ) {
          return headerDiameter * 1.0 ; // provisional
        }
        else {
          return headerDiameter * 0.5 + branchDiameter * 0.5 ; // provisional
        }
      }
      else {
        return branchDiameter * 0.5 + GetWeldMinDistance( branchDiameter ) ; // provisional
      }
    }

    public double GetTeeBranchLength( double headerDiameter, double branchDiameter )
    {
      if ( JunctionType.Tee == CurveType.PreferredJunctionType ) {
        if ( headerDiameter < branchDiameter ) {
          return headerDiameter * 1.0 + GetReducerLength( headerDiameter, branchDiameter ) ; // provisional
        }
        else {
          return headerDiameter * 0.5 + branchDiameter * 0.5 ; // provisional
        }
      }
      else {
        return headerDiameter * 0.5 + GetWeldMinDistance( branchDiameter ) ; // provisional
      }
    }

    public double GetReducerLength( double diameter1, double diameter2 )
    {
      if ( diameter1 <= 0 || diameter2 <= 0 || Math.Abs( diameter1 - diameter2 ) < _diameterTolerance ) return 0 ;

      // TODO: find reducer size

      return 0 ;
    }

    public double GetWeldMinDistance( double diameter )
    {
      return 1.0 / 120 ;  // 1/10 inches.
    }


    #region Get MEPSystemType

    private static MEPSystemType GetSystemType( Document document, IEnumerable<Connector> connectors )
    {
      return connectors.Select( connector => GetSystemType( document, connector ) ).NonNull().First()! ;
    }
    private static MEPSystemType? GetSystemType( Document document, Connector connector )
    {
      var systemClassification = GetSystemClassification( connector ) ;
      foreach ( var type in document.GetAllElements<MEPSystemType>() ) {
        if ( IsCompatibleMEPSystemType( type, systemClassification ) ) return type ;
      }

      return null ;
    }

    private static bool IsCompatibleMEPSystemType( MEPSystemType type, MEPSystemClassification systemClassification )
    {
      return ( type.SystemClassification == systemClassification ) ;
    }

    private static MEPSystemClassification GetSystemClassification( Connector connector )
    {
      return connector.Domain switch
      {
        Domain.DomainPiping => GetSystemClassification( connector.PipeSystemType ),
        Domain.DomainHvac => GetSystemClassification( connector.DuctSystemType ),
        Domain.DomainElectrical => GetSystemClassification( connector.ElectricalSystemType ),
        Domain.DomainCableTrayConduit => GetSystemClassification( connector.ElectricalSystemType ),
        _ => null,
      } ?? throw new KeyNotFoundException() ;
    }
    private static MEPSystemClassification? GetSystemClassification<T>( T systemType ) where T : Enum
    {
      try {
        if ( Enum.TryParse( systemType.ToString(), out MEPSystemClassification result ) ) {
          return result ;
        }

        return null ;
      }
      catch {
        return null ;
      }
    }

    #endregion

    #region Create MEPSystem

    private static MEPSystem? CreateMEPSystem( Document document, Connector baseConnector, IReadOnlyCollection<Connector> allConnectors )
    {
      return baseConnector.Domain switch
      {
        Domain.DomainHvac => CreateMechanicalMEPSystem( document, baseConnector, allConnectors ),
        Domain.DomainPiping => CreatePipingMEPSystem( document, baseConnector, allConnectors ),
        _ => null,
      } ;
    }

    private static MEPSystem CreateMechanicalMEPSystem( Document document, Connector connector, IReadOnlyCollection<Connector> allConnectors )
    {
      allConnectors.ForEach( EraseOldMEPSystem ) ;
      var system = document.Create.NewMechanicalSystem( connector, allConnectors.ToConnectorSet(), connector.DuctSystemType ) ;
      SetMEPSystemParameters( system, connector ) ;
      return system ;
    }
    private static MEPSystem CreatePipingMEPSystem( Document document, Connector connector, IReadOnlyCollection<Connector> allConnectors )
    {
      allConnectors.ForEach( EraseOldMEPSystem ) ;
      var system = document.Create.NewPipingSystem( connector, allConnectors.ToConnectorSet(), connector.PipeSystemType ) ;
      SetMEPSystemParameters( system, connector ) ;
      return system ;
    }

    private static void EraseOldMEPSystem( Connector c )
    {
      if ( c.MEPSystem is not {  } mepSystem ) return ;

      if ( mepSystem.BaseEquipmentConnector.GetIndicator() == c.GetIndicator() ) {
        mepSystem.Document.Delete( mepSystem.Id ) ;
      }
      else {
        mepSystem.Remove( new[] { c }.ToConnectorSet() ) ;
        if ( mepSystem.Elements.IsEmpty ) {
          mepSystem.Document.Delete( mepSystem.Id ) ;
        }
      }
    }

    private static void SetMEPSystemParameters( MEPSystem system, Connector connector )
    {
      // TODO
    }

    #endregion

    #region Get MEPCurveType
    
    private MEPCurveType GetMEPCurveType( Document document, IReadOnlyCollection<Connector> connectors, MEPSystemType systemType )
    {
      HashSet<int>? available = null ;
      foreach ( var connector in connectors.Where( c => IsCompatibleMEPSystemType( systemType, GetSystemClassification( c ) ) ) ) {
        var (concreteType, isCompatibleType) = GetIsCompatibleFunc( connector ) ;
        var curveTypes = document.GetAllElements<MEPCurveType>( concreteType ).Where( isCompatibleType ).Select( e => e.Id.IntegerValue ) ;
        if ( null == available ) {
          available = curveTypes.ToHashSet() ;
        }
        else {
          available.IntersectWith( curveTypes ) ;
        }

        if ( 0 == available.Count ) throw new InvalidOperationException( $"Available {nameof( MEPCurveType )} is not found." ) ;
      }
      if ( null == available ) throw new InvalidOperationException( $"Available {nameof( MEPCurveType )} is not found." ) ;

      return document.GetElementById<MEPCurveType>( available.First() )! ;
    }

    private (Type, Func<MEPCurveType, bool>) GetIsCompatibleFunc( Connector connector )
    {
      return connector.Domain switch
      {
        Domain.DomainHvac => ( typeof( DuctType ), type => IsCompatibleDuctType( type, connector ) ),
        Domain.DomainPiping => ( typeof( PipeType ), type => IsCompatiblePipeType( type, connector ) ),
        _ => ( typeof( MEPCurveType ), type => HasCompatibleShape( type, connector ) ),
      } ;
    }

    private bool IsCompatibleDuctType( MEPCurveType type, Connector connector )
    {
      if ( false == HasCompatibleShape( type, connector ) ) return false ;
      if ( type is not DuctType dt ) return false ;

      return true ;
    }

    private bool IsCompatiblePipeType( MEPCurveType type, Connector connector )
    {
      if ( false == HasCompatibleShape( type, connector ) ) return false ;
      if ( type is not PipeType pt ) return false ;

      return true ;
    }

    private bool HasCompatibleShape( MEPCurveType type, Connector connector )
    {
      if ( type.Shape != connector.Shape ) return false ;

      var nominalDiameter = connector.GetDiameter() ;
      if ( false == HasAnyNominalDiameter( type, nominalDiameter ) ) return false ;
      // TODO: other parameters

      return true ;
    }

    private bool HasAnyNominalDiameter( MEPCurveType type, double nominalDiameter )
    {
      var document = type.Document ;
      var rpm = type.RoutingPreferenceManager ;
      return GetRules( rpm, RoutingPreferenceRuleGroupType.Segments ).All( rule => HasAnyNominalDiameter( document, rule, nominalDiameter ) ) ;
    }

    private static IEnumerable<RoutingPreferenceRule> GetRules( RoutingPreferenceManager rpm, RoutingPreferenceRuleGroupType groupType )
    {
      var count = rpm.GetNumberOfRules( groupType ) ;
      for ( var i = 0 ; i < count ; ++i ) {
        yield return rpm.GetRule( groupType, i ) ;
      }
    }

    private bool HasAnyNominalDiameter( Document document, RoutingPreferenceRule rule, double nominalDiameter )
    {
      if ( false == GetCriteria( rule ).OfType<PrimarySizeCriterion>().All( criterion => IsMatchRange( criterion, nominalDiameter ) ) ) return false ;

      var segment = document.GetElementById<Segment>( rule.MEPPartId ) ;
      return ( null != segment ) && HasAnyNominalDiameter( segment, nominalDiameter ) ;
    }

    private static IEnumerable<RoutingCriterionBase> GetCriteria( RoutingPreferenceRule rule )
    {
      var count = rule.NumberOfCriteria ;
      for ( var i = 0 ; i < count ; ++i ) {
        yield return rule.GetCriterion( i ) ;
      }
    }

    private static bool IsMatchRange( PrimarySizeCriterion criterion, double nominalDiameter )
    {
      return criterion.MinimumSize <= nominalDiameter && nominalDiameter <= criterion.MaximumSize ;
    }


    private bool HasAnyNominalDiameter( Segment segment, double nominalDiameter )
    {
      return segment.GetSizes().Any( size => Math.Abs( size.NominalDiameter - nominalDiameter ) < _diameterTolerance ) ;
    }

    #endregion
  }
}