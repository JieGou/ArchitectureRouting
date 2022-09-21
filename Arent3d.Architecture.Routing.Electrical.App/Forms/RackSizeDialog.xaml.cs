using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Interop ;

namespace Arent3d.Architecture.Routing.Electrical.App.Forms
{
  public partial class RackSizeDialog : Window
  {
    public string Material { get ; set ; } = "" ;
    public bool IsAutoSizing { get ; set ; }
    public int NumberOfRack => int.Parse( cmbNumberOfRack.Text ) ;
    public double WidthInMillimeter => double.Parse( cmbSizes.Text ) ;
    
    public RackSizeDialog()
    {
      InitializeComponent() ;
      
      cmbSizes.ItemsSource = Enumerable.Range(1, 10).Select(x => x * 100.0).ToArray() ;
      cmbSizes.SelectedIndex = 1 ;
      
      cmbNumberOfRack.ItemsSource = Enumerable.Range(1, 30).ToArray() ;
      cmbNumberOfRack.SelectedIndex = 0 ;

      IsAutoSizing = true ;
      chkAutoSize.IsChecked = true ;
      cmbSizes.IsEnabled = false ;
      cmbNumberOfRack.IsEnabled = false ;

      var helper = new WindowInteropHelper( this ) { Owner = Autodesk.Windows.ComponentManager.ApplicationWindow } ;
    }

    private void OnOkClicked( object sender, RoutedEventArgs e )
    {
      if( double.TryParse( cmbSizes.Text, out var size ) && size >= 10)
        this.DialogResult = true ;
      else
        MessageBox.Show("有効なサイズを入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);
    }

    private void OnChangeMaterial( object sender, RoutedEventArgs e )
    {
      Material = (string)((sender as RadioButton)?.Content?? "");
    }

    private void OnClickAutoCalculate( object sender, RoutedEventArgs e )
    {
      IsAutoSizing = chkAutoSize.IsChecked?? false ;
      cmbSizes.IsEnabled = ! IsAutoSizing ;
      cmbNumberOfRack.IsEnabled = ! IsAutoSizing ;
    }
  }
}