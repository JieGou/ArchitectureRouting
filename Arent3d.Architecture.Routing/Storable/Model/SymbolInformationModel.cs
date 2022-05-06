using System.Collections.ObjectModel ;
using Arent3d.Architecture.Routing.Storable.Model ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class SymbolInformationModel
  {
    public string Id { get ; set ; }
    public SymbolModel Symbol { get ; set ; }
    public CeedDetailInformationModel CeedDetailInformation { get ; set ; }
 
    public SymbolInformationModel( string? id, SymbolModel? symbol, CeedDetailInformationModel? ceedDetailInformation )
    {
      Id = id ?? "-1" ;
      Symbol = symbol ?? new SymbolModel( null, null, 10, 100, 5, null, 10 ) ;
      CeedDetailInformation = ceedDetailInformation ?? new CeedDetailInformationModel( new ObservableCollection<QueryData>(), "" ) ;
    }
  }
}