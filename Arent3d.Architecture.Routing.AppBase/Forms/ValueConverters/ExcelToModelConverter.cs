using System ;
using System.Collections.Generic ;
using System.Globalization ;
using System.IO ;
using System.Linq ;
using System.Text ;
using Arent3d.Architecture.Routing.Storable.Model ;
using NPOI.HSSF.UserModel ;
using NPOI.SS.UserModel ;
using NPOI.XSSF.UserModel ;

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
        FileStream fs = new FileStream( path, FileMode.Open, FileAccess.Read ) ;
        XSSFWorkbook wb = new XSSFWorkbook( fs ) ;
        ISheet workSheet = wb.NumberOfSheets < 2 ? wb.GetSheetAt( wb.ActiveSheetIndex ) : wb.GetSheetAt( 1 ) ;
        const int startRow = 7 ;
        var endRow = workSheet.LastRowNum ;
        for ( var i = startRow ; i <= endRow ; i++ ) {
          List<string> ceeDModelNumbers = new List<string>() ;
          List<string> ceeDSetCodes = new List<string>() ;
          List<string> modelNumbers = new List<string>() ;
          List<string> conditions = new List<string>() ;
          string generalDisplayDeviceSymbols = string.Empty ;
          string floorPlanSymbol = string.Empty ;
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
            if ( ! string.IsNullOrEmpty( symbol ) && ! symbol.Contains( "又は" ) ) floorPlanSymbol = symbol ;

            var conditionCell = workSheet.GetRow( j ).GetCell( 8 ) ;
            var condition = GetCellValue( conditionCell ) ;
            if ( ! string.IsNullOrEmpty( condition ) && condition.EndsWith( "の場合" ) ) conditions.Add( condition.Replace( "の場合", "" ).Replace( "・", "" ) ) ;
          }

          if ( ! ceeDModelNumbers.Any() ) {
            CreateCeeDModel( ceedModelData, equipmentSymbols, string.Empty, string.Empty, generalDisplayDeviceSymbols, modelNumbers, floorPlanSymbol, ceeDName, string.Empty ) ;
          }
          else {
            for ( var k = 0 ; k < ceeDModelNumbers.Count ; k++ ) {
              var ceeDSetCode = ceeDSetCodes.Any() ? ceeDSetCodes[ k ] : string.Empty ;
              var condition = conditions.Count > k ? conditions[ k ] : string.Empty ;
              CreateCeeDModel( ceedModelData, equipmentSymbols, ceeDModelNumbers[ k ], ceeDSetCode, generalDisplayDeviceSymbols, modelNumbers, floorPlanSymbol, ceeDName, condition ) ;
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

    private static void CreateCeeDModel( List<CeedModel> ceeDModelData, List<EquipmentSymbol> equipmentSymbols, string ceeDModelNumber, string ceeDSetCode, string generalDisplayDeviceSymbols, List<string> modelNumbers, string floorPlanSymbol, string ceeDName, string condition )
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
            ceeDModelData.AddRange( from modelNumber in modelNumberList select new CeedModel( ceeDModelNumber, ceeDSetCode, generalDisplayDeviceSymbol, modelNumber, floorPlanSymbol, ceeDName, condition ) ) ;
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
            ceeDModelData.AddRange( from modelNumber in symbolModelNumber select new CeedModel( ceeDModelNumber, ceeDSetCode, generalDisplayDeviceSymbol, modelNumber, floorPlanSymbol, ceeDName, condition ) ) ;
          else {
            if ( equipmentSymbols.Any() ) {
              var modelNumberList = equipmentSymbols.Where( s => s.Symbol == generalDisplayDeviceSymbol ).Select( s => s.ModelNumber ).Distinct().ToList() ;
              if ( modelNumberList.Any() ) {
                ceeDModelData.AddRange( from modelNumber in modelNumberList select new CeedModel( ceeDModelNumber, ceeDSetCode, generalDisplayDeviceSymbol, modelNumber, floorPlanSymbol, ceeDName, condition ) ) ;
              }
              else {
                ceeDModelData.Add( new CeedModel( ceeDModelNumber, ceeDSetCode, generalDisplayDeviceSymbol, string.Empty, floorPlanSymbol, ceeDName, condition ) ) ;
              }
            }
            else {
              ceeDModelData.Add( new CeedModel( ceeDModelNumber, ceeDSetCode, generalDisplayDeviceSymbol, string.Empty, floorPlanSymbol, ceeDName, condition ) ) ;
            }
          }
        }
      }
    }

    private static List<EquipmentSymbol> GetAllEquipmentSymbols( string path )
    {
      List<EquipmentSymbol> equipmentSymbols = new List<EquipmentSymbol>() ;
      try {
        FileStream fs = new FileStream( path, FileMode.Open, FileAccess.Read ) ;
        HSSFWorkbook wb = new HSSFWorkbook( fs ) ;
        ISheet workSheet = wb.GetSheetAt( wb.ActiveSheetIndex ) ;
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
      List<string> modelNumbers = new List<string>() ;

      try {
        var extension = Path.GetExtension( path ) ;
        switch ( string.IsNullOrEmpty( extension ) ) {
          case false when extension == ".xlsx" :
          {
            FileStream fs = new FileStream( path, FileMode.Open, FileAccess.Read ) ;
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
  }
}