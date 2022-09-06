using System.Windows ;
using System.Windows.Interop ;
using Arent3d.Revit ;

namespace Arent3d.Architecture.Routing.Electrical.App.Forms
{
  public partial class RackSizeDialog : Window
  {
    private readonly double[] _rackSizes = { 200, 300, 400, 500, 600} ;
    public RackSizeDialog()
    {
      InitializeComponent() ;
      cmbSizes.ItemsSource = _rackSizes ;
      cmbSizes.SelectedIndex = 0 ;
      var helper = new WindowInteropHelper( this ) { Owner = Autodesk.Windows.ComponentManager.ApplicationWindow } ;
    }

    public void Deconstruct( out double width, out bool isRoundBendingAnnotation )
    {
      width = double.Parse(cmbSizes.Text);
      isRoundBendingAnnotation = rdRound.IsChecked?? false ;
    }

    private void OnOkClicked( object sender, RoutedEventArgs e )
    {
      if( double.TryParse( cmbSizes.Text, out var size ) && size >= 10)
        this.DialogResult = true ;
      else
        MessageBox.Show("有効なサイズを入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);
    }
  }
}