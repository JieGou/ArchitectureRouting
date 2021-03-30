using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arent3d.Architecture.Routing.App.Forms
{
    public class WindowBase : Window
    {
        public delegate void ClickEventHandler(object sender, RoutedEventArgs e);
    }
}
