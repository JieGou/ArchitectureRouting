using System ;
using Autodesk.Revit.DB ;
using System.Windows ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  /// <summary>
  /// SetProperty.xaml の相互作用ロジック
  /// </summary>
  public partial class AutoVavRoutePropertyDialog
  {
    public AutoVavRoutePropertyDialog()
    {
      InitializeComponent() ;
    }

    public AutoVavRoutePropertyDialog( Document document, RoutePropertyTypeList propertyTypeList, RouteProperties properties )
    {
      InitializeComponent() ;
      // Automatically resize height and width relative to content
      SizeToContent = SizeToContent.WidthAndHeight ; 
      WindowStartupLocation = WindowStartupLocation.CenterScreen ;
      AutoVavEdit.DisplayUnitSystem = document.DisplayUnitSystem ;
      UpdateProperties( propertyTypeList, properties ) ;
    }

    private void UpdateProperties( RoutePropertyTypeList propertyTypeList, RouteProperties properties )
    {
      AutoVavEdit.SetRouteProperties( propertyTypeList, properties ) ;
      AutoVavEdit.ResetDialog() ;
    }


    private void Dialog2Buttons_OnLeftOnClick( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void Dialog2Buttons_OnRightOnClick( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
      Close() ;
    }

    public MEPSystemType? GetSystemType() => AutoVavEdit.SystemType ;

    public MEPCurveType GetCurveType() => AutoVavEdit.CurveType ?? throw new InvalidOperationException() ;

    public double GetDiameter() => AutoVavEdit.Diameter ?? throw new InvalidOperationException() ;

    public bool GetRouteOnPipeSpace() => AutoVavEdit.IsRouteOnPipeSpace ?? throw new InvalidOperationException() ;

    public FixedHeight? GetFromFixedHeight()
    {
      if ( true != AutoVavEdit.UseFromFixedHeight ) return null ;
      return FixedHeight.CreateOrNull( AutoVavEdit.FromLocationType, AutoVavEdit.FromFixedHeight ) ;
    }

    public AvoidType GetAvoidType() => AutoVavEdit.AvoidType ?? throw new InvalidOperationException() ;

    public Opening? GetShaft()
    {
      return AutoVavEdit.Shaft ;
    }
    
  }
}