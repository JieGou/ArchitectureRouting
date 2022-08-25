using System ;
using System.Runtime.InteropServices ;
using System.Windows ;
using System.Drawing ;
using System.Windows.Input ;
using System.Windows.Interop ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class ModelessOkCancelDialog : Window
  {
    [DllImport( "user32.dll" )]
    private static extern bool SetForegroundWindow( IntPtr hWnd ) ;

    [DllImport( "user32.dll" )]
    private static extern void keybd_event( byte bVk, byte bScan, int dwFlags, int dwExtraInfo ) ;
    public bool IsCancel { get ; private set ; }

    public ModelessOkCancelDialog()
    {
      IsCancel = true ;
      InitializeComponent() ;
      var helper = new WindowInteropHelper( this ) { Owner = Autodesk.Windows.ComponentManager.ApplicationWindow } ;
    }
    
    private void OnClickFinish( object sender, RoutedEventArgs e )
    {
      IsCancel = false ;
      SendEscToRevit() ;
    }

    private void OnClickCancel( object sender, RoutedEventArgs e )
    {
      IsCancel = true ;
      SendEscToRevit() ;
    }

    private void SendEscToRevit()
    {
      FocusRevit() ;
      // create ESC key press and release event
      keybd_event( 0x1B, 0, 0, 0 ) ;
      keybd_event( 0x1B, 0, 2, 0 ) ;
    }
    
    public void FocusRevit()
    {
      IntPtr rvtPtr = Autodesk.Windows.ComponentManager.ApplicationWindow ;
      SetForegroundWindow( rvtPtr ) ;
    }

    public void AlignToView( UIView? uiView )
    {
      if ( uiView == null )
        return ;
      var rec = uiView.GetWindowRectangle() ;
      using ( Graphics g = Graphics.FromHwnd( IntPtr.Zero ) ) {
        Left = 96 / g.DpiX * rec.Left ;
        Top = 96 / g.DpiY * rec.Top ;
      }
    }

    private void OnKeyUp( object sender, KeyEventArgs e )
    {
      if(e.Key != Key.Enter)
        return;
      
      IsCancel = false ;
      SendEscToRevit() ;
    }
  }
}