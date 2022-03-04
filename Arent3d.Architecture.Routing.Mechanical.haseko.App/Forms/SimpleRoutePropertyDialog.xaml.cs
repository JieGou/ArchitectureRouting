using System ;
using Autodesk.Revit.DB ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Forms;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Forms
{
  /// <summary>
  /// SetProperty.xaml の相互作用ロジック
  /// </summary>
  public partial class SimpleRoutePropertyDialog : Window, IRoutePropertyDialog
  {
    private RoutePropertyTypeList? _propertyTypeList ;
    public SimpleRoutePropertyDialog()
    {
      InitializeComponent() ;
    }

    public SimpleRoutePropertyDialog( Document document, RoutePropertyTypeList propertyTypeList, RouteProperties properties)
    {
      InitializeComponent() ;
      WindowStartupLocation = WindowStartupLocation.CenterScreen ;
      FromToEdit.DisplayUnitSystem = document.DisplayUnitSystem ;
      _propertyTypeList = propertyTypeList ;
      UpdateProperties( _propertyTypeList, properties ) ;
    }

    private void UpdateProperties( RoutePropertyTypeList propertyTypeList, RouteProperties properties )
    {
      FromToEdit.SetRouteProperties( propertyTypeList, properties ) ;
      FromToEdit.ResetDialog() ;
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

    public MEPSystemType? GetSystemType() => FromToEdit.SystemType ;

    public MEPCurveType GetCurveType() => FromToEdit.CurveType ?? throw new InvalidOperationException() ;

    public double GetDiameter() => FromToEdit.Diameter ?? throw new InvalidOperationException() ;

    public bool GetRouteOnPipeSpace() => FromToEdit.IsRouteOnPipeSpace ?? throw new InvalidOperationException() ;

    public FixedHeight? GetFromFixedHeight()
    {
      if ( true != FromToEdit.UseFromFixedHeight || null == _propertyTypeList) return null ;

      var fromFixedHeight = FromToEdit.FromFixedHeight + _propertyTypeList.FromHeightRangeAsCeilingLevel.Item2 ;
      return FixedHeight.CreateOrNull( FromToEdit.FromLocationType, fromFixedHeight ) ;
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