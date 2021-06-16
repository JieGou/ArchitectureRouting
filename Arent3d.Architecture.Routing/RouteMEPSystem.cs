using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.DB.Plumbing ;


namespace Arent3d.Architecture.Routing
{
  public class RouteMEPSystem
  {
    public double DiameterTolerance { get ; }
    public double AngleTolerance { get ; }

    public MEPSystemType? MEPSystemType { get ; }
    public MEPSystem? MEPSystem { get ; }
    public MEPCurveType CurveType { get ; }
    

    public RouteMEPSystem( Document document, SubRoute subRoute )
    {
      DiameterTolerance = document.Application.VertexTolerance ;
      AngleTolerance = document.Application.AngleTolerance ;

      MEPSystemType = subRoute.Route.GetMEPSystemType() ;
      //MEPSystem = CreateMEPSystem( document, connector, allConnectors ) ;
      MEPSystem = null ;

      CurveType = subRoute.GetMEPCurveType() ?? throw new InvalidOperationException() ;
    }

    private SizeTable<double>? _90ElbowSize ;
    public double Get90ElbowSize( double diameter )
    {
      return ( _90ElbowSize ??= new( Calculate90ElbowSize ) ).Get( diameter ) ;
    }

    private SizeTable<double>? _45ElbowSize ;
    public double Get45ElbowSize( double diameter )
    {
      return ( _45ElbowSize ??= new( Calculate45ElbowSize ) ).Get( diameter ) ;
    }

    private SizeTable<(double, double)>? _teeHeaderLength ;
    public double GetTeeHeaderLength( double headerDiameter, double branchDiameter )
    {
      if ( JunctionType.Tee == CurveType.PreferredJunctionType ) {
        return ( _teeHeaderLength ??= new( CalculateTeeHeaderLength ) ).Get( ( headerDiameter, branchDiameter ) ) ;
      }
      else {
        return branchDiameter * 0.5 ; // provisional
      }
    }

    private SizeTable<(double, double)>? _teeBranchLength ;
    public double GetTeeBranchLength( double headerDiameter, double branchDiameter )
    {
      if ( JunctionType.Tee == CurveType.PreferredJunctionType ) {
        return ( _teeBranchLength ??= new( CalculateTeeBranchLength ) ).Get( ( headerDiameter, branchDiameter ) ) ;
      }
      else {
        return headerDiameter * 0.5 ; // provisional
      }
    }

    private SizeTable<(double, double)>? _reducerLength ;
    public double GetReducerLength( double diameter1, double diameter2 )
    {
      if ( diameter1 <= 0 || diameter2 <= 0 || Math.Abs( diameter1 - diameter2 ) < DiameterTolerance ) return 0 ;

      return ( _reducerLength ??= new( CalculateReducerLength ) ).Get( ( diameter1, diameter2 ) ) ;
    }

    public double GetWeldMinDistance( double diameter )
    {
      return 1.0 / 120 ;  // 1/10 inches.
    }

    private double Calculate90ElbowSize( double diameter )
    {
      return diameter * 1.5 ; // provisional
    }
    private double Calculate45ElbowSize( double diameter )
    {
      return diameter * 1.5 ; // provisional
    }
    private double CalculateTeeHeaderLength( (double, double) value )
    {
      var (headerDiameter, branchDiameter) = value ;
      if ( headerDiameter < branchDiameter ) {
        return headerDiameter * 1.0 ; // provisional
      }
      else {
        return headerDiameter * 0.5 + branchDiameter * 0.5 ; // provisional
      }
    }
    private double CalculateTeeBranchLength( (double, double) value )
    {
      var (headerDiameter, branchDiameter) = value ;
      if ( headerDiameter < branchDiameter ) {
        return headerDiameter * 1.0 + GetReducerLength( headerDiameter, branchDiameter ) ; // provisional
      }
      else {
        return headerDiameter * 0.5 + branchDiameter * 0.5 ; // provisional
      }
    }

    public double CalculateReducerLength( (double, double) value )
    {
      var (diameter1, diameter2) = value ;

      // TODO: find reducer size

      return 0 ;
    }


    #region Get MEPSystemType

    public static MEPSystemType? GetSystemType( Document document, Connector connector )
    {
      var systemClassification = GetSystemClassificationInfo( connector ) ;
      
      return document.GetAllElements<MEPSystemType>().FirstOrDefault( systemClassification.IsCompatibleTo ) ;
    }

    private static MEPSystemClassificationInfo GetSystemClassificationInfo( Connector connector )
    {
      return MEPSystemClassificationInfo.From( connector ) ?? throw new KeyNotFoundException() ;
    }

    #endregion

    #region Create MEPSystem

    private static MEPSystem? CreateMEPSystem( Document document, Connector baseConnector, IReadOnlyCollection<Connector> allConnectors )
    {
      return baseConnector.Domain switch
      {
        Domain.DomainHvac => CreateMechanicalMEPSystem( document, baseConnector, allConnectors ),
        Domain.DomainPiping => CreatePipingMEPSystem( document, baseConnector, allConnectors ),
        Domain.DomainCableTrayConduit => null,
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

      if ( new ConnectorId( mepSystem.BaseEquipmentConnector ) == new ConnectorId( c ) ) {
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

    public static MEPCurveType GetMEPCurveType( Document document, IEnumerable<Connector> connectors, MEPSystemType? systemType )
    {
      return GetBestForAllMEPCurveType( document, connectors, systemType )
             ?? throw new InvalidOperationException( $"Available {nameof( MEPCurveType )} is not found." ) ;
    }
    private static MEPCurveType? GetBestForAllMEPCurveType( Document document, IEnumerable<Connector> connectors, MEPSystemType? systemType )
    {
      var diameterTolerance = document.Application.VertexTolerance ;
      Dictionary<int, CompatibilityPriority>? available = null ;
      foreach ( var connector in connectors.Where( c => GetSystemClassificationInfo( c ).IsCompatibleTo( systemType ) ) ) {
        var (concreteType, getCompatibilityPriority) = GetCompatibilityPriorityFunc( connector, diameterTolerance ) ;
        var curveTypes = document.GetAllElements<MEPCurveType>( concreteType ) ;
        if ( null == available ) {
          available = new Dictionary<int, CompatibilityPriority>() ;
          foreach ( var curveType in curveTypes ) {
            if ( getCompatibilityPriority( curveType ) is not { } priority ) continue ;
            available.Add( curveType.Id.IntegerValue, priority ) ;
          }
        }
        else {
          var intersected = new HashSet<int>() ;
          foreach ( var curveType in curveTypes ) {
            if ( getCompatibilityPriority( curveType ) is not { } priority ) continue ;

            var id = curveType.Id.IntegerValue ;
            if ( false == available.TryGetValue( id, out var oldPriority ) ) continue ;

            if ( priority.IsLessCompatibleThan( oldPriority ) ) {
              available[ id ] = priority ;
            }

            intersected.Add( id ) ;
          }

          foreach ( var id in available.Keys.Where( id => false == intersected.Contains( id ) ).EnumerateAll() ) {
            available.Remove( id ) ;
          }
        }

        if ( 0 == available.Count ) return null ;
      }
      if ( null == available ) return null ;

      var curveTypeId = ElementId.InvalidElementId.IntegerValue ;
      CompatibilityPriority? bestPriority = null ;

      foreach ( var (id, priority) in available ) {
        if ( null == bestPriority || bestPriority.IsLessCompatibleThan( priority ) ) {
          bestPriority = priority ;
          curveTypeId = id ;
        }
        else if ( false == priority.IsLessCompatibleThan( bestPriority ) ) {
          // on same value, smaller curve type id is used.
          if ( curveTypeId > id ) {
            curveTypeId = id ;
          }
        }
      }

      return document.GetElementById<MEPCurveType>( curveTypeId ) ;
    }

    private static (Type, Func<MEPCurveType, CompatibilityPriority?>) GetCompatibilityPriorityFunc( Connector connector, double diameterTolerance )
    {
      return connector.Domain switch
      {
        Domain.DomainHvac => ( typeof( DuctType ), type => GetDuctCompatibilityPriority( type, connector, diameterTolerance ) ),
        Domain.DomainPiping => ( typeof( PipeType ), type => GetPipeCompatibilityPriority( type, connector, diameterTolerance ) ),
        Domain.DomainCableTrayConduit => ( typeof( ConduitType ), type => GetConduitCompatibilityPriority( type, connector, diameterTolerance ) ),
        _ => ( typeof( MEPCurveType ), _ => null ),
      } ;
    }

    private static CompatibilityPriority? GetDuctCompatibilityPriority( MEPCurveType type, Connector connector, double diameterTolerance )
    {
      if ( type is not DuctType dt ) return null ;

      return GetShapeCompatibilityPriority( type, connector, diameterTolerance ) ;
    }

    private static CompatibilityPriority? GetPipeCompatibilityPriority( MEPCurveType type, Connector connector, double diameterTolerance )
    {
      if ( type is not PipeType pt ) return null ;

      return GetShapeCompatibilityPriority( type, connector, diameterTolerance ) ;
    }

    private static CompatibilityPriority? GetConduitCompatibilityPriority( MEPCurveType type, Connector connector, double diameterTolerance )
    {
      if ( type is not ConduitType ct ) return null ;

      return GetShapeCompatibilityPriority( type, connector, diameterTolerance ) ;
    }

    private static CompatibilityPriority? GetShapeCompatibilityPriority( MEPCurveType type, Connector connector, double diameterTolerance )
    {
      var priorityType = ( type.Shape == connector.Shape ) ? CompatibilityPriorityType.SameShape : CompatibilityPriorityType.DifferentShape ;

      var nominalDiameter = connector.GetDiameter() ;
      if ( type.HasAnyNominalDiameter( nominalDiameter, diameterTolerance ) ) return new CompatibilityPriority( priorityType, 0 ) ;

      var list = type.GetNominalDiameters( diameterTolerance ) ;
      if ( 0 == list.Count ) return null ;

      int index = list.BinarySearch( nominalDiameter ) ;
      if ( index < 0 ) index = ~index ;

      var diff1 = ( index == 0 ) ? double.MaxValue : nominalDiameter - list[ index - 1 ] ;
      var diff2 = ( index == list.Count ) ? double.MaxValue : list[ index ] - nominalDiameter ;

      return new CompatibilityPriority( priorityType, Math.Min( diff1, diff2 ) ) ;
    }

    private enum CompatibilityPriorityType
    {
      DifferentShape,
      SameShape,
    }

    private class CompatibilityPriority
    {
      private readonly CompatibilityPriorityType _priorityType ;
      private readonly double _diffValue ;
      
      public CompatibilityPriority( CompatibilityPriorityType priorityType, double diffValue )
      {
        _priorityType = priorityType ;
        _diffValue = diffValue ;
      }

      public bool IsLessCompatibleThan( CompatibilityPriority another )
      {
        var type = ( (int) _priorityType ) - ( (int) another._priorityType ) ;
        if ( 0 != type ) return ( type < 0 ) ;

        return ( _diffValue > another._diffValue ) ;
      }
    }

    #endregion
  }
}