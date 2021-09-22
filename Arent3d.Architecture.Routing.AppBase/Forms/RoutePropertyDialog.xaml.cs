using System ;
using Autodesk.Revit.DB ;
using System.Windows ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  /// <summary>
  /// SetProperty.xaml の相互作用ロジック
  /// </summary>
  public partial class RoutePropertyDialog : Window
  {
    public RoutePropertyDialog()
    {
      InitializeComponent() ;
    }

    public RoutePropertyDialog( Document document, RoutePropertyTypeList propertyTypeList, RouteProperties properties )
    {
      InitializeComponent() ;

      FromToEdit.DisplayUnitSystem = document.DisplayUnitSystem ;
      UpdateProperties( propertyTypeList, properties ) ;
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

    public MEPSystemType? GetSelectSystemType() => FromToEdit.SystemType ;

    public MEPCurveType GetSelectCurveType() => FromToEdit.CurveType ?? throw new InvalidOperationException() ;

    public double GetSelectDiameter() => FromToEdit.Diameter ?? throw new InvalidOperationException() ;

    public bool GetRouteOnPipeSpace() => FromToEdit.IsRouteOnPipeSpace ?? throw new InvalidOperationException() ;

    public double? GetFixedHeight()
    {
      if ( true != FromToEdit.UseFixedHeight ) return null ;
      return FromToEdit.FixedHeight ;
    }

    public AvoidType GetSelectedAvoidType() => FromToEdit.AvoidType ?? throw new InvalidOperationException() ;

    public ElementId GetShaftElementId()
    {
      // TODO
      return ElementId.InvalidElementId ;
    }
  }
}