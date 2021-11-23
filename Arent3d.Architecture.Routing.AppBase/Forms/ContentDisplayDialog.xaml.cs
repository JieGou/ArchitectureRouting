using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class ContentDisplayDialog : Window
  {
    private List<PickUpModel> _pickUpModels ;

    public ContentDisplayDialog( PickUpViewModel pickUpViewModel )
    {
      InitializeComponent() ;
      this.DataContext = pickUpViewModel ;
      _pickUpModels = pickUpViewModel.PickUpModels ;
    }

    private void DataGrid_LoadingRow( object sender, DataGridRowEventArgs e )
    {
      e.Row.Header = ( e.Row.GetIndex() + 1 ).ToString() ;
    }

    private void Button_Update( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void Button_DisplaySwitching( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void Button_ExportFile( object sender, RoutedEventArgs e )
    {
      string fileName = @"F:/ドーコンOFF_" + DateTime.Now.ToString( "MMddyyyyHHmmss" ) + ".dat" ;
      try {
        using FileStream fs = new FileStream( fileName, FileMode.Create, FileAccess.Write ) ;
        StreamWriter sw = new StreamWriter( fs ) ;
        foreach ( var pickUpModel in _pickUpModels ) {
          string line = "\"" + pickUpModel.ProductName + "\",\"" + pickUpModel.ModelNumber + "\"" ;
          sw.WriteLine( line ) ;
        }

        sw.Close() ;
        fs.Close() ;
        MessageBox.Show( "Export data successfully. File path is " + fileName, "Result Message" ) ;
      }
      catch ( Exception ex ) {
        MessageBox.Show( "Export data failed because " + ex, "Error Message" ) ;
      }
    }

    private void Button_Delete( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void Button_Save( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void Button_Cancel( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
      Close() ;
    }
  }
}