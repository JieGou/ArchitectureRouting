using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using ControlLib ;
using LengthConverter = Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters.LengthConverter ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class RangeRouteWithPassEditControl : UserControl
  {
    public static readonly DependencyProperty SystemTypeEditableProperty = DependencyProperty.Register( "SystemTypeEditable", typeof( bool ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty ShaftEditableProperty = DependencyProperty.Register( "ShaftEditable", typeof( bool ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty CurveTypeEditableProperty = DependencyProperty.Register( "CurveTypeEditable", typeof( bool ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty UseSystemTypeProperty = DependencyProperty.Register( "UseSystemType", typeof( bool ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty UseShaftProperty = DependencyProperty.Register( "UseShaft", typeof( bool ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty UseCurveTypeProperty = DependencyProperty.Register( "UseCurveType", typeof( bool ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty DiameterIndexProperty = DependencyProperty.Register( "DiameterIndex", typeof( int ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( -1 ) ) ;
    public static readonly DependencyProperty SystemTypeIndexProperty = DependencyProperty.Register( "SystemTypeIndex", typeof( int ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( -1 ) ) ;
    public static readonly DependencyProperty ShaftIndexProperty = DependencyProperty.Register( "ShaftIndex", typeof( int ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( -1 ) ) ;
    public static readonly DependencyProperty CurveTypeIndexProperty = DependencyProperty.Register( "CurveTypeIndex", typeof( int ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( -1 ) ) ;
    public static readonly DependencyProperty IsRouteOnPipeSpaceProperty = DependencyProperty.Register( "IsRouteOnPipeSpace", typeof( bool? ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( (bool?)true ) ) ;
    public static readonly DependencyProperty UseFromFixedHeightProperty = DependencyProperty.Register( "UseFromFixedHeight", typeof( bool? ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( (bool?)false ) ) ;
    public static readonly DependencyProperty FromFixedHeightProperty = DependencyProperty.Register( "FromFixedHeight", typeof( double? ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( 0.0, FromFixedHeight_Changed ) ) ;
    public static readonly DependencyProperty FromLocationTypeIndexProperty = DependencyProperty.Register( "FromLocationTypeIndex", typeof( int ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( 0, FromLocationTypeIndex_PropertyChanged ) ) ;
    public static readonly DependencyProperty UseToFixedHeightProperty = DependencyProperty.Register( "UseToFixedHeight", typeof( bool? ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( (bool?)false ) ) ;
    // public static readonly DependencyProperty ToFixedHeightProperty = DependencyProperty.Register( "ToFixedHeight", typeof( double? ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( 0.0, ToFixedHeight_Changed ) ) ;
    public static readonly DependencyProperty AvoidTypeIndexProperty = DependencyProperty.Register( "AvoidTypeIndex", typeof( int ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( 0 ) ) ;
    private static readonly DependencyPropertyKey CanApplyPropertyKey = DependencyProperty.RegisterReadOnly( "CanApply", typeof( bool ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( false ) ) ;
    private static readonly DependencyPropertyKey IsChangedPropertyKey = DependencyProperty.RegisterReadOnly( "IsChanged", typeof( bool ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( false ) ) ;
    private static readonly DependencyPropertyKey IsDifferentLevelPropertyKey = DependencyProperty.RegisterReadOnly( "IsDifferentLevel", typeof( bool ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( false ) ) ;
    public static readonly DependencyProperty AllowIndeterminateProperty = DependencyProperty.Register( "AllowIndeterminate", typeof( bool ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( default( bool ) ) ) ;
    public static readonly DependencyProperty DisplayUnitSystemProperty = DependencyProperty.Register( "DisplayUnitSystem", typeof( DisplayUnit ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( DisplayUnit.IMPERIAL ) ) ;
    private static readonly DependencyPropertyKey FromMinimumHeightAsFloorLevelPropertyKey = DependencyProperty.RegisterReadOnly( "FromMinimumHeightAsFloorLevel", typeof( double ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey FromMaximumHeightAsFloorLevelPropertyKey = DependencyProperty.RegisterReadOnly( "FromMaximumHeightAsFloorLevel", typeof( double ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey FromMinimumHeightAsCeilingLevelPropertyKey = DependencyProperty.RegisterReadOnly( "FromMinimumHeightAsCeilingLevel", typeof( double ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey FromMaximumHeightAsCeilingLevelPropertyKey = DependencyProperty.RegisterReadOnly( "FromMaximumHeightAsCeilingLevel", typeof( double ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey FromDefaultHeightAsFloorLevelPropertyKey = DependencyProperty.RegisterReadOnly( "FromDefaultHeightAsFloorLevel", typeof( double ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey FromDefaultHeightAsCeilingLevelPropertyKey = DependencyProperty.RegisterReadOnly( "FromDefaultHeightAsCeilingLevel", typeof( double ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey ToMinimumHeightAsFloorLevelPropertyKey = DependencyProperty.RegisterReadOnly( "ToMinimumHeightAsFloorLevel", typeof( double ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey ToMaximumHeightAsFloorLevelPropertyKey = DependencyProperty.RegisterReadOnly( "ToMaximumHeightAsFloorLevel", typeof( double ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey ToMinimumHeightAsCeilingLevelPropertyKey = DependencyProperty.RegisterReadOnly( "ToMinimumHeightAsCeilingLevel", typeof( double ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey ToMaximumHeightAsCeilingLevelPropertyKey = DependencyProperty.RegisterReadOnly( "ToMaximumHeightAsCeilingLevel", typeof( double ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey ToDefaultHeightAsFloorLevelPropertyKey = DependencyProperty.RegisterReadOnly( "ToDefaultHeightAsFloorLevel", typeof( double ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey ToDefaultHeightAsCeilingLevelPropertyKey = DependencyProperty.RegisterReadOnly( "ToDefaultHeightAsCeilingLevel", typeof( double ), typeof( RangeRouteWithPassEditControl ), new PropertyMetadata( 0.0 ) ) ;
    public RangeRouteWithPassEditControl()
    {
      InitializeComponent() ;
      
    }
    
    //ToHeightSetting
    private bool? UseToFixedHeightOrg { get ; set ; }
    private double? ToFixedHeightOrg { get ; set ; }

    public bool? UseToFixedHeight
    {
      get => (bool?)GetValue( UseToFixedHeightProperty ) ;
      private set => SetValue( UseToFixedHeightProperty, value ) ;
    }
    private double FromMinimumHeightAsFloorLevel
    {
      get => (double)GetValue( FromMinimumHeightAsFloorLevelPropertyKey.DependencyProperty ) ;
      set => SetValue( FromMinimumHeightAsFloorLevelPropertyKey, value ) ;
    }
    private double FromMaximumHeightAsFloorLevel
    {
      get => (double)GetValue( FromMaximumHeightAsFloorLevelPropertyKey.DependencyProperty ) ;
      set => SetValue( FromMaximumHeightAsFloorLevelPropertyKey, value ) ;
    }
    private double FromMinimumHeightAsCeilingLevel
    {
      get => (double)GetValue( FromMinimumHeightAsCeilingLevelPropertyKey.DependencyProperty ) ;
      set => SetValue( FromMinimumHeightAsCeilingLevelPropertyKey, value ) ;
    }
    public double FromMaximumHeightAsCeilingLevel
    {
      get => (double)GetValue( FromMaximumHeightAsCeilingLevelPropertyKey.DependencyProperty ) ;
      set => SetValue( FromMaximumHeightAsCeilingLevelPropertyKey, value ) ;
    }
    private double FromDefaultHeightAsFloorLevel
    {
      get => (double)GetValue( FromDefaultHeightAsFloorLevelPropertyKey.DependencyProperty ) ;
      set => SetValue( FromDefaultHeightAsFloorLevelPropertyKey, value ) ;
    }
    private double FromDefaultHeightAsCeilingLevel
    {
      get => (double)GetValue( FromDefaultHeightAsCeilingLevelPropertyKey.DependencyProperty ) ;
      set => SetValue( FromDefaultHeightAsCeilingLevelPropertyKey, value ) ;
    }
    private double ToMinimumHeightAsFloorLevel
    {
      get => (double)GetValue( ToMinimumHeightAsFloorLevelPropertyKey.DependencyProperty ) ;
      set => SetValue( ToMinimumHeightAsFloorLevelPropertyKey, value ) ;
    }
    private double ToMaximumHeightAsFloorLevel
    {
      get => (double)GetValue( ToMaximumHeightAsFloorLevelPropertyKey.DependencyProperty ) ;
      set => SetValue( ToMaximumHeightAsFloorLevelPropertyKey, value ) ;
    }
    private double ToMinimumHeightAsCeilingLevel
    {
      get => (double)GetValue( ToMinimumHeightAsCeilingLevelPropertyKey.DependencyProperty ) ;
      set => SetValue( ToMinimumHeightAsCeilingLevelPropertyKey, value ) ;
    }
    private double ToMaximumHeightAsCeilingLevel
    {
      get => (double)GetValue( ToMaximumHeightAsCeilingLevelPropertyKey.DependencyProperty ) ;
      set => SetValue( ToMaximumHeightAsCeilingLevelPropertyKey, value ) ;
    }
    private double ToDefaultHeightAsFloorLevel
    {
      get => (double)GetValue( ToDefaultHeightAsFloorLevelPropertyKey.DependencyProperty ) ;
      set => SetValue( ToDefaultHeightAsFloorLevelPropertyKey, value ) ;
    }
    private double ToDefaultHeightAsCeilingLevel
    {
      get => (double)GetValue( ToDefaultHeightAsCeilingLevelPropertyKey.DependencyProperty ) ;
      set => SetValue( ToDefaultHeightAsCeilingLevelPropertyKey, value ) ;
    }

    public bool CanApply
    {
      get => (bool)GetValue( CanApplyPropertyKey.DependencyProperty ) ;
      private set => SetValue( CanApplyPropertyKey, value ) ;
    }
    public event EventHandler? ValueChanged ;
    public bool IsChanged
    {
      get => (bool)GetValue( IsChangedPropertyKey.DependencyProperty ) ;
      private set => SetValue( IsChangedPropertyKey, value ) ;
    }
    private void OnValueChanged( EventArgs e )
    {
      CanApply = CheckCanApply() ;
      IsChanged = CanApply && CheckIsChanged() ;
      ValueChanged?.Invoke( this, e ) ;
    }
    private bool CheckIsChanged()
    {
      //   if ( UseSystemType && SystemTypeOrg.GetValidId() != SystemType.GetValidId() ) return true ;
      //   if ( UseCurveType && CurveTypeOrg.GetValidId() != CurveType.GetValidId() ) return true ;
      //   if ( false == LengthEquals( DiameterOrg, Diameter, VertexTolerance ) ) return true ;
      //   if ( IsRouteOnPipeSpace != IsRouteOnPipeSpaceOrg ) return true ;
      //   if ( UseFromFixedHeight != UseFromFixedHeightOrg ) return true ;
      // if ( true == UseFromFixedHeight ) {
      //   if ( FromLocationTypeOrg != FromLocationType ) return true ;
      //   if ( false == LengthEquals( FromFixedHeightOrg, FromFixedHeight, VertexTolerance ) ) return true ;
      // }
      // if ( IsDifferentLevel ) {
      //   if ( true == UseToFixedHeight ) {
      //     if ( ToLocationTypeOrg != ToLocationType ) return true ;
      //     if ( false == LengthEquals( ToFixedHeightOrg, ToFixedHeight, VertexTolerance ) ) return true ;
      //   }
      // }
      // if ( AvoidTypeOrg != AvoidType ) return true ;
      // if ( UseShaft && ShaftOrg.GetValidId() != Shaft.GetValidId() ) return true ;
      //
      // return false ;
      return true ;
    }
    private bool CheckCanApply()
    {
      // if ( false == AllowIndeterminate ) {
      //   if ( UseSystemType && null == SystemType ) return false ;
      //   if ( UseCurveType && null == CurveType ) return false ;
      //   if ( null == Diameter ) return false ;
      //   if ( null == IsRouteOnPipeSpace ) return false ;
      //   if ( null == UseFromFixedHeight ) return false ;
      //   if ( null == FromLocationType ) return false ;
      //   if ( null == FromFixedHeight ) return false ;
      //   if ( IsDifferentLevel ) {
      //     if ( null == UseToFixedHeight ) return false ;
      //     if ( null == ToLocationType ) return false ;
      //     if ( null == ToFixedHeight ) return false ;
      //   }
      // }

      return true ;
    }
    private static void FromLocationTypeIndex_PropertyChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
    {
      ( d as RangeRouteWithPassEditControl )?.OnFromLocationTypeChanged() ;
    }

    private void SetMinMax( NumericUpDown numericUpDown, FixedHeightType locationType, double minimumValue, double maximumValue )
    {
      var lengthConverter = GetLengthConverter( DisplayUnitSystem ) ;
      numericUpDown.MinValue = Math.Round( lengthConverter.ConvertUnit( minimumValue ), 5, MidpointRounding.AwayFromZero ) ;
      numericUpDown.MaxValue = Math.Round( lengthConverter.ConvertUnit( maximumValue ), 5, MidpointRounding.AwayFromZero ) ;
      numericUpDown.Value = Math.Max( numericUpDown.MinValue, Math.Min( numericUpDown.Value, numericUpDown.MaxValue ) ) ;
      numericUpDown.ToolTip = $"{numericUpDown.MinValue} ～ {numericUpDown.MaxValue}" ;
    }
    private void SetAvailableParameterList( RoutePropertyTypeList propertyTypeList )
    {
      // Diameters.Clear() ;
      // SystemTypes.Clear() ;
      // CurveTypes.Clear() ;
      // Shafts.Clear() ;
      //
      // // System type
      // if ( propertyTypeList.SystemTypes is {} systemTypes ) {
      //   foreach ( var s in systemTypes ) {
      //     SystemTypes.Add( s ) ;
      //   }
      //
      //   UseSystemType = true ;
      // }
      // else {
      //   UseSystemType = false ;
      // }
      //
      // Shafts.Add( new OpeningProxy( null ) ) ;
      // if ( propertyTypeList.Shafts is {} shafts ) {
      //   foreach ( var shaft in shafts ) {
      //     Shafts.Add( new OpeningProxy( shaft ) ) ;
      //   }
      //
      //   UseShaft = true ;
      // }
      // else {
      //   UseShaft = false ;
      // }
      //
      // // Curve type
      // foreach ( var c in propertyTypeList.CurveTypes ) {
      //   CurveTypes.Add( c ) ;
      // }
      //
      // IsDifferentLevel = propertyTypeList.HasDifferentLevel ;

      ( FromMinimumHeightAsFloorLevel, FromMaximumHeightAsFloorLevel ) = propertyTypeList.FromHeightRangeAsFloorLevel ;
      ( FromMinimumHeightAsCeilingLevel, FromMaximumHeightAsCeilingLevel ) = propertyTypeList.FromHeightRangeAsCeilingLevel ;
      FromDefaultHeightAsFloorLevel = propertyTypeList.FromDefaultHeightAsFloorLevel ;
      FromDefaultHeightAsCeilingLevel = propertyTypeList.FromDefaultHeightAsCeilingLevel ;

      ( ToMinimumHeightAsFloorLevel, ToMaximumHeightAsFloorLevel ) = propertyTypeList.ToHeightRangeAsFloorLevel ;
      ( ToMinimumHeightAsCeilingLevel, ToMaximumHeightAsCeilingLevel ) = propertyTypeList.ToHeightRangeAsCeilingLevel ;
      ToDefaultHeightAsFloorLevel = propertyTypeList.ToDefaultHeightAsFloorLevel ;
      ToDefaultHeightAsCeilingLevel = propertyTypeList.ToDefaultHeightAsCeilingLevel ;
    }
    public void SetRouteProperties( RoutePropertyTypeList propertyTypeList, RouteProperties properties )
    {
      // VertexTolerance = properties.VertexTolerance ;
      SetAvailableParameterList( propertyTypeList ) ;
      
      // SystemTypeOrg = properties.SystemType ;
      // CurveTypeOrg = properties.CurveType ;
      // DiameterOrg = properties.Diameter ;
      // ShaftOrg = properties.Shaft ;
      //
      // IsRouteOnPipeSpaceOrg = properties.IsRouteOnPipeSpace ;

      UseFromFixedHeightOrg = properties.UseFromFixedHeight ;
      if ( null == UseFromFixedHeightOrg ) {
        FromLocationTypeOrg = null ;
        FromFixedHeightOrg = null ;
      }
      else {
        FromLocationTypeOrg = properties.FromFixedHeight?.Type ?? FixedHeightType.Floor ;
        FromFixedHeightOrg = properties.FromFixedHeight?.Height ?? GetFromDefaultHeight( FromLocationTypeOrg.Value ) ;
      }

      // UseToFixedHeightOrg = properties.UseToFixedHeight ;
      // if ( null == UseToFixedHeightOrg ) {
      //   ToLocationTypeOrg = null ;
      //   ToFixedHeightOrg = null ;
      // }
      // else {
      //   ToLocationTypeOrg = properties.ToFixedHeight?.Type ?? FixedHeightType.Ceiling ;
      //   ToFixedHeightOrg = properties.ToFixedHeight?.Height ?? GetFromDefaultHeight( ToLocationTypeOrg.Value ) ;
      // }

      AvoidTypeOrg = properties.AvoidType ;
    }
    private bool? UseFromFixedHeightOrg { get ; set ; }
    private double? FromFixedHeightOrg { get ; set ; }
    internal FixedHeightType? FromLocationTypeOrg { get ; set ; }
    
    public void ResetDialog()
    {
      // SystemType = SystemTypeOrg ;
      // CurveType = CurveTypeOrg ;
      // Diameter = DiameterOrg ;
      // Shaft = ShaftOrg ;
      //
      // IsRouteOnPipeSpace = IsRouteOnPipeSpaceOrg ;

      UseFromFixedHeight = UseFromFixedHeightOrg ;
      FromLocationType = FromLocationTypeOrg ;
      FromFixedHeight = FromFixedHeightOrg ;
      // UseToFixedHeight = UseToFixedHeightOrg ;
      // ToLocationType = ToLocationTypeOrg ;
      // ToFixedHeight = ToFixedHeightOrg ;

      OnFromLocationTypeChanged() ;
      // OnToLocationTypeChanged() ;

      // AvoidType = AvoidTypeOrg ;

      CanApply = false ;
    }
    // public AvoidType? AvoidType
    // {
    //   get => GetAvoidTypeOnIndex( AvoidTypes.Keys, (int)GetValue( AvoidTypeIndexProperty ) ) ;
    //   private set => SetValue( AvoidTypeIndexProperty, GetAvoidTypeIndex( AvoidTypes.Keys, value ) ) ;
    // }
    private AvoidType? AvoidTypeOrg { get ; set ; }
    private void OnFromLocationTypeChanged()
    {
      if ( FromLocationType is not { } locationType ) return ;

      var minimumValue = ( locationType == FixedHeightType.Ceiling ? FromMinimumHeightAsCeilingLevel : FromMinimumHeightAsFloorLevel ) ;
      var maximumValue = ( locationType == FixedHeightType.Ceiling ? FromMaximumHeightAsCeilingLevel : FromMaximumHeightAsFloorLevel ) ;
      SetMinMax( FromFixedHeightNumericUpDown, locationType, minimumValue, maximumValue ) ;
    }
    public FixedHeightType? FromLocationType
    {
      get => GetLocationTypeOnIndex( LocationTypes.Keys, (int)GetValue( FromLocationTypeIndexProperty ) ) ;
      private set => SetValue( FromLocationTypeIndexProperty, GetLocationTypeIndex( LocationTypes.Keys, value ) ) ;
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

    public bool? UseFromFixedHeight
    {
      get => (bool?)GetValue( UseFromFixedHeightProperty ) ;
      private set => SetValue( UseFromFixedHeightProperty, value ) ;
    }

    public IReadOnlyDictionary<FixedHeightType, string> LocationTypes { get ; } = new Dictionary<FixedHeightType, string>
    {
      [ FixedHeightType.Floor ] = "FL",
      [ FixedHeightType.Ceiling] = "CL",
    } ;

    public object FromLocationTypeIndex
    {
      get { throw new System.NotImplementedException() ; }
    }

    private void Height_OnChecked( object sender, RoutedEventArgs e )
    {
      OnValueChanged( EventArgs.Empty ) ;
    }

    private void Height_OnUnchecked( object sender, RoutedEventArgs e )
    {
      OnValueChanged( EventArgs.Empty ) ;
    }
    public double? FromFixedHeight
    {
      get => (double?)GetValue( FromFixedHeightProperty ) ;
      private set => SetValue( FromFixedHeightProperty, value ) ;
    }
    // public double? ToFixedHeight
    // {
    //   get => (double?)GetValue( ToFixedHeightProperty ) ;
    //   private set => SetValue( ToFixedHeightProperty, value ) ;
    // }
    private void LocationTypeComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      OnValueChanged( EventArgs.Empty ) ;

      if ( e.RemovedItems.OfType<KeyValuePair<FixedHeightType, string>?>().FirstOrDefault() is not { } oldValue || e.AddedItems.OfType<KeyValuePair<FixedHeightType, string>?>().FirstOrDefault() is not { } newValue ) return ;
      if ( oldValue.Key == newValue.Key ) return ;

      if ( object.ReferenceEquals( sender, FromLocationTypeComboBox ) ) {
        FromFixedHeight = GetFromDefaultHeight( newValue.Key ) ;
      }
      // else {
      //   ToFixedHeight = GetToDefaultHeight( newValue.Key ) ;
      // }
    }
    private double? GetFromDefaultHeight( FixedHeightType newValue )
    {
      return ( FixedHeightType.Ceiling == newValue ) ? FromDefaultHeightAsCeilingLevel : FromDefaultHeightAsFloorLevel ;
    }
    private double? GetToDefaultHeight( FixedHeightType newValue )
    {
      return ( FixedHeightType.Ceiling == newValue ) ? ToDefaultHeightAsCeilingLevel : ToDefaultHeightAsFloorLevel ;
    }
    public DisplayUnit DisplayUnitSystem
    {
      get { return (DisplayUnit)GetValue( DisplayUnitSystemProperty ) ; }
      set { SetValue( DisplayUnitSystemProperty, value ) ; }
    }
    private void FromFixedHeightNumericUpDown_OnValueChanged( object sender, ValueChangedEventArgs e )
    {
      // Manually update FixedHeight because Value binding is not called.
      FromFixedHeight = GetLengthConverter( DisplayUnitSystem ).ConvertBackUnit( FromFixedHeightNumericUpDown.Value ) ;

      OnValueChanged( EventArgs.Empty ) ;
    }

    private static void FromFixedHeight_Changed( DependencyObject d, DependencyPropertyChangedEventArgs e )
    {
      if ( d is not RangeRouteWithPassEditControl rangeRouteWithPassEditControl ) return ;

      if ( e.NewValue is not double newValue ) {
        rangeRouteWithPassEditControl.FromFixedHeightNumericUpDown.CanHaveNull = true ;
        rangeRouteWithPassEditControl.FromFixedHeightNumericUpDown.HasValidValue = false ;
      }
      else {
        rangeRouteWithPassEditControl.FromFixedHeightNumericUpDown.CanHaveNull = false ;
        rangeRouteWithPassEditControl.FromFixedHeightNumericUpDown.Value = Math.Round( GetLengthConverter( rangeRouteWithPassEditControl.DisplayUnitSystem ).ConvertUnit( newValue ), 5, MidpointRounding.AwayFromZero ) ;
      }
    }

    // private static void ToFixedHeight_Changed( DependencyObject d, DependencyPropertyChangedEventArgs e )
    // {
    //   if ( d is not RangeRouteWithPassEditControl rangeRouteWithPassEditControl ) return ;
    //
    //   if ( e.NewValue is not double newValue ) {
    //     rangeRouteWithPassEditControl.ToFixedHeightNumericUpDown.CanHaveNull = true ;
    //     rangeRouteWithPassEditControl.ToFixedHeightNumericUpDown.HasValidValue = false ;
    //   }
    //   else {
    //     rangeRouteWithPassEditControl.ToFixedHeightNumericUpDown.CanHaveNull = false ;
    //     rangeRouteWithPassEditControl.ToFixedHeightNumericUpDown.Value = Math.Round( GetLengthConverter( rangeRouteWithPassEditControl.DisplayUnitSystem ).ConvertUnit( newValue ), 5, MidpointRounding.AwayFromZero ) ;
    //   }
    // }

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
  }
}