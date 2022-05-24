using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Windows ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit.I18n ;
using MessageBox = System.Windows.MessageBox ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class ElectricalSymbolAggregationViewModel : NotifyPropertyChanged
  {
    public RelayCommand<Window> ExportCsvCommand => new(ExportCsv) ;
    public RelayCommand<Window> CancelCommand => new(Cancel) ;

    public List<ElectricalSymbolAggregationModel> ElectricalSymbolAggregationList { get ; set ; }

    public ElectricalSymbolAggregationViewModel( List<ElectricalSymbolAggregationModel> electricalSymbolAggregationList )
    {
      ElectricalSymbolAggregationList = electricalSymbolAggregationList ;
    }
    
    private void Cancel( Window window )
    {
      window.DialogResult = false ;
      window.Close() ;
    }

    private void ExportCsv( Window window )
    {
      const string fileName = "file_name.dat" ;
      SaveFileDialog saveFileDialog = new SaveFileDialog { FileName = fileName, Filter = "CSV files (*.dat)|*.dat", InitialDirectory = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ) } ;

      if ( saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK ) return ;
      try {
        using ( StreamWriter sw = new StreamWriter( saveFileDialog.FileName ) ) {
          foreach ( var p in ElectricalSymbolAggregationList ) {
            List<string> param = new() { p.ProductCode, p.ProductName, p.Number.ToString(), p.Unit } ;
            string line = "\"" + string.Join( "\",\"", param ) + "\"" ;
            sw.WriteLine( line ) ;
          }

          sw.Flush() ;
          sw.Close() ;
        }

        MessageBox.Show( "Export data successfully.", "Result Message" ) ;
      }
      catch ( Exception ex ) {
        MessageBox.Show( "Export data failed because " + ex, "Error Message" ) ;
      }

      window.DialogResult = true ;
      window.Close() ;
    }
  }
}