using System.Windows ;
using System.Windows.Controls ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  /// <summary>
  /// Dialog3Buttons.xaml の相互作用ロジック
  /// </summary>
  public partial class Dialog3Buttons : UserControl
  {
    public Dialog3Buttons()
    {
      InitializeComponent() ;
    }

    public event ClickEventHandler? OnOKClick ;
    public event ClickEventHandler? OnApplyClick ;
    public event ClickEventHandler? OnCancelClick ;

    private void Apply_Click( object sender, RoutedEventArgs e )
    {
      if ( OnApplyClick != null ) {
        OnApplyClick( this, e ) ;
      }
    }

    private void OK_Click( object sender, RoutedEventArgs e )
    {
      if ( OnOKClick != null ) {
        OnOKClick( this, e ) ;
      }
    }

    private void Cancel_Click( object sender, RoutedEventArgs e )
    {
      if ( OnCancelClick != null ) {
        OnCancelClick( this, e ) ;
      }
    }
  }
}