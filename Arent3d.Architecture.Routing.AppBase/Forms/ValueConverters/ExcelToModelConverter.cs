using System ;
using System.Collections.Generic ;
using System.Globalization ;
using System.IO ;
using System.Linq ;
using System.Runtime.InteropServices ;
using System.Text ;
using System.Windows ;
using Arent3d.Architecture.Routing.Storable.Model ;
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
    public static List<CeedModel> GetAllCeeDModelNumber( string path, string path2, List<ConnectorFamilyTypeModel> connectorFamilyTypeModels )
    {
      const string defaultSymbol = "Dummy" ;
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
        const int startRow = 7 ;
        var endRow = workSheet.LastRowNum ;
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

        var listFloorPlanImages = GetSymbolImages( path, blocks, "F8", "F" ) ;

        #region Load Instrumentation Image (Comment out)

        //var listInstrumentationImages = GetSymbolImages( path, blocks, "G8", "G" ) ;

        #endregion

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
            if ( ! string.IsNullOrEmpty( symbol ) && ! symbol.Contains( "又は" ) ) floorPlanSymbol = symbol ;

            var instrumentationSymbolCell = workSheet.GetRow( j ).GetCell( 6 ) ;
            var instrumentSymbol = GetCellValue( instrumentationSymbolCell ) ;
            if ( ! string.IsNullOrEmpty( instrumentSymbol ) && ! instrumentSymbol.Contains( "又は" ) ) instrumentationSymbol = instrumentSymbol ;

            var conditionCell = workSheet.GetRow( j ).GetCell( 8 ) ;
            var condition = GetCellValue( conditionCell ) ;
            if ( ! string.IsNullOrEmpty( condition ) && condition.EndsWith( "の場合" ) ) conditions.Add( condition.Replace( "の場合", "" ).Replace( "・", "" ) ) ;
          }

          var symbolBytes = listFloorPlanImages.Where( b => b.Position == firstIndexGroup + 1 ).ToList().OrderBy( b => b.MarginLeft ) ;
          var floorPlanImages = symbolBytes.Select( b => b.Image ).ToList() ;
          var instrumentationImages = new List<Image>() ;

          #region Load Instrumentation Image (Comment out)

          //var instrumentationSymbolBytes = listInstrumentationImages.Where( b => b.Position == firstIndexGroup + 1).ToList().OrderBy(b=>b.MarginLeft) ;
          //var instrumentationImages = instrumentationSymbolBytes.Select( b => b.Image ).ToList() ;

          #endregion

          if ( ! ceeDModelNumbers.Any() ) {
            switch ( floorPlanImages.Count ) {
              case 1 when string.IsNullOrEmpty( floorPlanSymbol ) :
                CreateCeeDModel( ceedModelData, equipmentSymbols, string.Empty, string.Empty, generalDisplayDeviceSymbols, modelNumbers, floorPlanSymbol, instrumentationSymbol, ceeDName, string.Empty, floorPlanImages, instrumentationImages, true ) ;
                break ;
              case > 1 :
                floorPlanSymbol = defaultSymbol ;
                CreateCeeDModel( ceedModelData, equipmentSymbols, string.Empty, string.Empty, generalDisplayDeviceSymbols, modelNumbers, floorPlanSymbol, instrumentationSymbol, ceeDName, string.Empty, floorPlanImages, instrumentationImages, false, true ) ;
                break ;
              default :
                CreateCeeDModel( ceedModelData, equipmentSymbols, string.Empty, string.Empty, generalDisplayDeviceSymbols, modelNumbers, floorPlanSymbol, instrumentationSymbol, ceeDName, string.Empty, floorPlanImages, instrumentationImages ) ;
                break ;
            }
          }
          else {
            for ( var k = 0 ; k < ceeDModelNumbers.Count ; k++ ) {
              var ceeDSetCode = ceeDSetCodes.Any() ? ceeDSetCodes[ k ] : string.Empty ;
              var condition = conditions.Count > k ? conditions[ k ] : string.Empty ;
              switch ( floorPlanImages.Count ) {
                case 1 when string.IsNullOrEmpty( floorPlanSymbol ) :
                  CreateCeeDModel( ceedModelData, equipmentSymbols, ceeDModelNumbers[ k ], ceeDSetCode, generalDisplayDeviceSymbols, modelNumbers, floorPlanSymbol, instrumentationSymbol, ceeDName, condition, floorPlanImages, instrumentationImages, true ) ;
                  break ;
                case > 1 :
                  floorPlanSymbol = defaultSymbol ;
                  CreateCeeDModel( ceedModelData, equipmentSymbols, ceeDModelNumbers[ k ], ceeDSetCode, generalDisplayDeviceSymbols, modelNumbers, floorPlanSymbol, instrumentationSymbol, ceeDName, condition, floorPlanImages, instrumentationImages, false, true ) ;
                  break ;
                default :
                  CreateCeeDModel( ceedModelData, equipmentSymbols, ceeDModelNumbers[ k ], ceeDSetCode, generalDisplayDeviceSymbols, modelNumbers, floorPlanSymbol, instrumentationSymbol, ceeDName, condition, floorPlanImages, instrumentationImages ) ;
                  break ;
              }
            }
          }

          i-- ;
        }

        SetFamilyTypeName( ceedModelData, connectorFamilyTypeModels ) ;
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

    private static void CreateCeeDModel( List<CeedModel> ceeDModelData, List<EquipmentSymbol> equipmentSymbols, string ceeDModelNumber, string ceeDSetCode, string generalDisplayDeviceSymbols, List<string> modelNumbers, string floorPlanSymbol, string instrumentationSymbol, string ceeDName, string condition, List<Image> floorPlanImages, List<Image> instrumentationImages, bool isFloorPlanImages = false, bool isDummySymbol = false )
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
              AddCeedModel( ceeDModelData, ceeDModelNumber, ceeDSetCode, generalDisplayDeviceSymbol, modelNumber, floorPlanSymbol, instrumentationSymbol, ceeDName, condition, floorPlanImages, instrumentationImages, isFloorPlanImages, isDummySymbol ) ;
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
          if ( symbolModelNumber.Any() )
            foreach ( var modelNumber in symbolModelNumber ) {
              AddCeedModel( ceeDModelData, ceeDModelNumber, ceeDSetCode, generalDisplayDeviceSymbol, modelNumber, floorPlanSymbol, instrumentationSymbol, ceeDName, condition, floorPlanImages, instrumentationImages, isFloorPlanImages, isDummySymbol ) ;
            }
          else {
            if ( equipmentSymbols.Any() ) {
              var modelNumberList = equipmentSymbols.Where( s => s.Symbol == generalDisplayDeviceSymbol ).Select( s => s.ModelNumber ).Distinct().ToList() ;
              if ( modelNumberList.Any() ) {
                foreach ( var modelNumber in modelNumberList ) {
                  AddCeedModel( ceeDModelData, ceeDModelNumber, ceeDSetCode, generalDisplayDeviceSymbol, modelNumber, floorPlanSymbol, instrumentationSymbol, ceeDName, condition, floorPlanImages, instrumentationImages, isFloorPlanImages, isDummySymbol ) ;
                }
              }
              else {
                AddCeedModel( ceeDModelData, ceeDModelNumber, ceeDSetCode, generalDisplayDeviceSymbol, string.Empty, floorPlanSymbol, instrumentationSymbol, ceeDName, condition, floorPlanImages, instrumentationImages, isFloorPlanImages, isDummySymbol ) ;
              }
            }
            else {
              AddCeedModel( ceeDModelData, ceeDModelNumber, ceeDSetCode, generalDisplayDeviceSymbol, string.Empty, floorPlanSymbol, instrumentationSymbol, ceeDName, condition, floorPlanImages, instrumentationImages, isFloorPlanImages, isDummySymbol ) ;
            }
          }
        }
      }
    }

    private static void AddCeedModel( ICollection<CeedModel> ceeDModelData, string ceeDModelNumber, string ceeDSetCode, string generalDisplayDeviceSymbol, string modelNumber, string floorPlanSymbol, string instrumentationSymbol, string ceeDName, string condition, List<Image> floorPlanImages, List<Image> instrumentationImages, bool isFloorPlanImages, bool isDummySymbol )
    {
      if ( isFloorPlanImages )
        ceeDModelData.Add( new CeedModel( ceeDModelNumber, ceeDSetCode, generalDisplayDeviceSymbol, modelNumber, floorPlanImages, instrumentationImages, floorPlanSymbol, instrumentationSymbol, ceeDName, condition, string.Empty, string.Empty ) ) ;
      else if ( isDummySymbol )
        ceeDModelData.Add( new CeedModel( ceeDModelNumber, ceeDSetCode, generalDisplayDeviceSymbol, modelNumber, floorPlanImages, instrumentationImages, floorPlanSymbol, instrumentationSymbol, ceeDName, condition, string.Empty ) ) ;
      else 
        ceeDModelData.Add( new CeedModel( ceeDModelNumber, ceeDSetCode, generalDisplayDeviceSymbol, modelNumber, floorPlanSymbol, instrumentationSymbol, ceeDName, condition, string.Empty, string.Empty, string.Empty ) ) ;
    }

    private static void SetFamilyTypeName( IEnumerable<CeedModel> ceeDModelData, ICollection<ConnectorFamilyTypeModel> connectorFamilyTypeModels )
    {
      var familyType = connectorFamilyTypeModels.Select( c => c.Base64Images ).ToList() ;
      foreach ( var ceedModel in ceeDModelData ) {
        if ( ! string.IsNullOrEmpty( ceedModel.Base64FloorPlanImages ) ) {
          SetConnectorFamilyTypeModel( ceedModel, connectorFamilyTypeModels, familyType, ceedModel.Base64FloorPlanImages ) ;
        }
        else if ( ! string.IsNullOrEmpty( ceedModel.FloorPlanSymbol ) ) {
          SetConnectorFamilyTypeModel( ceedModel, connectorFamilyTypeModels, familyType, ceedModel.FloorPlanSymbol ) ;
        }
        else {
          ceedModel.FamilyTypeName = string.Empty ;
        }
      }
    }

    private static void SetConnectorFamilyTypeModel( CeedModel ceedModel, ICollection<ConnectorFamilyTypeModel> connectorFamilyTypeModels, ICollection<string> familyType, string floorPlanSymbol )
    {
      const string defaultFamilyTypeName = "FamilyType" ;
      if ( familyType.Contains( floorPlanSymbol ) ) {
        var connectorFamilyType = connectorFamilyTypeModels.FirstOrDefault( c => c.Base64Images == floorPlanSymbol ) ;
        if ( connectorFamilyType == null ) {
          ceedModel.FamilyTypeName = defaultFamilyTypeName + ( connectorFamilyTypeModels.Count + 1 ) ;
          connectorFamilyTypeModels.Add( new ConnectorFamilyTypeModel( floorPlanSymbol, ceedModel.FamilyTypeName, string.Empty ) ) ;
        }
        else {
          ceedModel.FamilyTypeName = connectorFamilyType.FamilyTypeName ;
        }
      }
      else {
        familyType.Add( floorPlanSymbol ) ;
        ceedModel.FamilyTypeName = defaultFamilyTypeName + ( connectorFamilyTypeModels.Count + 1 ) ;
        connectorFamilyTypeModels.Add( new ConnectorFamilyTypeModel( floorPlanSymbol, ceedModel.FamilyTypeName, string.Empty ) ) ;
      }
    }

    public static void SetConnectorFamilyTypeName( IEnumerable<ConnectorFamilyTypeModel> connectorFamilyTypeModels )
    {
      foreach ( var connectorFamilyTypeModel in connectorFamilyTypeModels ) {
        if ( ! string.IsNullOrEmpty( connectorFamilyTypeModel.ConnectorFamilyTypeName ) ) continue ;
        var familyTypeName = connectorFamilyTypeModel.FamilyTypeName ;
        switch ( familyTypeName ) {
          case "FamilyType1" :
          case "FamilyType2" :
          case "FamilyType3" :
          case "FamilyType4" :
          case "FamilyType83" :
          case "FamilyType97" :
          case "FamilyType98" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide1.GetFieldName() ;
            break ;
          case "FamilyType7" :
          case "FamilyType8" :
          case "FamilyType9" :
          case "FamilyType10" :
          case "FamilyType29" :
          case "FamilyType31" :
          case "FamilyType32" :
          case "FamilyType35" :
          case "FamilyType36" :
          case "FamilyType37" :
          case "FamilyType38" :
          case "FamilyType39" :
          case "FamilyType40" :
          case "FamilyType41" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide2.GetFieldName() ;
            break ;
          case "FamilyType11" :
          case "FamilyType12" :
          case "FamilyType13" :
          case "FamilyType33" :
          case "FamilyType34" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide5.GetFieldName() ;
            break ;
          case "FamilyType14" :
          case "FamilyType15" :
          case "FamilyType16" :
          case "FamilyType17" :
          case "FamilyType22" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide6.GetFieldName() ;
            break ;
          case "FamilyType18" :
          case "FamilyType19" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide7.GetFieldName() ;
            break ;
          case "FamilyType20" :
          case "FamilyType21" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide8.GetFieldName() ;
            break ;
          case "FamilyType23" :
          case "FamilyType25" :
          case "FamilyType26" :
          case "FamilyType30" :
          case "FamilyType42" :
          case "FamilyType43" :
          case "FamilyType44" :
          case "FamilyType45" :
          case "FamilyType46" :
          case "FamilyType47" :
          case "FamilyType48" :
          case "FamilyType49" :
          case "FamilyType50" :
          case "FamilyType51" :
          case "FamilyType52" :
          case "FamilyType53" :
          case "FamilyType54" :
          case "FamilyType55" :
          case "FamilyType56" :
          case "FamilyType57" :
          case "FamilyType58" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide9.GetFieldName() ;
            break ;
          case "FamilyType27" :
          case "FamilyType59" :
          case "FamilyType60" :
          case "FamilyType61" :
          case "FamilyType62" :
          case "FamilyType63" :
          case "FamilyType64" :
          case "FamilyType65" :
          case "FamilyType66" :
          case "FamilyType67" :
          case "FamilyType68" :
          case "FamilyType69" :
          case "FamilyType78" : 
          case "FamilyType99" :
          case "FamilyType100" :
          case "FamilyType101" :
          case "FamilyType102" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide10.GetFieldName() ;
            break ;
          case "FamilyType90" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide11.GetFieldName() ;
            break ;
          case "FamilyType70" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide19.GetFieldName() ;
            break ;
          case "FamilyType71" :
          case "FamilyType72" :
          case "FamilyType73" :
          case "FamilyType74" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide20.GetFieldName() ;
            break ;
          case "FamilyType75" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide21.GetFieldName() ;
            break ;
          case "FamilyType76" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide22.GetFieldName() ;
            break ;
          case "FamilyType77" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide23.GetFieldName() ;
            break ;
          case "FamilyType79" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide24.GetFieldName() ;
            break ;
          case "FamilyType80" :
          case "FamilyType82" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide25.GetFieldName() ;
            break ;
          case "FamilyType81" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide26.GetFieldName() ;
            break ;
          case "FamilyType84" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide27.GetFieldName() ;
            break ;
          case "FamilyType87" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide28.GetFieldName() ;
            break ;
          case "FamilyType85" :
          case "FamilyType86" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide29.GetFieldName() ;
            break ;
          case "FamilyType88" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide30.GetFieldName() ;
            break ;
          case "FamilyType89" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide31.GetFieldName() ;
            break ;
          case "FamilyType91" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide32.GetFieldName() ;
            break ;
          case "FamilyType92" :
          case "FamilyType93" :
          case "FamilyType94" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide33.GetFieldName() ;
            break ;
          case "FamilyType95" :
          case "FamilyType96" :
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide34.GetFieldName() ;
            break ;
          default:
            connectorFamilyTypeModel.ConnectorFamilyTypeName = ConnectorOneSideFamilyType.ConnectorOneSide1.GetFieldName() ;
            break;
        }
      }
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
            using StreamReader reader = new StreamReader( path, Encoding.GetEncoding( "shift-jis" ), true ) ;
            List<string> lines = new List<string>() ;
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
      const string sheetName = "セットコード一覧表" ;
      var symbolImages = new List<SymbolImage>() ;
      var excelApp = new Application { Visible = false, ScreenUpdating = false, DisplayStatusBar = false, EnableEvents = false } ;

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