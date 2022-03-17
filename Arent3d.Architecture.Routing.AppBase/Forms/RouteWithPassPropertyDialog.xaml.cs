using System ;
using Autodesk.Revit.DB ;
using System.Windows ;
using System.Windows.Media ;
using Visibility = System.Windows.Visibility ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public interface IRouteWithPassPropertyDialog: IRoutePropertyDialog
  {
    FixedHeight? GetPowerToPassFromFixedHeight() ;
  }
  /// <summary>
  /// SetProperty.xaml の相互作用ロジック
  /// </summary>
  public partial class RouteWithPassPropertyDialog : Window, IRouteWithPassPropertyDialog
  {
    public RouteWithPassPropertyDialog()
    {
      InitializeComponent() ;
    }

    public RouteWithPassPropertyDialog( Document document, RoutePropertyTypeList propertyTypeList, RouteProperties properties )
    {
      InitializeComponent() ;
      FromToEdit.FirstConnectorThroughHeightText.Content = "First connector through height from Pass to Sensors" ;
      FromToEdit.FromToEditControlBorder.BorderThickness = new Thickness( 0 ) ;
      WindowStartupLocation = WindowStartupLocation.CenterScreen ;
      FromToEdit.DisplayUnitSystem = document.DisplayUnitSystem ;
      RangeRouteWithPassEdit.DisplayUnitSystem = document.DisplayUnitSystem ;
      
      UpdateProperties( propertyTypeList, properties ) ;
    }

    private void UpdateProperties( RoutePropertyTypeList propertyTypeList, RouteProperties properties )
    {
      FromToEdit.SetRouteProperties( propertyTypeList, properties ) ;
      FromToEdit.FromLocationTypeOrg = properties.FromFixedHeight?.Type ?? FixedHeightType.Floor ;
      FromToEdit.FromFixedHeightOrg = properties.FromFixedHeight?.Height ?? FromToEdit.GetFromDefaultHeight() ;
      FromToEdit.ResetDialog() ;
      RangeRouteWithPassEdit.SetRouteProperties( propertyTypeList, properties ) ;
      RangeRouteWithPassEdit.ResetDialog() ;
    }


    private void Dialog2Buttons_OnLeftOnClick( object sender, RoutedEventArgs e )
    {
      this.DialogResult = true ;
      this.Close() ;
    }

    private void Dialog2Buttons_OnRightOnClick( object sender, RoutedEventArgs e )
    {
      this.DialogResult = false ;
      this.Close() ;
    }

    public FixedHeight? GetPowerToPassFromFixedHeight()
    {
      if ( true != RangeRouteWithPassEdit.UseFromFixedHeight ) return null ;
      return FixedHeight.CreateOrNull( RangeRouteWithPassEdit.FromLocationType, RangeRouteWithPassEdit.FromFixedHeight ) ;
    }

    public MEPSystemType? GetSystemType() => FromToEdit.SystemType ;

    public MEPCurveType GetCurveType() => FromToEdit.CurveType ?? throw new InvalidOperationException() ;

    public double GetDiameter() => FromToEdit.Diameter ?? throw new InvalidOperationException() ;

    public bool GetRouteOnPipeSpace() => FromToEdit.IsRouteOnPipeSpace ?? throw new InvalidOperationException() ;

    public FixedHeight? GetFromFixedHeight()
    {
      if ( true != FromToEdit.UseFromFixedHeight ) return null ;
      return FixedHeight.CreateOrNull( FromToEdit.FromLocationType, FromToEdit.FromFixedHeight ) ;
    }
    
    public FixedHeight? GetToFixedHeight()
    {
      if ( true != FromToEdit.UseToFixedHeight ) return null ;
      return FixedHeight.CreateOrNull( FromToEdit.ToLocationType, FromToEdit.ToFixedHeight ) ;
    }

    public AvoidType GetAvoidType() => FromToEdit.AvoidType ?? throw new InvalidOperationException() ;

    public Opening? GetShaft()
    {
      return FromToEdit.Shaft ;
    }
  }
}