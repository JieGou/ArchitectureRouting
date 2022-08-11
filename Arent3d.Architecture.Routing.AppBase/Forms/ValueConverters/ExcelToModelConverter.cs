using System ;
using System.Collections.Generic ;
using System.Globalization ;
using System.IO ;
using System.Linq ;
using System.Runtime.InteropServices ;
using System.Text ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Utility ;
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
    private const string DefaultSymbol = "Dummy" ;

    public static List<ElectricalCategoryModel> GetElectricalCategories( string path,  ref Dictionary<string, string> dictData, string sheetName = "Eco" )
    {
      List<ElectricalCategoryModel> electricalCategoryList = new() ;
      using var fs = new FileStream( path, FileMode.Open, FileAccess.Read ) ;
      try {
        XSSFWorkbook wb = new ( fs ) ;
        //ISheet workSheet  = wb.NumberOfSheets < 2 ? wb.GetSheetAt( wb.ActiveSheetIndex ) : wb.GetSheetAt( 1 ) ;
        ISheet workSheet  = wb.GetSheet( sheetName ) ; 
        ISheet workSheetKey  = wb.GetSheet( sheetName + "Key" ) ; 
        const int startRow = 0 ;
        var endRow = workSheet.LastRowNum ; 
 
        for ( var i = startRow ; i <= endRow ; i++ ) {
          var row = workSheet.GetRow( i ) ;
          var cells = row?.Cells ;
          var col1 = null != cells ? GetCellValue( cells[ 0 ] ) : string.Empty ;
          var col2 = null != cells ? cells.Count > 1 ? GetCellValue( cells[ 1 ] ) : string.Empty : string.Empty ;
          var col3 = null != cells ? cells.Count > 2 ? GetCellValue( cells[ 2 ] ) : string.Empty : string.Empty ; 
          electricalCategoryList.Add( new ElectricalCategoryModel( col1, col2, col3 ));
          
          var rowKey = workSheetKey.GetRow( i ) ;
          var cellKeys = rowKey?.Cells ;
          if ( ! string.IsNullOrEmpty( col1 ) ) {
            var col1Key = null != cellKeys ? GetCellValue( cellKeys[ 0 ] ) : string.Empty ;
            dictData.Add( col1, col1Key );
          }
          
          if ( ! string.IsNullOrEmpty( col2 ) ) {
            var col2Key = null != cellKeys ? GetCellValue( cellKeys[ 1 ] ) : string.Empty ;
            dictData.Add( col2, col2Key );
          }
          
          if ( ! string.IsNullOrEmpty( col3 ) ) {
            var col3Key = null != cellKeys ? GetCellValue( cellKeys[ 2 ] ) : string.Empty ;
            dictData.Add( col3, col3Key );
          }
            
        }

        return electricalCategoryList ;
      }
      catch ( Exception ) {
        return new List<ElectricalCategoryModel>() ;
      }
      finally {
        fs.Close() ;
        fs.Dispose() ;
      }
    }
    
    public static List<CeedModel> GetAllCeedModelNumber( string path, string path2 )
    {
      List<CeedModel> ceedModelData = new List<CeedModel>() ;

      var equipmentSymbols = new List<EquipmentSymbol>() ;
      if ( ! string.IsNullOrEmpty( path2 ) ) equipmentSymbols = GetAllEquipmentSymbols( path2 ) ;
      var extension = Path.GetExtension( path ) ;
      using var fs = new FileStream( path, FileMode.Open, FileAccess.Read ) ;
      try {
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
        const int startRow = 10 ;
        const int ceedNameColumnIndex = 4 ;
        var endRow = workSheet.LastRowNum ;
        Dictionary<int, int> blocks = new() ;

        // Get list block row in column C
        for ( var i = startRow ; i <= endRow ; i++ ) {
          var record = workSheet.GetRow( i ).GetCell( ceedNameColumnIndex ) ;
          if ( record == null || record.CellStyle.IsHidden ) continue ;
          var cellValue = GetCellValue( record ) ;
          if ( string.IsNullOrEmpty( cellValue ) ) continue ;
          var nextCellValue = GetCellValue( record ) ;
          var firstIndexGroup = i ;
          do {
            i++ ;
            if ( i > endRow ) break ;
            cellValue = nextCellValue ;
            record = workSheet.GetRow( i ).GetCell( ceedNameColumnIndex ) ;
            if ( record == null ) break ;
            nextCellValue = GetCellValue( record ) ;
          } while ( ! ( string.IsNullOrEmpty( cellValue ) && ! string.IsNullOrEmpty( nextCellValue ) ) ) ;

          var lastIndexGroup = i ;
          blocks.Add( firstIndexGroup + 1, lastIndexGroup + 1 ) ;
          i-- ;
        }

        var listFloorPlanImages = GetSymbolImages( path, blocks, "G10", "G" ) ;

        #region Load Instrumentation Image (Comment out)

        //var listInstrumentationImages = GetSymbolImages( path, blocks, "G8", "G" ) ;

        #endregion

        for ( var i = startRow ; i <= endRow ; i++ ) {
          List<string> ceedModelNumbers = new() ;
          List<string> ceedSetCodes = new() ;
          List<string> modelNumbers = new() ;
          string generalDisplayDeviceSymbols = string.Empty ;
          string floorPlanSymbol = string.Empty ;
          string instrumentationSymbol = string.Empty ;
          string ceedName = string.Empty ;
          string legendDisplay = string.Empty ;

          var record = workSheet.GetRow( i ).GetCell( ceedNameColumnIndex ) ;
          if ( record == null || record.CellStyle.IsHidden ) continue ;
          var name = GetCellValue( record ) ;
          if ( string.IsNullOrEmpty( name ) ) continue ;
          var firstIndexGroup = i ;
          var nextName = GetCellValue( record ) ;
          do {
            i++ ;
            if ( i > endRow ) break ;
            name = nextName ;
            record = workSheet.GetRow( i ).GetCell( ceedNameColumnIndex ) ;
            if ( record == null ) break ;
            nextName = GetCellValue( record ) ;
          } while ( ! ( string.IsNullOrEmpty( name ) && ! string.IsNullOrEmpty( nextName ) ) ) ;

          var lastIndexGroup = i ;
          for ( var j = firstIndexGroup ; j < lastIndexGroup ; j++ ) {
            var legendDisplayCell = workSheet.GetRow( j ).GetCell( 0 ) ;
            var legendDisplayValue = GetCellValue( legendDisplayCell ) ;
            if ( ! string.IsNullOrEmpty( legendDisplayValue ) ) legendDisplay = legendDisplayValue ;
            
            var ceedModelNumberCell = workSheet.GetRow( j ).GetCell( 1 ) ;
            var ceedModelNumber = GetCellValue( ceedModelNumberCell ) ;
            if ( ! string.IsNullOrEmpty( ceedModelNumber ) ) ceedModelNumbers.Add( ceedModelNumber ) ;
            
            var ceedSetCodeCell = workSheet.GetRow( j ).GetCell( 2 ) ;
            var ceedSetCode = GetCellValue( ceedSetCodeCell ) ;
            if ( ! string.IsNullOrEmpty( ceedSetCode ) ) ceedSetCodes.Add( ceedSetCode ) ;

            var generalDisplayDeviceSymbolCell = workSheet.GetRow( j ).GetCell( 3 ) ;
            var generalDisplayDeviceSymbol = GetCellValue( generalDisplayDeviceSymbolCell ) ;
            if ( ! string.IsNullOrEmpty( generalDisplayDeviceSymbol ) && ! generalDisplayDeviceSymbol.Contains( "．" ) )
              generalDisplayDeviceSymbols = generalDisplayDeviceSymbol ;

            var ceedNameCell = workSheet.GetRow( j ).GetCell( ceedNameColumnIndex ) ;
            var modelName = GetCellValue( ceedNameCell ) ;
            if ( ! string.IsNullOrEmpty( modelName ) ) ceedName = modelName ;

            var modelNumberCell = workSheet.GetRow( j ).GetCell( 5 ) ;
            var modelNumber = GetCellValue( modelNumberCell ) ;
            if ( ! string.IsNullOrEmpty( modelNumber ) ) modelNumbers.Add( modelNumber ) ;

            var symbolCell = workSheet.GetRow( j ).GetCell( 6 ) ;
            var symbol = GetCellValue( symbolCell ) ;
            if ( ! string.IsNullOrEmpty( symbol ) && ! symbol.Contains( "又は" ) ) floorPlanSymbol = symbol ;
          }

          var symbolBytes = listFloorPlanImages.Where( b => b.Position == firstIndexGroup + 1 ).ToList().OrderBy( b => b.MarginLeft ) ;
          var floorPlanImages = symbolBytes.Select( b => b.Image ).ToList() ;
          var instrumentationImages = new List<Image>() ;

          #region Load Instrumentation Image (Comment out)

          //var instrumentationSymbolBytes = listInstrumentationImages.Where( b => b.Position == firstIndexGroup + 1).ToList().OrderBy(b=>b.MarginLeft) ;
          //var instrumentationImages = instrumentationSymbolBytes.Select( b => b.Image ).ToList() ;

          #endregion

          if ( ! ceedModelNumbers.Any() ) {
            switch ( floorPlanImages.Count ) {
              case 1 when string.IsNullOrEmpty( floorPlanSymbol ) :
                CreateCeedModel( ceedModelData, equipmentSymbols, legendDisplay, string.Empty, string.Empty, generalDisplayDeviceSymbols, modelNumbers, floorPlanSymbol, instrumentationSymbol, ceedName, floorPlanImages, instrumentationImages, true ) ;
                break ;
              case > 1 :
                floorPlanSymbol = DefaultSymbol ;
                CreateCeedModel( ceedModelData, equipmentSymbols, legendDisplay, string.Empty, string.Empty, generalDisplayDeviceSymbols, modelNumbers, floorPlanSymbol, instrumentationSymbol, ceedName, floorPlanImages, instrumentationImages, false, true ) ;
                break ;
              default :
                CreateCeedModel( ceedModelData, equipmentSymbols, legendDisplay, string.Empty, string.Empty, generalDisplayDeviceSymbols, modelNumbers, floorPlanSymbol, instrumentationSymbol, ceedName, floorPlanImages, instrumentationImages ) ;
                break ;
            }
          }
          else {
            for ( var k = 0 ; k < ceedModelNumbers.Count ; k++ ) {
              var ceedSetCode = ceedSetCodes.Any() ? ceedSetCodes[ k ] : string.Empty ;
              switch ( floorPlanImages.Count ) {
                case 1 when string.IsNullOrEmpty( floorPlanSymbol ) :
                  CreateCeedModel( ceedModelData, equipmentSymbols, legendDisplay, ceedModelNumbers[ k ], ceedSetCode, generalDisplayDeviceSymbols, modelNumbers, floorPlanSymbol, instrumentationSymbol, ceedName, floorPlanImages, instrumentationImages, true ) ;
                  break ;
                case > 1 :
                  floorPlanSymbol = DefaultSymbol ;
                  CreateCeedModel( ceedModelData, equipmentSymbols, legendDisplay, ceedModelNumbers[ k ], ceedSetCode, generalDisplayDeviceSymbols, modelNumbers, floorPlanSymbol, instrumentationSymbol, ceedName, floorPlanImages, instrumentationImages, false, true ) ;
                  break ;
                default :
                  CreateCeedModel( ceedModelData, equipmentSymbols, legendDisplay, ceedModelNumbers[ k ], ceedSetCode, generalDisplayDeviceSymbols, modelNumbers, floorPlanSymbol, instrumentationSymbol, ceedName, floorPlanImages, instrumentationImages ) ;
                  break ;
              }
            }
          }

          i-- ;
        }
      }
      catch ( Exception ) {
        return new List<CeedModel>() ;
      }
      finally {
        fs.Close() ;
        fs.Dispose() ;
      }

      return ceedModelData ;
    }

    private static void CreateCeedModel( List<CeedModel> ceedModelData, List<EquipmentSymbol> equipmentSymbols, string legendDisplay, string ceedModelNumber, string ceedSetCode, string generalDisplayDeviceSymbols, List<string> modelNumbers, string floorPlanSymbol, string instrumentationSymbol, string ceedName, List<Image> floorPlanImages, List<Image> instrumentationImages, bool isFloorPlanImages = false, bool isDummySymbol = false )
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
            foreach ( var modelNumber in modelNumberList ) {
              AddCeedModel( ceedModelData, legendDisplay, ceedModelNumber, ceedSetCode, generalDisplayDeviceSymbol, modelNumber, floorPlanSymbol, instrumentationSymbol, ceedName, floorPlanImages, instrumentationImages, isFloorPlanImages, isDummySymbol ) ;
            }

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
          if ( symbolModelNumber.Any() ) {
            foreach ( var modelNumber in symbolModelNumber ) {
              AddCeedModel( ceedModelData, legendDisplay, ceedModelNumber, ceedSetCode, generalDisplayDeviceSymbol, modelNumber, floorPlanSymbol, instrumentationSymbol, ceedName, floorPlanImages, instrumentationImages, isFloorPlanImages, isDummySymbol ) ;
            }
          }
          else {
            if ( equipmentSymbols.Any() ) {
              var modelNumberList = equipmentSymbols.Where( s => s.Symbol == generalDisplayDeviceSymbol ).Select( s => s.ModelNumber ).Distinct().ToList() ;
              if ( modelNumberList.Any() ) {
                foreach ( var modelNumber in modelNumberList ) {
                  AddCeedModel( ceedModelData, legendDisplay, ceedModelNumber, ceedSetCode, generalDisplayDeviceSymbol, modelNumber, floorPlanSymbol, instrumentationSymbol, ceedName, floorPlanImages, instrumentationImages, isFloorPlanImages, isDummySymbol ) ;
                }
              }
              else {
                AddCeedModel( ceedModelData, legendDisplay, ceedModelNumber, ceedSetCode, generalDisplayDeviceSymbol, string.Empty, floorPlanSymbol, instrumentationSymbol, ceedName, floorPlanImages, instrumentationImages, isFloorPlanImages, isDummySymbol ) ;
              }
            }
            else {
              AddCeedModel( ceedModelData, legendDisplay, ceedModelNumber, ceedSetCode, generalDisplayDeviceSymbol, string.Empty, floorPlanSymbol, instrumentationSymbol, ceedName, floorPlanImages, instrumentationImages, isFloorPlanImages, isDummySymbol ) ;
            }
          }
        }
      }
    }

    private static void AddCeedModel( ICollection<CeedModel> ceedModelData, string legendDisplay, string ceedModelNumber, string ceedSetCode, string generalDisplayDeviceSymbol, string modelNumber, string floorPlanSymbol, string instrumentationSymbol, string ceedName, List<Image> floorPlanImages, List<Image> instrumentationImages, bool isFloorPlanImages, bool isDummySymbol )
    {
      var floorPlanType = SetFloorPlanType( ceedModelNumber, generalDisplayDeviceSymbol, floorPlanSymbol ) ;
      if ( isFloorPlanImages )
        ceedModelData.Add( new CeedModel( legendDisplay, ceedModelNumber, ceedSetCode, generalDisplayDeviceSymbol, modelNumber, floorPlanImages, instrumentationImages, floorPlanSymbol, instrumentationSymbol, ceedName, string.Empty, floorPlanType ) ) ;
      else if ( isDummySymbol )
        ceedModelData.Add( new CeedModel( legendDisplay, ceedModelNumber, ceedSetCode, generalDisplayDeviceSymbol, modelNumber, floorPlanImages, instrumentationImages, floorPlanSymbol, instrumentationSymbol, ceedName, floorPlanType ) ) ;
      else
        ceedModelData.Add( new CeedModel( legendDisplay, ceedModelNumber, ceedSetCode, generalDisplayDeviceSymbol, modelNumber, floorPlanSymbol, instrumentationSymbol, ceedName, string.Empty, string.Empty, floorPlanType ) ) ;
    }

    private static string SetFloorPlanType( string ceedModelNumber, string generalDisplayDeviceSymbol, string floorPlanSymbol )
    {
      string defaultFloorPlanType = string.Empty ;
      if ( floorPlanSymbol == DefaultSymbol ) return defaultFloorPlanType ;
      if ( string.IsNullOrEmpty( ceedModelNumber ) ) {
        if ( ! string.IsNullOrEmpty( generalDisplayDeviceSymbol ) && generalDisplayDeviceSymbol.Contains( "BOX" ) )
          return ConnectorOneSideFamilyType.ConnectorOneSide32.GetFieldName() ;
        if ( ! string.IsNullOrEmpty( generalDisplayDeviceSymbol ) && ( generalDisplayDeviceSymbol.Contains( "CVV" ) || generalDisplayDeviceSymbol.Contains( "CVR" ) ) )
          return ConnectorOneSideFamilyType.ConnectorOneSide34.GetFieldName() ;
      }
      else {
        var arrCeedSetCode = ceedModelNumber.Split( '_' ) ;
        var charCode = arrCeedSetCode.First() ;
        var numberCode = int.Parse( arrCeedSetCode.ElementAt( 1 ) ) ;
        switch ( charCode ) {
          case "A" :
          case "B" :
          case "C" :
          case "D" when ( numberCode is >= 1 and <= 6 ) :
          case "D" when ( numberCode is >= 12 and <= 14 ) :
          case "G" when ( numberCode is >= 1 and <= 4 ) :
          case "G" when numberCode == 12 :
          case "G" when numberCode == 15 :
          case "H" when numberCode == 7 :
          case "H" when numberCode == 12 :
          case "H" when numberCode == 14 :
          case "L" when numberCode == 27 :
          case "M" when numberCode == 5 :
          case "N" when ( numberCode is >= 1 and <= 2 ) :
            return ConnectorOneSideFamilyType.ConnectorOneSide1.GetFieldName() ;
          case "D" when ( numberCode is >= 7 and <= 11 ) :
          case "D" when numberCode == 15 :
          case "H" when numberCode == 3 :
          case "H" when numberCode == 8 :
          case "H" when numberCode == 13 :
          case "H" when numberCode == 15 :
          case "H" when numberCode == 16 :
          case "H" when ( numberCode is >= 19 and <= 22 ) :
            return ConnectorOneSideFamilyType.ConnectorOneSide2.GetFieldName() ;
          case "E" :
          case "H" when numberCode == 9 :
          case "H" when numberCode == 10 :
            return ConnectorOneSideFamilyType.ConnectorOneSide5.GetFieldName() ;
          case "F" when ( numberCode is >= 1 and <= 12 ) :
          case "F" when ( numberCode is >= 18 and <= 23 ) :
            return ConnectorOneSideFamilyType.ConnectorOneSide6.GetFieldName() ;
          case "F" when ( numberCode is >= 13 and <= 15 ) :
            return ConnectorOneSideFamilyType.ConnectorOneSide7.GetFieldName() ;
          case "F" when ( numberCode is >= 16 and <= 17 ) :
            return ConnectorOneSideFamilyType.ConnectorOneSide8.GetFieldName() ;
          case "G" when ( numberCode is >= 5 and <= 11 ) :
          case "G" when numberCode == 14 :
          case "H" when numberCode == 4 :
          case "H" when numberCode == 5 :
          case "H" when numberCode == 23 :
          case "I" when numberCode != 16 :
            return ConnectorOneSideFamilyType.ConnectorOneSide9.GetFieldName() ;
          case "H" when ( numberCode is >= 1 and <= 2 ) :
          case "H" when numberCode == 11 :
          case "H" when ( numberCode is >= 17 and <= 18 ) :
          case "H" when numberCode == 24 :
          case "I" :
          case "K" :
          case "mb" :
          case "L" when numberCode == 21 :
          case "N" when ( numberCode is >= 3 and <= 6 ) :
            return ConnectorOneSideFamilyType.ConnectorOneSide10.GetFieldName() ;
          case "lp" when ( numberCode <= 22 && numberCode != 13 ) :
            return ConnectorOneSideFamilyType.ConnectorOneSide11.GetFieldName() ;
          case "lp" when numberCode == 13 :
          case "lp" when numberCode == 23 :
          case "lp" when numberCode == 24 :
            return ConnectorOneSideFamilyType.ConnectorOneSide12.GetFieldName() ;
          case "M" when numberCode == 7 :
          case "th" when numberCode == 33 :
          case "th" when numberCode == 34 :
            return ConnectorOneSideFamilyType.ConnectorOneSide13.GetFieldName() ;
          case "lp" when ( numberCode is >= 34 and <= 42 ) :
          case "J" when numberCode == 1 :
            return ConnectorOneSideFamilyType.ConnectorOneSide14.GetFieldName() ;
          case "lp" when ( numberCode is >= 25 and <= 33 ) :
            return ConnectorOneSideFamilyType.ConnectorOneSide15.GetFieldName() ;
          case "lp" when numberCode == 43 :
            return ConnectorOneSideFamilyType.ConnectorOneSide16.GetFieldName() ;
          case "lp" when ( numberCode is >= 44 and <= 49 ) :
            return ConnectorOneSideFamilyType.ConnectorOneSide17.GetFieldName() ;
          case "L" when ( numberCode is >= 1 and <= 4 ) :
          case "L" when numberCode == 10 :
            return ConnectorOneSideFamilyType.ConnectorOneSide19.GetFieldName() ;
          case "L" when ( numberCode is >= 5 and <= 9 ) :
          case "L" when ( numberCode is >= 11 and <= 17 ) :
            return ConnectorOneSideFamilyType.ConnectorOneSide20.GetFieldName() ;
          case "L" when numberCode == 18 :
            return ConnectorOneSideFamilyType.ConnectorOneSide21.GetFieldName() ;
          case "L" when numberCode == 19 :
            return ConnectorOneSideFamilyType.ConnectorOneSide22.GetFieldName() ;
          case "L" when numberCode == 20 :
          case "L" when numberCode == 31 :
            return ConnectorOneSideFamilyType.ConnectorOneSide23.GetFieldName() ;
          case "th" when numberCode == 14 :
            return ConnectorOneSideFamilyType.ConnectorOneSide24.GetFieldName() ;
          case "L" when numberCode == 22 :
          case "L" when ( numberCode is >= 24 and <= 26 ) :
            return ConnectorOneSideFamilyType.ConnectorOneSide25.GetFieldName() ;
          case "L" when numberCode == 23 :
            return ConnectorOneSideFamilyType.ConnectorOneSide26.GetFieldName() ;
          case "th" when numberCode == 16 :
            return ConnectorOneSideFamilyType.ConnectorOneSide27.GetFieldName() ;
          case "L" when numberCode == 28 :
          case "th" when numberCode == 18 :
            return ConnectorOneSideFamilyType.ConnectorOneSide28.GetFieldName() ;
          case "th" when numberCode == 17 :
          case "th" when numberCode == 32 :
            return ConnectorOneSideFamilyType.ConnectorOneSide29.GetFieldName() ;
          case "L" when numberCode == 29 :
            return ConnectorOneSideFamilyType.ConnectorOneSide30.GetFieldName() ;
          case "op" :
            return ConnectorOneSideFamilyType.ConnectorOneSide31.GetFieldName() ;
          case "M" when ( numberCode is >= 1 and <= 4 ) :
            return ConnectorOneSideFamilyType.ConnectorOneSide33.GetFieldName() ;
          case "M" when numberCode == 6 :
            return ConnectorOneSideFamilyType.ConnectorOneSide35.GetFieldName() ;
          case "M" when numberCode == 8 :
            return ConnectorOneSideFamilyType.ConnectorOneSide36.GetFieldName() ;
        }
      }

      return defaultFloorPlanType ;
    }

    private static List<EquipmentSymbol> GetAllEquipmentSymbols( string path )
    {
      List<EquipmentSymbol> equipmentSymbols = new List<EquipmentSymbol>() ;
      var extension = Path.GetExtension( path ) ;
      using var fs = new FileStream( path, FileMode.Open, FileAccess.Read ) ;
      try {
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
      finally {
        fs.Close() ;
        fs.Dispose() ;
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
    
    public static List<ConnectorFamilyReplacement> GetConnectorFamilyReplacements( string path )
    {
      List<ConnectorFamilyReplacement> connectorFamilyReplacements = new() ;
      var extension = Path.GetExtension( path ) ;
      using var fs = new FileStream( path, FileMode.Open, FileAccess.Read ) ;
      try {
        ISheet? workSheet = null ;
        switch ( string.IsNullOrEmpty( extension ) ) {
          case false when extension == ".xls" :
          {
            HSSFWorkbook wb = new( fs ) ;
            workSheet = wb.GetSheetAt( wb.ActiveSheetIndex ) ;
            break ;
          }
          case false when extension == ".xlsx" :
          {
            XSSFWorkbook wb = new( fs ) ;
            workSheet = wb.GetSheetAt( wb.ActiveSheetIndex ) ;
            break ;
          }
        }

        if ( workSheet == null ) return connectorFamilyReplacements ;
        const int startRow = 1 ;
        var endRow = workSheet.LastRowNum ;
        for ( var i = startRow ; i <= endRow ; i++ ) {
          var equipmentSymbolsCell = workSheet.GetRow( i ).GetCell( 0 ) ;
          if ( equipmentSymbolsCell == null || equipmentSymbolsCell.CellStyle.IsHidden ) continue ;
          var equipmentSymbols = GetCellValue( equipmentSymbolsCell ) ;
          if ( string.IsNullOrEmpty( equipmentSymbols ) ) continue ;
          var connectorFamilyFileCell = workSheet.GetRow( i ).GetCell( 1 ) ;
          var connectorFamilyFile = connectorFamilyFileCell == null ? string.Empty : GetCellValue( connectorFamilyFileCell ) ;
          connectorFamilyReplacements.Add( new ConnectorFamilyReplacement( equipmentSymbols, connectorFamilyFile ) ) ;
        }
      }
      catch ( Exception ) {
        return new List<ConnectorFamilyReplacement>() ;
      }
      finally {
        fs.Close() ;
        fs.Dispose() ;
      }

      return connectorFamilyReplacements ;
    }

    public class ConnectorFamilyReplacement
    {
      public readonly string DeviceSymbols ;
      public readonly string ConnectorFamilyFile ;

      public ConnectorFamilyReplacement( string? deviceSymbols, string? connectorFamilyFile )
      {
        DeviceSymbols = deviceSymbols ?? string.Empty ;
        ConnectorFamilyFile = connectorFamilyFile ?? string.Empty ;
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

            fs.Close() ;
            fs.Dispose() ;
            break ;
          }
          case false when extension == ".csv" :
          {
            using var reader = new StreamReader( path, Encoding.GetEncoding( "shift-jis" ), true ) ;
            while ( ! reader.EndOfStream ) {
              var line = reader.ReadLine() ;
              var values = line!.Split( ',' ) ;
              var modelNumber = values.Length > 1 ? values[ 1 ].Trim() : values[ 0 ].Trim() ;
              if ( ! string.IsNullOrEmpty( modelNumber ) && ! modelNumbers.Contains( modelNumber ) )
                modelNumbers.Add( modelNumber ) ;
            }

            reader.Close() ;
            reader.Dispose() ;
            break ;
          }
        }
      }
      catch ( Exception ) {
        return new List<string>() ;
      }

      return modelNumbers ;
    }

    public static List<DetailTableItemModel> GetReferenceDetailTableModels( string path )
    {
      var referenceDetailTableItemModels = new List<DetailTableItemModel>() ;

      try {
        using StreamReader reader = new( path ) ;
        while ( ! reader.EndOfStream ) {
          var line = reader.ReadLine() ;
          var values = line!.Split( ';' ) ;
          if ( values.Length <= 29 ) continue ;
          var detailTableItemModel = new DetailTableItemModel( false, values[ 0 ], values[ 1 ], values[ 2 ], values[ 3 ], values[ 4 ], values[ 5 ], values[ 6 ],
            values[ 7 ], values[ 8 ], values[ 9 ], values[ 10 ], values[ 11 ], values[ 12 ], values[ 13 ], values[ 14 ],
            values[ 15 ], values[ 16 ], values[ 17 ], values[ 18 ], values[ 19 ], values[ 20 ], double.Parse( values[ 21 ] ),
            int.Parse( values[ 22 ] ), values[ 23 ], values[ 24 ], bool.Parse( values[ 25 ] ), bool.Parse( values[ 26 ] ), values[ 27 ],
            values[ 28 ], bool.Parse( values[ 29 ] ), bool.Parse( values[ 30 ] ), values[ 31 ] ) ;
          referenceDetailTableItemModels.Add( detailTableItemModel ) ;
        }

        reader.Close() ;
        reader.Dispose() ;
      }
      catch ( Exception ) {
        return new List<DetailTableItemModel>() ;
      }

      return referenceDetailTableItemModels ;
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

    private static List<SymbolImage> GetSymbolImages( string filePath, Dictionary<int, int> rowBlocks, string startCell, string selectColumn )
    {
      var symbolImages = new List<SymbolImage>() ;
      var excelApp = new Application { Visible = false, ScreenUpdating = false, DisplayStatusBar = false, EnableEvents = false } ;

      var excelWorkbook = excelApp.Workbooks.Open( filePath, Type.Missing, false, Type.Missing, Type.Missing, Type.Missing, false, XlPlatform.xlWindows, Type.Missing, true, false, Type.Missing, Type.Missing, Type.Missing, Type.Missing ) ;
      try {
        if ( excelWorkbook != null ) {
          var sheet = excelWorkbook.Sheets.Count > 1 ? (Worksheet) excelWorkbook.Sheets[ 1 ] : (Worksheet) excelWorkbook.ActiveSheet ;
          sheet.DisplayPageBreaks = false ;
          var xlSheets = excelWorkbook.Sheets ;
          var newSheet = (Worksheet) xlSheets.Add( xlSheets[ 1 ], Type.Missing, Type.Missing, Type.Missing ) ;
          var xlRange = sheet.UsedRange ;
          var endRow = xlRange.Rows.Count ;

          //Copy shapes to new sheet
          var range1 = sheet.Range[ startCell, selectColumn + endRow ] ;
          var range2 = newSheet.Range[ startCell, selectColumn + endRow ] ;
          range2.ColumnWidth = range1.ColumnWidth ;
          range1.Copy( range2 ) ;

          //convert shapes to image
          var isException = true ;
          var countWhile = 0 ;
          // if having a problem regarding clipboard, try again less than 3 times, before giving up with an error message.
          while ( isException && countWhile < 3 ) {
            try {
              symbolImages = new List<SymbolImage>() ;
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
                  symbolImages.Add( new SymbolImage( block.Key, image, marginLeft ) ) ;
                  isException = false ;
                }
              }
              else {
                break ;
              }
            }
            catch {
              isException = true ;
              countWhile++ ;
            }
          }
        }
      }
      catch ( Exception e ) {
        MessageBox.Show( " Error: " + e ) ;
      }
      finally {
        excelWorkbook?.Close( false ) ;
        if ( excelWorkbook != null ) Marshal.ReleaseComObject( excelWorkbook ) ;
        excelApp.Quit() ;
        Marshal.ReleaseComObject( excelApp ) ;
      }

      return symbolImages ;
    }

    public static List<RegistrationOfBoardDataModel> GetAllRegistrationOfBoardDataModel(string path)
    {
      List<RegistrationOfBoardDataModel> registrationOfBoardDataModelData = new List<RegistrationOfBoardDataModel>();

      var extension = Path.GetExtension(path);
      using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
      try
      {
        ISheet? workSheet = null;
        switch (string.IsNullOrEmpty(extension))
        {
          case false when extension == ".xls":
          {
            HSSFWorkbook wb = new HSSFWorkbook(fs);
            workSheet = wb.NumberOfSheets < 2 ? wb.GetSheetAt(wb.ActiveSheetIndex) : wb.GetSheetAt(1);
            break;
          }
          case false when extension == ".xlsx":
          {
            XSSFWorkbook wb = new XSSFWorkbook(fs);
            workSheet = wb.NumberOfSheets < 2 ? wb.GetSheetAt(wb.ActiveSheetIndex) : wb.GetSheetAt(1);
            break;
          }
        }

        if (workSheet == null) return registrationOfBoardDataModelData;

        const int startRow = 7;
        var endRow = workSheet.LastRowNum;

        // Get list data
        for (var i = startRow; i <= endRow; i++)
        {
          var autoControlPanel = workSheet.GetRow(i).GetCell(0);
          var autoControlPanelValue = GetCellValue(autoControlPanel).Normalize( NormalizationForm.FormKC );

          var signalDestination = workSheet.GetRow(i).GetCell(1);
          var signalDestinationValue = GetCellValue(signalDestination).Normalize( NormalizationForm.FormKC );

          var kind1 = workSheet.GetRow(i).GetCell(2);
          var kind1Value = GetCellValue(kind1);
          
          var number1 = workSheet.GetRow(i).GetCell(3);
          var number1Value = GetCellValue(number1);
          
          var kind2 = workSheet.GetRow(i).GetCell(4);
          var kind2Value = GetCellValue(kind2);
          
          var number2 = workSheet.GetRow(i).GetCell(5);
          var number2Value = GetCellValue(number2);

          var remark = workSheet.GetRow(i).GetCell(6);
          var remarkValue = GetCellValue(remark);
          
          var materialCode1 = workSheet.GetRow(i).GetCell(7);
          var materialCode1Value = GetCellValue(materialCode1);
          
          var materialCode2 = workSheet.GetRow(i).GetCell(8);
          var materialCode2Value = GetCellValue(materialCode2);

          var item = new RegistrationOfBoardDataModel
          {
            AutoControlPanel = autoControlPanelValue,
            SignalDestination = signalDestinationValue,
            Kind1 = kind1Value,
            Number1 = number1Value,
            Kind2 = kind2Value,
            Number2 = number2Value,
            Remark = remarkValue,
            MaterialCode1 = materialCode1Value,
            MaterialCode2 = materialCode2Value
          } ;
          
          if (item.SignalDestination != "---")
          {
            registrationOfBoardDataModelData.Add(item);
          }
        }
      }
      catch (Exception)
      {
        return new List<RegistrationOfBoardDataModel>();
      }
      finally
      {
        fs.Close();
        fs.Dispose();
      }

      return registrationOfBoardDataModelData;
    }
  }

  public class SymbolImage
  {
    public SymbolImage( int position, Image image, float marginLeft )
    {
      this.Position = position ;
      Image = image ;
      MarginLeft = marginLeft ;
    }

    public int Position { get ; set ; }
    public Image Image { get ; set ; }
    public float MarginLeft { get ; set ; }
  }
}