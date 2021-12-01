namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class ConduitInformationModel
  {
    public bool ConduitStatus { get ; set ; }
    public string Floor { get ; set ; }
    public string ConduitDetail { get ; set ; }
    public string WireType { get ; set ; }
    public string ElectricCircuit { get ; set ; }
    public string ElectricalWireType { get ; set ; }
    public string ElectricError { get ; set ; }
    public string Done1 { get ; set ; }
    public string Done2 { get ; set ; }
    public string Done3 { get ; set ; }
    public string Plumbing1 { get ; set ; }
    public string Plumbing2 { get ; set ; }
    public string Plumbing3 { get ; set ; }
    public string Seven { get ; set ; }
    public string GardenWork { get ; set ; }
    public string Classification { get ; set ; }
    public string ConstructionItem { get ; set ; }
    public string PlumbingItem { get ; set ; }
    public string Remark { get ; set ; }

    public ConduitInformationModel( 
      bool conduitStatus, 
      string floor, 
      string conduitDetail,
      string wireType, 
      string electricCircuit, 
      string electricalWireType, 
      string electricError,
      string done1,
      string done2,
      string done3,
      string plumbing1,
      string plumbing2,
      string plumbing3,
      string seven,
      string gardenWork,
      string classification,
      string constructionItem,
      string plumbingItem,
      string remark)
    {
      ConduitStatus = conduitStatus ;
      Floor = floor ;
      ConduitDetail = conduitDetail ;
      WireType = wireType ;
      ElectricCircuit = electricCircuit ;
      ElectricalWireType = electricalWireType ;
      ElectricError = electricError ;
      Done1 = done1 ;
      Done2 = done2 ;
      Done3 = done3 ;
      Plumbing1 = plumbing1 ;
      Plumbing2 = plumbing2 ;
      Plumbing3 = plumbing3 ;
      Seven = seven ;
      GardenWork = gardenWork ;
      Classification = classification ;
      ConstructionItem = constructionItem ;
      PlumbingItem = plumbingItem ;
      Remark = remark ;
    }
  }
}