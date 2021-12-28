using System ;
using System.Collections.Generic ;
using System.Diagnostics ;
using System.Globalization ;
using System.IO ;
using System.Linq ;
using System.Runtime.InteropServices ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Forms ;
using System.Windows.Input ;
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
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      CeedStorable ceeDStorable = _document.GetCeeDStorable() ;
      {
        List<CeedModel> ceeDModelData = GetAllCeeDModelNumber( filePath ) ;
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

    private static List<SymbolImage> GetSymbolImages( string filePath, Dictionary<int, int> rowBlocks )
    {
     
      const string sheetName = "セットコード一覧表" ;
      const string startCell = "F8" ;
      const string selectColumn = "F" ;
      var symbolImages = new List<SymbolImage>() ;
      var excelApp = new Application
      {
        Visible = false,
        ScreenUpdating = false,
        DisplayStatusBar = false,
        //Calculation = XlCalculation.xlCalculationManual,
        EnableEvents = false
      } ;

      var excelWorkbook = excelApp.Workbooks.Open( filePath, Type.Missing, false, Type.Missing, Type.Missing, Type.Missing, false, XlPlatform.xlWindows, Type.Missing, true, false, Type.Missing, Type.Missing, Type.Missing, Type.Missing ) ;
      try {
        if ( excelWorkbook != null ) {
          var sheet = (Worksheet) excelWorkbook.Sheets[ sheetName ] ;
          sheet.DisplayPageBreaks = false ;
          var xlSheets = excelWorkbook.Sheets as Sheets ;
          var newSheet = (Worksheet) xlSheets.Add( xlSheets[ 1 ], Type.Missing, Type.Missing, Type.Missing ) ;
          var xlRange = sheet.UsedRange ;
          var endRow = xlRange.Rows.Count ;

          //Copy shapes to new sheet
          var range1 = sheet.Range[ startCell, selectColumn + endRow ] ;
          var range2 = newSheet.Range[ startCell, selectColumn + endRow ] ;
          range2.ColumnWidth = range1.ColumnWidth ;
          range1.Copy( range2 ) ;
          
          //convert shapes to image
          if ( newSheet.Shapes.Count > 0 ) {
            int rowNumber ;
            Clipboard.Clear() ;
            foreach ( Shape shape in newSheet.Shapes ) {
              rowNumber = shape.TopLeftCell.Row ;
              var marginLeft = shape.Left ;
              var block = rowBlocks.LastOrDefault( c => c.Key <= rowNumber ) ;
              shape.Copy() ; // sometime get error here
              if ( ! Clipboard.ContainsImage() ) continue ;
              var image = Clipboard.GetImage() ;
              if ( image == null ) continue ;
              // using ( var ms = new MemoryStream() ) {
              //   ms.Position = 0 ;
              //   image.Save( ms, ImageFormat.Png ) ;
              //   var imgData = ms.ToArray() ;
              // }
              symbolImages.Add( new SymbolImage( block.Key, image, marginLeft) ) ;
            }
          }
        }
      }
      catch ( Exception e ) {
        MessageBox.Show( " Error: " + e) ;
      }
      finally {
        excelWorkbook?.Close( false ) ;
        if ( excelWorkbook != null ) Marshal.ReleaseComObject( excelWorkbook ) ;
        excelApp.Quit() ;
        Marshal.ReleaseComObject( excelApp ) ;
      }
      
      return symbolImages ;
    }
    
    private void LoadData( CeedStorable ceeDStorable )
    {
      var viewModel = new ViewModel.CeedViewModel( ceeDStorable ) ;
      this.DataContext = viewModel ;
      _allCeeDModels = viewModel ;
      CmbCeeDModelNumbers.ItemsSource = viewModel.CeeDModelNumbers ;
      CmbModelNumbers.ItemsSource = viewModel.ModelNumbers ;
    }

    private static List<CeedModel> GetAllCeeDModelNumber( string path )
    {
      List<CeedModel> ceedModelData = new List<CeedModel>() ;

      try {
        FileStream fs = new FileStream( path, FileMode.Open, FileAccess.Read ) ;
        XSSFWorkbook wb = new XSSFWorkbook( fs ) ;
        ISheet workSheet = wb.NumberOfSheets < 2 ? wb.GetSheetAt( wb.ActiveSheetIndex ) : wb.GetSheetAt( 1 ) ;

        XSSFDrawing drawing = (XSSFDrawing) workSheet.DrawingPatriarch ;
        Dictionary<int, int> blocks = new Dictionary<int, int>() ;
        const int startRow = 7 ;
        var endRow = workSheet.LastRowNum ;

        //get list block row in column C
        for ( var i = startRow ; i <= endRow ; i++ ) {
          var record = workSheet.GetRow( i ).GetCell( 3 ) ;
          if ( record == null || record.CellStyle.IsHidden ) continue ;
          var cellValue = GetCellValue( record ) ;
          if ( string.IsNullOrEmpty( cellValue ) ) continue ;
          var nextCellValue = GetCellValue( record ) ;
          var firstIndexGroup = i ;
          do {
            i++ ;
            if ( i > endRow ) break ;
            cellValue = nextCellValue ;
            record = workSheet.GetRow( i ).GetCell( 3 ) ;
            if ( record == null ) break ;
            nextCellValue = GetCellValue( record ) ;
          } while ( ! ( string.IsNullOrEmpty( cellValue ) && ! string.IsNullOrEmpty( nextCellValue ) ) ) ;

          var lastIndexGroup = i ;
          blocks.Add( firstIndexGroup + 1, lastIndexGroup+1 ) ;
          i-- ;
        }
        var listFloorPlan = GetSymbolImages( path, blocks ) ;
        
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
            if ( ! string.IsNullOrEmpty( symbol ) ) floorPlanSymbol = symbol ;

            var keisosymbolCell = workSheet.GetRow( j ).GetCell( 6 ) ;
            var keisosymbol = GetCellValue( keisosymbolCell ) ;
            if ( ! string.IsNullOrEmpty( keisosymbol ) && ! keisosymbol.Contains( "又は" ) ) instrumentationSymbol = keisosymbol ;
          }
      
          var strModelNumbers = modelNumbers.Any() ? string.Join( "\n", modelNumbers ) : string.Empty ;
          if ( ! ceeDModelNumbers.Any() ) {
            CeedModel ceeDModel = new CeedModel( string.Empty, string.Empty, generalDisplayDeviceSymbols, strModelNumbers, floorPlanSymbol, ceeDName ) ;
            ceedModelData.Add( ceeDModel ) ;
          }
          else {
            for ( var k = 0 ; k < ceeDModelNumbers.Count ; k++ ) {
              var ceeDSetCode = ceeDSetCodes.Any() ? ceeDSetCodes[ k ] : string.Empty ;
              var symbolBytes = listFloorPlan.Where( b => b.Postion == firstIndexGroup + 1).ToList().OrderBy(b=>b.MarginLeft) ;
              var floorPlanImages = symbolBytes.Select( b => b.Image ).ToList() ;
             
              CeedModel ceeDModel ;
              if ( floorPlanImages.Any()  ) {
                ceeDModel = new CeedModel( ceeDModelNumbers[ k ], ceeDSetCode, generalDisplayDeviceSymbols, strModelNumbers, floorPlanImages, floorPlanSymbol, ceeDName ) ;
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

  public class SymbolImage
  {
    public SymbolImage( int postion, Image image, float marginLeft )
    {
      this.Postion = postion ;
      // SymbolByte = symbolByte ;
      Image = image ;
      MarginLeft = marginLeft ;
    }

    public int Postion { get ; set ; }
    // public byte[] SymbolByte { get ; set ; }
    public Image Image { get ; set ; }
    public float MarginLeft { get ; set ; } 
  }
}