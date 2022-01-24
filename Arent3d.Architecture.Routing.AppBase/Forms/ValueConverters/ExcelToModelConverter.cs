using System ;
using System.Collections.Generic ;
using System.Globalization ;
using System.IO ;
using System.Linq ;
using System.Runtime.InteropServices ;
using System.Text ;
using System.Windows ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Microsoft.Office.Interop.Excel ;
using NPOI.HSSF.UserModel ;
using NPOI.SS.UserModel ;
using NPOI.XSSF.UserModel ;
using Application = Microsoft.Office.Interop.Excel.Application ;
using Image = System.Drawing.Image ;
using Clipboard = System.Windows.Forms.Clipboard ;

namespace Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters
{
  public static class ExcelToModelConverter
  {
    public static List<CeedModel> GetAllCeeDModelNumber( string path, string path2 )
    {
      List<CeedModel> ceedModelData = new List<CeedModel>() ;

      try {
        var equipmentSymbols = new List<EquipmentSymbol>() ;
        if ( ! string.IsNullOrEmpty( path2 ) ) equipmentSymbols = GetAllEquipmentSymbols( path2 ) ;
        var extension = Path.GetExtension( path ) ;
        FileStream fs = new FileStream( path, FileMode.Open, FileAccess.Read ) ;
        ISheet? workSheet = null ;
        switch ( string.IsNullOrEmpty( extension ) ) {
          case false when extension == ".xls" :
          {
            HSSFWorkbook wb = new HSSFWorkbook( fs ) ;
            workSheet = wb.NumberOfSheets < 2 ? wb.GetSheetAt( wb.ActiveSheetIndex ) : wb.GetSheetAt( 1 ) ;
            break ;
          }
          case false when extension == ".xlsx" :
          {
            XSSFWorkbook wb = new XSSFWorkbook( fs ) ;
            workSheet = wb.NumberOfSheets < 2 ? wb.GetSheetAt( wb.ActiveSheetIndex ) : wb.GetSheetAt( 1 ) ;
            break ;
          }
        }

        if ( workSheet == null ) return ceedModelData ;
        const int startRow = 7 ;
        var endRow = workSheet.LastRowNum ;
        XSSFDrawing drawing = (XSSFDrawing) workSheet.DrawingPatriarch ;
        Dictionary<int, int> blocks = new Dictionary<int, int>() ;

        // Get list block row in column C
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
          blocks.Add( firstIndexGroup + 1, lastIndexGroup + 1 ) ;
          i-- ;
        }

        var listFloorPlan = GetSymbolImages( path, blocks ) ;
        var listInstrucmentChart = GetInstructionChartImages( path, blocks ) ;

        for ( var i = startRow ; i <= endRow ; i++ ) {
          List<string> ceeDModelNumbers = new List<string>() ;
          List<string> ceeDSetCodes = new List<string>() ;
          List<string> modelNumbers = new List<string>() ;
          List<string> conditions = new List<string>() ;
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
            if ( ! string.IsNullOrEmpty( generalDisplayDeviceSymbol ) && ! generalDisplayDeviceSymbol.Contains( "．" ) )
              generalDisplayDeviceSymbols = generalDisplayDeviceSymbol ;

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

            var conditionCell = workSheet.GetRow( j ).GetCell( 8 ) ;
            var condition = GetCellValue( conditionCell ) ;
            if ( ! string.IsNullOrEmpty( condition ) && condition.EndsWith( "の場合" ) ) conditions.Add( condition.Replace( "の場合", "" ).Replace( "・", "" ) ) ;
          }
          var strModelNumbers = modelNumbers.Any() ? string.Join( "\n", modelNumbers ) : string.Empty ;
          if ( ! ceeDModelNumbers.Any() ) {
            CreateCeeDModel( ceedModelData, equipmentSymbols, string.Empty, string.Empty, generalDisplayDeviceSymbols, modelNumbers, floorPlanSymbol, instrumentationSymbol, ceeDName, string.Empty ) ;
          }
          else {
            for ( var k = 0 ; k < ceeDModelNumbers.Count ; k++ ) {
              var ceeDSetCode = ceeDSetCodes.Any() ? ceeDSetCodes[ k ] : string.Empty ;
              var symbolBytes = listFloorPlan.Where( b => b.Position == firstIndexGroup + 1).ToList().OrderBy(b=>b.MarginLeft) ;
              var floorPlanImages = symbolBytes.Select( b => b.Image ).ToList() ;
               var symbolChartBytes = listInstrucmentChart.Where( b => b.Position == firstIndexGroup + 1).ToList().OrderBy(b=>b.MarginLeft) ;
               var instrucmentChartImages = symbolChartBytes.Select( b => b.Image ).ToList() ;
              var condition = conditions.Count > k ? conditions[ k ] : string.Empty ;
              CeedModel ceeDModel ;
              if ( floorPlanImages.Any() ) {
                ceeDModel = new CeedModel( ceeDModelNumbers[ k ], ceeDSetCode, generalDisplayDeviceSymbols, strModelNumbers, floorPlanImages,instrucmentChartImages, floorPlanSymbol, instrumentationSymbol, ceeDName, condition, string.Empty ) ;
              }
              else {
                ceeDModel = new CeedModel( ceeDModelNumbers[ k ], ceeDSetCode, generalDisplayDeviceSymbols, strModelNumbers, floorPlanSymbol, instrumentationSymbol, ceeDName, condition, string.Empty, string.Empty ) ;
              }
              ceedModelData.Add( ceeDModel ) ;
            }
          }

          i-- ;
        }
      }
      catch ( Exception ) {
        return new List<CeedModel>() ;
      }

      return ceedModelData ;
    }

    private static void CreateCeeDModel( List<CeedModel> ceeDModelData, List<EquipmentSymbol> equipmentSymbols, string ceeDModelNumber, string ceeDSetCode, string generalDisplayDeviceSymbols, List<string> modelNumbers, string floorPlanSymbol, string instrumentationSymbol, string ceeDName, string condition )
    {
      var symbols = generalDisplayDeviceSymbols.Split( '\n' ) ;
      var symbolsNotHaveModelNumber = new List<string>() ;
      var otherSymbolModelNumber = new List<string>() ;
      foreach ( var symbol in symbols ) {
        var generalDisplayDeviceSymbol = symbol.Normalize( NormalizationForm.FormKC ) ;
        if ( string.IsNullOrEmpty( generalDisplayDeviceSymbol ) ) continue ;
        if ( equipmentSymbols.Any() ) {
          var modelNumberList = equipmentSymbols.Where( s => s.Symbol == generalDisplayDeviceSymbol && modelNumbers.Contains( s.ModelNumber ) ).Select( s => s.ModelNumber ).Distinct().ToList() ;
          if ( modelNumberList.Any() ) {
            ceeDModelData.AddRange( from modelNumber in modelNumberList select new CeedModel( ceeDModelNumber, ceeDSetCode, generalDisplayDeviceSymbol, modelNumber, floorPlanSymbol, instrumentationSymbol, ceeDName, condition, string.Empty, string.Empty ) ) ;
            otherSymbolModelNumber.AddRange( modelNumberList ) ;
          }
          else {
            symbolsNotHaveModelNumber.Add( symbol ) ;
          }
        }
        else {
          symbolsNotHaveModelNumber.Add( symbol ) ;
        }
      }

      if ( ! symbolsNotHaveModelNumber.Any() ) return ;
      {
        foreach ( var symbol in symbolsNotHaveModelNumber ) {
          var generalDisplayDeviceSymbol = symbol.Normalize( NormalizationForm.FormKC ) ;
          if ( string.IsNullOrEmpty( generalDisplayDeviceSymbol ) ) continue ;
          var symbolModelNumber = modelNumbers.Where( m => ! otherSymbolModelNumber.Contains( m ) ).ToList() ;
          if ( symbolModelNumber.Any() )
            ceeDModelData.AddRange( from modelNumber in symbolModelNumber select new CeedModel( ceeDModelNumber, ceeDSetCode, generalDisplayDeviceSymbol, modelNumber, floorPlanSymbol, instrumentationSymbol, ceeDName, condition, string.Empty, string.Empty ) ) ;
          else {
            if ( equipmentSymbols.Any() ) {
              var modelNumberList = equipmentSymbols.Where( s => s.Symbol == generalDisplayDeviceSymbol ).Select( s => s.ModelNumber ).Distinct().ToList() ;
              if ( modelNumberList.Any() ) {
                ceeDModelData.AddRange( from modelNumber in modelNumberList select new CeedModel( ceeDModelNumber, ceeDSetCode, generalDisplayDeviceSymbol, modelNumber, floorPlanSymbol, instrumentationSymbol, ceeDName, condition, string.Empty, string.Empty ) ) ;
              }
              else {
                ceeDModelData.Add( new CeedModel( ceeDModelNumber, ceeDSetCode, generalDisplayDeviceSymbol, string.Empty, floorPlanSymbol, instrumentationSymbol, ceeDName, condition, string.Empty, string.Empty ) ) ;
              }
            }
            else {
              ceeDModelData.Add( new CeedModel( ceeDModelNumber, ceeDSetCode, generalDisplayDeviceSymbol, string.Empty, floorPlanSymbol, instrumentationSymbol, ceeDName, condition, string.Empty, string.Empty ) ) ;
            }
          }
        }
      }
    }

    private static List<EquipmentSymbol> GetAllEquipmentSymbols( string path )
    {
      List<EquipmentSymbol> equipmentSymbols = new List<EquipmentSymbol>() ;
      try {
        var extension = Path.GetExtension( path ) ;
        FileStream fs = new FileStream( path, FileMode.Open, FileAccess.Read ) ;
        ISheet? workSheet = null ;
        switch ( string.IsNullOrEmpty( extension ) ) {
          case false when extension == ".xls" :
          {
            HSSFWorkbook wb = new HSSFWorkbook( fs ) ;
            workSheet = wb.GetSheetAt( wb.ActiveSheetIndex ) ;
            break ;
          }
          case false when extension == ".xlsx" :
          {
            XSSFWorkbook wb = new XSSFWorkbook( fs ) ;
            workSheet = wb.GetSheetAt( wb.ActiveSheetIndex ) ;
            break ;
          }
        }

        if ( workSheet == null ) return equipmentSymbols ;
        const int startRow = 1 ;
        var endRow = workSheet.LastRowNum ;
        for ( var i = startRow ; i <= endRow ; i++ ) {
          var record = workSheet.GetRow( i ).GetCell( 0 ) ;
          if ( record == null || record.CellStyle.IsHidden ) continue ;
          var symbol = GetCellValue( record ) ;
          if ( string.IsNullOrEmpty( symbol ) ) continue ;
          var modelNumberCell = workSheet.GetRow( i ).GetCell( 4 ) ;
          var modelNumber = modelNumberCell == null ? string.Empty : GetCellValue( modelNumberCell ) ;
          equipmentSymbols.Add( new EquipmentSymbol( symbol, modelNumber ) ) ;
        }
      }
      catch ( Exception ) {
        return new List<EquipmentSymbol>() ;
      }

      return equipmentSymbols ;
    }

    private class EquipmentSymbol
    {
      public readonly string Symbol ;
      public readonly string ModelNumber ;

      public EquipmentSymbol( string? symbol, string? modelNumber )
      {
        Symbol = symbol ?? string.Empty ;
        ModelNumber = modelNumber ?? string.Empty ;
      }
    }

    public static List<string> GetModelNumberToUse( string path )
    {
      var modelNumbers = new List<string>() ;

      try {
        var extension = Path.GetExtension( path ) ;
        switch ( string.IsNullOrEmpty( extension ) ) {
          case false when extension == ".xlsx" :
          {
            using var fs = new FileStream( path, FileMode.Open, FileAccess.Read ) ;
            XSSFWorkbook wb = new XSSFWorkbook( fs ) ;
            ISheet workSheet = wb.GetSheetAt( wb.ActiveSheetIndex ) ;
            var endRow = workSheet.LastRowNum ;
            for ( var i = 1 ; i <= endRow ; i++ ) {
              var record = workSheet.GetRow( i ).GetCell( 1 ) ;
              if ( record == null || record.CellStyle.IsHidden ) continue ;
              var strModelNumber = GetCellValue( record ) ;
              if ( string.IsNullOrEmpty( strModelNumber ) ) continue ;
              var arrModelNumbers = strModelNumber.Split( '\n' ) ;
              foreach ( var modelNumber in arrModelNumbers ) {
                if ( ! string.IsNullOrEmpty( modelNumber ) && ! modelNumbers.Contains( modelNumber ) ) {
                  modelNumbers.Add( modelNumber ) ;
                }
              }
            }

            break ;
          }
          case false when extension == ".csv" :
          {
            using StreamReader reader = new StreamReader( path, Encoding.GetEncoding( "shift-jis" ), true ) ;
            List<string> lines = new List<string>() ;
            while ( ! reader.EndOfStream ) {
              var line = reader.ReadLine() ;
              var values = line!.Split( ',' ) ;
              var modelNumber = values.Length > 1 ? values[ 1 ].Trim() : values[ 0 ].Trim() ;
              if ( ! string.IsNullOrEmpty( modelNumber ) && ! modelNumbers.Contains( modelNumber ) )
                modelNumbers.Add( modelNumber ) ;
            }

            break ;
          }
        }
      }
      catch ( Exception ) {
        return new List<string>() ;
      }

      return modelNumbers ;
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
              shape.Copy() ;
              if ( ! Clipboard.ContainsImage() ) continue ;
              var image = Clipboard.GetImage() ;
              if ( image == null ) continue ;
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
     private static List<SymbolImage> GetInstructionChartImages( string filePath, Dictionary<int, int> rowBlocks )
    {
      const string sheetName = "セットコード一覧表" ;
      const string startCell = "G8" ;
      const string selectColumn = "G" ;
      var symbolImages = new List<SymbolImage>() ;
      var excelApp = new Application
      {
        Visible = false,
        ScreenUpdating = false,
        DisplayStatusBar = false,
        EnableEvents = false
      } ;

      var excelWorkbook = excelApp.Workbooks.Open( filePath, Type.Missing, false, Type.Missing, Type.Missing, Type.Missing, false, XlPlatform.xlWindows, Type.Missing, true, false, Type.Missing, Type.Missing, Type.Missing, Type.Missing ) ;
    
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
              shape.Copy() ;
              if ( ! Clipboard.ContainsImage() ) continue ;
              var image = Clipboard.GetImage() ;
              if ( image == null ) continue ;
              symbolImages.Add( new SymbolImage( block.Key, image, marginLeft) ) ;
            }
          }
        }

        return symbolImages ;
    }
  }
  
  public class SymbolImage
  {
    public SymbolImage( int postion, Image image, float marginLeft )
    {
      this.Position = postion ;
      Image = image ;
      MarginLeft = marginLeft ;
    }

    public int Position { get ; set ; }
    public Image Image { get ; set ; }
    public float MarginLeft { get ; set ; } 
  }
}