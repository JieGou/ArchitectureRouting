namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class WiresAndCablesModel
  {
    public string WireType { get ; set ; }
    public string DiameterOrNominal { get ; set ; }
    public string DOrANumberOfHeartsOrLogarithm { get ; set ; }
    public string COrPCrossSectionalArea { get ; set ; }
    public string Name { get ; set ; }
    public string Classification { get ; set ; }
    public string FinishedOuterDiameter { get ; set ; }
    public string NumberOfConnections { get ; set ; }
    
    public WiresAndCablesModel( string wireType, string diameterOrNominal, string dOrANumberOfHeartsOrLogarithm, string cOrPCrossSectionalArea, string name, string classification, string finishedOuterDiameter, string numberOfConnections )
    {
      WireType = wireType ;
      DiameterOrNominal = diameterOrNominal ;
      DOrANumberOfHeartsOrLogarithm = dOrANumberOfHeartsOrLogarithm ;
      COrPCrossSectionalArea = cOrPCrossSectionalArea ;
      Name = name ;
      Classification = classification ;
      FinishedOuterDiameter = finishedOuterDiameter ;
      NumberOfConnections = numberOfConnections ;
    }
  }
}