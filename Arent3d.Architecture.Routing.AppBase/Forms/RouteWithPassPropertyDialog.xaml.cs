using System ;
using Autodesk.Revit.DB ;
using System.Windows ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public interface IRouteWithPassPropertyDialog: IRoutePropertyDialog
  {
    MEPSystemType? GetPowerToPassSystemType() ;
    MEPCurveType GetPowerToPassCurveType() ;
    double GetPowerToPassDiameter() ;
    bool GetPowerToPassRouteOnPipeSpace() ;
    FixedHeight? GetPowerToPassFromFixedHeight() ;
    FixedHeight? GetPowerToPassToFixedHeight() ;
    AvoidType GetPowerToPassAvoidType() ;
    Opening? GetPowerToPassShaft() ;
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

    public RouteWithPassPropertyDialog( Document document, RoutePropertyTypeList powerToPassPropertyTypeList, RouteProperties powerToPassProperties, RoutePropertyTypeList passToSensorsPropertyTypeList, RouteProperties passToSensorsProperties )
    {
      InitializeComponent() ;
      WindowStartupLocation = WindowStartupLocation.CenterScreen ;
      PowerToPassEdit.DisplayUnitSystem = document.DisplayUnitSystem ;
      UpdateProperties( powerToPassPropertyTypeList, powerToPassProperties,passToSensorsPropertyTypeList, passToSensorsProperties ) ;
      // result.Add( ( nameBase + "PowerToPass", new RouteSegment( classificationInfo, systemType, curveType, powerConnectorEndPoint, passConnectorUpEndPoint, diameter, routeProperty.GetRouteOnPipeSpace(), routeProperty.GetFromFixedHeight(), sensorFixedHeight, avoidType, routeProperty.GetShaft()?.UniqueId ) ) ) ;
    }

    private void UpdateProperties( RoutePropertyTypeList powerToPassPropertyTypeList, RouteProperties powerToPassProperties, RoutePropertyTypeList passToSensorsPropertyTypeList, RouteProperties passToSensorsProperties )
    {
      PowerToPassEdit.SetRouteProperties( powerToPassPropertyTypeList, powerToPassProperties ) ;
      PassToSensorsEdit.SetRouteProperties( passToSensorsPropertyTypeList, passToSensorsProperties ) ;
      PowerToPassEdit.ResetDialog() ;
      PassToSensorsEdit.ResetDialog();
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

    public MEPSystemType? GetPowerToPassSystemType() => PowerToPassEdit.SystemType ;

    public MEPCurveType GetPowerToPassCurveType() => PowerToPassEdit.CurveType ?? throw new InvalidOperationException() ;

    public double GetPowerToPassDiameter() => PowerToPassEdit.Diameter ?? throw new InvalidOperationException() ;

    public bool GetPowerToPassRouteOnPipeSpace() => PowerToPassEdit.IsRouteOnPipeSpace ?? throw new InvalidOperationException() ;

    public FixedHeight? GetPowerToPassFromFixedHeight()
    {
      if ( true != PowerToPassEdit.UseFromFixedHeight ) return null ;
      return FixedHeight.CreateOrNull( PowerToPassEdit.FromLocationType, PowerToPassEdit.FromFixedHeight ) ;
    }
    
    public FixedHeight? GetPowerToPassToFixedHeight()
    {
      if ( true != PowerToPassEdit.UseToFixedHeight ) return null ;
      return FixedHeight.CreateOrNull( PowerToPassEdit.ToLocationType, PowerToPassEdit.ToFixedHeight ) ;
    }

    public AvoidType GetPowerToPassAvoidType() => PowerToPassEdit.AvoidType ?? throw new InvalidOperationException() ;

    public Opening? GetPowerToPassShaft()
    {
      return PowerToPassEdit.Shaft ;
    }
    
    
    
    public MEPSystemType? GetSystemType() => PassToSensorsEdit.SystemType ;

    public MEPCurveType GetCurveType() => PassToSensorsEdit.CurveType ?? throw new InvalidOperationException() ;

    public double GetDiameter() => PassToSensorsEdit.Diameter ?? throw new InvalidOperationException() ;

    public bool GetRouteOnPipeSpace() => PassToSensorsEdit.IsRouteOnPipeSpace ?? throw new InvalidOperationException() ;

    public FixedHeight? GetFromFixedHeight()
    {
      if ( true != PassToSensorsEdit.UseFromFixedHeight ) return null ;
      return FixedHeight.CreateOrNull( PassToSensorsEdit.FromLocationType, PassToSensorsEdit.FromFixedHeight ) ;
    }
    
    public FixedHeight? GetToFixedHeight()
    {
      if ( true != PassToSensorsEdit.UseToFixedHeight ) return null ;
      return FixedHeight.CreateOrNull( PassToSensorsEdit.ToLocationType, PassToSensorsEdit.ToFixedHeight ) ;
    }

    public AvoidType GetAvoidType() => PassToSensorsEdit.AvoidType ?? throw new InvalidOperationException() ;

    public Opening? GetShaft()
    {
      return PassToSensorsEdit.Shaft ;
    }
  }
}