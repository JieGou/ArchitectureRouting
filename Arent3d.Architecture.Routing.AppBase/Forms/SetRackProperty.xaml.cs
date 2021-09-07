using Autodesk.Revit.DB ;
using ControlLib ;
using System.Collections.Generic ;
using System.Windows ;


namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  /// <summary>
  /// SetProperty.xaml の相互作用ロジック
  /// </summary>
  public partial class SetRackProperty : Window
  {
    public double FixedHeight { get ; set ; }
    public double FixedThickness { get ; set ; }

    public SetRackProperty()
    {
      InitializeComponent() ;
    }

    public void UpdateParameters( double fixedHeight, double fixedThickness )
    {
      HeightNud.Value = fixedHeight ;
      ThicknessNud.Value = fixedThickness ;
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

    private void Dialog2Buttons_Loaded( object sender, RoutedEventArgs e )
    {
    }

    private void HeightNud_OnValueChanged( object sender, ValueChangedEventArgs e )
    {
      FixedHeight = HeightNud.Value ;
    }

    private void ThicknessNud_OnValueChanged( object sender, ValueChangedEventArgs e )
    {
      FixedThickness = ThicknessNud.Value ;
    }
  }
}