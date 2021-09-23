﻿using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Autodesk.Revit.DB ;
using System.Collections.ObjectModel ;
using System.Text.RegularExpressions ;
using System.Windows.Controls ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using ControlLib ;
using LengthConverter = Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters.LengthConverter ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class FromToEditControl : UserControl
  {
    private const string DefaultCurveTypeLabel = "Type" ;
    private const double DefaultCurrentMinValue = 0 ;
    private const double DefaultCurrentMaxValue = 10000 ;

    public event EventHandler? ValueChanged ;

    private void OnValueChanged( EventArgs e )
    {
      CanApply = CheckCanApply() ;
      IsChanged = CanApply && CheckIsChanged() ;
      ValueChanged?.Invoke( this, e ) ;
    }

    public static readonly DependencyProperty SystemTypeEditableProperty = DependencyProperty.Register( "SystemTypeEditable", typeof( bool ), typeof( FromToEditControl ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty ShaftEditableProperty = DependencyProperty.Register( "ShaftEditable", typeof( bool ), typeof( FromToEditControl ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty CurveTypeEditableProperty = DependencyProperty.Register( "CurveTypeEditable", typeof( bool ), typeof( FromToEditControl ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty UseSystemTypeProperty = DependencyProperty.Register( "UseSystemType", typeof( bool ), typeof( FromToEditControl ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty UseShaftProperty = DependencyProperty.Register( "UseShaft", typeof( bool ), typeof( FromToEditControl ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty UseCurveTypeProperty = DependencyProperty.Register( "UseCurveType", typeof( bool ), typeof( FromToEditControl ), new PropertyMetadata( true ) ) ;
    public static readonly DependencyProperty DiameterIndexProperty = DependencyProperty.Register( "DiameterIndex", typeof( int ), typeof( FromToEditControl ), new PropertyMetadata( -1 ) ) ;
    public static readonly DependencyProperty SystemTypeIndexProperty = DependencyProperty.Register( "SystemTypeIndex", typeof( int ), typeof( FromToEditControl ), new PropertyMetadata( -1 ) ) ;
    public static readonly DependencyProperty ShaftIndexProperty = DependencyProperty.Register( "ShaftIndex", typeof( int ), typeof( FromToEditControl ), new PropertyMetadata( -1 ) ) ;
    public static readonly DependencyProperty CurveTypeIndexProperty = DependencyProperty.Register( "CurveTypeIndex", typeof( int ), typeof( FromToEditControl ), new PropertyMetadata( -1 ) ) ;
    public static readonly DependencyProperty CurveTypeLabelProperty = DependencyProperty.Register( "CurveTypeLabel", typeof( string ), typeof( FromToEditControl ), new PropertyMetadata( DefaultCurveTypeLabel ) ) ;
    public static readonly DependencyProperty IsRouteOnPipeSpaceProperty = DependencyProperty.Register( "IsRouteOnPipeSpace", typeof( bool? ), typeof( FromToEditControl ), new PropertyMetadata( (bool?)true ) ) ;
    public static readonly DependencyProperty UseFixedHeightProperty = DependencyProperty.Register( "UseFixedHeight", typeof( bool? ), typeof( FromToEditControl ), new PropertyMetadata( (bool?)false ) ) ;
    public static readonly DependencyProperty FixedHeightProperty = DependencyProperty.Register( "FixedHeight", typeof( double ), typeof( FromToEditControl ), new PropertyMetadata( 0.0, FixedHeight_Changed ) ) ;
    public static readonly DependencyProperty AvoidTypeIndexProperty = DependencyProperty.Register( "AvoidTypeIndex", typeof( int ), typeof( FromToEditControl ), new PropertyMetadata( 0 ) ) ;
    public static readonly DependencyProperty CurrentMinValueProperty = DependencyProperty.Register( "CurrentMinValue", typeof( double ), typeof( FromToEditControl ), new PropertyMetadata( DefaultCurrentMinValue ) ) ;
    public static readonly DependencyProperty CurrentMaxValueProperty = DependencyProperty.Register( "CurrentMaxValue", typeof( double ), typeof( FromToEditControl ), new PropertyMetadata( DefaultCurrentMaxValue ) ) ;
    private static readonly DependencyPropertyKey CanApplyPropertyKey = DependencyProperty.RegisterReadOnly( "CanApply", typeof( bool ), typeof( FromToEditControl ), new PropertyMetadata( false ) ) ;
    private static readonly DependencyPropertyKey IsChangedPropertyKey = DependencyProperty.RegisterReadOnly( "IsChanged", typeof( bool ), typeof( FromToEditControl ), new PropertyMetadata( false ) ) ;
    public static readonly DependencyProperty AllowIndeterminateProperty = DependencyProperty.Register( "AllowIndeterminate", typeof( bool ), typeof( FromToEditControl ), new PropertyMetadata( default( bool ) ) ) ;
    public static readonly DependencyProperty DisplayUnitSystemProperty = DependencyProperty.Register( "DisplayUnitSystem", typeof( DisplayUnit ), typeof( FromToEditControl ), new PropertyMetadata( DisplayUnit.IMPERIAL ) ) ;

    //Diameter Info
    private double VertexTolerance { get ; set ; }
    public ObservableCollection<double> Diameters { get ; } = new ObservableCollection<double>() ;
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
    public ObservableCollection<MEPSystemType> SystemTypes { get ; } = new ObservableCollection<MEPSystemType>() ;
    private MEPSystemType? SystemTypeOrg { get ; set ; }
    public MEPSystemType? SystemType
    {
      get => GetTypeOnIndex( SystemTypes, (int)GetValue( SystemTypeIndexProperty ) ) ;
      private set => SetValue( SystemTypeIndexProperty, GetTypeIndex( SystemTypes, value ) ) ;
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
    public ObservableCollection<Opening> Shafts { get ; } = new ObservableCollection<Opening>() ;
    private Opening? ShaftOrg { get ; set ; }
    public Opening? Shaft
    {
      get => GetTypeOnIndex( Shafts, (int)GetValue( SystemTypeIndexProperty ) ) ;
      private set => SetValue( SystemTypeIndexProperty, GetTypeIndex( Shafts, value ) ) ;
    }
    public bool ShaftsEditable
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
    public ObservableCollection<MEPCurveType> CurveTypes { get ; } = new ObservableCollection<MEPCurveType>() ;
    private MEPCurveType? CurveTypeOrg { get ; set ; }
    public MEPCurveType? CurveType
    {
      get => GetTypeOnIndex( CurveTypes, (int)GetValue( CurveTypeIndexProperty ) ) ;
      private set
      {
        SetValue( CurveTypeIndexProperty, GetTypeIndex( CurveTypes, value ) ) ;
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

      if ( currentDiameter is {} d ) {
        SetCurrentValue( DiameterIndexProperty, UIHelper.FindClosestIndex( Diameters, d ) );
      }
      else {
        SetCurrentValue( DiameterIndexProperty, -1 );
      }
    }

    public DisplayUnit DisplayUnitSystem
    {
      get { return (DisplayUnit)GetValue( DisplayUnitSystemProperty ) ; }
      set { SetValue( DisplayUnitSystemProperty, value ) ; }
    }

    private string CurveTypeLabel
    {
      get => (string) GetValue( CurveTypeLabelProperty ) ;
      set => SetValue( CurveTypeLabelProperty, value ) ;
    }
    public bool CurveTypeEditable
    {
      get => (bool) GetValue( CurveTypeEditableProperty ) ;
      set => SetValue( CurveTypeEditableProperty, value ) ;
    }
    private bool UseCurveType
    {
      get => (bool) GetValue( UseCurveTypeProperty ) ;
      set => SetValue( UseCurveTypeProperty, value ) ;
    }

    private static T? GetTypeOnIndex<T>( IReadOnlyList<T> values, int index ) where T : class
    {
      if ( index < 0 || values.Count <= index ) return null ;
      return values[ index ] ;
    }
    private static int GetTypeIndex<T>( IEnumerable<T> elementTypes, T? value ) where T : Element
    {
      if ( value is not { } elementType ) return -1 ;

      return elementTypes.FindIndex( e => e.Id == elementType.Id ) ;
    }

    //Direct Info
    private bool? IsRouteOnPipeSpaceOrg { get ; set ; }
    public bool? IsRouteOnPipeSpace
    {
      get => (bool?) GetValue( IsRouteOnPipeSpaceProperty ) ;
      private set => SetValue( IsRouteOnPipeSpaceProperty, value ) ;
    }

    //HeightSetting
    private bool? UseFixedHeightOrg { get ; set ; }
    private double FixedHeightOrg { get ; set ; }
    public bool? UseFixedHeight
    {
      get => (bool?) GetValue( UseFixedHeightProperty ) ;
      private set => SetValue( UseFixedHeightProperty, value ) ;
    }
    public double FixedHeight
    {
      get => (double) GetValue( FixedHeightProperty ) ;
      private set => SetValue( FixedHeightProperty, value ) ;
    }
    public double ConnectorFixedHeight => FixedHeight - ( ( Diameter ?? 0.0 ) / 2 ) ;

    public double CurrentMinValue
    {
      get => (double) GetValue( CurrentMinValueProperty ) ;
      private set => SetValue( CurrentMinValueProperty, value ) ;
    }
    public double CurrentMaxValue
    {
      get => (double) GetValue( CurrentMaxValueProperty ) ;
      private set => SetValue( CurrentMaxValueProperty, value ) ;
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

    public IReadOnlyDictionary<AvoidType, string> AvoidTypes { get ; } = new Dictionary<AvoidType, string>
    {
      [ Routing.AvoidType.Whichever ] = "Dialog.Forms.FromToEditControl.ProcessConstraints.None".GetAppStringByKeyOrDefault( "Whichever" ),
      [ Routing.AvoidType.NoAvoid ] = "Dialog.Forms.FromToEditControl.ProcessConstraints.NoPocket".GetAppStringByKeyOrDefault( "Don't avoid From-To" ),
      [ Routing.AvoidType.AvoidAbove ] = "Dialog.Forms.FromToEditControl.ProcessConstraints.NoDrainPocket".GetAppStringByKeyOrDefault( "Avoid on From-To" ),
      [ Routing.AvoidType.AvoidBelow ] = "Dialog.Forms.FromToEditControl.ProcessConstraints.NoVentPocket".GetAppStringByKeyOrDefault( "Avoid below From-To" ),
    } ;

    public bool CanApply
    {
      get => (bool)GetValue( CanApplyPropertyKey.DependencyProperty ) ;
      private set => SetValue( CanApplyPropertyKey, value ) ;
    }

    public bool IsChanged
    {
      get => (bool)GetValue( IsChangedPropertyKey.DependencyProperty ) ;
      private set => SetValue( IsChangedPropertyKey, value ) ;
    }

    public bool AllowIndeterminate
    {
      get { return (bool)GetValue( AllowIndeterminateProperty ) ; }
      set { SetValue( AllowIndeterminateProperty, value ) ; }
    }

    private bool CheckCanApply()
    {
      if ( false == AllowIndeterminate ) {
        if ( UseSystemType && null == SystemType ) return false ;
        if ( UseCurveType && null == CurveType ) return false ;
        if ( null == Diameter ) return false ;
        if ( null == IsRouteOnPipeSpace ) return false ;
        if ( null == UseFixedHeight ) return false ;
        if ( double.IsNaN( FixedHeight ) ) return false ;
      }

      return true ;
    }

    private bool CheckIsChanged()
    {
      if ( UseSystemType && SystemTypeOrg?.Id != SystemType?.Id ) return true ;
      if ( UseCurveType && CurveTypeOrg?.Id != CurveType?.Id ) return true ;
      if ( false == LengthEquals( DiameterOrg, Diameter, VertexTolerance ) ) return true ;
      if ( IsRouteOnPipeSpace != IsRouteOnPipeSpaceOrg ) return true ;
      if ( UseFixedHeight != UseFixedHeightOrg ) return true ;
      if ( true == UseFixedHeight && false == LengthEquals( FixedHeightOrg, FixedHeight, VertexTolerance ) ) return true ;
      if ( AvoidTypeOrg != AvoidType ) return true ;

      return false ;
    }

    public FromToEditControl()
    {
      InitializeComponent() ;

      ClearDialog() ;
    }

    /// <summary>
    /// Get LableName from CurveType
    /// </summary>
    /// <param name="targetStrings"></param>
    /// <returns></returns>
    public string GetTypeLabel( string targetStrings )
    {
      string[] splitStrings = Regex.Split( targetStrings, "Type" ) ;

      return splitStrings[ 0 ] + " Type" ;
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

    /// <summary>
    /// Update Diameters, SystemTypes, Shafts and CurveTypes
    /// </summary>
    /// <param name="systemTypes"></param>
    /// <param name="shafts"></param>
    /// <param name="curveTypes"></param>
    private void SetAvailableParameterList( IList<MEPSystemType>? systemTypes, IList<Opening>? shafts, IList<MEPCurveType> curveTypes)
    {
      Diameters.Clear() ;
      SystemTypes.Clear() ;
      Shafts.Clear();
      CurveTypes.Clear() ;

      // System type
      if ( systemTypes != null ) {
        foreach ( var s in systemTypes ) {
          SystemTypes.Add( s ) ;
        }

        UseSystemType = true ;
      }
      else {
        UseSystemType = false ;
      }

      // Shafts
      if ( shafts != null ) {
        foreach ( var s in shafts ) {
          Shafts.Add( s ) ;
        }
        UseShaft = true ;
      }
      else {
        UseShaft = false ;
      }
      
      // Curve type
      foreach ( var c in curveTypes ) {
        CurveTypes.Add( c ) ;
      }
    }

    public void SetRouteProperties( RoutePropertyTypeList propertyTypeList, RouteProperties properties )
    {
      VertexTolerance = properties.VertexTolerance ;
      SetAvailableParameterList( propertyTypeList.SystemTypes, propertyTypeList.Shafts, propertyTypeList.CurveTypes ) ;

      SystemTypeOrg = properties.SystemType ;
      ShaftOrg = properties.Shaft ;
      CurveTypeOrg = properties.CurveType ;
      DiameterOrg = properties.Diameter ;

      IsRouteOnPipeSpaceOrg = properties.IsRouteOnPipeSpace ;
      UseFixedHeightOrg = properties.UseFixedHeight ;
      FixedHeightOrg = properties.FixedHeight ;
      AvoidTypeOrg = properties.AvoidType ;
    }

    public void ResetDialog()
    {
      SystemType = SystemTypeOrg ;
      CurveType = CurveTypeOrg ;
      Diameter = DiameterOrg ;

      IsRouteOnPipeSpace = IsRouteOnPipeSpaceOrg ;
      UseFixedHeight = UseFixedHeightOrg ;
      FixedHeight = FixedHeightOrg ;
      AvoidType = AvoidTypeOrg ;
      CanApply = false ;
    }

    public void ClearDialog()
    {
      Diameters.Clear() ;
      SystemTypes.Clear() ;
      CurveTypes.Clear() ;

      DiameterOrg = null ;
      SystemTypeOrg = null ;
      CurveTypeOrg = null ;

      IsRouteOnPipeSpaceOrg = false ;
      UseFixedHeightOrg = false ;
      FixedHeightOrg = 0.0 ;
      AvoidTypeOrg = Routing.AvoidType.Whichever ;

      ResetDialog() ;
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

    private KeyValuePair<AvoidType, string> GetAvoidTypeKeyValuePair( AvoidType avoidTypeKey )
    {
      return new KeyValuePair<AvoidType, string>( avoidTypeKey, AvoidTypes[ avoidTypeKey ] ) ;
    }

    private void FixedHeightNumericUpDown_OnValueChanged( object sender, ValueChangedEventArgs e )
    {
      // Manually update FixedHeight because Value binding is not called.
      FixedHeight = GetLengthConverter( DisplayUnitSystem ).ConvertBackUnit( FixedHeightNumericUpDown.Value ) ;

      OnValueChanged( EventArgs.Empty ) ;
    }

    private static void FixedHeight_Changed( DependencyObject d, DependencyPropertyChangedEventArgs e )
    {
      if ( d is not FromToEditControl fromToEditControl ) return ;
      if ( e.NewValue is not double newValue ) return ;

      fromToEditControl.FixedHeightNumericUpDown.Value = Math.Round( GetLengthConverter( fromToEditControl.DisplayUnitSystem ).ConvertUnit( newValue ), 5, MidpointRounding.AwayFromZero ) ;
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