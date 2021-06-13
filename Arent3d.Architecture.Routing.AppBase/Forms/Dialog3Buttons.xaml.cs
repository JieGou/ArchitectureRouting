using System.Windows ;
using System.Windows.Controls ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  /// <summary>
  /// Dialog3Buttons.xaml の相互作用ロジック
  /// </summary>
  public partial class Dialog3Buttons : UserControl
  {
    public string LeftButton
    {
        get { return (string)GetValue(LeftButtonProperty); }
        set { SetValue(LeftButtonProperty, value); }
    }

    public static readonly DependencyProperty LeftButtonProperty = DependencyProperty.Register("LeftButtont",
        typeof(string),
        typeof(Dialog3Buttons),
        new PropertyMetadata("OK"));

    public string CenterButton
    {
        get { return (string)GetValue(CenterButtonProperty); }
        set { SetValue(CenterButtonProperty, value); }
    }

    public static readonly DependencyProperty CenterButtonProperty = DependencyProperty.Register("CenterButton",
        typeof(string),
        typeof(Dialog3Buttons),
        new PropertyMetadata("Apply"));



        public string RightButton
    {
        get { return (string)GetValue(RightButtonProperty); }
        set { SetValue(RightButtonProperty, value); }
    }

    public static readonly DependencyProperty RightButtonProperty = DependencyProperty.Register("RightButton",
        typeof(string),
        typeof(Dialog3Buttons),
        new PropertyMetadata("Cancel"));


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