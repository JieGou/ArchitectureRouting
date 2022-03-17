using System ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.Forms
{
  public interface IRouteWithPassPropertyDialog : IRoutePropertyDialog
  {
    FixedHeight? GetPowerToPassFromFixedHeight() ;
  }

  /// <summary>
  /// SetProperty.xaml の相互作用ロジック
  /// </summary>
  public partial class RouteWithPassPropertyDialog : IRouteWithPassPropertyDialog
  {
    public RouteWithPassPropertyDialog()
    {
      InitializeComponent() ;
    }

    public RouteWithPassPropertyDialog( Document document, RoutePropertyTypeList propertyTypeList, RouteProperties properties )
    {
      InitializeComponent() ;
      WindowStartupLocation = WindowStartupLocation.CenterScreen ;
      RangeRouteWithPassEdit.DisplayUnitSystem = document.DisplayUnitSystem ;
      UpdateProperties( propertyTypeList, properties ) ;
    }

    private void UpdateProperties( RoutePropertyTypeList propertyTypeList, RouteProperties properties )
    {
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
      if ( true != RangeRouteWithPassEdit.UseFromPowerToPassFixedHeight ) return null ;
      return FixedHeight.CreateOrNull( RangeRouteWithPassEdit.FromPowerToPassLocationType, RangeRouteWithPassEdit.FromPowerToPassFixedHeight ) ;
    }

    public MEPSystemType? GetSystemType() => RangeRouteWithPassEdit.SystemType ;

    public MEPCurveType GetCurveType() => RangeRouteWithPassEdit.CurveType ?? throw new InvalidOperationException() ;

    public double GetDiameter() => RangeRouteWithPassEdit.Diameter ?? throw new InvalidOperationException() ;

    public bool GetRouteOnPipeSpace() => RangeRouteWithPassEdit.IsRouteOnPipeSpace ?? throw new InvalidOperationException() ;

    public FixedHeight? GetFromFixedHeight()
    {
      if ( true != RangeRouteWithPassEdit.UseFromFixedHeight ) return null ;
      return FixedHeight.CreateOrNull( RangeRouteWithPassEdit.FromLocationType, RangeRouteWithPassEdit.FromFixedHeight ) ;
    }

    public FixedHeight? GetToFixedHeight()
    {
      if ( true != RangeRouteWithPassEdit.UseToFixedHeight ) return null ;
      return FixedHeight.CreateOrNull( RangeRouteWithPassEdit.ToLocationType, RangeRouteWithPassEdit.ToFixedHeight ) ;
    }

    public AvoidType GetAvoidType() => RangeRouteWithPassEdit.AvoidType ?? throw new InvalidOperationException() ;

    public Opening? GetShaft()
    {
      return RangeRouteWithPassEdit.Shaft ;
    }
  }
}