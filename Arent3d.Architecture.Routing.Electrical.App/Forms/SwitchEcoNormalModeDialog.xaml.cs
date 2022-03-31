using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Forms
{
  public enum EcoNormalMode
  {
    EcoMode,
    NormalMode
  }

  public partial class SwitchEcoNormalModeDialog
  {
    private const string RequiredEcoNormalMode = "Please select Eco or Normal mode from combo box" ;
    public SwitchEcoNormalModeDialog( UIApplication uiApplication ) : base( uiApplication )
    {
      InitializeComponent() ;
    }

    public IReadOnlyDictionary<EcoNormalMode, string> EcoNormalModes { get ; } = new Dictionary<EcoNormalMode, string> { [ EcoNormalMode.EcoMode ] = "エコモード", [ EcoNormalMode.NormalMode ] = "ノーマル", } ;

    public bool? ApplyForProject ;

    private void Button_BtnApplyForProject_Click( object sender, RoutedEventArgs e )
    {
      if ( EcoNormalModeComboBox.SelectedIndex == -1 ) {
        MessageBox.Show( RequiredEcoNormalMode ) ;
        return;
      }
      ApplyForProject = true ;
      DialogResult = true ;
    }

    private void Button_BtnApplyForARange_Click( object sender, RoutedEventArgs e )
    {
      if ( EcoNormalModeComboBox.SelectedIndex == -1 ) {
        MessageBox.Show( RequiredEcoNormalMode ) ;
        return;
      }
      ApplyForProject = false ;
      DialogResult = true ;
    }

    private void Button_Cancel_Click( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
    }

    public EcoNormalMode EcoNormalMode ;

    private void EcoNormalModeComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      OnValueChanged( EventArgs.Empty ) ;
      var oldValue = e.RemovedItems.OfType<KeyValuePair<EcoNormalMode, string>>().FirstOrDefault() ;
      var newValue = e.AddedItems.OfType<KeyValuePair<EcoNormalMode, string>>().FirstOrDefault() ;
      if ( oldValue.Key == newValue.Key ) return ;
      EcoNormalMode = newValue.Key ;
    }

    private void OnValueChanged( EventArgs e )
    {
      ValueChanged?.Invoke( this, e ) ;
    }

    public event EventHandler? ValueChanged ;
  }
}