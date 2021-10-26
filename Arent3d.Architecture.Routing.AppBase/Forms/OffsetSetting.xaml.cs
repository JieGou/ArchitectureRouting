using System.Windows ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  /// <summary>
  ///   SetProperty.xaml の相互作用ロジック
  /// </summary>
  public partial class OffsetSetting : Window
  {
    public OffsetSetting()
    {
      InitializeComponent() ;
    }

    public OffsetSetting( Document document )
    {
      InitializeComponent() ;
      WindowStartupLocation = WindowStartupLocation.CenterScreen ;
    }

    private void OffsetButtons_OnLeftOnClick( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void OffsetButtons_OnRightOnClick( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
      Close() ;
    }
  }
}