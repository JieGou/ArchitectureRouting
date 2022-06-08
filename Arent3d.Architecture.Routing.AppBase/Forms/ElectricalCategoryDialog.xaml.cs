using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Input ;
using System.Windows.Media ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using NPOI.POIFS.NIO ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class ElectricalCategoryDialog
  {
    
    public ElectricalCategoryDialog(ElectricalCategoryViewModel viewModel)
    {
      InitializeComponent() ;
      DataContext = viewModel ;
      Style cellStyle = new( typeof( DataGridCell ) ) ;
      cellStyle.Setters.Add( new EventSetter( MouseDoubleClickEvent, new MouseButtonEventHandler( Cell_DoubleClick ) ) ) ;
      DataGridEco.CellStyle = cellStyle ;
      DataGridNormal.CellStyle = cellStyle ;
    }
    
    private void Cell_DoubleClick( object sender, MouseButtonEventArgs e )
    {
      var dataGridCellTarget = (DataGridCell) sender ;
      var parent = VisualTreeHelper.GetParent(dataGridCellTarget);
      while(parent != null && parent.GetType() != typeof(DataGrid))
      {
        parent = VisualTreeHelper.GetParent(parent);
      }

      if(parent == null) return;
      var dataGrid = (DataGrid) parent ;
      var textBlock = dataGridCellTarget.Content as TextBlock ;
      var cellValue = textBlock?.Text ;

      if(string.IsNullOrEmpty( cellValue )) return;
      
      var data  = ( (ElectricalCategoryViewModel) DataContext ).LoadData(dataGrid.Name.Equals( "DataGridEco" ),  cellValue! ) ;
      if ( null == data ) return ;
      DialogResult = true ;
      Close() ;
    }
  }
}