using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Forms
{
    public class WindowBase : Window
    {
        public WindowBase(UIDocument uiDoc)
        {
            //Set RevitWindow To owner
            System.Windows.Interop.WindowInteropHelper? helper = new System.Windows.Interop.WindowInteropHelper(this);
            HwndSource? hwndSource = HwndSource.FromHwnd(uiDoc.Application.MainWindowHandle);
            if ( hwndSource != null ) {
                Window? wnd = hwndSource.RootVisual as Window;
                this.Owner = wnd ;
            }
            
        }
        public delegate void ClickEventHandler(object sender, RoutedEventArgs e);
    }
}
