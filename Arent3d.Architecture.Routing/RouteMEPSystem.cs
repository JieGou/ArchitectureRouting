using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.DB.Plumbing ;
using Autodesk.Revit.DB.Structure.StructuralSections ;

namespace Arent3d.Architecture.Routing
{
  public class RouteMEPSystem
  {
    public MEPSystemType MEPSystemType { get ; }
    public MEPSystem? MEPSystem { get ; }
    public MEPCurveType CurveType { get ; }

    public RouteMEPSystem( Document document, Route route )
    {
      var allConnectors = route.GetAllConnectors( document ).EnumerateAll() ;
      var (connector, mepSystemType) = GetSystemType( document, allConnectors ) ;
      MEPSystemType = mepSystemType ;

      //MEPSystem = CreateMEPSystem( document, connector, allConnectors ) ;
      MEPSystem = null ;

      CurveType = GetMEPCurveType( document, connector ) ;
    }

    #region Get MEPSystemType

    private static (Connector, MEPSystemType) GetSystemType( Document document, IEnumerable<Connector> connectors )
    {
      return connectors.Select( connector => ( connector, GetSystemType( document, connector ) ) ).First( tuple => null != tuple.Item2 )! ;
    }
    private static MEPSystemType? GetSystemType( Document document, Connector connector )
    {
      var systemClassification = GetSystemClassification( connector ) ;
      foreach ( var type in document.GetAllElements<MEPSystemType>() ) {
        if ( type.SystemClassification == systemClassification ) {
          return type ;
        }
      }

      return null ;
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
    
    private static MEPCurveType GetMEPCurveType( Document document, Connector connector )
    {
      var (concreteType, isCompatibleType) = GetIsCompatibleFunc( connector ) ;

      var curveTypes = new FilteredElementCollector( document ).OfClass( concreteType ).OfType<MEPCurveType>().Where( isCompatibleType ).ToList() ;
      
      return curveTypes.FirstOrDefault() ?? throw new InvalidOperationException( $"{nameof( MEPCurveType )} for connector {connector.Owner.Name}:{connector.Id} is not found." ) ;
    }

    private static (Type, Func<MEPCurveType, bool>) GetIsCompatibleFunc( Connector connector )
    {
      return connector.Domain switch
      {
        Domain.DomainHvac => ( typeof( DuctType ), type => IsCompatibleDuctType( type, connector ) ),
        Domain.DomainPiping => ( typeof( PipeType ), type => IsCompatiblePipeType( type, connector ) ),
        _ => ( typeof( MEPCurveType ), type => HasCompatibleShape( type, connector ) ),
      } ;
    }

    private static bool IsCompatibleDuctType( MEPCurveType type, Connector connector )
    {
      if ( false == HasCompatibleShape( type, connector ) ) return false ;
      if ( type is not DuctType dt ) return false ;

      return true ;
    }

    private static bool IsCompatiblePipeType( MEPCurveType type, Connector connector )
    {
      if ( false == HasCompatibleShape( type, connector ) ) return false ;
      if ( type is not PipeType pt ) return false ;

      return true ;
    }

    private static bool HasCompatibleShape( MEPCurveType type, Connector connector )
    {
      if ( type.Shape != connector.Shape ) return false ;

      // TODO: other parameters

      return true ;
    }

    #endregion
  }
}