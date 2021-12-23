using System ;
using System.Collections.Generic ;
using System.Diagnostics ;
using System.Drawing.Imaging ;
using System.Globalization ;
using System.IO ;
using System.Linq ;
using System.Net ;
using System.Runtime.InteropServices ;
using System.Runtime.Serialization.Formatters.Binary ;
using System.Text ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Forms ;
using System.Windows.Input ;
using System.Windows.Media.Imaging ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Microsoft.Office.Interop.Excel ;
using NPOI.SS.UserModel ;
using NPOI.XSSF.UserModel ;
using Application = Microsoft.Office.Interop.Excel.Application ;
using CellType = NPOI.SS.UserModel.CellType ;
using Clipboard = System.Windows.Forms.Clipboard ;
using Image = System.Drawing.Image ;
using MessageBox = System.Windows.MessageBox ;
using Style = System.Windows.Style ;
using Window = System.Windows.Window ;
using Worksheet = Microsoft.Office.Interop.Excel.Worksheet ;


namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CeeDModelDialog : Window
  {
    private readonly Document _document ;
    private CeedViewModel? _allCeeDModels ;
    private string _ceeDModelNumberSearch ;
    private string _modelNumberSearch ;
    public string SelectedSetCode ;

    private void Row_DoubleClick( object sender, DataGridViewCellEventArgs e )
    {
      MessageBox.Show( e.RowIndex.ToString() ) ;
    }

    public CeeDModelDialog( Document document )
    {
      InitializeComponent() ;
      _document = document ;
      _allCeeDModels = null ;
      _ceeDModelNumberSearch = string.Empty ;
      _modelNumberSearch = string.Empty ;
      SelectedSetCode = string.Empty ;

      var oldCeeDStorable = _document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( oldCeeDStorable != null ) {
        LoadData( oldCeeDStorable ) ;
      }

      Style rowStyle = new Style( typeof( DataGridRow ) ) ;
      rowStyle.Setters.Add( new EventSetter( DataGridRow.MouseDoubleClickEvent, new MouseButtonEventHandler( Row_DoubleClick ) ) ) ;
      DtGrid.RowStyle = rowStyle ;
    }

    private void Row_DoubleClick( object sender, MouseButtonEventArgs e )
    {
      var selectedItem = (CeedModel) DtGrid.SelectedValue ;
      SelectedSetCode = selectedItem.CeeDSetCode ;
      DialogResult = true ;
      Close() ;
    }

    private void Button_Click( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void Button_Reset( object sender, RoutedEventArgs e )
    {
      this.DataContext = _allCeeDModels ;
      CmbCeeDModelNumbers.SelectedIndex = -1 ;
      CmbCeeDModelNumbers.Text = "" ;
      CmbModelNumbers.SelectedIndex = -1 ;
      CmbModelNumbers.Text = "" ;
    }

    private void CmbCeeDModelNumbers_TextChanged( object sender, TextChangedEventArgs e )
    {
      _ceeDModelNumberSearch = ! string.IsNullOrEmpty( CmbCeeDModelNumbers.Text ) ? CmbCeeDModelNumbers.Text : string.Empty ;
    }

    private void CmbModelNumbers_TextChanged( object sender, TextChangedEventArgs e )
    {
      _modelNumberSearch = ! string.IsNullOrEmpty( CmbModelNumbers.Text ) ? CmbModelNumbers.Text : string.Empty ;
    }

    private void Button_Search( object sender, RoutedEventArgs e )
    {
      if ( _allCeeDModels == null ) return ;
      if ( string.IsNullOrEmpty( _ceeDModelNumberSearch ) && string.IsNullOrEmpty( _modelNumberSearch ) ) {
        this.DataContext = _allCeeDModels ;
      }
      else {
        List<CeedModel> ceeDModels = new List<CeedModel>() ;
        switch ( string.IsNullOrEmpty( _ceeDModelNumberSearch ) ) {
          case false when ! string.IsNullOrEmpty( _modelNumberSearch ) :
            ceeDModels = _allCeeDModels.CeedModels.Where( c => c.CeeDModelNumber.Contains( _ceeDModelNumberSearch ) && c.ModelNumber.Contains( _modelNumberSearch ) ).ToList() ;
            break ;
          case false when string.IsNullOrEmpty( _modelNumberSearch ) :
            ceeDModels = _allCeeDModels.CeedModels.Where( c => c.CeeDModelNumber.Contains( _ceeDModelNumberSearch ) ).ToList() ;
            break ;
          case true when ! string.IsNullOrEmpty( _modelNumberSearch ) :
            ceeDModels = _allCeeDModels.CeedModels.Where( c => c.ModelNumber.Contains( _modelNumberSearch ) ).ToList() ;
            break ;
        }

        CeedViewModel ceeDModelsSearch = new CeedViewModel( _allCeeDModels.CeedStorable, ceeDModels ) ;
        this.DataContext = ceeDModelsSearch ;
      }
    }

    private void Button_LoadData( object sender, RoutedEventArgs e )
    {
      OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Csv files (*.xlsx)|*.xlsx", Multiselect = false } ;
      string filePath = string.Empty ;
      if ( openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
        filePath = openFileDialog.FileName ;
      }

      var listHeimenZu = GetListHeimenZu( filePath ) ;
      var listKeisoZu = new List<SymbolBytes>() ;// GetListKeisoZu( filePath ) ;
      
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      CeedStorable ceeDStorable = _document.GetCeeDStorable() ;
      {
        List<CeedModel> ceeDModelData = GetAllCeeDModelNumber( filePath,listHeimenZu,listKeisoZu ) ;
        if ( ! ceeDModelData.Any() ) return ;
        ceeDStorable.CeedModelData = ceeDModelData ;
        LoadData( ceeDStorable ) ;
      
        try {
          using Transaction t = new Transaction( _document, "Save data" ) ;
          t.Start() ;
          ceeDStorable.Save() ;
          t.Commit() ;
        }
        catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        }
      }
    }

   private List<SymbolBytes> GetListHeimenZu( string filePath )
    {
      Stopwatch stopWatch = new Stopwatch();
      stopWatch.Start();
      string workbookPath = filePath ;
      var sheetName = "セットコード一覧表" ;
      var listHeimenZu = new List<SymbolBytes>() ;
      Application? excelApp = new Application() ;
      var excelWorkbook = excelApp.Workbooks.Open( workbookPath, Type.Missing, false, Type.Missing, Type.Missing, Type.Missing, false, XlPlatform.xlWindows, Type.Missing, true, false, Type.Missing, Type.Missing, Type.Missing, Type.Missing ) ;
      try {
        if ( excelWorkbook != null ) {
          Worksheet sheet = (Worksheet) excelWorkbook.Sheets[ sheetName ] ;
          var xlSheets = excelWorkbook.Sheets as Sheets ;
          var newSheet = (Worksheet) xlSheets.Add( xlSheets[ 1 ], Type.Missing, Type.Missing, Type.Missing ) ;
          newSheet.Name = "newsheet" ;
          var shapes = new List<Shape>() ;
          Range xlRange = sheet.UsedRange ;
          var endRow = xlRange.Rows.Count ;
          
          for ( var i = 8 ; i <= endRow ; i++ ) {
            // var cellValue = (sheet.Cells[i, 1] as Range)?.Value.ToString();
            // if(string.IsNullOrEmpty(cellValue)) break;
            
            Range cell = (Range) sheet.Cells[ i, 3 ] ; //一般表示用機器記号
            if ( cell.Value == null ) continue ;
            var strCellValue = cell.Value.ToString() ;
            if ( string.IsNullOrEmpty( strCellValue ) ) continue ;
            var firstIndexGroup = i ;
            var nextName = cell.Value.ToString() ;
            do {
              i++ ;
              if ( i > endRow ) break ;
              //strCellValue = nextName ;
              cell = (Range) sheet.Cells[ i + 1, 3 ] ;
              if ( cell.Value != null ) {
                nextName = cell.Value.ToString() ;
                break ;
              }
            } while ( ! string.IsNullOrEmpty( nextName ) ) ;

            var lastIndexGroup = i ;
            
            //COPY SHAPE TO NEW SHEET
            var range1 = sheet.Range[ "F" + firstIndexGroup, "F" + lastIndexGroup ] ;
            //Range range2 = newSheet.get_Range("A1", "A"+(lastIndexGroup-firstIndexGroup).ToString());
            Range range2 = newSheet.get_Range( "F" + firstIndexGroup, "F" + lastIndexGroup ) ;
            range1.Copy( range2 ) ;
            //CONVERT SHARP TO BYTE 
            if ( newSheet.Shapes.Count > 0 ) {
              for ( int j = 1 ; j <= newSheet.Shapes.Count ; j++ ) {
                var shape = newSheet.Shapes.Item( j ) ;
                shape.Copy() ;
                if ( ! Clipboard.ContainsImage() ) continue ;
                var image = Clipboard.GetImage() ;
                if ( image == null ) continue ;
                using ( var ms = new MemoryStream() ) {
                  ms.Position = 0 ;
                  image.Save( ms, ImageFormat.Png ) ;
                  var   imgData = ms.ToArray();
                  listHeimenZu.Add( new SymbolBytes( firstIndexGroup, imgData,1 ) ) ;
                }
                Clipboard.Clear() ;
              }
            }
            foreach ( Shape s in newSheet.Shapes ) {
              s.Delete();
            }
            // else {
            //   var symbols = ((Array)range2.Value) ;
            //   string symbol = string.Empty ;
            //   foreach (object s in symbols) {
            //     if(s!=null) symbol += s.ToString() ;
            //   }
            //   listHeimenZu.Add( new SymbolBytes( firstIndexGroup, Encoding.ASCII.GetBytes( symbol ), 2 ) ) ;
            // }

           range2.Delete() ;
          }
        }
      }
      catch ( Exception e ) {
        Console.WriteLine( e ) ;
      }
      finally {
        //excelWorkbook?.Save() ;
        excelWorkbook?.Close( false ) ;
        excelApp.Quit() ;
        if ( excelWorkbook != null ) Marshal.ReleaseComObject( excelWorkbook ) ;
        Marshal.ReleaseComObject( excelApp ) ;
      }
      stopWatch.Stop();
     TimeSpan ts = stopWatch.Elapsed;
     // MessageBox.Show( listHeimenZu.Count +"time: "+ts.TotalSeconds) ;
      return listHeimenZu ;
    }
 private List<SymbolBytes> GetListKeisoZu( string filePath )
    {
      Stopwatch stopWatch = new Stopwatch();
      stopWatch.Start();
      string workbookPath = filePath ;
      var sheetName = "セットコード一覧表" ;
      var listKeisoZu = new List<SymbolBytes>() ;
      Application? excelApp = new Application() ;
      var excelWorkbook = excelApp.Workbooks.Open( workbookPath, Type.Missing, false, Type.Missing, Type.Missing, Type.Missing, false, XlPlatform.xlWindows, Type.Missing, true, false, Type.Missing, Type.Missing, Type.Missing, Type.Missing ) ;
      try {
        if ( excelWorkbook != null ) {
          Worksheet sheet = (Worksheet) excelWorkbook.Sheets[ sheetName ] ;
          var xlSheets = excelWorkbook.Sheets as Sheets ;
          var newSheet = (Worksheet) xlSheets.Add( xlSheets[ 1 ], Type.Missing, Type.Missing, Type.Missing ) ;
          newSheet.Name = "newsheet" ;
          var shapes = new List<Shape>() ;
          Range xlRange = sheet.UsedRange ;
          var endRow = xlRange.Rows.Count ;
          
          for ( var i = 8 ; i <= endRow ; i++ ) {
            // var cellValue = (sheet.Cells[i, 1] as Range)?.Value.ToString();
            // if(string.IsNullOrEmpty(cellValue)) break;
            
            Range cell = (Range) sheet.Cells[ i, 3 ] ; //一般表示用機器記号
            if ( cell.Value == null ) continue ;
            var strCellValue = cell.Value.ToString() ;
            if ( string.IsNullOrEmpty( strCellValue ) ) continue ;
            var firstIndexGroup = i ;
            var nextName = cell.Value.ToString() ;
            do {
              i++ ;
              if ( i > endRow ) break ;
              //strCellValue = nextName ;
              cell = (Range) sheet.Cells[ i + 1, 3 ] ;
              if ( cell.Value != null ) {
                nextName = cell.Value.ToString() ;
                break ;
              }
            } while ( ! string.IsNullOrEmpty( nextName ) ) ;

            var lastIndexGroup = i ;
            
            //COPY SHAPE TO NEW SHEET
            var range1 = sheet.Range[ "G" + firstIndexGroup, "G" + lastIndexGroup ] ;
            //Range range2 = newSheet.get_Range("A1", "A"+(lastIndexGroup-firstIndexGroup).ToString());
            Range range2 = newSheet.get_Range( "G" + firstIndexGroup, "G" + lastIndexGroup ) ;
            range1.Copy( range2 ) ;
            //CONVERT SHARP TO BYTE 
            if ( newSheet.Shapes.Count > 0 ) {
              for ( int j = 1 ; j <= newSheet.Shapes.Count ; j++ ) {
                var shape = newSheet.Shapes.Item( j ) ;
                shape.Copy() ;
                if ( ! Clipboard.ContainsImage() ) continue ;
                var image = Clipboard.GetImage() ;
                if ( image == null ) continue ;
                using ( var ms = new MemoryStream() ) {
                  ms.Position = 0 ;
                  image.Save( ms, ImageFormat.Png ) ;
                  var   imgData = ms.ToArray();
                  listKeisoZu.Add( new SymbolBytes( firstIndexGroup, imgData,1 ) ) ;
                }
                Clipboard.Clear() ;
              }
            }
            foreach ( Shape s in newSheet.Shapes ) {
              s.Delete();
            }
            // else {
            //   var symbols = ((Array)range2.Value) ;
            //   string symbol = string.Empty ;
            //   foreach (object s in symbols) {
            //     if(s!=null) symbol += s.ToString() ;
            //   }
            //   listHeimenZu.Add( new SymbolBytes( firstIndexGroup, Encoding.ASCII.GetBytes( symbol ), 2 ) ) ;
            // }

           range2.Delete() ;
          }
        }
      }
      catch ( Exception e ) {
        Console.WriteLine( e ) ;
      }
      finally {
        //excelWorkbook?.Save() ;
        excelWorkbook?.Close( false ) ;
        excelApp.Quit() ;
        if ( excelWorkbook != null ) Marshal.ReleaseComObject( excelWorkbook ) ;
        Marshal.ReleaseComObject( excelApp ) ;
      }
     //  stopWatch.Stop();
     // TimeSpan ts = stopWatch.Elapsed;
     //  MessageBox.Show( listKeisoZu.Count +"time: "+ts.TotalSeconds) ;
      return listKeisoZu ;
    }
    private void LoadData( CeedStorable ceeDStorable )
    {
      var viewModel = new ViewModel.CeedViewModel( ceeDStorable ) ;
      this.DataContext = viewModel ;
      _allCeeDModels = viewModel ;
      CmbCeeDModelNumbers.ItemsSource = viewModel.CeeDModelNumbers ;
      CmbModelNumbers.ItemsSource = viewModel.ModelNumbers ;
    }

    private static List<CeedModel> GetAllCeeDModelNumber( string path,List<SymbolBytes> listHeimenZu, List<SymbolBytes> listKeisoZu)
    {
      List<CeedModel> ceedModelData = new List<CeedModel>() ;

      try {
        FileStream fs = new FileStream( path, FileMode.Open, FileAccess.Read ) ;
        XSSFWorkbook wb = new XSSFWorkbook( fs ) ;
        ISheet workSheet = wb.NumberOfSheets < 2 ? wb.GetSheetAt( wb.ActiveSheetIndex ) : wb.GetSheetAt( 1 ) ;

        XSSFDrawing drawing = (XSSFDrawing) workSheet.DrawingPatriarch ;
       
        const int startRow = 7 ;
        var endRow = workSheet.LastRowNum ;
        for ( var i = startRow ; i <= endRow ; i++ ) {
          List<string> ceeDModelNumbers = new List<string>() ;
          List<string> ceeDSetCodes = new List<string>() ;
          List<string> modelNumbers = new List<string>() ;
          string generalDisplayDeviceSymbols = string.Empty ;
          string floorPlanSymbol = string.Empty ;
          string instrumentationSymbol = string.Empty ;
          string ceeDName = string.Empty ;

          var record = workSheet.GetRow( i ).GetCell( 3 ) ;
          if ( record == null || record.CellStyle.IsHidden ) continue ;
          var name = GetCellValue( record ) ;
          if ( string.IsNullOrEmpty( name ) ) continue ;
          var firstIndexGroup = i ;
          var nextName = GetCellValue( record ) ;
          do {
            i++ ;
            if ( i > endRow ) break ;
            name = nextName ;
            record = workSheet.GetRow( i ).GetCell( 3 ) ;
            if ( record == null ) break ;
            nextName = GetCellValue( record ) ;
          } while ( ! ( string.IsNullOrEmpty( name ) && ! string.IsNullOrEmpty( nextName ) ) ) ;

          var lastIndexGroup = i ;
          for ( var j = firstIndexGroup ; j < lastIndexGroup ; j++ ) {
            var ceeDSetCodeCell = workSheet.GetRow( j ).GetCell( 0 ) ;
            var ceeDSetCode = GetCellValue( ceeDSetCodeCell ) ;
            if ( ! string.IsNullOrEmpty( ceeDSetCode ) ) ceeDSetCodes.Add( ceeDSetCode ) ;

            var ceeDModelNumberCell = workSheet.GetRow( j ).GetCell( 1 ) ;
            var ceeDModelNumber = GetCellValue( ceeDModelNumberCell ) ;
            if ( ! string.IsNullOrEmpty( ceeDModelNumber ) ) ceeDModelNumbers.Add( ceeDModelNumber ) ;

            var generalDisplayDeviceSymbolCell = workSheet.GetRow( j ).GetCell( 2 ) ;
            var generalDisplayDeviceSymbol = GetCellValue( generalDisplayDeviceSymbolCell ) ;
            if ( ! string.IsNullOrEmpty( generalDisplayDeviceSymbol ) && ! generalDisplayDeviceSymbol.Contains( "．" ) ) generalDisplayDeviceSymbols = generalDisplayDeviceSymbol ;

            var ceeDNameCell = workSheet.GetRow( j ).GetCell( 3 ) ;
            var modelName = GetCellValue( ceeDNameCell ) ;
            if ( ! string.IsNullOrEmpty( modelName ) ) ceeDName = modelName ;

            var modelNumberCell = workSheet.GetRow( j ).GetCell( 4 ) ;
            var modelNumber = GetCellValue( modelNumberCell ) ;
            if ( ! string.IsNullOrEmpty( modelNumber ) ) modelNumbers.Add( modelNumber ) ;

            var symbolCell = workSheet.GetRow( j ).GetCell( 5 ) ;
            var symbol = GetCellValue( symbolCell ) ;
            if ( ! string.IsNullOrEmpty( symbol ) && ! symbol.Contains( "又は" ) ) floorPlanSymbol = symbol ;
            
            var keisosymbolCell = workSheet.GetRow( j ).GetCell( 6 ) ;
            var keisosymbol = GetCellValue( keisosymbolCell ) ;
            if ( ! string.IsNullOrEmpty( keisosymbol ) && ! keisosymbol.Contains( "又は" ) ) instrumentationSymbol = keisosymbol ;
          }

          var strModelNumbers = modelNumbers.Any() ? string.Join( "\n", modelNumbers ) : string.Empty ;
          if ( ! ceeDModelNumbers.Any() ) {
            CeedModel ceeDModel = new CeedModel( string.Empty, string.Empty, generalDisplayDeviceSymbols, strModelNumbers, floorPlanSymbol, instrumentationSymbol,ceeDName ) ;
            ceedModelData.Add( ceeDModel ) ;
          }
          else {
            for ( var k = 0 ; k < ceeDModelNumbers.Count ; k++ ) {
              var ceeDSetCode = ceeDSetCodes.Any() ? ceeDSetCodes[ k ] : string.Empty ;
              List<byte[]>? heimenSymbolBytes = listHeimenZu.Where( b => b.Postion == firstIndexGroup + 1 && b.SymbolType == 1 ).Select( b => b.SymbolByte ).ToList() ;
              List<byte[]> keisoSymbolBytes = listKeisoZu.Where( b => b.Postion == firstIndexGroup + 1 && b.SymbolType == 1 ).Select( b => b.SymbolByte ).ToList() ;
              CeedModel ceeDModel ;
              if ( heimenSymbolBytes.Any()  ) {
                ceeDModel = new CeedModel( ceeDModelNumbers[ k ], ceeDSetCode, generalDisplayDeviceSymbols, strModelNumbers, heimenSymbolBytes,keisoSymbolBytes, ceeDName ) ;
              }
              else {
                ceeDModel = new CeedModel( ceeDModelNumbers[ k ], ceeDSetCode, generalDisplayDeviceSymbols, strModelNumbers, floorPlanSymbol,instrumentationSymbol, ceeDName ) ;
              }

              ceedModelData.Add( ceeDModel ) ;
            }
          }

          i-- ;
        }
      }
      catch ( Exception ex ) {
        var a = ex.StackTrace ;
        MessageBox.Show( a ) ;
        return new List<CeedModel>() ;
      }

      return ceedModelData ;
    }

    private static string GetCellValue( ICell? cell )
    {
      string cellValue = string.Empty ;
      if ( cell == null ) return cellValue ;
      cellValue = cell.CellType switch
      {
        CellType.Blank => string.Empty,
        CellType.Numeric => DateUtil.IsCellDateFormatted( cell ) ? cell.DateCellValue.ToString( CultureInfo.InvariantCulture ) : cell.NumericCellValue.ToString( CultureInfo.InvariantCulture ),
        CellType.String => cell.StringCellValue,
        _ => cellValue
      } ;

      return cellValue ;
    }
  }

  public class SymbolBytes
  {
    public SymbolBytes( int postion, byte[] symbolByte, int symbolType )
    {
      this.Postion = postion ;
      SymbolByte = symbolByte ;
      SymbolType = symbolType ;
    }

    public int Postion { get ; set ; }
    public byte[] SymbolByte { get ; set ; }
    
    public int SymbolType { get ; set ; } //1: image; 2: text
  }
}