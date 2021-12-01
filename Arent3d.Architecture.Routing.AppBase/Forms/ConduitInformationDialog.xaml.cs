using System.Windows;
using Arent3d.Architecture.Routing.AppBase.ViewModel;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
    public partial class ConduitInformationDialog : Window
    {
        public ConduitInformationDialog(ConduitInformationViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
