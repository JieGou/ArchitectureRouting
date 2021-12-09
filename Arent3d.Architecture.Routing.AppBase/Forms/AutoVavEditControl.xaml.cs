using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Arent3d.Revit;
using Arent3d.Revit.I18n;
using Arent3d.Utility;
using Autodesk.Revit.DB;
using ControlLib;
using LengthConverter = Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters.LengthConverter ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class AutoVavEditControl
  {
    private const string DefaultCurveTypeLabel = "Type" ;

    private void OnValueChanged( EventArgs e )
    {
      CanApply = CheckCanApply() ;
      IsChanged = CanApply && CheckIsChanged() ;
    }

    public static readonly DependencyProperty UseSystemTypeProperty = DependencyProperty.Register( "UseSystemType", typeof( bool ), typeof( AutoVavEditControl ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty DiameterIndexProperty = DependencyProperty.Register( "DiameterIndex", typeof( int ), typeof( AutoVavEditControl ), new PropertyMetadata( -1 ) ) ;
    public static readonly DependencyProperty SystemTypeIndexProperty = DependencyProperty.Register( "SystemTypeIndex", typeof( int ), typeof( AutoVavEditControl ), new PropertyMetadata( -1 ) ) ;
    public static readonly DependencyProperty CurveTypeIndexProperty = DependencyProperty.Register( "CurveTypeIndex", typeof( int ), typeof( AutoVavEditControl ), new PropertyMetadata( -1 ) ) ;
    public static readonly DependencyProperty CurveTypeLabelProperty = DependencyProperty.Register( "CurveTypeLabel", typeof( string ), typeof( AutoVavEditControl ), new PropertyMetadata( DefaultCurveTypeLabel ) ) ;
    public static readonly DependencyProperty UseFromFixedHeightProperty = DependencyProperty.Register( "UseFromFixedHeight", typeof( bool? ), typeof( AutoVavEditControl ), new PropertyMetadata( (bool?)false ) ) ;
    public static readonly DependencyProperty FromFixedHeightProperty = DependencyProperty.Register( "FromFixedHeight", typeof( double? ), typeof( AutoVavEditControl ), new PropertyMetadata( 0.0, FromFixedHeight_Changed ) ) ;
    public static readonly DependencyProperty FromLocationTypeIndexProperty = DependencyProperty.Register( "FromLocationTypeIndex", typeof( int ), typeof( AutoVavEditControl ), new PropertyMetadata( 0, FromLocationTypeIndex_PropertyChanged ) ) ;
    public static readonly DependencyProperty AvoidTypeIndexProperty = DependencyProperty.Register( "AvoidTypeIndex", typeof( int ), typeof( AutoVavEditControl ), new PropertyMetadata( 0 ) ) ;
    private static readonly DependencyPropertyKey CanApplyPropertyKey = DependencyProperty.RegisterReadOnly( "CanApply", typeof( bool ), typeof( AutoVavEditControl ), new PropertyMetadata( false ) ) ;
    private static readonly DependencyPropertyKey IsChangedPropertyKey = DependencyProperty.RegisterReadOnly( "IsChanged", typeof( bool ), typeof( AutoVavEditControl ), new PropertyMetadata( false ) ) ;
    public static readonly DependencyProperty AllowIndeterminateProperty = DependencyProperty.Register( "AllowIndeterminate", typeof( bool ), typeof( AutoVavEditControl ), new PropertyMetadata( default( bool ) ) ) ;
    public static readonly DependencyProperty DisplayUnitSystemProperty = DependencyProperty.Register( "DisplayUnitSystem", typeof( DisplayUnit ), typeof( AutoVavEditControl ), new PropertyMetadata( DisplayUnit.IMPERIAL ) ) ;
    private static readonly DependencyPropertyKey FromMinimumHeightAsFloorLevelPropertyKey = DependencyProperty.RegisterReadOnly( "FromMinimumHeightAsFloorLevel", typeof( double ), typeof( AutoVavEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey FromMaximumHeightAsFloorLevelPropertyKey = DependencyProperty.RegisterReadOnly( "FromMaximumHeightAsFloorLevel", typeof( double ), typeof( AutoVavEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey FromMinimumHeightAsCeilingLevelPropertyKey = DependencyProperty.RegisterReadOnly( "FromMinimumHeightAsCeilingLevel", typeof( double ), typeof( AutoVavEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey FromMaximumHeightAsCeilingLevelPropertyKey = DependencyProperty.RegisterReadOnly( "FromMaximumHeightAsCeilingLevel", typeof( double ), typeof( AutoVavEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey FromDefaultHeightAsFloorLevelPropertyKey = DependencyProperty.RegisterReadOnly( "FromDefaultHeightAsFloorLevel", typeof( double ), typeof( AutoVavEditControl ), new PropertyMetadata( 0.0 ) ) ;
    private static readonly DependencyPropertyKey FromDefaultHeightAsCeilingLevelPropertyKey = DependencyProperty.RegisterReadOnly( "FromDefaultHeightAsCeilingLevel", typeof( double ), typeof( AutoVavEditControl ), new PropertyMetadata( 0.0 ) ) ;

    //Diameter Info
    private double VertexTolerance { get ; set ; }
    private ObservableCollection<double> Diameters { get ; } = new() ;
    private double? DiameterOrg { get ; set ; }

    public double? Diameter
    {
      get => GetDiameterOnIndex( Diameters, (int)GetValue( DiameterIndexProperty ) ) ;
      private set => SetValue( DiameterIndexProperty, GetDiameterIndex( Diameters, value, VertexTolerance ) ) ;
    }

    private static double? GetDiameterOnIndex( IReadOnlyList<double> diameters, int index )
    {
      if ( index < 0 || diameters.Count <= index ) return null ;
      return diameters[ index ] ;
    }

    private static int GetDiameterIndex( IReadOnlyCollection<double> diameters, double? value, double tolerance )
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
    private ObservableCollection<MEPSystemType> SystemTypes { get ; } = new() ;
    private MEPSystemType? SystemTypeOrg { get ; set ; }

    public MEPSystemType? SystemType
    {
      get => GetItemOnIndex( SystemTypes, (int)GetValue( SystemTypeIndexProperty ) ) ;
      private set => SetValue( SystemTypeIndexProperty, GetItemIndex( SystemTypes, value ) ) ;
    }

    private bool UseSystemType
    {
      get => (bool)GetValue( UseSystemTypeProperty ) ;
      set => SetValue( UseSystemTypeProperty, value ) ;
    }

    //CurveType Info
    private ObservableCollection<MEPCurveType> CurveTypes { get ; } = new() ;
    private MEPCurveType? CurveTypeOrg { get ; set ; }

    public MEPCurveType? CurveType
    {
      get => GetItemOnIndex( CurveTypes, (int)GetValue( CurveTypeIndexProperty ) ) ;
      private set
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
      get { return (DisplayUnit)GetValue( DisplayUnitSystemProperty ) ; }
      set { SetValue( DisplayUnitSystemProperty, value ) ; }
    }

    private string CurveTypeLabel
    {
      get => (string)GetValue( CurveTypeLabelProperty ) ;
      set => SetValue( CurveTypeLabelProperty, value ) ;
    }

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

    //HeightSetting
    private bool? UseFromFixedHeightOrg { get ; set ; }
    private double? FromFixedHeightOrg { get ; set ; }

    public bool? UseFromFixedHeight
    {
      get => (bool?)GetValue( UseFromFixedHeightProperty ) ;
      private set => SetValue( UseFromFixedHeightProperty, value ) ;
    }

    public double? FromFixedHeight
    {
      get => (double?)GetValue( FromFixedHeightProperty ) ;
      private set => SetValue( FromFixedHeightProperty, value ) ;
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
    private double FromMaximumHeightAsCeilingLevel
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

    private static void FromLocationTypeIndex_PropertyChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
    {
      ( d as AutoVavEditControl )?.OnFromLocationTypeChanged() ;
    }

    private void OnFromLocationTypeChanged()
    {
      if ( FromLocationType is not { } locationType ) return ;

      var minimumValue = ( locationType == FixedHeightType.Ceiling ? FromMinimumHeightAsCeilingLevel : FromMinimumHeightAsFloorLevel ) ;
      var maximumValue = ( locationType == FixedHeightType.Ceiling ? FromMaximumHeightAsCeilingLevel : FromMaximumHeightAsFloorLevel ) ;
      SetMinMax( FromFixedHeightNumericUpDown, locationType, minimumValue, maximumValue ) ;
    }

    private void SetMinMax( NumericUpDown numericUpDown, FixedHeightType locationType, double minimumValue, double maximumValue )
    {
      var lengthConverter = GetLengthConverter( DisplayUnitSystem ) ;
      numericUpDown.MinValue = Math.Round( lengthConverter.ConvertUnit( minimumValue ), 5, MidpointRounding.AwayFromZero ) ;
      numericUpDown.MaxValue = Math.Round( lengthConverter.ConvertUnit( maximumValue ), 5, MidpointRounding.AwayFromZero ) ;
      numericUpDown.Value = Math.Max( numericUpDown.MinValue, Math.Min( numericUpDown.Value, numericUpDown.MaxValue ) ) ;
      numericUpDown.ToolTip = $"{numericUpDown.MinValue} ～ {numericUpDown.MaxValue}" ;
    }

    //AvoidType
    private AvoidType? AvoidTypeOrg { get ; set ; }

    public AvoidType? AvoidType
    {
      get => GetAvoidTypeOnIndex( AvoidTypes.Keys, (int)GetValue( AvoidTypeIndexProperty ) ) ;
      private set => SetValue( AvoidTypeIndexProperty, GetAvoidTypeIndex( AvoidTypes.Keys, value ) ) ;
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

    private IReadOnlyDictionary<AvoidType, string> AvoidTypes { get ; } = new Dictionary<AvoidType, string>
    {
      [ Routing.AvoidType.Whichever ] = "Dialog.Forms.FromToEditControl.ProcessConstraints.None".GetAppStringByKeyOrDefault( "Whichever" ),
      [ Routing.AvoidType.NoAvoid ] = "Dialog.Forms.FromToEditControl.ProcessConstraints.NoPocket".GetAppStringByKeyOrDefault( "Don't avoid From-To" ),
      [ Routing.AvoidType.AvoidAbove ] = "Dialog.Forms.FromToEditControl.ProcessConstraints.NoDrainPocket".GetAppStringByKeyOrDefault( "Avoid on From-To" ),
      [ Routing.AvoidType.AvoidBelow ] = "Dialog.Forms.FromToEditControl.ProcessConstraints.NoVentPocket".GetAppStringByKeyOrDefault( "Avoid below From-To" ),
    } ;

    //LocationType
    private FixedHeightType? FromLocationTypeOrg { get ; set ; }

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

    private IReadOnlyDictionary<FixedHeightType, string> LocationTypes { get ; } = new Dictionary<FixedHeightType, string>
    {
      [ FixedHeightType.Floor ] = "FL",
      [ FixedHeightType.Ceiling] = "CL",
    } ;

    private bool CanApply
    {
      get => (bool)GetValue( CanApplyPropertyKey.DependencyProperty ) ;
      set => SetValue( CanApplyPropertyKey, value ) ;
    }

    private bool IsChanged
    {
      get => (bool)GetValue( IsChangedPropertyKey.DependencyProperty ) ;
      set => SetValue( IsChangedPropertyKey, value ) ;
    }

    private bool AllowIndeterminate
    {
      get { return (bool)GetValue( AllowIndeterminateProperty ) ; }
      set { SetValue( AllowIndeterminateProperty, value ) ; }
    }

    private bool CheckCanApply()
    {
      if ( false == AllowIndeterminate ) {
        if ( null == UseFromFixedHeight ) return false ;
        if ( null == FromLocationType ) return false ;
        if ( null == FromFixedHeight ) return false ;
      }

      return true ;
    }

    private bool CheckIsChanged()
    {
      if ( UseFromFixedHeight != UseFromFixedHeightOrg ) return true ;
      if ( true == UseFromFixedHeight ) {
        if ( FromLocationTypeOrg != FromLocationType ) return true ;
        if ( false == LengthEquals( FromFixedHeightOrg, FromFixedHeight, VertexTolerance ) ) return true ;
      }

      return false ;
    }

    public AutoVavEditControl()
    {
      InitializeComponent() ;

      ClearDialog() ;
    }

    private void SetAvailableParameterList( RoutePropertyTypeList propertyTypeList )
    {
      // System type
      if ( propertyTypeList.SystemTypes is {} systemTypes ) {
        foreach ( var s in systemTypes ) {
          SystemTypes.Add( s ) ;
        }

        UseSystemType = true ;
      }
      else {
        UseSystemType = false ;
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

      UseFromFixedHeightOrg = properties.UseFromFixedHeight ;
      if ( null == UseFromFixedHeightOrg ) {
        FromLocationTypeOrg = null ;
        FromFixedHeightOrg = null ;
      }
      else {
        FromLocationTypeOrg = properties.FromFixedHeight?.Type ?? FixedHeightType.Ceiling ;
        FromFixedHeightOrg = properties.FromFixedHeight?.Height ?? GetFromDefaultHeight( FromLocationTypeOrg.Value ) ;
      }

      AvoidTypeOrg = properties.AvoidType ;
    }

    public void ResetDialog()
    {
      SystemType = SystemTypeOrg ;
      CurveType = CurveTypeOrg ;
      Diameter = DiameterOrg ;
      UseFromFixedHeight = UseFromFixedHeightOrg ;
      FromLocationType = FromLocationTypeOrg ;
      FromFixedHeight = FromFixedHeightOrg ;

      OnFromLocationTypeChanged() ;

      AvoidType = AvoidTypeOrg ;

      CanApply = false ;
    }

    private void ClearDialog()
    {
      Diameters.Clear() ;
      SystemTypes.Clear() ;
      CurveTypes.Clear() ;

      DiameterOrg = null ;
      SystemTypeOrg = null ;
      CurveTypeOrg = null ;

      UseFromFixedHeightOrg = false ;
      FromLocationTypeOrg = FixedHeightType.Floor ;
      FromFixedHeight = null ;

      AvoidTypeOrg = Routing.AvoidType.Whichever ;

      ResetDialog() ;
    }

    private double? GetFromDefaultHeight( FixedHeightType newValue )
    {
      return ( FixedHeightType.Ceiling == newValue ) ? FromDefaultHeightAsCeilingLevel : FromDefaultHeightAsFloorLevel ;
    }

    private void FromFixedHeightNumericUpDown_OnValueChanged( object sender, ValueChangedEventArgs e )
    {
      // Manually update FixedHeight because Value binding is not called.
      FromFixedHeight = GetLengthConverter( DisplayUnitSystem ).ConvertBackUnit( FromFixedHeightNumericUpDown.Value ) ;

      OnValueChanged( EventArgs.Empty ) ;
    }

    private static void FromFixedHeight_Changed( DependencyObject d, DependencyPropertyChangedEventArgs e )
    {
      if ( d is not AutoVavEditControl fromToEditControl ) return ;

      if ( e.NewValue is not double newValue ) {
        fromToEditControl.FromFixedHeightNumericUpDown.CanHaveNull = true ;
        fromToEditControl.FromFixedHeightNumericUpDown.HasValidValue = false ;
      }
      else {
        fromToEditControl.FromFixedHeightNumericUpDown.CanHaveNull = false ;
        fromToEditControl.FromFixedHeightNumericUpDown.Value = Math.Round( GetLengthConverter( fromToEditControl.DisplayUnitSystem ).ConvertUnit( newValue ), 5, MidpointRounding.AwayFromZero ) ;
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
  }
}