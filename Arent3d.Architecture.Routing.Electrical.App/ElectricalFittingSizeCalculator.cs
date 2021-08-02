using Arent3d.Architecture.Routing.FittingSizeCalculators ;
using Arent3d.Architecture.Routing.FittingSizeCalculators.MEPCurveGenerators ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App
{
  internal class ElectricalFittingSizeCalculator : IFittingSizeCalculator
  {
    public double Calc90ElbowSize( Document document, IMEPCurveGenerator mepCurveGenerator, double diameter )
    {
      return DefaultFittingSizeCalculator.Instance.Calc90ElbowSize( document, mepCurveGenerator, diameter ) ;
    }

    public double Calc45ElbowSize( Document document, IMEPCurveGenerator mepCurveGenerator, double diameter )
    {
      return DefaultFittingSizeCalculator.Instance.Calc45ElbowSize( document, mepCurveGenerator, diameter ) ;
    }

    public (double Header, double Branch) CalculateTeeLengths( Document document, IMEPCurveGenerator mepCurveGenerator, double headerDiameter, double branchDiameter )
    {
      var header = DefaultFittingSizeCalculator.Instance.Calc90ElbowSize( document, mepCurveGenerator, headerDiameter ) ;
      var branch = ( headerDiameter == branchDiameter ? header : DefaultFittingSizeCalculator.Instance.Calc90ElbowSize( document, mepCurveGenerator, branchDiameter ) ) ;
      return ( header, branch ) ;
    }

    public double CalculateReducerLength( Document document, IMEPCurveGenerator mepCurveGenerator, double diameter1, double diameter2 )
    {
      return DefaultFittingSizeCalculator.Instance.CalculateReducerLength( document, mepCurveGenerator, diameter1, diameter2 ) ;
    }
  }
}