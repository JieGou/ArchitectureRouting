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
    public SwitchEcoNormalModeDialog( UIApplication uiApplication, bool? isEcoMode ) : base( uiApplication )
    {
      InitializeComponent() ;
      EcoNormalModeComboBox.SelectedItem = isEcoMode is null or true ? EcoNormalMode.EcoMode : EcoNormalMode.NormalMode ;
    }
    public IReadOnlyDictionary<EcoNormalMode, string> EcoNormalModes { get ; } = new Dictionary<EcoNormalMode, string>
    {
      [ EcoNormalMode.EcoMode ] = "エコモード",
      [ EcoNormalMode.NormalMode] = "ノーマル",
    } ;

    public bool? ApplyForProject = null ;
    private void Button_BtnApplyForProject_Click( object sender, RoutedEventArgs e )
    {
      ApplyForProject = true ;
      DialogResult = true ;
    }

    private void Button_BtnApplyForARange_Click( object sender, RoutedEventArgs e )
    {
      ApplyForProject = false ;
      DialogResult = true ;
    }

    private void Button_Cancel_Click( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
    }

    private EcoNormalMode EcoNormalMode = EcoNormalMode.NormalMode;
    private void EcoNormalModeComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      OnValueChanged( EventArgs.Empty ) ;
      if ( e.RemovedItems.OfType<KeyValuePair<EcoNormalMode, string>?>().FirstOrDefault() is not { } oldValue || e.AddedItems.OfType<KeyValuePair<EcoNormalMode, string>?>().FirstOrDefault() is not { } newValue ) return ;
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
