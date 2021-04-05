using System.Windows ;
using System.Windows.Controls ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  public delegate void ClickEventHandler( object sender, RoutedEventArgs e ) ;

  public partial class Dilog2Buttons : UserControl
  {
    public Dilog2Buttons()
    {
      InitializeComponent() ;
    }

    public event ClickEventHandler? ImportOnClick ;

    public event ClickEventHandler? ExportOnClick ;

    private void Import_OnClick( object sender, RoutedEventArgs e )
    {
      if ( ImportOnClick != null ) {
        ImportOnClick( this, e ) ;
      }
    }

    private void Export_OnClick( object sender, RoutedEventArgs e )
    {
      if ( ExportOnClick != null ) {
        ExportOnClick( this, e ) ;
      }
    }
  }
}