using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
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
    private const string EcoModeKey = "Dialog.Electrical.SwitchEcoNormalModeDialog.EcoNormalMode.EcoMode" ;
    private const string EcoModeDefaultString = "Eco Mode" ;
    private const string NormalModeKey = "Dialog.Electrical.SwitchEcoNormalModeDialog.EcoNormalMode.NormalMode" ;
    private const string NormalModeDefaultString = "Normal Mode" ;
    public static readonly DependencyProperty EcoNormalModeComboBoxIndexProperty = DependencyProperty.Register( "EcoNormalModeComboBoxIndex", typeof( int ), typeof( SwitchEcoNormalModeDialog ), new PropertyMetadata( 1 ) ) ;

    public SwitchEcoNormalModeDialog( UIApplication uiApplication, bool? isProjectInEcoMode ) : base( uiApplication )
    {
      InitializeComponent() ;
      if ( isProjectInEcoMode != null )
        EcoNormalModeComboBox.SelectedIndex = isProjectInEcoMode == true ? 0 : 1 ;
    }

    public IReadOnlyDictionary<EcoNormalMode, string> EcoNormalModes { get ; } = new Dictionary<EcoNormalMode, string>
    {
      [ EcoNormalMode.EcoMode ] = EcoModeKey.GetAppStringByKeyOrDefault(EcoModeDefaultString), 
      [ EcoNormalMode.NormalMode ] = NormalModeKey.GetAppStringByKeyOrDefault(NormalModeDefaultString),
    } ;

    public bool? ApplyForProject ;

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

    public EcoNormalMode? SelectedMode
    {
      get => GetEcoNormalModeOnIndex( EcoNormalModes.Keys, (int) GetValue( EcoNormalModeComboBoxIndexProperty ) ) ;
      private set => SetValue( EcoNormalModeComboBoxIndexProperty, GetEcoNormalModeIndex( EcoNormalModes.Keys, value ) ) ;
    }

    private static EcoNormalMode? GetEcoNormalModeOnIndex( IEnumerable<EcoNormalMode> ecoNormalModes, int index )
    {
      if ( index < 0 ) return null ;
      return ecoNormalModes.ElementAtOrDefault( index ) ;
    }

    private static int GetEcoNormalModeIndex( IEnumerable<EcoNormalMode> ecoNormalModes, EcoNormalMode? ecoNormalMode )
    {
      return ( ecoNormalMode is { } type ? ecoNormalModes.IndexOf( type ) : -1 ) ;
    }

    private void EcoNormalModeComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      OnValueChanged( EventArgs.Empty ) ;
      var oldValue = e.RemovedItems.OfType<KeyValuePair<EcoNormalMode, string>>().FirstOrDefault() ;
      var newValue = e.AddedItems.OfType<KeyValuePair<EcoNormalMode, string>>().FirstOrDefault() ;
      if ( oldValue.Key == newValue.Key ) return ;
      SelectedMode = newValue.Key ;
    }

    private void OnValueChanged( EventArgs e )
    {
      ValueChanged?.Invoke( this, e ) ;
    }
    public event EventHandler? ValueChanged ;
  }
}