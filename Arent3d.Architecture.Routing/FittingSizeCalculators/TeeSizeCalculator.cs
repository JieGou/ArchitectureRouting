using System ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.FittingSizeCalculators.MEPCurveGenerators ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.FittingSizeCalculators
{
  internal class TeeSizeCalculator : SizeCalculatorBase
  {
    public TeeSizeCalculator( Document document, IMEPCurveGenerator fittingGenerator, double diameter1, double diameter2 ) : base( document, fittingGenerator, GetStraightLineLength( diameter1, diameter2 ) )
    {
    }

    private static double GetStraightLineLength( double diameter1, double diameter2 ) => Math.Max( Math.Max( diameter1, diameter2 ) * 50, 1 ) ; // diameter * 50 or 1ft (greater)

    protected override IReadOnlyList<XYZ> EndDirections => new[] { new XYZ( -1, 0, 0 ), new XYZ( 1, 0, 0 ), new XYZ( 0, 1, 0 ) } ;

    protected override void GenerateFittingFromConnectors( IReadOnlyList<Connector> connectors )
    {
      if ( 2 != connectors.Count ) return ;

      Document.Create.NewTransitionFitting( connectors[ 0 ], connectors[ 1 ] ) ;
    }

    private (double HeaderSize, double BranchSize)? _teeSizes ;

    private static (double HeaderSize, double BranchSize) GetTeeSize( IReadOnlyList<XYZ>? connectorPositions )
    {
      if ( null == connectorPositions || 3 != connectorPositions.Count ) return ( 0, 0 ) ;

      var headerSize = Math.Max( connectorPositions[ 0 ].GetLength(), connectorPositions[ 1 ].GetLength() ) ;
      var branchSize = connectorPositions[ 2 ].GetLength() ;
      return ( headerSize, branchSize ) ;
    }

    public double HeaderSize => ( _teeSizes ??= GetTeeSize( ConnectorPositions ) ).HeaderSize ;
    public double BranchSize => ( _teeSizes ??= GetTeeSize( ConnectorPositions ) ).BranchSize ;
  }
}