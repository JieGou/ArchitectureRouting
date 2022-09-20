using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Interop ;

namespace Arent3d.Architecture.Routing.Electrical.App.Forms
{
  public partial class RackSizeDialog : Window
  {
    public string Material { get ; set ; } = "" ;
    public bool IsAutoSizing { get ; set ; } = false ;
    public RackSizeDialog()
    {
      InitializeComponent() ;
      
      cmbSizes.ItemsSource = Enumerable.Range(1, 10).Select(x => x * 100.0).ToArray() ;
      cmbSizes.SelectedIndex = 1 ;
      
      cmbNumberOfRack.ItemsSource = Enumerable.Range(1, 30).ToArray() ;
      cmbNumberOfRack.SelectedIndex = 0 ;

      var helper = new WindowInteropHelper( this ) { Owner = Autodesk.Windows.ComponentManager.ApplicationWindow } ;
    }

    public double SelectedWidthInMillimeter( )
    {
      return double.Parse(cmbSizes.Text);
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
      cmbSizes.Text = "" ;
      cmbNumberOfRack.SelectedIndex = -1 ;
      IsAutoSizing = true ;
    }
  }
}