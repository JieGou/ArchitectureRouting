using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows.Controls ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class AddWiringInformationDialog 
  {
    private readonly Document _document ;
    public AddWiringInformationDialog(AddWiringInformationViewModel viewModel, Document document)
    { 
      InitializeComponent() ;
      DataContext = viewModel ;
      _document = document ;
    }

    // private void Selector_OnSelectionChanged( object sender, SelectionChangedEventArgs e )
    // {
    //   if ( sender is not ComboBox comboBox ) return ;
    //   var wireType = comboBox.SelectedValue.ToString() ; 
    //   
    //   var csvStorable = _document.GetCsvStorable() ;
    //   var wiresAndCablesModelData = csvStorable.WiresAndCablesModelData ; 
    //    
    //   var wireSizesOfWireType = wiresAndCablesModelData.Where( w => w.WireType == wireType ).Select( w => w.DiameterOrNominal ).Distinct().ToList() ;
    //   ((AddWiringInformationViewModel)DataContext).SelectedDetailTableModel!.WireSizes = new ObservableCollection<string>(wireSizesOfWireType) ; 
    //   
    // }
  }
  
  public abstract class DesignAddWiringInformationViewModel : AddWiringInformationViewModel
  {
    protected DesignAddWiringInformationViewModel( Document document, ObservableCollection<DetailTableModel> detailTableModels, ObservableCollection<string> conduitTypes, ObservableCollection<string> constructionItems, ObservableCollection<string> levels, ObservableCollection<string> wireTypes, ObservableCollection<string> earthTypes, ObservableCollection<string> numbers, ObservableCollection<string> constructionClassificationTypes, ObservableCollection<string> signalTypes ) : base( document, detailTableModels, conduitTypes, constructionItems, levels, wireTypes, earthTypes, numbers, constructionClassificationTypes, signalTypes )
    {
    }
  }
}