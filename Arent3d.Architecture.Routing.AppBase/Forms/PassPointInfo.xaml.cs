using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
    /// <summary>
    /// PassPointInfo.xaml 的交互逻辑
    /// </summary>
    public partial class PassPointInfo : UserControl
    {
        public string XPoint
        {
            get { return (string)GetValue(XPointProperty); }
            set { SetValue(XPointProperty, value); }
        }

        public string YPoint
        {
            get { return (string)GetValue(YPointProperty); }
            set { SetValue(YPointProperty, value); }
        }

        public string ZPoint
        {
            get { return (string)GetValue(ZPointProperty); }
            set { SetValue(ZPointProperty, value); }
        }
        public static readonly DependencyProperty XPointProperty = DependencyProperty.Register("XPoint",
                                    typeof(string),
                                    typeof(PassPointInfo),
                                 new PropertyMetadata("1"));
        public static readonly DependencyProperty YPointProperty = DependencyProperty.Register("YPoint",
                            typeof(string),
                            typeof(PassPointInfo),
                         new PropertyMetadata("2"));
        public static readonly DependencyProperty ZPointProperty = DependencyProperty.Register("ZPoint",
                            typeof(string),
                            typeof(PassPointInfo),
                         new PropertyMetadata("3"));
        public PassPointInfo()
        {
            InitializeComponent();
            
        }
    }
}
