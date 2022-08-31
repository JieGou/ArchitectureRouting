using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;
using System ;
using System.Globalization ;
using System.IO ;
using System.Linq ;
using System.Reflection ;
using System.Text ;
using System.Windows ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.ViewModel.Models ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using NPOI.XSSF.UserModel ;
using NPOI.SS.UserModel ;
using NPOI.SS.Util ;
using MessageBox = System.Windows.Forms.MessageBox ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit.I18n ;
using CellType = NPOI.SS.UserModel.CellType ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Microsoft.Win32 ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class PickUpReportViewModel : NotifyPropertyChanged
  {
    #region Constances

    private const string SummaryFileType = "拾い出し集計表" ;
    private const string ConfirmationFileType = "拾い根拠確認表" ;
    private const string OnText = "ON" ;
    private const string OffText = "OFF" ;
    private const string SummaryFileName = "_拾い出し集計表.xlsx" ;
    private const string ConfirmationFileName = "_拾い根拠確認表.xlsx" ;
    private const string DefaultConstructionItem = "未設定" ;
    private const string LengthItem = "長さ物" ;
    private const string ConstructionMaterialItem = "工事部材" ;
    private const string EquipmentMountingItem = "機器取付" ;
    private const string WiringItem = "結線" ;
    private const string BoardItem  = "盤搬入据付" ;
    private const string InteriorRepairEquipmentItem = "内装・補修・設備" ;
    private const string OtherItem = "その他" ;
    private const string SummaryTemplateFileName = "拾い出し集計表_template.xls" ;
    private const string ConfirmationTemplateFileName = "拾い根拠確認表_template.xls" ;
    private const string ResourceFolderName = "resources" ;

    #endregion Constances

    #region Properties and Fields

    private readonly Document _document ;
    private readonly List<HiroiMasterModel> _hiroiMasterModels ;
    private readonly IEnumerable<PickUpItemModel> _pickUpItemModels ;
    
    public IEnumerable<PickUpSettingItem> FileTypeSettings { get ; }

    public IEnumerable<PickUpSettingItem> OutputReportSettingCollection { get ; private set ; }

    private string _pathName ;

    public string PathName
    {
      get => _pathName ;
      set
      {
        _pathName = value ;
        OnPropertyChanged();
      }
    }
    
    public bool IsExportCsvFile { get ; set ; }

    private string _fileName ;

    public string FileName
    {
      get => _fileName ;
      set
      {
        _fileName = value ;
        OnPropertyChanged();
      }
    }

    private bool _isOutputItemsEnable ;

    public bool IsOutputItemsEnable
    {
      get => _isOutputItemsEnable;
      set
      {
        _isOutputItemsEnable = value ;
        OnPropertyChanged() ;
      }
    }
    
    private bool _isPickUpNumberOn ;

    public bool IsPickUpNumberOn
    {
      get => _isPickUpNumberOn ;
      set
      {
        if ( value == _isPickUpNumberOn ) return ;
        _isPickUpNumberOn = value ;
        OnPropertyChanged() ;
      }
    }
    
    private string PickUpNumberStatusString => IsPickUpNumberOn ? OnText : OffText ;

    #endregion Properties and Fields

    #region Command

    public ICommand GetSaveLocationCommand => new RelayCommand( OnGetSaveLocationExecute ) ;
    public ICommand CancelCommand => new RelayCommand<Window>( OnCancelExecute ) ;
    public ICommand ExportFileCommand => new RelayCommand<Window>( OnExportFileExecute ) ;
    public ICommand SettingCommand => new RelayCommand( OnShowOutputSettingExecute ) ;
    public ICommand ApplyOutputSettingCommand => new RelayCommand<Window>( OnApplyOutputSettingExecute ) ;
    
    #endregion Command

    #region Constructor
    
    public PickUpReportViewModel( Document document, List<PickUpItemModel>? pickUpItemModels = null, bool isExportCsvFile = true)
    {
      _document = document ;
      IsPickUpNumberOn = true ;
      IsExportCsvFile = isExportCsvFile ;
      FileTypeSettings =  GetFileTypeSettings();
      OutputReportSettingCollection = GetOutputReportSettings();
      _hiroiMasterModels = GetHiroiMasterModels() ;
      _pathName = string.Empty ;
      _fileName = string.Empty ;
      _pickUpItemModels = InitPickUpModels(pickUpItemModels) ;
    }

    #endregion Constructor
    
    #region Initialize
    
    private IEnumerable<PickUpItemModel> InitPickUpModels(List<PickUpItemModel>? pickUpItemModels = null)
    {
      if ( pickUpItemModels != null ) return new List<PickUpItemModel>( pickUpItemModels );
      
      var dataStorage = _document.FindOrCreateDataStorage<PickUpModel>( false ) ;
      var storagePickUpService = new StorageService<DataStorage, PickUpModel>( dataStorage ) ;
      var version = storagePickUpService.Data.PickUpData.Any() ? storagePickUpService.Data.PickUpData.Max( x => x.Version ) : string.Empty ;
      return ! string.IsNullOrEmpty( version ) ? 
        new List<PickUpItemModel>( storagePickUpService.Data.PickUpData.Where( x => x.Version == version ) ) :
        new List<PickUpItemModel>() ;
    }

    private static IEnumerable<PickUpSettingItem> GetFileTypeSettings()
    {
      yield return new PickUpSettingItem( SummaryFileType, false ) ;
      yield return new PickUpSettingItem( ConfirmationFileType, false )  ;
    }

    public static IEnumerable<PickUpSettingItem> GetOutputReportSettings()
    {
      yield return new PickUpSettingItem( LengthItem, true )  ;
      yield return new PickUpSettingItem( ConstructionMaterialItem, true )  ;
      yield return new PickUpSettingItem( EquipmentMountingItem, false ) ;
      yield return new PickUpSettingItem( WiringItem, false ) ;
      yield return new PickUpSettingItem( BoardItem, false ) ;
      yield return new PickUpSettingItem( InteriorRepairEquipmentItem, true ) ;
      yield return new PickUpSettingItem( OtherItem, false ) ;
    }
    
    private List<HiroiMasterModel> GetHiroiMasterModels()
    {
      var csvStorable = _document.GetAllStorables<CsvStorable>().FirstOrDefault() ;
      return csvStorable != null ? csvStorable.HiroiMasterModelData : new List<HiroiMasterModel>() ;
    }

    #endregion Initialize

    #region Command execute and can execute
    
    private void OnGetSaveLocationExecute()
    {
      const string tempFileName = "フォルダを選択してください。" ;
      var saveFileDialog = new SaveFileDialog
      {
        FileName = "App.SelectFolder".GetAppStringByKeyOrDefault( tempFileName ), InitialDirectory = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments )
      } ;

      if ( saveFileDialog.ShowDialog() is not true ) return ;
      PathName = Path.GetDirectoryName( saveFileDialog.FileName )! ;
    }

    private void OnExportFileExecute( Window window )
    {
      if ( !CanExecuteExportFile( out var errorMess, out var outputPickUpModels, out var fileNames ) ) {
        MessageBox.Show( errorMess, @"Warning!" ) ;
        return;
      }

      try {
        if ( ! IsExportCsvFile ) {
          ExportToDatFile( outputPickUpModels ) ;
        }
        else {
          ExportToExcelFile( fileNames,outputPickUpModels ) ;  
        }
        
        MessageBox.Show( @"Export pick-up output file successfully.", @"Message" ) ;
      }
      catch ( Exception ex ) {
        MessageBox.Show( @"Export file failed because " + ex, @"Error message" ) ;
      }
      finally {
        window.DialogResult = true ;
        window.Close() ;  
      }
    }
    
    private static void OnCancelExecute( Window window)
    {
      window.DialogResult = false ;
      window.Close() ;
    }
    
    private void OnShowOutputSettingExecute()
    {
      var settingOutputPickUpReport = new SettingOutputPickUpReport( this ) ;
      
      var previousOutputSettingCollection = ( from outPutSettingItem in OutputReportSettingCollection select new PickUpSettingItem(outPutSettingItem.Name,outPutSettingItem.IsSelected) ).ToList() ;
      
      settingOutputPickUpReport.ShowDialog();

      if ( settingOutputPickUpReport.DialogResult == true ) return ;

      OutputReportSettingCollection = previousOutputSettingCollection ;
    }
    
    private static void OnApplyOutputSettingExecute( Window window )
    {
      window.DialogResult = true ;
      window.Close() ;
    }

    private bool CanExecuteExportFile( out string errorMess, out IEnumerable<PickUpItemModel> pickUpModels, out IEnumerable<string> fileNames )
    {
      var errorMessList = new List<string>() ;
      errorMess = "Please" ;
      var isCanExport = ! string.IsNullOrEmpty( PathName ) && ! string.IsNullOrEmpty( FileName ) ;
      fileNames = GetFileList().EnumerateAll() ;
      if ( IsExportCsvFile ) {
        isCanExport = isCanExport && fileNames.Any() ;
        if (!fileNames.Any()) errorMessList.Add( "select the file type" ) ; 
      }
      if ( string.IsNullOrEmpty( PathName ) ) errorMessList.Add( "select the output folder" ) ;
      if ( string.IsNullOrEmpty( FileName ) ) errorMessList.Add( "input the file name" ) ;
      var errMessCount = errorMessList.Count ;

      if ( errMessCount == 1 ) errorMess += $" {errorMessList.FirstOrDefault()}" ;
      else {
        for ( var i = 0 ; i < errMessCount ; i++ ) {
          errorMess += $" {errorMessList[ i ]}" ;
          if ( i < errMessCount - 1 ) {
            errorMess += $",{Environment.NewLine}and" ;
          }
        }

        errorMess += "!" ;
      }

      if ( ! isCanExport ) {
        pickUpModels = new List<PickUpItemModel>() ;
        return false ;
      }

      pickUpModels = GetOutputPickUpModels().EnumerateAll() ;

      if ( pickUpModels.Any() ) return true ;
      errorMess = "Don't have pick up data." ;
      return false ;
    }

    #endregion Command execute and can excute

    #region Excel File Handle

    private string GetFileName(string fileName)
    {
      return string.IsNullOrEmpty( _fileName ) ? fileName : $"{_fileName}{fileName}" ;
    }

    private IEnumerable<string> GetFileList()
    {
      var fileTypes = FileTypeSettings.Where( f => f.IsSelected ).Select( f => f.Name ) ;
      var pickUpNumberStatus = $"{FileName}{PickUpNumberStatusString}" ;
      foreach ( var fileType in fileTypes ) {
        yield return $"{pickUpNumberStatus}_{fileType}.xlsx" ;
      }
    }

    private static List<string> GetConstructionItemList(IEnumerable<PickUpItemModel>  pickUpItemModels)
    {
      var constructionItemList = new List<string>() ;
      foreach ( var pickUpModel in pickUpItemModels.Where( pickUpModel =>
                 ! constructionItemList.Contains( pickUpModel.ConstructionItems ) && pickUpModel.EquipmentType == PickUpViewModel.ProductType.Conduit.GetFieldName() ) ) {
        constructionItemList.Add( pickUpModel.ConstructionItems ) ;
      }

      return constructionItemList ;
    }

    private void ExportToExcelFile(IEnumerable<string> fileNames, IEnumerable<PickUpItemModel> outputPickUpModels )
    {
      var constructionItemList = GetConstructionItemList( outputPickUpModels ) ;
      if ( ! constructionItemList.Any() ) constructionItemList.Add( DefaultConstructionItem ) ;
      foreach ( var fileName in fileNames ) {
        var workbook = new XSSFWorkbook() ;

        if ( fileName.Contains( SummaryFileName ) ) {
          workbook = CreateWorkBook( constructionItemList, outputPickUpModels, SummaryTemplateFileName, SheetType.Summary ) ;
        }
        else if ( fileName.Contains( ConfirmationFileName ) ) {
          workbook = CreateWorkBook( constructionItemList, outputPickUpModels, ConfirmationTemplateFileName, SheetType.Confirmation ) ;
        }

        workbook.RemoveSheetAt( 0 ) ;
        var fileNameToOutPut = GetFileName( fileName ) ;
        var fs = new FileStream( PathName + @"\" + fileNameToOutPut, FileMode.OpenOrCreate ) ;
        workbook.Write( fs ) ;
        workbook.Close() ;
        workbook.Close() ;
        fs.Close() ;
      }
    }

    private XSSFWorkbook CreateWorkBook(IEnumerable<string> constructionItemList, IEnumerable<PickUpItemModel> outputPickUpModels, string templateFileName, SheetType sheetType )
    {
      var resourcesPath = Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location )!, ResourceFolderName ) ;
      var filePath = Path.Combine( resourcesPath, templateFileName ) ;
      using var fsStream = new FileStream( filePath, FileMode.Open, FileAccess.Read ) ;
      var workbook = new XSSFWorkbook( fsStream ) ;
      foreach ( var sheetName in constructionItemList ) {
        var sheetCopy = workbook.GetSheetAt( 0 ).CopySheet( sheetName, true ) ;
        CreateSheet( sheetType, workbook, sheetCopy, sheetName, outputPickUpModels ) ;
      }

      return workbook ;
    }

    private enum SheetType
    {
      Confirmation,
      Summary
    }

    private void CreateSheet( SheetType sheetType, IWorkbook workbook, ISheet sheet, string sheetName, IEnumerable<PickUpItemModel> pickUpItemModels )
    {
      List<string> levels = _document.GetAllElements<Level>().Select( l => l.Name ).ToList() ;
      var codeList = GetCodeList(pickUpItemModels) ;
      var fileName = FileName ;
      int rowStart ;
      switch ( sheetType ) {
        case SheetType.Confirmation :
          rowStart = 0 ;
          var view = _document.ActiveView ;
          var scale = view.Scale ;
          HeightSettingStorable settingStorables = _document.GetHeightSettingStorable() ;
          var numberCount = 0 ;
          
          foreach ( var level in levels ) {
            if( pickUpItemModels.All( x => x.Floor != level ) ) continue;
            if( pickUpItemModels.Where( x => x.Floor == level ).All( p => p.ConstructionItems != sheetName ) ) continue;
            numberCount++ ;
            var height = settingStorables.HeightSettingsData.Values.FirstOrDefault( x => x.LevelName == level )?.Elevation ?? 0 ;
            
            rowStart = numberCount == 1 ? CreateHeaderConfirmation( sheet, rowStart, fileName, sheetName, level, scale, height ) : CreateTemplateConfirmation( workbook, workbook.GetSheetAt( 0 ), sheet, rowStart, fileName, sheetName, level, scale, height ) ;
            
            List<KeyValuePair<string, List<PickUpItemModel>>> dictionaryDataPickUpModel = new List<KeyValuePair<string, List<PickUpItemModel>>>() ;
            
            foreach ( var code in codeList ) {
              var dataPickUpModels = pickUpItemModels
                .Where( p => p.ConstructionItems == sheetName && p.Construction == code && p.Floor == level )
                .GroupBy( x => x.ProductCode, ( key, p ) => new { ProductCode = key, PickUpModels = p.ToList() } ) ;
            
              foreach ( var dataPickUpModel in dataPickUpModels ) {
                if ( dictionaryDataPickUpModel.Any( l => l.Key == dataPickUpModel.ProductCode ) && ! IsLengthObject( dataPickUpModel.PickUpModels.First() ) ) {
                  var dataPickUpModelExist = dictionaryDataPickUpModel.Single( x => x.Key == dataPickUpModel.ProductCode ) ;
                  dataPickUpModelExist.Value.AddRange( dataPickUpModel.PickUpModels );
                }
                else {
                  dictionaryDataPickUpModel.Add( new KeyValuePair<string, List<PickUpItemModel>>(dataPickUpModel.ProductCode, dataPickUpModel.PickUpModels) );
                }
              }
            }

            var dictionaryDataPickUpModelOrder = dictionaryDataPickUpModel.OrderBy( x => x.Value.First().Tani == "m" ? 1 : 2).ThenBy( c => c.Value.First().ProductName ).ThenBy( c => c.Value.First().Standard ) ; ;
            var pickUpNumberForConduitsToPullBox = WireLengthNotationManager.GetPickUpNumberForConduitsToPullBox(_document,pickUpItemModels.Where( p=> p.Floor == level ).ToList()) ;
            
            int countNum = 0 ;

            var lastItem = dictionaryDataPickUpModelOrder.Last() ;
            foreach ( var dataPickUpModel in dictionaryDataPickUpModelOrder) {
              var isLastItem = dataPickUpModel.Equals( lastItem ) ;
              rowStart = AddConfirmationPickUpRow( dataPickUpModel.Value, sheet, rowStart, pickUpNumberForConduitsToPullBox, workbook, fileName, sheetName, level, scale, height, ref countNum , isLastItem) ;
              if ( isLastItem ) rowStart -- ;
            }
            
            while ( (rowStart + 1) % 62 != 0 ) {
              rowStart++ ;
            }
            
            rowStart += 1 ;
            sheet.SetRowBreak( rowStart - 1 );
          }
          break ;
        case SheetType.Summary :
          rowStart = 0 ;
          Dictionary<int, string> levelColumns = new Dictionary<int, string>() ;
          var index = 5 ;
          foreach ( var level in levels ) {
            if(pickUpItemModels.All( x => x.Floor != level )) continue;
            levelColumns.Add( index, level ) ;
            index++ ;
          }
          
          rowStart = CreateHeaderSummary( sheet, rowStart, fileName, sheetName, levelColumns ) ;
          
          List<KeyValuePair<string, List<PickUpItemModel>>> dictionaryDataPickUpModelSummary = new List<KeyValuePair<string, List<PickUpItemModel>>>() ;
          foreach ( var code in codeList ) {
            var dataPickUpModels = pickUpItemModels
              .Where( p => p.ConstructionItems == sheetName && p.Construction == code )
              .GroupBy( x => x.ProductCode, ( key, p ) => new { ProductCode = key, PickUpModels = p.ToList() } ) ;
            foreach ( var dataPickUpModel in dataPickUpModels ) {
              if ( dictionaryDataPickUpModelSummary.Any( l => l.Key == dataPickUpModel.ProductCode ) && ! IsLengthObject( dataPickUpModel.PickUpModels.First() ) ) {
                var dataPickUpModelExist = dictionaryDataPickUpModelSummary.Single( x => x.Key == dataPickUpModel.ProductCode ) ;
                dataPickUpModelExist.Value.AddRange( dataPickUpModel.PickUpModels );
              }
              else {
                dictionaryDataPickUpModelSummary.Add( new KeyValuePair<string, List<PickUpItemModel>>(dataPickUpModel.ProductCode, dataPickUpModel.PickUpModels) );
              }
            }
          }
          
          var dictionaryDataPickUpModelOrderSummary = dictionaryDataPickUpModelSummary.OrderBy( x => x.Value.First().Tani == "m" ? 1 : 2).ThenBy( c => c.Value.First().ProductName ).ThenBy( c => c.Value.First().Standard ).ToList() ;

          int count = 0 ;
          
          foreach ( var dataPickUpModel in dictionaryDataPickUpModelOrderSummary ) {
            if ( rowStart + 2 == (dictionaryDataPickUpModelOrderSummary.Count * 2 + 4)) {
              rowStart = AddSummaryPickUpRow( dataPickUpModel.Value, sheet, rowStart, levelColumns, index ) ;
            }
            else {
              rowStart = AddSummaryPickUpRow( dataPickUpModel.Value, sheet, rowStart, levelColumns, index ) ;
            }

            count++ ;
            if ( rowStart % 54 == 0 && count != dictionaryDataPickUpModelOrderSummary.Count) {
              sheet.SetRowBreak( rowStart - 1 ) ;
              rowStart = CreateTemplateSummary( workbook, workbook.GetSheetAt( 0 ), sheet, rowStart , fileName, sheetName, levelColumns ) ;
            }
          }
          
          break ;
      }
    }

    private void CopyFromSourceToDestinationRow(IWorkbook workbook, ISheet sourceWorksheet, ISheet destinationWorksheet, int sourceRowNum, int destinationRowNum) {
        // Get the source / new row
        XSSFRow sourceRow = (XSSFRow)sourceWorksheet.GetRow(sourceRowNum);
        XSSFRow newRow = (XSSFRow)destinationWorksheet.CreateRow(destinationRowNum);

      
        // Set heightfrom old row and apply to new row
        newRow.HeightInPoints = sourceRow.HeightInPoints ;
        
        // Loop through source columns to add to new row
        for (int i = 0; i < sourceRow.LastCellNum; i++) {
            // Grab a copy of the old/new cell
            XSSFCell oldCell = (XSSFCell)sourceRow.GetCell(i);
            XSSFCell newCell = (XSSFCell)newRow.CreateCell(i);
            // If the old cell is null jump to next cell
            if (oldCell == null) {
                continue;
            }

            // Copy style from old cell and apply to new cell
            XSSFCellStyle newCellStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            newCellStyle.CloneStyleFrom(oldCell.CellStyle);
            newCell.CellStyle = newCellStyle ;

            // If there is a cell comment, copy
            if (oldCell.CellComment != null) {
              newCell.CellComment = oldCell.CellComment ;
            }

            // If there is a cell hyperlink, copy
            if (oldCell.Hyperlink != null) {
              newCell.Hyperlink = oldCell.Hyperlink ;
            }

            // Set the cell data type
            newCell.SetCellType( oldCell.CellType ) ;

            // Set the cell data value
            switch (oldCell.CellType) {
            case CellType.Blank:// Cell.CELL_TYPE_BLANK:
                newCell.SetCellValue( oldCell.StringCellValue );
                break;
            case CellType.Boolean:
                newCell.SetCellValue( oldCell.BooleanCellValue );
                break;
            case CellType.Formula:
                newCell.SetCellValue( oldCell.CellFormula );
                break;
            case CellType.Numeric:
                newCell.SetCellValue( oldCell.NumericCellValue );
                break;
            case CellType.String:
                newCell.SetCellValue( oldCell.StringCellValue );
                break;
            }
        }

        // If there are any merged regions in the source row, copy to new row
        for (int i = 0; i < sourceWorksheet.NumMergedRegions; i++) {
            CellRangeAddress cellRangeAddress = sourceWorksheet.GetMergedRegion(i);
            if (cellRangeAddress.FirstRow == sourceRow.RowNum) {
                CellRangeAddress newCellRangeAddress = new CellRangeAddress(newRow.RowNum,
                        (newRow.RowNum + (cellRangeAddress.LastRow - cellRangeAddress.FirstRow)),
                        cellRangeAddress.FirstColumn, cellRangeAddress.LastColumn);
                destinationWorksheet.AddMergedRegion(newCellRangeAddress);
            }
        }
    }

    private List<string> GetCodeList(IEnumerable<PickUpItemModel> pickUpItemModels )
    {
      var codeList = new List<string>() ;
      foreach ( var pickUpModel in pickUpItemModels.Where( pickUpModel => ! codeList.Contains( pickUpModel.Construction ) ) ) {
        codeList.Add( pickUpModel.Construction ) ;
      }
      return codeList ;
    }

    private int AddSummaryPickUpRow( 
      IReadOnlyCollection<PickUpItemModel> pickUpModels,
      ISheet sheet, 
      int rowStart,
      IReadOnlyDictionary<int, string> levelColumns, 
      int index
    )
    {
      if ( ! pickUpModels.Any() ) return rowStart ;
      var pickUpModel = pickUpModels.First() ;
      var rowName = sheet.GetRow( rowStart ) ;
      var isLengthObject = IsLengthObject( pickUpModel ) ;
      SetCellValue( rowName, 1, pickUpModel.ProductName ) ;
      SetCellValue( rowName, 4, isLengthObject ? "ｍ" : pickUpModel.Tani ) ;

      rowStart++ ;
      var rowStandard = sheet.GetRow( rowStart ) ;
      SetCellValue( rowStandard, 2, pickUpModel.Standard) ;

      double total = 0 ;
      for ( var i = 5 ; i < index ; i++ ) {
        double quantityFloor = 0 ;
        var level = levelColumns[ i ] ;
        quantityFloor = CalculateTotalByFloor( pickUpModels.Where( item => item.Floor == level ).ToList() ) ;
        SetCellValue( rowName, i, quantityFloor == 0 ? string.Empty : $"{Math.Round( quantityFloor, isLengthObject ? 1 : 2 )}" ) ;
        total += quantityFloor ;
      }
      
      SetCellValue( rowName, index, total == 0 ? string.Empty : $"{Math.Round( total, isLengthObject ? 1 : 2 )}") ;
      
      rowStart ++ ;
      return rowStart ;
    }

    private static List<string> GetPickUpNumbersList( List<PickUpItemModel> pickUpModels )
    {
      var pickUpNumberList = new List<string>() ;
      foreach ( var pickUpModel in pickUpModels.Where( pickUpModel => ! pickUpNumberList.Contains( pickUpModel.PickUpNumber ) ) ) {
        pickUpNumberList.Add( pickUpModel.PickUpNumber ) ;
      }

      return pickUpNumberList ;
    }

    private int AddConfirmationPickUpRow( List<PickUpItemModel> pickUpModels, ISheet sheet, int rowStart, IReadOnlyDictionary<string, int> pickUpNumberForConduitsToPullBox,
      IWorkbook workbook, string fileName, string sheetName, string level, int scale, double height, ref int countNum , bool isLastItem = false)
    {
      if ( ! pickUpModels.Any() ) return rowStart ;
      var pickUpNumbers = GetPickUpNumbersList( pickUpModels ) ;
      var pickUpModel = pickUpModels.First() ;

      if ( countNum == 59 && !isLastItem) {
        sheet.SetRowBreak( rowStart - 1 );
        rowStart = CreateTemplateConfirmation( workbook, workbook.GetSheetAt( 0 ), sheet, rowStart, fileName, sheetName, level, scale, height ) ;
        countNum = 0 ;
      }
      
      var row = sheet.GetRow( rowStart ) ;
      var isLengthObject = IsLengthObject( pickUpModel ) ;
      double total = 0 ;
      Dictionary<string, int> trajectory = new Dictionary<string, int>() ;
      var routes = RouteCache.Get( DocumentKey.Get( _document ) ) ;
      var inforDisplays = GetInfoDisplays( pickUpModels, routes ) ;
      var isMoreTwoWireBook = IsMoreTwoWireBook( pickUpModels ) ;

      SetCellValue( row, 1, pickUpModel.ProductName ) ;
      SetCellValue( row, 2, pickUpModel.Standard ) ;
      SetCellValue( row, 4, isLengthObject ? "ｍ" : pickUpModel.Tani ) ;
      foreach ( var pickUpNumber in pickUpNumbers ) {
        string stringNotTani = string.Empty ;
        Dictionary<string, double> notSeenQuantities = new Dictionary<string, double>() ;
        Dictionary<string, double> notSeenQuantitiesPullBox = new Dictionary<string, double>() ;
        var items = pickUpModels.Where( p => p.PickUpNumber == pickUpNumber ).ToList() ;
        var itemFirst = items.First() ;
        var wireBook = ( string.IsNullOrEmpty( itemFirst.WireBook ) || itemFirst.WireBook == "1" ) ? string.Empty : itemFirst.WireBook ;
        var itemsGroupByRoute = items.Where( item => ! string.IsNullOrEmpty( item.Quantity ) ).GroupBy( i => i.RelatedRouteName ) ;
        var listSeenQuantity = new List<double>() ;
        var listSeenQuantityPullBox = new List<string>() ;
        var valueDetailTableStr = string.Empty ;
        double totalBasedOnCreateTable = 0 ;
        foreach ( var itemGroupByRoute in itemsGroupByRoute ) {
          valueDetailTableStr = wireBook ;
          double seenQuantity = 0 ;

          var lastSegment = GetLastSegment( itemGroupByRoute.Key, routes ) ;
          var isSegmentConnectedToPullBox = IsSegmentConnectedToPullBox( lastSegment ) ;
          var isSegmentFromPowerToPullBox = IsSegmentFromPowerToPullBox( lastSegment ) ;
          var pickUpNumberForConduitToPullBox = pickUpNumberForConduitsToPullBox.SingleOrDefault( p => p.Key == itemGroupByRoute.Key ) ;
          var pickUpNumberPullBox = pickUpNumberForConduitToPullBox.Value ;

          foreach ( var item in itemGroupByRoute ) {
            double.TryParse( item.Quantity, out var quantity ) ;
            if ( ! string.IsNullOrEmpty( item.Direction ) ) {
              if ( isSegmentFromPowerToPullBox ) {
                if ( ! notSeenQuantitiesPullBox.Keys.Contains( item.Direction ) ) {
                  notSeenQuantitiesPullBox.Add( item.Direction, 0 ) ;
                }

                notSeenQuantitiesPullBox[ item.Direction ] += Math.Round(quantity,1) ;
              }
              else {
                if ( ! notSeenQuantities.Keys.Contains( item.Direction ) ) {
                  notSeenQuantities.Add( item.Direction, 0 ) ;
                }

                notSeenQuantities[ item.Direction ] += Math.Round(quantity,1) ;
              }
            }
            else {
              if ( ! isLengthObject ) stringNotTani += string.IsNullOrEmpty( stringNotTani ) ? item.SumQuantity : $"＋{item.SumQuantity}" ;
              seenQuantity += Math.Round(quantity,1) ;
            }

            totalBasedOnCreateTable += Math.Round(quantity,1) ;
          }

          if ( seenQuantity > 0 ) {
            if ( isSegmentConnectedToPullBox ) {
              var countStr = string.Empty ;
              var inforDisplay = inforDisplays.SingleOrDefault( x => x.RouteNameRef == itemGroupByRoute.Key ) ;
              var numberPullBox = IsPickUpNumberOn ? $"[{( pickUpNumberPullBox )}]" : string.Empty ;
              if ( inforDisplay != null && inforDisplay.IsDisplay ) {
                countStr = ( inforDisplay.NumberDisplay == 1 ? string.Empty : $"×{inforDisplay.NumberDisplay}" ) +
                           ( isMoreTwoWireBook ? $"×N" : string.IsNullOrEmpty( valueDetailTableStr ) ? string.Empty : $"×{valueDetailTableStr}" ) ;
                inforDisplay.IsDisplay = false ;
                if ( isSegmentFromPowerToPullBox ) {
                  listSeenQuantityPullBox.Add( numberPullBox +
                                               $"({Math.Round( seenQuantity, isLengthObject ? 1 : 2 ).ToString( CultureInfo.InvariantCulture )}＋↓{Math.Round( notSeenQuantitiesPullBox.First().Value, isLengthObject ? 1 : 2 )})" +
                                               countStr ) ;
                }
                else {
                  listSeenQuantityPullBox.Add( numberPullBox + Math.Round( seenQuantity, isLengthObject ? 1 : 2 ).ToString( CultureInfo.InvariantCulture ) + countStr ) ;
                }
              }
            }
            else {
              listSeenQuantity.Add( Math.Round( seenQuantity, isLengthObject ? 1 : 2 ) ) ;
            }
          }
        }

        total += ! string.IsNullOrEmpty( wireBook )
          ? Math.Round( totalBasedOnCreateTable, isLengthObject ? 1 : 2 ) * int.Parse( wireBook )
          : Math.Round( totalBasedOnCreateTable, isLengthObject ? 1 : 2 ) ;

        var number = IsPickUpNumberOn && ! string.IsNullOrEmpty( pickUpNumber ) ? "[" + pickUpNumber + "]" : string.Empty ;
        var seenQuantityStr = isLengthObject ? string.Join( "＋", listSeenQuantity ) : string.Join( "＋", stringNotTani.Split( '+' ) ) ;

        var seenQuantityPullBoxStr = string.Empty ;
        if ( listSeenQuantityPullBox.Any() ) {
          seenQuantityPullBoxStr = string.Join( $"＋", listSeenQuantityPullBox ) ;
        }

        var notSeenQuantityStr = string.Empty ;
        foreach ( var (_, value) in notSeenQuantities ) {
          notSeenQuantityStr += value > 0 ? "＋↓" + Math.Round( value, isLengthObject ? 1 : 2 ) : string.Empty ;
        }

        var key = isLengthObject
          ? ( "(" + seenQuantityStr + notSeenQuantityStr + ")" ) + ( string.IsNullOrEmpty( valueDetailTableStr ) ? string.Empty : $"×{valueDetailTableStr}" ) +
            ( string.IsNullOrEmpty( seenQuantityPullBoxStr ) ? string.Empty : $"＋{seenQuantityPullBoxStr}" )
          : ( seenQuantityStr + notSeenQuantityStr ) ;
        var itemKey = trajectory.FirstOrDefault( t => t.Key.Contains( key ) ).Key ;

        if ( string.IsNullOrEmpty( itemKey ) )
          trajectory.Add( number + key, 1 ) ;
        else {
          trajectory[ itemKey ]++ ;
        }
      }

      List<string> trajectoryStr = ( from item in trajectory select item.Value == 1 ? item.Key : item.Key + "×" + item.Value ).ToList() ;
      int lengthOfCellMerge = GetWidthOfCellMerge( sheet, 5, 15 ) ;

      var valueOfCell = string.Empty ;
      var trajectoryStrCount = trajectoryStr.Count ;
      var count = 0 ;

      if ( trajectoryStrCount > 1 ) {
        for ( var i = 0 ; i < trajectoryStrCount ; i++ ) {
          valueOfCell += trajectoryStr[ i ] + (i == trajectoryStrCount - 1 ? "": "＋");
          if ( valueOfCell.Length * 3  < lengthOfCellMerge/256.0 && i < trajectoryStrCount - 1 ) continue;
          if ( count == 0 ) {
            SetCellValue( row,5, valueOfCell ) ;
            count++ ;
            countNum++ ;
          }
          else {
            rowStart++ ;
            if ( countNum == 59 ) {
              sheet.SetRowBreak( rowStart - 1 );
              rowStart = CreateTemplateConfirmation( workbook, workbook.GetSheetAt( 0 ), sheet, rowStart, fileName, sheetName, level, scale, height ) ;
              countNum = 0 ;
            }
            var rowTrajectory = sheet.GetRow( rowStart ) ;
            SetCellValue( rowTrajectory, 5, valueOfCell  ) ;
            countNum++ ;
          }

          valueOfCell = string.Empty ;
        }
        SetCellValue( row, 16, $"{Math.Round( total, isLengthObject ? 1 : 2 )}" ) ;
      }
      else {
        SetCellValue( row, 5, string.Join( "＋", trajectoryStr ) ) ;
        SetCellValue( row, 16, $"{Math.Round( total, isLengthObject ? 1 : 2 )}"  ) ;
        countNum++ ;
      }
      
      rowStart++ ;
      return rowStart ;
    }

    private static int GetWidthOfCellMerge( ISheet sheet, int firstCellIndex, int lastCellIndex )
    {
      int result = 0 ;
      for ( int i = firstCellIndex ; i <= lastCellIndex ; i++ ) {
        result += sheet.GetColumnWidth( i ) ;
      }

      return result ;
    }

    private static void SetCellValue( IRow currentRow, int cellIndex, string value )
    {
      ICell cell = currentRow.GetCell( cellIndex ) ;
      cell.SetCellValue( value ) ;
    }

    private IEnumerable<PickUpItemModel> GetOutputPickUpModels()
    {
      if ( ! IsOutputItemsEnable ) return _pickUpItemModels ;
      var settings = OutputReportSettingCollection.Where( s => s.IsSelected ).Select( s => s.Name ) ;

      return _pickUpItemModels
        .Where(p=> 
          _hiroiMasterModels.Any( h => 
            (int.Parse( h.Buzaicd ) ==  int.Parse( p.ProductCode.Split( '-' ).First())) 
            && (settings.Contains( h.Syurui )) )) ;
    }

    private bool IsLengthObject( PickUpItemModel pickUpModel )
    {
      return pickUpModel.Tani == "m" ;
    }
    
    private RouteSegment? GetLastSegment( string routeName, RouteCache routes )
    {
      if ( string.IsNullOrEmpty( routeName ) ) return null ;
      var route = routes.SingleOrDefault( x => x.Key == routeName ) ;
      return route.Value.RouteSegments.LastOrDefault();
    }
    
    private bool IsSegmentConnectedToPullBox( RouteSegment? lastSegment )
    {
      if ( lastSegment == null ) return false ;
      var toEndPointKey = lastSegment.ToEndPoint.Key ;
      var toElementId = toEndPointKey.GetElementUniqueId() ;
      if ( string.IsNullOrEmpty( toElementId ) ) 
        return false ;
      var toConnector = _document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_ElectricalFixtures )
        .FirstOrDefault( c => c.UniqueId == toElementId ) ;
      return toConnector != null && toConnector.GetConnectorFamilyType() == ConnectorFamilyType.PullBox ;
    }
    
    private bool IsSegmentFromPowerToPullBox( RouteSegment? lastSegment )
    {
      if ( lastSegment == null ) return false ;
      var fromEndPointKey = lastSegment.FromEndPoint.Key ;
      var toEndPointKey = lastSegment.ToEndPoint.Key ;
      var fromElementId = fromEndPointKey.GetElementUniqueId() ;
      var toElementId = toEndPointKey.GetElementUniqueId() ;
      if ( string.IsNullOrEmpty( toElementId ) || string.IsNullOrEmpty( fromElementId ) ) 
        return false ;
      var fromConnector = _document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_ElectricalFixtures )
        .FirstOrDefault( c => c.UniqueId == fromElementId ) ;
      var toConnector = _document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_ElectricalFixtures )
        .FirstOrDefault( c => c.UniqueId == toElementId ) ;
      return fromConnector != null && toConnector != null && fromConnector.GetConnectorFamilyType() == ConnectorFamilyType.Power && toConnector.GetConnectorFamilyType() == ConnectorFamilyType.PullBox;
    }
    
    private List<InfoDisplay> GetInfoDisplays(List<PickUpItemModel> pickUpModels, RouteCache routes)
    {
      var routesNameRef = pickUpModels.Select( x => x.RelatedRouteName ).Distinct() ;
      var inforDisplays = new List<InfoDisplay>() ;
      foreach ( var routeNameRef in routesNameRef ) {
        var lastSegment = GetLastSegment( routeNameRef, routes ) ;
        if ( lastSegment == null ) continue ;
        if ( IsSegmentConnectedToPullBox( lastSegment ) ) {
          var idPullBox = lastSegment.FromEndPoint.Key.GetElementUniqueId() ;
          if ( inforDisplays.Any( x => x.IdPullBox == idPullBox ) ) {
            var inforDisplay = inforDisplays.SingleOrDefault( x => x.IdPullBox == idPullBox ) ;
            if ( inforDisplay == null ) continue;
            inforDisplay.NumberDisplay += 1 ;
            inforDisplay.RouteNameRef = routeNameRef ;
          }
          else {
            inforDisplays.Add( new InfoDisplay(true, idPullBox, 1, routeNameRef) );
          }
        }
      }

      return inforDisplays ;
    }
    private bool IsMoreTwoWireBook( List<PickUpItemModel> pickUpModels )
    {
      var pickUpNumbers = GetPickUpNumbersList( pickUpModels ) ;
      if ( pickUpModels.Count < 2 ) return false ;
      var wireBooks = new List<string>() ;
      foreach ( var pickUpNumber in pickUpNumbers ) {
        var items = pickUpModels.Where( p => p.PickUpNumber == pickUpNumber ).ToList() ;
        var itemFirst = items.First() ;
        var wireBook = itemFirst.WireBook ;
        int wireBookInt ;
        if ( int.TryParse( wireBook, out wireBookInt ) && wireBookInt > 1 && ! wireBooks.Contains( wireBook ) ) {
          wireBooks.Add( wireBook );
        }
      }
      
      return wireBooks.Count > 1 ;
    }

    private double CalculateTotalByFloor(List<PickUpItemModel> pickUpModels)
    {
      double result = 0 ;
      if ( pickUpModels.Count < 1 ) return result ;
      if ( IsLengthObject( pickUpModels.First() ) ) {
        var pickUpModelsByNumber = PickUpModelByNumber( PickUpViewModel.ProductType.Conduit, pickUpModels ) ;
        foreach ( var pickUpModelByNumber in pickUpModelsByNumber ) {
          var wireBook = pickUpModelByNumber.WireBook ;
          if ( ! string.IsNullOrEmpty( wireBook ) ) {
            result += ( Math.Round( double.Parse( pickUpModelByNumber.Quantity ), 1 ) ) * int.Parse( wireBook )  ;
          }
          else {
            result += ( Math.Round( double.Parse( pickUpModelByNumber.Quantity ), 1 )) ;
          }
        }
      }
      else {
        foreach ( var item in pickUpModels ) {
          double.TryParse( item.Quantity, out var quantity ) ;
          result += Math.Round(quantity, 1) ;
        }
      }
     
      return result ;
    }
    
    private List<PickUpItemModel> PickUpModelByNumber( PickUpViewModel.ProductType productType, List<PickUpItemModel> pickUpModels )
    {
      List<PickUpItemModel> result = new() ;
      
      var equipmentType = productType.GetFieldName() ;
      var pickUpModelsByNumber = pickUpModels.Where( p => p.EquipmentType == equipmentType )
        .GroupBy( x => x.PickUpNumber )
        .Select( g => g.ToList() ) ;
      
      foreach ( var pickUpModelByNumber in pickUpModelsByNumber ) {
        var pickUpModelByProductCodes = PickUpModelByProductCode( pickUpModelByNumber ) ;
        result.AddRange(pickUpModelByProductCodes);
      }

      return result ;
    }
    
    private List<PickUpItemModel> PickUpModelByProductCode( List<PickUpItemModel> pickUpModels )
    {
      List<PickUpItemModel> pickUpModelByProductCodes = new() ;
      
      var pickUpModelsByProductCode = pickUpModels.GroupBy( x => x.ProductCode.Split( '-' ).First() )
        .Select( g => g.ToList() ) ;
        
      foreach ( var pickUpModelByProductCode in pickUpModelsByProductCode ) {
        var pickUpModelsByConstructionItemsAndConstruction = pickUpModelByProductCode.GroupBy( x => ( x.ConstructionItems, x.Construction ) )
          .Select( g => g.ToList() ) ;
          
        foreach ( var pickUpModelByConstructionItemsAndConstruction in pickUpModelsByConstructionItemsAndConstruction ) {
          var sumQuantity = pickUpModelByConstructionItemsAndConstruction.Sum( p => Math.Round(Convert.ToDouble( p.Quantity ), 1)) ;
            
          var pickUpModel = pickUpModelByConstructionItemsAndConstruction.FirstOrDefault() ;
          if ( pickUpModel == null ) 
            continue ;
            
          PickUpItemModel newPickUpModel = new(pickUpModel.Item, pickUpModel.Floor, pickUpModel.ConstructionItems, pickUpModel.EquipmentType, pickUpModel.ProductName, pickUpModel.Use, pickUpModel.UsageName, pickUpModel.Construction, pickUpModel.ModelNumber, pickUpModel.Specification, pickUpModel.Specification2, pickUpModel.Size, $"{sumQuantity}", pickUpModel.Tani, pickUpModel.Supplement, pickUpModel.Supplement2, pickUpModel.Group, pickUpModel.Layer, pickUpModel.Classification, pickUpModel.Standard, pickUpModel.PickUpNumber, pickUpModel.Direction, pickUpModel.ProductCode, pickUpModel.CeedSetCode, pickUpModel.DeviceSymbol, pickUpModel.Condition, pickUpModel.SumQuantity, pickUpModel.RouteName, null, pickUpModel.WireBook ) ;
          
          pickUpModelByProductCodes.Add( newPickUpModel ) ;
        }
      }

      return pickUpModelByProductCodes ;
    }

    private void CopyTemplateSummary( IWorkbook workbook, ISheet sourceWorkSheet, ISheet destinationWorksheet, int rowStart )
    {
      int i = 0 ;
      var tempRowStart = rowStart ;
      for ( int j = 1 ; j <= 54 ; j++ ) {
        CopyFromSourceToDestinationRow( workbook, sourceWorkSheet, destinationWorksheet, i, tempRowStart ) ;
        tempRowStart++ ;
        i++ ;
      }
    }

    private int CreateHeaderSummary( ISheet sheet, int rowStart, string fileName, string sheetName, IReadOnlyDictionary<int, string> levelColumns )
    {
      var row0 = sheet.GetRow( rowStart ) ;

      rowStart += 2 ;
      var row2= sheet.GetRow( rowStart ) ;
      rowStart++ ;
      var row3 = sheet.GetRow( rowStart ) ;
      SetCellValue( row0, 2, "【拾い出し集計表】" ) ;
      SetCellValue(  row0, 6,  fileName ) ;
      SetCellValue( row0, 14, sheetName ) ;
    
      foreach ( var levelColumn in levelColumns ) {
        SetCellValue( row2, levelColumn.Key, levelColumn.Value) ;
      }
          
      SetCellValue( row2, levelColumns.Last().Key + 1, "合計") ;
      var spaceS = "  " ;
      SetCellValue( row3, 1,  $"品{spaceS}名 / 規{spaceS}格") ;
      SetCellValue( row3, 4, "単位" ) ;
      rowStart++ ;
      return rowStart;
    }
    
    private int CreateTemplateSummary(IWorkbook workbook, ISheet sourceSheet, ISheet destinationSheet, int rowStart, string fileName, string sheetName, IReadOnlyDictionary<int, string> levelColumns)
    {
      CopyTemplateSummary(workbook, sourceSheet,destinationSheet, rowStart);
      
      rowStart = CreateHeaderSummary( destinationSheet, rowStart, fileName, sheetName, levelColumns ) ;

      return rowStart ;
    }
    
    private void CopyTemplateConfirmation( IWorkbook workbook, ISheet sourceWorkSheet, ISheet destinationWorksheet, int rowStart )
    {
      var tempRowStart = rowStart ;
      for ( int i = 0 ; i < 62 ; i++ ) {
        CopyFromSourceToDestinationRow( workbook, sourceWorkSheet, destinationWorksheet, i, tempRowStart ) ;
        tempRowStart++ ;
      }
    }
    
    private int CreateHeaderConfirmation( ISheet sheet, int rowStart, string fileName, string sheetName, string level, int scale, double height )
    {
      var row0 = sheet.GetRow( rowStart ) ;
      var row1 = sheet.GetRow( ++rowStart ) ;
      var row2 = sheet.GetRow( ++rowStart ) ;
      SetCellValue( row0, 2 , fileName ) ;
      SetCellValue( row0, 13, $"縮尺:" ) ;
      SetCellValue( row0, 14, $"1/{scale}" ) ;
      SetCellValue( row0, 15, $"階高:" ) ;
      SetCellValue( row0, 16, $"{Math.Round(height/1000,1)}ｍ" ) ;
      SetCellValue( row1, 1, "【入力確認表】" ) ;
      SetCellValue( row1, 2, "工事項目:" ) ;
      SetCellValue( row1, 3, sheetName ) ;
      SetCellValue( row1, 7, "図面番号:" ) ;
      SetCellValue( row1, 10, "階数:") ;
      SetCellValue( row1, 11, level) ;
      SetCellValue( row1, 12, "区間:") ;
      var space = "      " ;
      SetCellValue( row2, 1, $"品{space}名" ) ;
      SetCellValue( row2, 2,  $"規{space}格" ) ;
      SetCellValue( row2, 4, "単位" ) ;
      var nextSpace = "                " ;
      SetCellValue( row2, 5, $"軌{nextSpace}跡" ) ;
      SetCellValue( row2, 16, "合計数量") ;
      rowStart++ ;
      return rowStart;
    }
    
    private int CreateTemplateConfirmation(IWorkbook workbook, ISheet sourceSheet, ISheet destinationSheet, int rowStart, string fileName, string sheetName, string level, int scale, double height)
    {
      CopyTemplateConfirmation(workbook, sourceSheet,destinationSheet, rowStart);
      
      rowStart = CreateHeaderConfirmation( destinationSheet, rowStart, fileName, sheetName, level, scale, height ) ;

      return rowStart ;
    }
    
    #endregion Excel File Handle

    #region Export Dat file handle

    private void ExportToDatFile( IEnumerable<PickUpItemModel> outputPickUpItemModels )
    {
      outputPickUpItemModels = CalculateTotalQuantity( outputPickUpItemModels, _document ) ;
      var outputStrings = GetOutputDataToWriting( outputPickUpItemModels ) ;
      var fileName = $"{FileName}{PickUpNumberStatusString}.dat" ;
      var filePath = Path.Combine( PathName, fileName ) ;
      using var fsStream = new FileStream( filePath, FileMode.OpenOrCreate, FileAccess.Write ) ;
      var streamWriter = new StreamWriter( fsStream, new UnicodeEncoding() ) ;
      foreach ( var outputString in outputStrings ) {
        streamWriter.WriteLine( outputString ) ;
      }

      streamWriter.Flush() ;
      streamWriter.Close() ;
      fsStream.Close() ;
    }

    private List<string> GetOutputDataToWriting( IEnumerable<PickUpItemModel> pickUpItemModels )
    {
      var outPutStrings = new List<string>() ;
      var pickUpOutPutConstructionLists = GetPickUpOutputConstructionLists( pickUpItemModels, _document ) ;

      outPutStrings.Add( $"\"1\",\"{FileName}{PickUpNumberStatusString}\"" ) ;

      var (highestLevelIndex, lowestLevelIndex) = GetHighestAndLowestLevelHasData( pickUpItemModels, _document ) ;
      
      var lowestLevelName = string.Empty ;
      var highestLevelName = highestLevelIndex.ToString() ;
      
      if ( highestLevelIndex != lowestLevelIndex ) lowestLevelName = lowestLevelIndex.ToString() ;

      foreach ( var outputConstructionItem in pickUpOutPutConstructionLists.Where( outputConstructionItem => outputConstructionItem.OutputCollection.Any() ) ) {
        outPutStrings.Add( $"\"2\",\"{outputConstructionItem.ConstructionItemName}\",\"{highestLevelName}\",\"{lowestLevelName}\"" ) ;

        foreach ( var outPutLevel in from outputItem in outputConstructionItem.OutputCollection select outputItem.OutPutLevelItems.OrderBy( x=>x.LevelIndex ).ToList() ) {
          outPutStrings.AddRange( outPutLevel.Select( x => x.OutputString ) ) ;
        }
      }

      return outPutStrings ;
    }

    private static (int highestLevelIndex, int lowestLevelIndex) GetHighestAndLowestLevelHasData(IEnumerable<PickUpItemModel> pickUpItemModelCollection, Document document ) 
    {
      var allLevelNameCollection = pickUpItemModelCollection.Select( x => x.Floor ).Distinct().ToList() ;
      var allLevelsAndIndexCollection = GetLevelIndexOfLevelCollection( document ).ToList() ;

      var lowestLevelIndex = 10 ;
      var highestLevelIndex = -1 ;
      
      foreach ( var levelName in allLevelNameCollection ) {
        var levelAndIndex = allLevelsAndIndexCollection.FirstOrDefault( x => levelName.Contains(x.levelName) ) ;
        if ( string.IsNullOrEmpty( levelAndIndex.levelName ) ) continue ;
        if ( lowestLevelIndex > levelAndIndex.levelIndex ) {
          lowestLevelIndex = levelAndIndex.levelIndex ;
        }

        if ( highestLevelIndex < levelAndIndex.levelIndex ) {
          highestLevelIndex = levelAndIndex.levelIndex ;
        }
      }

      return ( highestLevelIndex, lowestLevelIndex ) ;
    }

    private static IEnumerable<PickUpOutputConstructionList> GetPickUpOutputConstructionLists( IEnumerable<PickUpItemModel> pickUpItemModels, Document document )
    {
      var pickUpOutPutConstructionLists = new List<PickUpOutputConstructionList>() ;

      var levelAndIndexCollection = GetLevelIndexOfLevelCollection( document ).EnumerateAll() ;

      if ( ! levelAndIndexCollection.Any() ) {
        throw new Exception( "Don't have any level in drawing, please check again!" ) ;
      } 

      foreach ( var pickUpItemModel in pickUpItemModels ) {
        var constructionName = pickUpItemModel.ConstructionItems ;

        if ( string.IsNullOrEmpty( constructionName ) ) {
          constructionName = DefaultConstructionItem ;
        }

        var constructionOutputList = pickUpOutPutConstructionLists.FirstOrDefault( c => c.ConstructionItemName == constructionName ) ;
        if ( constructionOutputList == null ) {
          constructionOutputList = new PickUpOutputConstructionList( constructionName ) ;
          pickUpOutPutConstructionLists.Add( constructionOutputList ) ;
        }

        if ( string.IsNullOrEmpty( pickUpItemModel.Floor ) ) continue ;

        var levelAndIndex = levelAndIndexCollection.FirstOrDefault( lv => pickUpItemModel.Floor.Contains( lv.levelName ) ) ;
        var outPutString = $"\"3\",\"{pickUpItemModel.ProductName}\",\"{pickUpItemModel.Specification}\",\"{pickUpItemModel.ProductCode}\",{pickUpItemModel.Quantity},\"\",\"\"" ;
        var outputItem = constructionOutputList.OutputCollection.FirstOrDefault( it => CompareProductCode( it.ProductCode, pickUpItemModel.ProductCode ) ) ;
        if ( outputItem == null ) {
          outputItem = new PickUpOutputList( pickUpItemModel.ProductCode ) ;
          constructionOutputList.OutputCollection.Add( outputItem ) ;
        }

        outputItem.OutPutLevelItems.Add( new PickUpOutPutLevelItem( levelAndIndex.levelIndex, outPutString ) ) ;

      }

      return pickUpOutPutConstructionLists ;
    }

    private static IEnumerable<(string levelName, int levelIndex)> GetLevelIndexOfLevelCollection(Document document)
    {
      var allLevels = document.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ) ;
      var positiveLevels = allLevels.Where( lv => (int)lv.Elevation.RevitUnitsToMillimeters() > 0 ).OrderBy( lv => lv.Elevation ) ;
      var negativeLevels = allLevels.Where( lv => (int)lv.Elevation.RevitUnitsToMillimeters() <= 0 ).OrderByDescending( lv => lv.Elevation ) ;

      var positiveIndex = 1 ;
      var negativeIndex = -1 ;
      
      foreach ( var level in positiveLevels ) {
        yield return ( level.Name, positiveIndex ) ;
        positiveIndex++ ;
      }

      foreach ( var level in negativeLevels ) {
        yield return ( level.Name, negativeIndex ) ;
        negativeIndex-- ;
      }
      
    }

    private static IEnumerable<PickUpItemModel> CalculateTotalQuantity(IEnumerable<PickUpItemModel> pickUpItemModels, Document document)
    {
      return pickUpItemModels
        .GroupBy( x => ( x.Floor, x.ConstructionItems, x.ProductCode ), new GroupPickUpItemComparer() ).Select( p =>
        {
          var first = p.First() ;
          var newModel = new PickUpItemModel( first ) ;
          newModel.ProductCode = newModel.ProductCode.Split( '-' ).FirstOrDefault() ?? newModel.ProductCode ;
          newModel.Quantity = $"{p.Sum( x => Convert.ToDouble( x.Quantity ) )}" ;
          return newModel ;
        } ).OrderBy( x => GetLevelIndexOfLevelCollection( document ).FirstOrDefault( y => y.levelName == x.Floor ) ) ;
    }

    public static bool CompareProductCode( string productCodeA, string productCodeB )
    {
      productCodeA = productCodeA.Split( '-' ).FirstOrDefault() ?? productCodeA ;
      productCodeB = productCodeB.Split( '-' ).FirstOrDefault() ?? productCodeB ;
      if ( int.TryParse( productCodeA, out var productCodeANumber ) &&
           int.TryParse( productCodeB, out var productCodeBNumber ) ) {
        return productCodeANumber == productCodeBNumber ;
      }
      
      return productCodeA == productCodeB ;
    }

    #endregion

    private class InfoDisplay
    {
      public bool IsDisplay { get ; set ; }
      public string? IdPullBox { get ; set ; }
      public int NumberDisplay { get ; set ; }
      public string? RouteNameRef { get ; set ; }

      public InfoDisplay( bool isDisplay, string? idPullBox, int numberDisplay, string? routeNameRef )
      {
        IsDisplay = isDisplay ;
        IdPullBox = idPullBox??string.Empty ;
        NumberDisplay = numberDisplay ;
        RouteNameRef = routeNameRef??string.Empty ; ;
      }
    }
  }
  
  public class PickUpSettingItem
  {
    public string Name { get ; }
    public bool IsSelected { get ; set ; }

    public PickUpSettingItem( string name, bool isSelected )
    {
      Name = name ;
      IsSelected = isSelected ;
    }
  }

  public class GroupPickUpItemComparer : EqualityComparer<(string levelName,string constructionItems,string productCode)>
  {
    public override bool Equals( (string levelName, string constructionItems, string productCode) first, (string levelName, string constructionItems, string productCode) second )
    {
      return first.levelName == second.levelName && first.constructionItems == second.constructionItems &&
             PickUpReportViewModel.CompareProductCode( first.productCode, second.productCode ) ;
    }

    public override int GetHashCode( (string levelName,string constructionItems,string productCode) obj )
    {
      return obj.GetHashCode() ;
    }
  }
}