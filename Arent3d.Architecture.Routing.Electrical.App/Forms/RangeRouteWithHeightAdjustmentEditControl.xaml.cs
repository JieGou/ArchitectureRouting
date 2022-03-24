using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using ControlLib ;
using LengthConverter = Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters.LengthConverter ;

namespace Arent3d.Architecture.Routing.Electrical.App.Forms
{
  public partial class RangeRouteWithHeightAdjustmentEditControl : UserControl
  {
    public static readonly DependencyProperty UseFromPowerToPassFixedHeightProperty = DependencyProperty.Register( "UseFromPowerToPassFixedHeight", typeof( bool? ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( (bool?) false ) ) ;
    public static readonly DependencyProperty FromPowerToPassFixedHeightProperty = DependencyProperty.Register( "FromPowerToPassFixedHeight", typeof( double? ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( 0.0, FromPowerToPassFixedHeight_Changed ) ) ;
    public static readonly DependencyProperty FromPowerToPassLocationTypeIndexProperty = DependencyProperty.Register( "FromPowerToPassLocationTypeIndex", typeof( int ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( 0, FromPowerToPassLocationTypeIndex_PropertyChanged ) ) ;
    public static readonly DependencyProperty SystemTypeEditableProperty = DependencyProperty.Register( "SystemTypeEditable", typeof( bool ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty ShaftEditableProperty = DependencyProperty.Register( "ShaftEditable", typeof( bool ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty CurveTypeEditableProperty = DependencyProperty.Register( "CurveTypeEditable", typeof( bool ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty UseSystemTypeProperty = DependencyProperty.Register( "UseSystemType", typeof( bool ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty UseShaftProperty = DependencyProperty.Register( "UseShaft", typeof( bool ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty UseCurveTypeProperty = DependencyProperty.Register( "UseCurveType", typeof( bool ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty DiameterIndexProperty = DependencyProperty.Register( "DiameterIndex", typeof( int ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( -1 ) ) ;
    public static readonly DependencyProperty SystemTypeIndexProperty = DependencyProperty.Register( "SystemTypeIndex", typeof( int ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( -1 ) ) ;
    public static readonly DependencyProperty ShaftIndexProperty = DependencyProperty.Register( "ShaftIndex", typeof( int ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( -1 ) ) ;
    public static readonly DependencyProperty CurveTypeIndexProperty = DependencyProperty.Register( "CurveTypeIndex", typeof( int ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( -1 ) ) ;
    public static readonly DependencyProperty CurveTypeLabelProperty = DependencyProperty.Register( "CurveTypeLabel", typeof( string ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( DefaultCurveTypeLabel ) ) ;
    public static readonly DependencyProperty IsRouteOnPipeSpaceProperty = DependencyProperty.Register( "IsRouteOnPipeSpace", typeof( bool? ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( (bool?) true ) ) ;
    public static readonly DependencyProperty UseFromPassToSensorsFixedHeightProperty = DependencyProperty.Register( "UsePassFixedHeight", typeof( bool? ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( (bool?) false ) ) ;
    public static readonly DependencyProperty FromPassToSensorsFixedHeightProperty = DependencyProperty.Register( "FromPassToSensorsFixedHeight", typeof( double? ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( 0.0, FromPassToSensorsFixedHeight_Changed ) ) ;
    public static readonly DependencyProperty FromPassToSensorsLocationTypeIndexProperty = DependencyProperty.Register( "FromPassToSensorsLocationTypeIndex", typeof( int ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( 0, FromPassToSensorsLocationTypeIndex_PropertyChanged ) ) ;
    public static readonly DependencyProperty AvoidTypeIndexProperty = DependencyProperty.Register( "AvoidTypeIndex", typeof( int ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( 0 ) ) ;
    private static readonly DependencyPropertyKey CanApplyPropertyKey = DependencyProperty.RegisterReadOnly( "CanApply", typeof( bool ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( false ) ) ;
    private static readonly DependencyPropertyKey IsChangedPropertyKey = DependencyProperty.RegisterReadOnly( "IsChanged", typeof( bool ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( false ) ) ;
    public static readonly DependencyProperty AllowIndeterminateProperty = DependencyProperty.Register( "AllowIndeterminate", typeof( bool ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( default( bool ) ) ) ;
    public static readonly DependencyProperty DisplayUnitSystemProperty = DependencyProperty.Register( "DisplayUnitSystem", typeof( DisplayUnit ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( DisplayUnit.IMPERIAL ) ) ;
    private static readonly DependencyPropertyKey FromMinimumHeightAsFloorLevelPropertyKey = DependencyProperty.RegisterReadOnly( "FromMinimumHeightAsFloorLevel", typeof( double ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey FromMaximumHeightAsFloorLevelPropertyKey = DependencyProperty.RegisterReadOnly( "FromMaximumHeightAsFloorLevel", typeof( double ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey FromMinimumHeightAsCeilingLevelPropertyKey = DependencyProperty.RegisterReadOnly( "FromMinimumHeightAsCeilingLevel", typeof( double ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey FromMaximumHeightAsCeilingLevelPropertyKey = DependencyProperty.RegisterReadOnly( "FromMaximumHeightAsCeilingLevel", typeof( double ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey FromDefaultHeightAsFloorLevelPropertyKey = DependencyProperty.RegisterReadOnly( "FromDefaultHeightAsFloorLevel", typeof( double ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey FromDefaultHeightAsCeilingLevelPropertyKey = DependencyProperty.RegisterReadOnly( "FromDefaultHeightAsCeilingLevel", typeof( double ), typeof( RangeRouteWithHeightAdjustmentEditControl ), new PropertyMetadata( 0.0 ) ) ;

    public RangeRouteWithHeightAdjustmentEditControl()
    {
      InitializeComponent() ;
    }

    private const string DefaultCurveTypeLabel = "Type" ;

    public event EventHandler? ValueChanged ;

    private void OnValueChanged( EventArgs e )
    {
      CanApply = CheckCanApply() ;
      IsChanged = CanApply && CheckIsChanged() ;
      ValueChanged?.Invoke( this, e ) ;
    }

    //Diameter Info
    private double VertexTolerance { get ; set ; }
    public ObservableCollection<double> Diameters { get ; } = new() ;
    private double? DiameterOrg { get ; set ; }

    internal double? Diameter
    {
      get => GetDiameterOnIndex( Diameters, (int) GetValue( DiameterIndexProperty ) ) ;
      set => SetValue( DiameterIndexProperty, GetDiameterIndex( Diameters, value, VertexTolerance ) ) ;
    }

    private static double? GetDiameterOnIndex( IReadOnlyList<double> diameters, int index )
    {
      if ( index < 0 || diameters.Count <= index ) return null ;
      return diameters[ index ] ;
    }

    private static int GetDiameterIndex( IReadOnlyList<double> diameters, double? value, double tolerance )
    {
      if ( value is not { } diameter ) {
        if ( 0 < diameters.Count ) return 0 ; // Use minimum value
        return -1 ;
      }

      return diameters.FindIndex( d => LengthEquals( d, diameter, tolerance ) ) ;
    }

    private static bool LengthEquals( double d1, double d2, double tolerance )
    {
      return Math.Abs( d1 - d2 ) < tolerance ;
    }

    private static bool LengthEquals( double? d1, double? d2, double tolerance )
    {
      if ( d1.HasValue != d2.HasValue ) return false ;
      if ( false == d1.HasValue ) return true ;

      return LengthEquals( d1.Value, d2!.Value, tolerance ) ;
    }

    //SystemType Info
    public ObservableCollection<MEPSystemType> SystemTypes { get ; } = new() ;
    private MEPSystemType? SystemTypeOrg { get ; set ; }

    public MEPSystemType? SystemType
    {
      get => GetItemOnIndex( SystemTypes, (int) GetValue( SystemTypeIndexProperty ) ) ;
      set => SetValue( SystemTypeIndexProperty, GetItemIndex( SystemTypes, value ) ) ;
    }

    public bool SystemTypeEditable
    {
      get => (bool) GetValue( SystemTypeEditableProperty ) ;
      set => SetValue( SystemTypeEditableProperty, value ) ;
    }

    private bool UseSystemType
    {
      get => (bool) GetValue( UseSystemTypeProperty ) ;
      set => SetValue( UseSystemTypeProperty, value ) ;
    }

    //Shafts Info
    public ObservableCollection<OpeningProxy> Shafts { get ; } = new() ;
    private Opening? ShaftOrg { get ; set ; }

    internal Opening? Shaft
    {
      get => GetItemOnIndex( Shafts, (int) GetValue( ShaftIndexProperty ) )?.Value ;
      set => SetValue( ShaftIndexProperty, GetShaftIndex( Shafts, value ) ) ;
    }

    public bool ShaftEditable
    {
      get => (bool) GetValue( ShaftEditableProperty ) ;
      set => SetValue( ShaftEditableProperty, value ) ;
    }

    private bool UseShaft
    {
      get => (bool) GetValue( UseShaftProperty ) ;
      set => SetValue( UseShaftProperty, value ) ;
    }

    //CurveType Info
    public ObservableCollection<MEPCurveType> CurveTypes { get ; } = new() ;
    private MEPCurveType? CurveTypeOrg { get ; set ; }

    internal MEPCurveType? CurveType
    {
      get => GetItemOnIndex( CurveTypes, (int) GetValue( CurveTypeIndexProperty ) ) ;
      set
      {
        SetValue( CurveTypeIndexProperty, GetItemIndex( CurveTypes, value ) ) ;
        if ( value is { } curveType ) {
          CurveTypeLabel = UIHelper.GetTypeLabel( curveType.GetType().Name ) ;
        }
        else {
          CurveTypeLabel = DefaultCurveTypeLabel ;
        }

        UpdateDiameterList() ;
      }
    }

    private void UpdateDiameterList()
    {
      var curveType = CurveType ;
      var currentDiameter = Diameter ;

      Diameters.Clear() ;
      if ( curveType?.GetNominalDiameters( VertexTolerance ) is { } diameters ) {
        diameters.ForEach( Diameters.Add ) ;
      }

      if ( currentDiameter is { } d ) {
        SetCurrentValue( DiameterIndexProperty, UIHelper.FindClosestIndex( Diameters, d ) ) ;
      }
      else {
        SetCurrentValue( DiameterIndexProperty, -1 ) ;
      }
    }

    public DisplayUnit DisplayUnitSystem
    {
      get => (DisplayUnit) GetValue( DisplayUnitSystemProperty ) ;
      set => SetValue( DisplayUnitSystemProperty, value ) ;
    }

    private string CurveTypeLabel
    {
      set => SetValue( CurveTypeLabelProperty, value ) ;
    }

    public bool CurveTypeEditable
    {
      get => (bool) GetValue( CurveTypeEditableProperty ) ;
      set => SetValue( CurveTypeEditableProperty, value ) ;
    }

    private bool UseCurveType => (bool) GetValue( UseCurveTypeProperty ) ;

    private static T? GetItemOnIndex<T>( IReadOnlyList<T> values, int index ) where T : class
    {
      if ( index < 0 || values.Count <= index ) return null ;
      return values[ index ] ;
    }

    private static int GetItemIndex<TElement>( IEnumerable<TElement> elements, TElement? value ) where TElement : Element
    {
      var valueId = value.GetValidId() ;
      if ( ElementId.InvalidElementId == valueId ) return -1 ;

      return elements.FindIndex( elm => elm.Id == valueId ) ;
    }

    private static int GetShaftIndex( IEnumerable<OpeningProxy> elements, Opening? value )
    {
      var valueId = value.GetValidId() ;
      return elements.FindIndex( elm => elm.Value?.Id == valueId ) ;
    }

    //Direct Info
    private bool? IsRouteOnPipeSpaceOrg { get ; set ; }

    public bool? IsRouteOnPipeSpace
    {
      get => (bool?) GetValue( IsRouteOnPipeSpaceProperty ) ;
      private set => SetValue( IsRouteOnPipeSpaceProperty, value ) ;
    }

    //HeightSetting
    private bool? UseFromPassToSensorsFixedHeightOrg { get ; set ; }
    private double? FromPassToSensorsFixedHeightOrg { get ; set ; }

    public bool? UseFromPassToSensorsFixedHeight
    {
      get => (bool?) GetValue( UseFromPassToSensorsFixedHeightProperty ) ;
      private set => SetValue( UseFromPassToSensorsFixedHeightProperty, value ) ;
    }

    internal double? FromPassToSensorsFixedHeight
    {
      get => (double?) GetValue( FromPassToSensorsFixedHeightProperty ) ;
      set => SetValue( FromPassToSensorsFixedHeightProperty, value ) ;
    }

    private double FromMinimumHeightAsFloorLevel
    {
      get => (double) GetValue( FromMinimumHeightAsFloorLevelPropertyKey.DependencyProperty ) ;
      set => SetValue( FromMinimumHeightAsFloorLevelPropertyKey, value ) ;
    }

    private double FromMaximumHeightAsFloorLevel
    {
      get => (double) GetValue( FromMaximumHeightAsFloorLevelPropertyKey.DependencyProperty ) ;
      set => SetValue( FromMaximumHeightAsFloorLevelPropertyKey, value ) ;
    }

    private double FromMinimumHeightAsCeilingLevel
    {
      get => (double) GetValue( FromMinimumHeightAsCeilingLevelPropertyKey.DependencyProperty ) ;
      set => SetValue( FromMinimumHeightAsCeilingLevelPropertyKey, value ) ;
    }

    private double FromMaximumHeightAsCeilingLevel
    {
      get => (double) GetValue( FromMaximumHeightAsCeilingLevelPropertyKey.DependencyProperty ) ;
      set => SetValue( FromMaximumHeightAsCeilingLevelPropertyKey, value ) ;
    }

    private double FromDefaultHeightAsFloorLevel
    {
      get => (double) GetValue( FromDefaultHeightAsFloorLevelPropertyKey.DependencyProperty ) ;
      set => SetValue( FromDefaultHeightAsFloorLevelPropertyKey, value ) ;
    }

    private double FromDefaultHeightAsCeilingLevel
    {
      get => (double) GetValue( FromDefaultHeightAsCeilingLevelPropertyKey.DependencyProperty ) ;
      set => SetValue( FromDefaultHeightAsCeilingLevelPropertyKey, value ) ;
    }

    private static void FromPassToSensorsLocationTypeIndex_PropertyChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
    {
      ( d as RangeRouteWithHeightAdjustmentEditControl )?.OnFromPassToSensorsLocationTypeChanged() ;
    }

    private void OnFromPassToSensorsLocationTypeChanged()
    {
      if ( FromPassToSensorsLocationType is not { } locationType ) return ;

      var minimumValue = ( locationType == FixedHeightType.Ceiling ? FromMinimumHeightAsCeilingLevel : FromMinimumHeightAsFloorLevel ) ;
      var maximumValue = ( locationType == FixedHeightType.Ceiling ? FromMaximumHeightAsCeilingLevel : FromMaximumHeightAsFloorLevel ) ;
      SetMinMax( FromPassToSensorsFixedHeightNumericUpDown, minimumValue, maximumValue ) ;
    }

    private void SetMinMax( NumericUpDown numericUpDown, double minimumValue, double maximumValue )
    {
      var lengthConverter = GetLengthConverter( DisplayUnitSystem ) ;
      numericUpDown.MinValue = Math.Round( lengthConverter.ConvertUnit( minimumValue ), 5, MidpointRounding.AwayFromZero ) ;
      numericUpDown.MaxValue = Math.Round( lengthConverter.ConvertUnit( maximumValue ), 5, MidpointRounding.AwayFromZero ) ;
      numericUpDown.Value = Math.Max( numericUpDown.MinValue, Math.Min( numericUpDown.Value, numericUpDown.MaxValue ) ) ;
      numericUpDown.ToolTip = $"{numericUpDown.MinValue} ～ {numericUpDown.MaxValue}" ;
    }

    //AvoidType
    private AvoidType? AvoidTypeOrg { get ; set ; }

    internal AvoidType? AvoidType
    {
      get => GetAvoidTypeOnIndex( AvoidTypes.Keys, (int) GetValue( AvoidTypeIndexProperty ) ) ;
      set => SetValue( AvoidTypeIndexProperty, GetAvoidTypeIndex( AvoidTypes.Keys, value ) ) ;
    }

    private static AvoidType? GetAvoidTypeOnIndex( IEnumerable<AvoidType> avoidTypes, int index )
    {
      if ( index < 0 ) return null ;
      return avoidTypes.ElementAtOrDefault( index ) ;
    }

    private static int GetAvoidTypeIndex( IEnumerable<AvoidType> avoidTypes, AvoidType? avoidType )
    {
      return ( avoidType is { } type ? avoidTypes.IndexOf( type ) : -1 ) ;
    }

    public IReadOnlyDictionary<AvoidType, string> AvoidTypes { get ; } = new Dictionary<AvoidType, string>
    {
      [ Routing.AvoidType.Whichever ] = "Dialog.Forms.RangeRouteWithHeightAdjustmentEditControl.ProcessConstraints.None".GetAppStringByKeyOrDefault( "Whichever" ), [ Routing.AvoidType.NoAvoid ] = "Dialog.Forms.RangeRouteWithHeightAdjustmentEditControl.ProcessConstraints.NoPocket".GetAppStringByKeyOrDefault( "Don't avoid From-To" ), [ Routing.AvoidType.AvoidAbove ] = "Dialog.Forms.RangeRouteWithHeightAdjustmentEditControl.ProcessConstraints.NoDrainPocket".GetAppStringByKeyOrDefault( "Avoid on From-To" ), [ Routing.AvoidType.AvoidBelow ] = "Dialog.Forms.RangeRouteWithHeightAdjustmentEditControl.ProcessConstraints.NoVentPocket".GetAppStringByKeyOrDefault( "Avoid below From-To" ),
    } ;

    //LocationType
    private FixedHeightType? FromPassToSensorsLocationTypeOrg { get ; set ; }

    internal FixedHeightType? FromPassToSensorsLocationType
    {
      get => GetLocationTypeOnIndex( LocationTypes.Keys, (int) GetValue( FromPassToSensorsLocationTypeIndexProperty ) ) ;
      set => SetValue( FromPassToSensorsLocationTypeIndexProperty, GetLocationTypeIndex( LocationTypes.Keys, value ) ) ;
    }

    private static FixedHeightType? GetLocationTypeOnIndex( IEnumerable<FixedHeightType> locationTypes, int index )
    {
      if ( index < 0 ) return null ;
      return locationTypes.ElementAtOrDefault( index ) ;
    }

    private static int GetLocationTypeIndex( IEnumerable<FixedHeightType> locationTypes, FixedHeightType? locationType )
    {
      return ( locationType is { } type ? locationTypes.IndexOf( type ) : -1 ) ;
    }

    public IReadOnlyDictionary<FixedHeightType, string> LocationTypes { get ; } = new Dictionary<FixedHeightType, string> { [ FixedHeightType.Floor ] = "FL", [ FixedHeightType.Ceiling ] = "CL", } ;

    private bool CanApply
    {
      get => (bool) GetValue( CanApplyPropertyKey.DependencyProperty ) ;
      set => SetValue( CanApplyPropertyKey, value ) ;
    }

    private bool IsChanged
    {
      set => SetValue( IsChangedPropertyKey, value ) ;
    }

    private bool AllowIndeterminate => (bool) GetValue( AllowIndeterminateProperty ) ;

    private bool CheckCanApply()
    {
      if ( false == AllowIndeterminate ) {
        if ( UseSystemType && null == SystemType ) return false ;
        if ( UseCurveType && null == CurveType ) return false ;
        if ( null == Diameter ) return false ;
        if ( null == IsRouteOnPipeSpace ) return false ;
        if ( null == UseFromPassToSensorsFixedHeight ) return false ;
        if ( null == UseFromPowerToPassFixedHeight ) return false ;
        if ( null == FromPassToSensorsLocationType ) return false ;
        if ( null == FromPowerToPassLocationType ) return false ;
        if ( null == FromPassToSensorsFixedHeight ) return false ;
        if ( null == FromPowerToPassFixedHeight ) return false ;
      }

      return true ;
    }

    private bool CheckIsChanged()
    {
      if ( UseSystemType && SystemTypeOrg.GetValidId() != SystemType.GetValidId() ) return true ;
      if ( UseCurveType && CurveTypeOrg.GetValidId() != CurveType.GetValidId() ) return true ;
      if ( false == LengthEquals( DiameterOrg, Diameter, VertexTolerance ) ) return true ;
      if ( IsRouteOnPipeSpace != IsRouteOnPipeSpaceOrg ) return true ;
      if ( UseFromPassToSensorsFixedHeight != UseFromPassToSensorsFixedHeightOrg ) return true ;
      if ( UseFromPowerToPassFixedHeight != UseFromPowerToPassFixedHeightOrg ) return true ;
      if ( true == UseFromPassToSensorsFixedHeight ) {
        if ( FromPassToSensorsLocationTypeOrg != FromPassToSensorsLocationType ) return true ;
        if ( false == LengthEquals( FromPassToSensorsFixedHeightOrg, FromPassToSensorsFixedHeight, VertexTolerance ) ) return true ;
      }

      if ( true == UseFromPowerToPassFixedHeight ) {
        if ( FromPowerToPassLocationTypeOrg != FromPowerToPassLocationType ) return true ;
        if ( false == LengthEquals( FromPowerToPassFixedHeightOrg, FromPowerToPassFixedHeight, VertexTolerance ) ) return true ;
      }

      if ( AvoidTypeOrg != AvoidType ) return true ;
      return UseShaft && ShaftOrg.GetValidId() != Shaft.GetValidId() ;
    }

    private void SystemTypeComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      OnValueChanged( EventArgs.Empty ) ;
    }

    private void ShaftComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      OnValueChanged( EventArgs.Empty ) ;
    }

    private void CurveTypeComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      OnValueChanged( EventArgs.Empty ) ;
    }

    private void DiameterComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      OnValueChanged( EventArgs.Empty ) ;
    }

    private void SetAvailableParameterList( RoutePropertyTypeList propertyTypeList )
    {
      Diameters.Clear() ;
      SystemTypes.Clear() ;
      CurveTypes.Clear() ;
      Shafts.Clear() ;

      // System type
      if ( propertyTypeList.SystemTypes is { } systemTypes ) {
        foreach ( var s in systemTypes ) {
          SystemTypes.Add( s ) ;
        }

        UseSystemType = true ;
      }
      else {
        UseSystemType = false ;
      }

      Shafts.Add( new OpeningProxy( null ) ) ;
      if ( propertyTypeList.Shafts is { } shafts ) {
        foreach ( var shaft in shafts ) {
          Shafts.Add( new OpeningProxy( shaft ) ) ;
        }

        UseShaft = true ;
      }
      else {
        UseShaft = false ;
      }

      // Curve type
      foreach ( var c in propertyTypeList.CurveTypes ) {
        CurveTypes.Add( c ) ;
      }

      ( FromMinimumHeightAsFloorLevel, FromMaximumHeightAsFloorLevel ) = propertyTypeList.FromHeightRangeAsFloorLevel ;
      ( FromMinimumHeightAsCeilingLevel, FromMaximumHeightAsCeilingLevel ) = propertyTypeList.FromHeightRangeAsCeilingLevel ;
      FromDefaultHeightAsFloorLevel = propertyTypeList.FromDefaultHeightAsFloorLevel ;
      FromDefaultHeightAsCeilingLevel = propertyTypeList.FromDefaultHeightAsCeilingLevel ;
    }

    public void SetRouteProperties( RoutePropertyTypeList propertyTypeList, RouteProperties properties )
    {
      VertexTolerance = properties.VertexTolerance ;
      SetAvailableParameterList( propertyTypeList ) ;

      SystemTypeOrg = properties.SystemType ;
      CurveTypeOrg = properties.CurveType ;
      DiameterOrg = properties.Diameter ;
      ShaftOrg = properties.Shaft ;

      IsRouteOnPipeSpaceOrg = properties.IsRouteOnPipeSpace ;

      UseFromPassToSensorsFixedHeightOrg = properties.UseFromFixedHeight ;
      if ( null == UseFromPassToSensorsFixedHeightOrg ) {
        FromPassToSensorsLocationTypeOrg = null ;
        FromPassToSensorsFixedHeightOrg = null ;
      }
      else {
        FromPassToSensorsLocationTypeOrg = properties.FromFixedHeight?.Type ?? FixedHeightType.Floor ;
        FromPassToSensorsFixedHeightOrg = properties.FromFixedHeight?.Height ?? GetFromDefaultHeight( FromPassToSensorsLocationTypeOrg.Value ) ;
      }

      UseFromPowerToPassFixedHeightOrg = properties.UseFromFixedHeight ;
      if ( null == UseFromPowerToPassFixedHeightOrg ) {
        FromPowerToPassLocationTypeOrg = null ;
        FromPowerToPassFixedHeightOrg = null ;
      }
      else {
        FromPowerToPassLocationTypeOrg = properties.FromFixedHeight?.Type ?? FixedHeightType.Ceiling ;
        FromPowerToPassFixedHeightOrg = properties.FromFixedHeight?.Height ?? GetFromDefaultHeight( FromPowerToPassLocationTypeOrg.Value ) ;
      }

      AvoidTypeOrg = properties.AvoidType ;
    }

    public void ResetDialog()
    {
      SystemType = SystemTypeOrg ;
      CurveType = CurveTypeOrg ;
      Diameter = DiameterOrg ;
      Shaft = ShaftOrg ;

      IsRouteOnPipeSpace = IsRouteOnPipeSpaceOrg ;

      UseFromPassToSensorsFixedHeight = UseFromPassToSensorsFixedHeightOrg ;
      UseFromPowerToPassFixedHeight = UseFromPowerToPassFixedHeightOrg ;
      FromPassToSensorsLocationType = FromPassToSensorsLocationTypeOrg ;
      FromPowerToPassLocationType = FromPowerToPassLocationTypeOrg ;
      FromPassToSensorsFixedHeight = FromPassToSensorsFixedHeightOrg ;
      FromPowerToPassFixedHeight = FromPowerToPassFixedHeightOrg ;

      OnFromPassToSensorsLocationTypeChanged() ;
      OnFromPowerToPassLocationTypeChanged() ;
      AvoidType = AvoidTypeOrg ;

      CanApply = false ;
    }

    private void Direct_OnChecked( object sender, RoutedEventArgs e )
    {
      OnValueChanged( EventArgs.Empty ) ;
    }

    private void Direct_OnUnchecked( object sender, RoutedEventArgs e )
    {
      OnValueChanged( EventArgs.Empty ) ;
    }

    private void Height_OnChecked( object sender, RoutedEventArgs e )
    {
      OnValueChanged( EventArgs.Empty ) ;
    }

    private void Height_OnUnchecked( object sender, RoutedEventArgs e )
    {
      OnValueChanged( EventArgs.Empty ) ;
    }

    private void AvoidTypeComboBox_OnSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      OnValueChanged( EventArgs.Empty ) ;
    }

    private void LocationTypeComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      OnValueChanged( EventArgs.Empty ) ;

      if ( e.RemovedItems.OfType<KeyValuePair<FixedHeightType, string>?>().FirstOrDefault() is not { } oldValue || e.AddedItems.OfType<KeyValuePair<FixedHeightType, string>?>().FirstOrDefault() is not { } newValue ) return ;
      if ( oldValue.Key == newValue.Key ) return ;

      if ( ReferenceEquals( sender, FromLocationTypeComboBox ) ) {
        FromPassToSensorsFixedHeight = GetFromDefaultHeight( newValue.Key ) ;
      }
    }

    private double? GetFromDefaultHeight( FixedHeightType newValue )
    {
      return ( FixedHeightType.Ceiling == newValue ) ? FromDefaultHeightAsCeilingLevel : FromDefaultHeightAsFloorLevel ;
    }

    private void FromPassToSensorsFixedHeightNumericUpDown_OnValueChanged( object sender, ValueChangedEventArgs e )
    {
      // Manually update FixedHeight because Value binding is not called.
      FromPassToSensorsFixedHeight = GetLengthConverter( DisplayUnitSystem ).ConvertBackUnit( FromPassToSensorsFixedHeightNumericUpDown.Value ) ;

      OnValueChanged( EventArgs.Empty ) ;
    }

    private static void FromPassToSensorsFixedHeight_Changed( DependencyObject d, DependencyPropertyChangedEventArgs e )
    {
      if ( d is not RangeRouteWithHeightAdjustmentEditControl rangeRouteWithHeightAdjustmentEditControl ) return ;

      if ( e.NewValue is not double newValue ) {
        rangeRouteWithHeightAdjustmentEditControl.FromPassToSensorsFixedHeightNumericUpDown.CanHaveNull = true ;
        rangeRouteWithHeightAdjustmentEditControl.FromPassToSensorsFixedHeightNumericUpDown.HasValidValue = false ;
      }
      else {
        rangeRouteWithHeightAdjustmentEditControl.FromPassToSensorsFixedHeightNumericUpDown.CanHaveNull = false ;
        rangeRouteWithHeightAdjustmentEditControl.FromPassToSensorsFixedHeightNumericUpDown.Value = Math.Round( GetLengthConverter( rangeRouteWithHeightAdjustmentEditControl.DisplayUnitSystem ).ConvertUnit( newValue ), 5, MidpointRounding.AwayFromZero ) ;
      }
    }

    private static LengthConverter GetLengthConverter( DisplayUnit displayUnitSystem )
    {
      return displayUnitSystem switch
      {
        DisplayUnit.METRIC => LengthConverter.Millimeters,
        DisplayUnit.IMPERIAL => LengthConverter.Inches,
        _ => LengthConverter.Default,
      } ;
    }

    public class OpeningProxy
    {
      internal OpeningProxy( Opening? opening )
      {
        Value = opening ;
      }

      public Opening? Value { get ; }
    }

    private static void FromPowerToPassLocationTypeIndex_PropertyChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
    {
      ( d as RangeRouteWithHeightAdjustmentEditControl )?.OnFromPowerToPassLocationTypeChanged() ;
    }

    private bool? UseFromPowerToPassFixedHeightOrg { get ; set ; }
    private double? FromPowerToPassFixedHeightOrg { get ; set ; }
    private FixedHeightType? FromPowerToPassLocationTypeOrg { get ; set ; }

    private void OnFromPowerToPassLocationTypeChanged()
    {
      if ( FromPowerToPassLocationType is not { } locationType ) return ;

      var minimumValue = ( locationType == FixedHeightType.Ceiling ? FromMinimumHeightAsCeilingLevel : FromMinimumHeightAsFloorLevel ) ;
      var maximumValue = ( locationType == FixedHeightType.Ceiling ? FromMaximumHeightAsCeilingLevel : FromMaximumHeightAsFloorLevel ) ;
      SetMinMax( FromPowerToPassFixedHeightNumericUpDown, minimumValue, maximumValue ) ;
    }

    public FixedHeightType? FromPowerToPassLocationType
    {
      get => GetLocationTypeOnIndex( LocationTypes.Keys, (int) GetValue( FromPowerToPassLocationTypeIndexProperty ) ) ;
      private set => SetValue( FromPowerToPassLocationTypeIndexProperty, GetLocationTypeIndex( LocationTypes.Keys, value ) ) ;
    }

    public bool? UseFromPowerToPassFixedHeight
    {
      get => (bool?) GetValue( UseFromPowerToPassFixedHeightProperty ) ;
      private set => SetValue( UseFromPowerToPassFixedHeightProperty, value ) ;
    }

    private void FromPowerToPassHeight_OnChecked( object sender, RoutedEventArgs e )
    {
      OnValueChanged( EventArgs.Empty ) ;
    }

    public double? FromPowerToPassFixedHeight
    {
      get => (double?) GetValue( FromPowerToPassFixedHeightProperty ) ;
      private set => SetValue( FromPowerToPassFixedHeightProperty, value ) ;
    }

    private void PowerToPassLocationTypeComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      OnValueChanged( EventArgs.Empty ) ;

      if ( e.RemovedItems.OfType<KeyValuePair<FixedHeightType, string>?>().FirstOrDefault() is not { } oldValue || e.AddedItems.OfType<KeyValuePair<FixedHeightType, string>?>().FirstOrDefault() is not { } newValue ) return ;
      if ( oldValue.Key == newValue.Key ) return ;

      if ( ReferenceEquals( sender, FromPowerToPassLocationTypeComboBox ) ) {
        FromPowerToPassFixedHeight = GetFromDefaultHeight( newValue.Key ) ;
      }
    }

    private void FromPowerToPassFixedHeightNumericUpDown_OnValueChanged( object sender, ValueChangedEventArgs e )
    {
      // Manually update FixedHeight because Value binding is not called.
      FromPowerToPassFixedHeight = GetLengthConverter( DisplayUnitSystem ).ConvertBackUnit( FromPowerToPassFixedHeightNumericUpDown.Value ) ;

      OnValueChanged( EventArgs.Empty ) ;
    }

    private static void FromPowerToPassFixedHeight_Changed( DependencyObject d, DependencyPropertyChangedEventArgs e )
    {
      if ( d is not RangeRouteWithHeightAdjustmentEditControl rangeRouteWithHeightAdjustmentEditControl ) return ;

      if ( e.NewValue is not double newValue ) {
        rangeRouteWithHeightAdjustmentEditControl.FromPowerToPassFixedHeightNumericUpDown.CanHaveNull = true ;
        rangeRouteWithHeightAdjustmentEditControl.FromPowerToPassFixedHeightNumericUpDown.HasValidValue = false ;
      }
      else {
        rangeRouteWithHeightAdjustmentEditControl.FromPowerToPassFixedHeightNumericUpDown.CanHaveNull = false ;
        rangeRouteWithHeightAdjustmentEditControl.FromPowerToPassFixedHeightNumericUpDown.Value = Math.Round( GetLengthConverter( rangeRouteWithHeightAdjustmentEditControl.DisplayUnitSystem ).ConvertUnit( newValue ), 5, MidpointRounding.AwayFromZero ) ;
      }
    }
  }
}