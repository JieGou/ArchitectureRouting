using System ;
using System.Collections.Generic ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.DB.Plumbing ;

namespace Arent3d.Architecture.Routing
{
  public abstract class MEPSystemClassificationInfo
  {
    public static MEPSystemClassificationInfo? From( Connector connector )
    {
      return connector.Domain switch
      {
        Domain.DomainPiping => Create( connector.PipeSystemType ),
        Domain.DomainHvac => Create( connector.DuctSystemType ),
        Domain.DomainElectrical => Create( connector.ElectricalSystemType ),
        Domain.DomainCableTrayConduit => CableTrayConduit,
        _ => null,
      } ;
    }

    private static MEPSystemClassificationInfo Create( PipeSystemType systemType ) => new PipeSystemClassificationInfo( systemType ) ;
    private static MEPSystemClassificationInfo Create( DuctSystemType systemType ) => new DuctSystemClassificationInfo( systemType ) ;
    private static MEPSystemClassificationInfo Create( ElectricalSystemType systemType ) => new ElectricalSystemClassificationInfo( systemType ) ;

    private static MEPSystemClassificationInfo CableTrayConduit { get ; } = new CableTrayConduitSystemClassificationInfo() ;
    public static MEPSystemClassificationInfo Undefined { get ; } = new UndefinedSystemClassificationInfo() ;


    public abstract Domain Domain { get ; }

    public abstract Type? GetCurveTypeClass() ;

    public abstract bool IsCompatibleTo( MEPSystemType type ) ;

    public abstract bool IsCompatibleTo( MEPSystemClassificationInfo another ) ;

    public bool IsCompatibleTo( Connector connector )
    {
      return ( connector.Domain == Domain ) && HasCompatibleSystemType( connector ) ;
    }

    protected abstract bool HasCompatibleSystemType( Connector connector ) ;

    #region Serialize

    private const string PipeTypeName = "p" ;
    private const string DuctTypeName = "d" ;
    private const string ElectricalTypeName = "e" ;
    private const string CableTrayConduitTypeName = "c" ;

    private static readonly char[] Splitter = { ':' } ;

    public static MEPSystemClassificationInfo? Deserialize( string serialized )
    {
      var array = serialized.Split( Splitter, 2 ) ;
      if ( 2 != array.Length ) return null ;

      return array[ 0 ] switch
      {
        PipeTypeName => PipeSystemClassificationInfo.DeserializeImpl( array[ 1 ] ),
        DuctTypeName => DuctSystemClassificationInfo.DeserializeImpl( array[ 1 ] ),
        ElectricalTypeName => ElectricalSystemClassificationInfo.DeserializeImpl( array[ 1 ] ),
        CableTrayConduitTypeName => CableTrayConduit,
        _ => null,
      } ;
    }

    public string Serialize()
    {
      return $"{TypeName}:{ValueName}" ;
    }
    
    protected abstract string TypeName { get ; }
    protected abstract string ValueName { get ; }

    #endregion


    private class PipeSystemClassificationInfo : MEPSystemClassificationInfo
    {
      private readonly PipeSystemType _systemType ;

      public PipeSystemClassificationInfo( PipeSystemType systemType ) => _systemType = systemType ;

      public override Domain Domain => Domain.DomainPiping ;
      public override Type? GetCurveTypeClass() => typeof( PipeType ) ;

      public override bool IsCompatibleTo( MEPSystemType type ) => (int) type.SystemClassification == (int) _systemType ;

      public override bool IsCompatibleTo( MEPSystemClassificationInfo another ) => another is PipeSystemClassificationInfo ps && _systemType == ps._systemType ;

      protected override bool HasCompatibleSystemType( Connector connector ) => connector.PipeSystemType == _systemType ;


      protected override string TypeName => PipeTypeName ;
      protected override string ValueName => _systemType.ToString() ;
      public static PipeSystemClassificationInfo? DeserializeImpl( string s )
      {
        if ( false == Enum.TryParse( s, out PipeSystemType systemType ) ) return null ;
        return new PipeSystemClassificationInfo( systemType ) ;
      }
    }
    private class DuctSystemClassificationInfo : MEPSystemClassificationInfo
    {
      private readonly DuctSystemType _systemType ;

      public override Domain Domain => Domain.DomainHvac ;
      public override Type? GetCurveTypeClass() => typeof( DuctType ) ;

      public DuctSystemClassificationInfo( DuctSystemType systemType ) => _systemType = systemType ;

      public override bool IsCompatibleTo( MEPSystemType type ) => (int) type.SystemClassification == (int) _systemType ;

      public override bool IsCompatibleTo( MEPSystemClassificationInfo another ) => another is DuctSystemClassificationInfo ds && _systemType == ds._systemType ;

      protected override bool HasCompatibleSystemType( Connector connector ) => connector.DuctSystemType == _systemType ;

      protected override string TypeName => DuctTypeName ;
      protected override string ValueName => _systemType.ToString() ;
      public static DuctSystemClassificationInfo? DeserializeImpl( string s )
      {
        if ( false == Enum.TryParse( s, out DuctSystemType systemType ) ) return null ;
        return new DuctSystemClassificationInfo( systemType ) ;
      }
    }
    private class ElectricalSystemClassificationInfo : MEPSystemClassificationInfo
    {
      private readonly ElectricalSystemType _systemType ;

      public ElectricalSystemClassificationInfo( ElectricalSystemType systemType ) => _systemType = systemType ;

      public override Domain Domain => Domain.DomainElectrical ;
      public override Type? GetCurveTypeClass() => null ;

      public override bool IsCompatibleTo( MEPSystemType type ) => (int) type.SystemClassification == (int) _systemType ;

      public override bool IsCompatibleTo( MEPSystemClassificationInfo another ) => another is ElectricalSystemClassificationInfo es && _systemType == es._systemType ;

      protected override bool HasCompatibleSystemType( Connector connector ) => connector.ElectricalSystemType == _systemType ;

      protected override string TypeName => ElectricalTypeName ;
      protected override string ValueName => _systemType.ToString() ;
      public static ElectricalSystemClassificationInfo? DeserializeImpl( string s )
      {
        if ( false == Enum.TryParse( s, out ElectricalSystemType systemType ) ) return null ;
        return new ElectricalSystemClassificationInfo( systemType ) ;
      }
    }
    private class CableTrayConduitSystemClassificationInfo : MEPSystemClassificationInfo
    {
      public override Domain Domain => Domain.DomainCableTrayConduit ;
      public override Type? GetCurveTypeClass() => typeof( ConduitType ) ;

      public override bool IsCompatibleTo( MEPSystemType type ) => false ;

      public override bool IsCompatibleTo( MEPSystemClassificationInfo another ) => another is CableTrayConduitSystemClassificationInfo ;

      protected override bool HasCompatibleSystemType( Connector connector ) => true ;

      protected override string TypeName => CableTrayConduitTypeName ;
      protected override string ValueName => string.Empty ;
    }
    private class UndefinedSystemClassificationInfo : MEPSystemClassificationInfo
    {
      public override Domain Domain => Domain.DomainUndefined ;
      public override Type? GetCurveTypeClass() => null ;

      public override bool IsCompatibleTo( MEPSystemType type ) => type.SystemClassification == MEPSystemClassification.UndefinedSystemClassification ;

      public override bool IsCompatibleTo( MEPSystemClassificationInfo another ) => false ;

      protected override bool HasCompatibleSystemType( Connector connector ) => false ;

      protected override string TypeName => throw new NotSupportedException() ;
      protected override string ValueName => throw new NotSupportedException() ;
    }
  }
}