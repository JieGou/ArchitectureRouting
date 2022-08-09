using System ;
using System.Runtime.InteropServices ;
using System.Windows ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class Ending_Buttons : Window
  {
    public bool isCancel ;
    public Ending_Buttons()
    {
      isCancel = true ;
      InitializeComponent() ;
    }
    
    

    private void OnClickFinish( object sender, RoutedEventArgs e )
    {
      isCancel = false ;
      SendEscToRevit() ;
    }

    private void OnClickCancel( object sender, RoutedEventArgs e )
    {
      isCancel = true ;
      SendEscToRevit() ;
    }
    
    private void SendEscToRevit()
    {
      FocusRevit();
      keybd_event(0x1B, 0, 0, 0);
      keybd_event(0x1B, 0, 2, 0);
    }
    
    [DllImport("user32.dll")]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    internal static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

    public void FocusRevit()
    {
      IntPtr rvtPtr = Autodesk.Windows.ComponentManager.ApplicationWindow;
      SetForegroundWindow(rvtPtr);
    }
  }
}