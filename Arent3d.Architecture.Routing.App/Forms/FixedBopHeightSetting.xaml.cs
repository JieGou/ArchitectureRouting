using System ;
using System.Windows ;
using Arent3d.Architecture.Routing.App.ViewModel ;
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
    }

    private void Dialog2Buttons_OnLeftOnClick( object sender, RoutedEventArgs e )
    {
      //TaskDialog.Show( _route.RouteName, heightTextBox.Text ) ;
      FixedBopHeightViewModel.ApplyFixedBopHeightChange(Convert.ToInt32(heightTextBox.Text) );
    }

    private void Dialog2Buttons_OnRightOnClick( object sender, RoutedEventArgs e )
    {
      throw new System.NotImplementedException() ;
    }
  }
}