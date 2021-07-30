using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.FittingSizeCalculators.MEPCurveGenerators ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.FittingSizeCalculators
{
  internal abstract class SizeCalculatorBase
  {
    protected Document Document { get ; }
    private IMEPCurveGenerator FittingGenerator { get ; }
    protected IReadOnlyList<XYZ>? ConnectorPositions { get ; }

    protected SizeCalculatorBase( Document document, IMEPCurveGenerator fittingGenerator, double straitLineLength )
    {
      Document = document ;
      FittingGenerator = fittingGenerator ;

      using var transaction = new SubTransaction( Document ) ;
      try {
        transaction.Start() ;

        var (curves, connectors) = CreateMEPCurvesToConnect( straitLineLength ) ;

        if ( null != connectors ) {
          ConnectorPositions = GenerateFitting( curves, connectors ) ;
        }
      }
      catch {
        ConnectorPositions = null ;
      }
      finally {
        transaction.RollBack() ;
      }
    }

    private (IReadOnlyList<MEPCurve>, IReadOnlyList<Connector>?) CreateMEPCurvesToConnect( double straitLineLength )
    {
      var curves = new List<MEPCurve>() ;
      var connectors = new List<Connector>() ;

      var center = XYZ.Zero ;
      foreach ( var endDirection in EndDirections ) {
        var curve = FittingGenerator.GenerateCurve( center, center + straitLineLength * endDirection ) ;
        if ( null == curve ) return ( curves, null ) ;

        curves.Add( curve ) ;

        if ( curve.GetConnectors().FirstOrDefault( c => c.Origin.IsAlmostEqualTo( center ) ) is not { } connector ) return ( curves, null ) ;

        connectors.Add( connector ) ;
      }

      return ( curves, connectors ) ;
    }

    protected abstract IReadOnlyList<XYZ> EndDirections { get ; }

    private IReadOnlyList<XYZ> GenerateFitting( IReadOnlyList<MEPCurve> curves, IReadOnlyList<Connector> connectors )
    {
      GenerateFittingFromConnectors( connectors ) ;

      Document.Regenerate() ;
      return GetConnectorPositions( curves ) ;
    }

    protected abstract void GenerateFittingFromConnectors( IReadOnlyList<Connector> connectors ) ;

    private static IReadOnlyList<XYZ> GetConnectorPositions( IReadOnlyList<MEPCurve> curves )
    {
      return curves.ConvertAll( GetConnectorPosition ) ;
    }

    private static XYZ GetConnectorPosition( MEPCurve curve )
    {
      return curve.GetConnectors().Select( conn => conn.Origin ).MinItemOrDefault( pos => pos.DotProduct( pos ) ) ?? throw new InvalidOperationException() ;
    }
  }
}