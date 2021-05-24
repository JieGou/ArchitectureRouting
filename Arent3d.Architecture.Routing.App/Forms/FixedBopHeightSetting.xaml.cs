using System ;
using System.Globalization ;
using System.Windows ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  public partial class FixedBopHeightSetting : WindowBase
  {
    public UIDocument? UiDoc;
    private Route _route ;

    public FixedBopHeightSetting(UIDocument uiDoc, Route selectedRoute) : base(uiDoc)
    {
      InitializeComponent() ;
      this.UiDoc = uiDoc ;
      _route = selectedRoute ;

      if ( _route.FirstFromConnector()?.GetConnector() is { } connector ) {
        if ( connector.Owner.Document.GetElementById<Level>( connector.Owner.LevelId ) is {} level ) {
          var heightFromFl = connector.Origin.Z - level.Elevation ;
          HeightTextBox.Text = heightFromFl.RevitUnitsToMillimeters().ToString( CultureInfo.InvariantCulture ) ;
        }
      }
      else {
        HeightTextBox.Text = 0.ToString() ;
      }
    }

    private void Dialog2Buttons_OnLeftOnClick( object sender, RoutedEventArgs e )
    {
      FixedBopHeightViewModel.ApplyFixedBopHeightChange(Convert.ToDouble(HeightTextBox.Text) );
    }

    private void Dialog2Buttons_OnRightOnClick( object sender, RoutedEventArgs e )
    {
      Close();
    }
  }
}