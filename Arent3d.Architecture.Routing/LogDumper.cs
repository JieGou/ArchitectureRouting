using System ;
using System.Collections.Generic ;
using System.Globalization ;
using System.Linq ;
using System.Xml ;
using Arent3d.Routing ;
using Arent3d.Routing.Conditions ;
using Arent3d.Utility ;
using MathLib ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// AutoRoutingTarget Dumper (for debug).
  /// </summary>
  internal static class LogDumper
  {
    #region Routing Targets Writer

    public static void DumpRoutingTargets( this IEnumerable<IAutoRoutingTarget> routingTargets, string file )
    {
      using var writer = XmlWriter.Create( file, new XmlWriterSettings { Indent = true, IndentChars = "  ", } ) ;

      writer.WriteStartDocument() ;
      writer.WriteTargets( "Targets", routingTargets ) ;
      writer.WriteEndDocument() ;
    }

    private static void WriteTargets( this XmlWriter writer, string elmName, IEnumerable<IAutoRoutingTarget> routingTargets )
    {
      using var _ = writer.WriteElement( elmName ) ;

      foreach ( var target in routingTargets ) {
        writer.WriteTarget( "Target", target ) ;
      }
    }

    private static void WriteTarget( this XmlWriter writer, string elmName, IAutoRoutingTarget target )
    {
      using var _ = writer.WriteElement( elmName ) ;

      writer.WriteCondition( "Condition", target.Condition ) ;

      if ( target.CreateConstraints() is {} constraints ) {
        writer.WriteConstraint( "Constraints", constraints ) ;
      }
    }

    private static void WriteCondition( this XmlWriter writer, string elmName, ICommonRoutingCondition targetCondition )
    {
      using var _ = writer.WriteElement( elmName ) ;

      writer.WriteElementString( "IsRoutingOnPipeRacks", targetCondition.IsRoutingOnPipeRacks.ToString() ) ;
      writer.WriteElementString( "Type", targetCondition.Type.ToString() ) ;
      writer.WriteElementString( "Priority", targetCondition.Priority.ToString() ) ;
      writer.WriteElementString( "LoopType", targetCondition.LoopType.ToString() ) ;
      writer.WriteElementString( "AllowHorizontalBranches", targetCondition.AllowHorizontalBranches.ToString() ) ;
      if ( targetCondition.FixedBopHeight.HasValue ) {
        writer.WriteElementString( "FixedBopHeight", targetCondition.FixedBopHeight.Value.ToString( CultureInfo.InvariantCulture ) ) ;
      }
    }

    private static void WriteConstraint( this XmlWriter writer, string elmName, IAutoRoutingSpatialConstraints constraints )
    {
      using var _ = writer.WriteElement( elmName ) ;

      foreach ( var ep in constraints.Starts ) {
        writer.WriteEndPoint( "Start", ep ) ;
      }
      foreach ( var ep in constraints.Destination ) {
        writer.WriteEndPoint( "Destination", ep ) ;
      }
    }

    private static void WriteEndPoint( this XmlWriter writer, string elmName, IAutoRoutingEndPoint ep )
    {
      using var _ = writer.WriteElement( elmName ) ;

      writer.WriteElementString( "IsStart", ep.IsStart.ToString() ) ;
      writer.WriteElementString( "Depth", ep.Depth.ToString() ) ;
      writer.WriteVector( "Position", ep.Position ) ;
      writer.WriteVector( "Direction", ep.Direction ) ;
      writer.WriteElementString( "PointType", ep.PointType.ToString() ) ;
      writer.WritePipeCondition( "PipeCondition", ep.PipeCondition ) ;
    }

    private static void WritePipeCondition( this XmlWriter writer, string elmName, IRouteCondition condition )
    {
      using var _ = writer.WriteElement( elmName ) ;

     writer.WriteElementString( "Diameter", condition.Diameter.Outside.ToString( CultureInfo.InvariantCulture ) ) ;
     writer.WriteElementString( "DiameterPipeAndInsulation", condition.DiameterPipeAndInsulation.ToString( CultureInfo.InvariantCulture ) ) ;
     writer.WriteElementString( "DiameterFlangeAndInsulation", condition.DiameterFlangeAndInsulation.ToString( CultureInfo.InvariantCulture ) ) ;
     writer.WriteElementString( "ProcessConstraint", condition.ProcessConstraint.ToString() ) ;
     writer.WriteElementString( "FluidPhase", condition.FluidPhase ) ;
    }

    private static void WriteVector( this XmlWriter writer, string elmName, Vector3d dir )
    {
      using var _ = writer.WriteElement( elmName ) ;

      writer.WriteElementString( "X", dir.x.ToString( CultureInfo.InvariantCulture ) ) ;
      writer.WriteElementString( "Y", dir.y.ToString( CultureInfo.InvariantCulture ) ) ;
      writer.WriteElementString( "Z", dir.z.ToString( CultureInfo.InvariantCulture ) ) ;
    }

    #endregion

    #region Routing Targets Reader

    public static IReadOnlyCollection<IAutoRoutingTarget> RoutingTargetsFromDump( string file )
    {
      using var reader = XmlReader.Create( file ) ;

      return reader.ReadTargets( "Targets" ) ;
    }

    private static IReadOnlyCollection<IAutoRoutingTarget> ReadTargets( this XmlReader reader, string elmName )
    {
      var list = new List<IAutoRoutingTarget>() ;

      reader.ReadToFollowing( elmName ) ;
      reader.ReadStartElement( elmName ) ;
      while ( reader.IsStartElement() ) {
        reader.ReadStartElement( "Target" ) ;
        list.Add( ReadTarget( reader ) ) ;
        reader.ReadEndElement() ;
      }

      reader.ReadEndElement() ;

      return list ;
    }

    private class DumpedAutoRoutingTarget : IAutoRoutingTarget
    {
      public IAutoRoutingSpatialConstraints? Constraints { get ; set ; }
      public IAutoRoutingSpatialConstraints? CreateConstraints() => Constraints ;

      public ICommonRoutingCondition? Condition { get ; set ; }

      public int RouteCount => Constraints!.Starts.Count() + Constraints.Destination.Count() - 1 ;

      public Action<IEnumerable<(IAutoRoutingEndPoint, Vector3d)>> PositionInitialized => x => { } ;
    }
    private static IAutoRoutingTarget ReadTarget( XmlReader reader )
    {
      var target = new DumpedAutoRoutingTarget() ;

      reader.ReadStartElement( "Condition" ) ;
      target.Condition = ReadCondition( reader ) ;
      reader.ReadEndElement() ;

      reader.ReadStartElement( "Constraints" ) ;
      target.Constraints = ReadConstraints( reader ) ;
      reader.ReadEndElement() ;

      return target ;
    }

    private class DumpedCommonRoutingCondition : ICommonRoutingCondition
    {
      public bool IsRoutingOnPipeRacks { get ; set ; }
      public LineType Type { get ; set ; }
      public int Priority { get ; set ; }
      public LoopType LoopType { get ; set ; }
      public bool AllowHorizontalBranches { get ; set ; }
      public double? FixedBopHeight { get ; set ; }
    }
    private static ICommonRoutingCondition ReadCondition( XmlReader reader )
    {
      var condition = new DumpedCommonRoutingCondition() ;

      condition.IsRoutingOnPipeRacks = ReadBool( reader, "IsRoutingOnPipeRacks" ) ;
      condition.Type = ReadEnum<LineType>( reader, "Type" ) ;
      condition.Priority = ReadInt( reader, "Priority" ) ;
      condition.LoopType = ReadEnum<LoopType>( reader, "LoopType" ) ;
      condition.AllowHorizontalBranches = ReadBool( reader, "AllowHorizontalBranches" ) ;
      if ( reader.IsStartElement() ) {
        condition.FixedBopHeight = ReadDouble( reader, "FixedBopHeight" ) ;
      }

      return condition ;
    }

    private class DumpedAutoRoutingSpatialConstraints : IAutoRoutingSpatialConstraints
    {
      public IList<IAutoRoutingEndPoint> Starts { get ; } = new List<IAutoRoutingEndPoint>() ;
      public IList<IAutoRoutingEndPoint> Destination { get ; } = new List<IAutoRoutingEndPoint>() ;

      IEnumerable<IAutoRoutingEndPoint> IAutoRoutingSpatialConstraints.Starts => Starts ;
      IEnumerable<IAutoRoutingEndPoint> IAutoRoutingSpatialConstraints.Destination => Destination ;
    }
    private static IAutoRoutingSpatialConstraints ReadConstraints( XmlReader reader )
    {
      var constraints = new DumpedAutoRoutingSpatialConstraints() ;

      while ( reader.IsStartElement() ) {
        if ( "Start" == reader.Name ) {
          reader.ReadStartElement() ;
          constraints.Starts.Add( ReadEndPoint( reader ) ) ;
          reader.ReadEndElement() ;
        }
        else if ( "Destination" == reader.Name ) {
          reader.ReadStartElement() ;
          constraints.Destination.Add( ReadEndPoint( reader ) ) ;
          reader.ReadEndElement() ;
        }
      }

      return constraints ;
    }

    private class DumpedAutoRoutingEndPoint : IAutoRoutingEndPoint
    {
      public Vector3d Position { get ; set ; }
      public Vector3d Direction { get ; set ; }
      public IRouteCondition? PipeCondition { get ; set ; }
      public bool IsStart { get ; set ; }
      public int Depth { get ; set ; }
      public RoutingPointType PointType { get ; set ; }
      public IStructureInfo? LinkedRack => null ;
    }
    private static IAutoRoutingEndPoint ReadEndPoint( XmlReader reader )
    {
      var ep = new DumpedAutoRoutingEndPoint() ;

      ep.IsStart = ReadBool( reader, "IsStart" ) ;
      ep.Depth = ReadInt( reader, "Depth" ) ;
      ep.Position = ReadVector( reader, "Position" ) ;
      ep.Direction = ReadVector( reader, "Direction" ) ;
      ep.PointType = ReadEnum<RoutingPointType>( reader, "PointType" ) ;

      reader.ReadStartElement( "PipeCondition" ) ;
      ep.PipeCondition = ReadPipeCondition( reader ) ;
      reader.ReadEndElement() ;

      return ep ;
    }

    private class DumpedRouteCondition : IRouteCondition
    {
      public IPipeDiameter? Diameter { get ; set ; }
      public double DiameterPipeAndInsulation { get ; set ; }
      public double DiameterFlangeAndInsulation { get ; set ; }
      public IPipeSpec Spec => DefaultPipeSpec.Instance ;
      public ProcessConstraint ProcessConstraint { get ; set ; }
      public string FluidPhase { get ; set ; } = string.Empty ;
    }
    private class PipeDiameter : IPipeDiameter
    {
      public PipeDiameter( double value )
      {
        Outside = value ;
        NPSmm = (int) Math.Floor( value * 1000 ) ;
      }

      public double Outside { get ; }
      public int NPSmm { get ; }
    }
    private static IRouteCondition ReadPipeCondition( XmlReader reader )
    {
      var condition = new DumpedRouteCondition() ;

      condition.Diameter = new PipeDiameter( ReadDouble( reader, "Diameter" ) ) ;
      condition.DiameterPipeAndInsulation = ReadDouble( reader, "DiameterPipeAndInsulation" ) ;
      condition.DiameterFlangeAndInsulation = ReadDouble( reader, "DiameterFlangeAndInsulation" ) ;
      condition.ProcessConstraint = ReadEnum<ProcessConstraint>( reader, "ProcessConstraint" ) ;
      condition.FluidPhase = reader.ReadElementString( "FluidPhase" ) ;

      return condition ;
    }

    private static bool ReadBool( XmlReader reader, string tagName )
    {
      var str = reader.ReadElementString( tagName ) ;
      return bool.TryParse( str, out var val ) ? val : default ;
    }
    private static int ReadInt( XmlReader reader, string tagName )
    {
      var str = reader.ReadElementString( tagName ) ;
      return int.TryParse( str, out var val ) ? val : default ;
    }
    private static double ReadDouble( XmlReader reader, string tagName )
    {
      var str = reader.ReadElementString( tagName ) ;
      return double.TryParse( str, NumberStyles.Any, CultureInfo.InvariantCulture, out var val ) ? val : default ;
    }
    private static TEnum ReadEnum<TEnum>( XmlReader reader, string tagName ) where TEnum : struct, Enum
    {
      var str = reader.ReadElementString( tagName ) ;
      return Enum.TryParse( str, out TEnum val ) ? val : default ;
    }
    private static Vector3d ReadVector( XmlReader reader, string tagName )
    {
      reader.ReadStartElement( tagName ) ;
      var x = ReadDouble( reader, "X" ) ;
      var y = ReadDouble( reader, "Y" ) ;
      var z = ReadDouble( reader, "Z" ) ;
      reader.ReadEndElement() ;

      return new Vector3d( x, y, z ) ;
    }

    private class DefaultPipeSpec : IPipeSpec
    {
      public static DefaultPipeSpec Instance { get ; } = new DefaultPipeSpec() ;
      
      public double GetLongElbowSize( IPipeDiameter diameter ) => Get90ElbowSize( diameter.Outside ) ;

      public double Get45ElbowSize( IPipeDiameter diameter ) => Get45ElbowSize( diameter.Outside ) ;

      public double GetTeeBranchLength( IPipeDiameter header, IPipeDiameter branch ) => GetTeeBranchLength( header.Outside, branch.Outside ) ;

      public double GetTeeHeaderLength( IPipeDiameter header, IPipeDiameter branch ) => GetTeeHeaderLength( header.Outside, branch.Outside ) ;

      public double GetWeldMinDistance( IPipeDiameter diameter ) => GetWeldMinDistance( diameter.Outside ) ;

      public string Name => "default" ;

      private DefaultPipeSpec()
      {
      }

      private double Get90ElbowSize( double diameter )
      {
        return diameter * 1.5 ;
      }

      private double Get45ElbowSize( double diameter )
      {
        return diameter * 1.5 ;
      }

      private double GetTeeHeaderLength( double headerDiameter, double branchDiameter )
      {
        if ( headerDiameter < branchDiameter ) {
          return headerDiameter * 1.0 ;
        }
        else {
          return headerDiameter * 0.5 + branchDiameter * 0.5 ;
        }
      }

      private double GetTeeBranchLength( double headerDiameter, double branchDiameter )
      {
        if ( headerDiameter < branchDiameter ) {
          return headerDiameter * 1.0 + GetReducerLength( headerDiameter, branchDiameter ) ; // provisional
        }
        else {
          return headerDiameter * 0.5 + branchDiameter * 0.5 ; // provisional
        }
      }

      private double GetReducerLength( double diameter1, double diameter2 )
      {
        return 0 ;
      }

      private double GetWeldMinDistance( double diameter )
      {
        return 1.0 / 120 ;  // 1/10 inches.
      }
    }
    
    #endregion
  }
}