using System.Collections.Generic ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel.Models
{
  public class PickUpOutputConstructionList
  {
    public string ConstructionItemName { get ; }
    public List<PickUpOutputList> OutputCollection { get ; } = new() ;

    public PickUpOutputConstructionList( string constructionItemName )
    {
      ConstructionItemName = constructionItemName ;
    }
  }
}