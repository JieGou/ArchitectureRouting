using System ;
using System.Windows ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Revit.UI ;
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
      FixedBopHeightViewModel.ApplyFixedBopHeightChange(Convert.ToDouble(heightTextBox.Text) );
    }

    private void Dialog2Buttons_OnRightOnClick( object sender, RoutedEventArgs e )
    {
      Close();
    }

    private void OkButton_OnClick( object sender, RoutedEventArgs e )
    {
      FixedBopHeightViewModel.ApplyFixedBopHeightChange(Convert.ToDouble(heightTextBox.Text) );
    }
  }
}