using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Forms ;
using System.Windows.Media ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Revit.I18n ;
using MessageBox = System.Windows.Forms.MessageBox ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class ImportDwgMappingDialog
  {
    private static string ImportDwgMappingNotUnique =
      "Please input unique Floor Name for all floor." ;
    
    public ImportDwgMappingDialog(ImportDwgMappingViewModel viewModel)
    {
      InitializeComponent() ;
      DataContext = viewModel ;
    }

    private void BtnSave_OnClick( object sender, RoutedEventArgs e )
    {
      if ( !IsValidImportDwgMappingModel() ) {
        MessageBox.Show( ImportDwgMappingNotUnique, "Warning", MessageBoxButtons.OK ) ;
        return;
      }
      DialogResult = true ;
    }

    private bool IsValidImportDwgMappingModel()
    {
      var importDwgMappingViewModel = this.DataContext as ImportDwgMappingViewModel ;
      if ( importDwgMappingViewModel == null ) return false ;
      return importDwgMappingViewModel.ImportDwgMappingModels.All( x => !string.IsNullOrEmpty( x.FloorName ) ) &&
             importDwgMappingViewModel.ImportDwgMappingModels.GroupBy( x => x.FloorName ).All( x => x.Count() == 1 ) ;
    }

    private void BtnCancel_OnClick( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
      Close() ;
    }
    
    private void DeleteImportDwgMappingItem(object sender, RoutedEventArgs e)
    {
      for ( var visual = sender as Visual ; visual != null ; visual = VisualTreeHelper.GetParent( visual ) as Visual ) {
        if ( visual is not DataGridRow dataGridRow ) continue ;
        if(dataGridRow.Item is not ImportDwgMappingModel item) return;
        if(DataContext is not ImportDwgMappingViewModel importDwgMappingViewModel) return;;
        var importDwgMappingModels = importDwgMappingViewModel.ImportDwgMappingModels.Where( x => !x.Id.Equals( item.Id ) ).ToList() ;
        var newImportDwgMappingViewModel = new ImportDwgMappingViewModel( importDwgMappingModels, importDwgMappingViewModel.FileItems ) ;
        DataContext = newImportDwgMappingViewModel ;
      }
    }

    private void BtnAdd_OnClick( object sender, RoutedEventArgs e )
    {
      const int floorHeightDistance = 3000 ;
      if(DataContext is not ImportDwgMappingViewModel importDwgMappingViewModel) return;
      var importDwgMappingModels = importDwgMappingViewModel.ImportDwgMappingModels.ToList();
      var currentMaxHeight = importDwgMappingModels.Max( x => x.FloorHeight ) ;
      importDwgMappingModels.Add( new ImportDwgMappingModel( string.Empty, string.Empty, currentMaxHeight + floorHeightDistance  ) );
      var newImportDwgMappingViewModel = new ImportDwgMappingViewModel( importDwgMappingModels, importDwgMappingViewModel.FileItems ) ;
      DataContext = newImportDwgMappingViewModel ;
    }
  }
}