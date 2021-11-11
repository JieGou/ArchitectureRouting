using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CeeDModelDialog : Window
  {
    private CeedViewModel? _allCeeDModels ;
    private string _ceeDModelNumberSearch ;
    private Document _document ;

    public CeeDModelDialog( Document document )
    {
      InitializeComponent() ;
      _document = document ;
      _allCeeDModels = null ;
      _ceeDModelNumberSearch = string.Empty ;
    }

    private void Button_Click( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void Button_Reset( object sender, RoutedEventArgs e )
    {
      this.DataContext = _allCeeDModels ;
      CmbCeeDModelNumbers.SelectedIndex = -1 ;
      CmbCeeDModelNumbers.Text = "" ;
    }

    private void CmbCeeDModelNumbers_TextChanged( object sender, TextChangedEventArgs e )
    {
      _ceeDModelNumberSearch = ! string.IsNullOrEmpty( CmbCeeDModelNumbers.Text ) ? CmbCeeDModelNumbers.Text : string.Empty ;
    }

    private void Button_Search( object sender, RoutedEventArgs e )
    {
      if ( _allCeeDModels != null ) {
        if ( ! string.IsNullOrEmpty( _ceeDModelNumberSearch ) ) {
          var ceeDModels = _allCeeDModels.CeedModels.Where( c => c.CeeDModelNumber.Contains( _ceeDModelNumberSearch ) ).ToList() ;
          CeedViewModel ceeDModelsSearch = new CeedViewModel( _allCeeDModels.CeedStorable, ceeDModels, _ceeDModelNumberSearch ) ;
          this.DataContext = ceeDModelsSearch ;
        }
        else {
          this.DataContext = _allCeeDModels ;
        }
      }
    }

    private void Button_LoadData( object sender, RoutedEventArgs e )
    {
      CeedStorable ceeDStorable = _document.GetCeeDStorable() ;
      {
        var viewModel = new ViewModel.CeedViewModel( ceeDStorable ) ;
        this.DataContext = viewModel ;
        _allCeeDModels = viewModel ;
        _ceeDModelNumberSearch = viewModel.CeeDNumberSearch ;
        CmbCeeDModelNumbers.ItemsSource = viewModel.CeeDModelNumbers ;
        try {
          using Transaction t = new Transaction( _document, "Save data" ) ;
          t.Start() ;
          ceeDStorable.Save() ;
          t.Commit() ;
        }
        catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        }
      }
    }
  }
}