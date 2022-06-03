using System.Collections.Generic ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class ElectricalSymbolAggregationDialog 
  {
    public ElectricalSymbolAggregationDialog(ElectricalSymbolAggregationViewModel viewModel)
    {
      InitializeComponent() ;
      DataContext = viewModel ;
    }
  }
  
  public abstract class DesignElectricalSymbolAggregationViewModel : ElectricalSymbolAggregationViewModel
  {
    protected DesignElectricalSymbolAggregationViewModel( List<ElectricalSymbolAggregationModel> electricalSymbolAggregationList ) : base( electricalSymbolAggregationList )
    {
    }
  }
}