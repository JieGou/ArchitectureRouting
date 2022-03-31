using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
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
    private const string RequiredEcoNormalMode = "Please select Eco or Normal mode from combo box" ;
    public static readonly DependencyProperty EcoNormalModeComboBoxIndexProperty = DependencyProperty.Register( "EcoNormalModeComboBoxIndex", typeof( int ), typeof( SwitchEcoNormalModeDialog ), new PropertyMetadata( 0, EcoNormalModeIndex_PropertyChanged ) ) ;
    public SwitchEcoNormalModeDialog( UIApplication uiApplication, bool? isProjectInEcoMode ) : base( uiApplication )
    {
      InitializeComponent() ;
      if(isProjectInEcoMode != null)
        EcoNormalModeComboBox.SelectedIndex = isProjectInEcoMode == true ? 0 : 1 ;
    }

    public IReadOnlyDictionary<EcoNormalMode, string> EcoNormalModes { get ; } = new Dictionary<EcoNormalMode, string>
    {
      [ EcoNormalMode.EcoMode ] = "エコモード", 
      [ EcoNormalMode.NormalMode ] = "ノーマル",
    } ;

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

    public EcoNormalMode? SelectedMode
    {
      get => GetLocationTypeOnIndex( EcoNormalModes.Keys, (int)GetValue( EcoNormalModeComboBoxIndexProperty ) ) ;
      private set => SetValue( EcoNormalModeComboBoxIndexProperty, GetLocationTypeIndex( EcoNormalModes.Keys, value ) ) ;
    }
    private static EcoNormalMode? GetLocationTypeOnIndex( IEnumerable<EcoNormalMode> ecoNormalModes, int index )
    {
      if ( index < 0 ) return null ;
      return ecoNormalModes.ElementAtOrDefault( index ) ;
    }

    private static int GetLocationTypeIndex( IEnumerable<EcoNormalMode> ecoNormalModes, EcoNormalMode? ecoNormalMode )
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
    private static void EcoNormalModeIndex_PropertyChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
    {
      ( d as SwitchEcoNormalModeDialog )?.OnEcoNormalModeChanged() ;
    }
    public event EventHandler? ValueChanged ;
    private void OnEcoNormalModeChanged()
    {
      // if ( SwitchEcoNormalModeDialog is not { } ecoNormalMode ) return ;
      //
      // var minimumValue = ( ecoNormalMode == FixedHeightType.Ceiling ? FromMinimumHeightAsCeilingLevel : FromMinimumHeightAsFloorLevel ) ;
      // var maximumValue = ( ecoNormalMode == FixedHeightType.Ceiling ? FromMaximumHeightAsCeilingLevel : FromMaximumHeightAsFloorLevel ) ;
      // SetMinMax( FromFixedHeightNumericUpDown, ecoNormalMode, minimumValue, maximumValue ) ;
    }
  }
}