using System.Windows;
using Arent3d.Architecture.Routing.AppBase.ViewModel;
using Autodesk.Revit.DB;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
    public partial class DisplaySettingDialog : Window
    {
        public DisplaySettingDialog(DisplaySettingViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        private DisplaySettingViewModel ViewModel => (DisplaySettingViewModel)DataContext;
    }

    public class DesignDisplaySettingViewModel : DisplaySettingViewModel
    {
        public DesignDisplaySettingViewModel(Document document) : base(default!)
        {
        }
    }
}