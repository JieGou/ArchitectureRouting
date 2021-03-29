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

namespace Arent3d.Architecture.Routing.App.Forms
{
    //public delegate void ClickEventHandlerButton(object sender, RoutedEventArgs e);

    /// <summary>
    /// Dialog3Buttons.xaml の相互作用ロジック
    /// </summary>
    public partial class Dialog3Buttons : UserControl
    {
        public Dialog3Buttons()
        {
            InitializeComponent();
        }

        public event ClickEventHandler? OnOKClick;
        public event ClickEventHandler? OnApplyClick;
        public event ClickEventHandler? OnCancelClick;

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (OnApplyClick != null)
            {
                OnApplyClick(this, e);
            }

        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if(OnOKClick != null)
            {
                OnOKClick(this, e);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (OnCancelClick != null)
            {
                OnCancelClick(this, e);
            }
        }
    }
}
