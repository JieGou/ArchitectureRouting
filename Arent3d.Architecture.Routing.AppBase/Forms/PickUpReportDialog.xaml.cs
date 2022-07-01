using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Windows ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using NPOI.XSSF.UserModel ;
using NPOI.SS.UserModel ;
using NPOI.SS.Util ;
using BorderStyle = NPOI.SS.UserModel.BorderStyle ;
using CheckBox = System.Windows.Controls.CheckBox ;
using MessageBox = System.Windows.Forms.MessageBox ;
using RadioButton = System.Windows.Controls.RadioButton ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class PickUpReportDialog : Window
  {
    private PickUpReportViewModel ViewModel => (PickUpReportViewModel)DataContext ;
    
    public PickUpReportDialog(PickUpReportViewModel viewModel )
    {
      InitializeComponent() ;
      DataContext = viewModel ;
    }
    
    private void DoconItem_Checked( object sender, RoutedEventArgs e )
    {
      ViewModel.DoconItemChecked( sender );
    }
    
    private void FileType_Checked( object sender, RoutedEventArgs e )
    {
      ViewModel.FileTypeChecked( sender );
    }
    
    private void FileType_Unchecked( object sender, RoutedEventArgs e )
    {
      ViewModel.FileTypeChecked( sender );
    }
  }

  public class DesignPickUpReportViewModel : PickUpReportViewModel
  {
    public DesignPickUpReportViewModel( Document document ) : base( default! )
    {
    }
  }
}