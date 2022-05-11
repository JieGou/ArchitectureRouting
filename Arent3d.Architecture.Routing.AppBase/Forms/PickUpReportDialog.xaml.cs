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
    private const string SummaryFileType = "拾い出し集計表" ;
    private const string ConfirmationFileType = "拾い根拠確認表" ;
    private const string DoconOff = "ドーコンOFF" ;
    private const string DoconOn = "ドーコンON" ;
    private const string On = "ON" ;
    private const string Off = "OFF" ;
    private const string SummaryFileName = "_拾い出し集計表.xlsx" ;
    private const string ConfirmationFileName = "_拾い根拠確認表.xlsx" ;
    private const string DefaultConstructionItem = "未設定" ;
    private readonly Document _document ;
    private readonly List<PickUpModel> _pickUpModels ;
    private readonly List<ListBoxItem> _fileTypes ;
    private readonly List<ListBoxItem> _doconTypes ;
    private string _path ;
    private List<string> _fileNames ;

    public PickUpReportDialog( Document document )
    {
      InitializeComponent() ;
      _document = document ;
      _pickUpModels = new List<PickUpModel>() ;
      _fileTypes = new List<ListBoxItem>() ;
      _doconTypes = new List<ListBoxItem>() ;
      _path = string.Empty ;
      _fileNames = new List<string>() ;
      CreateCheckBoxList() ;
      var pickUpStorable = _document.GetAllStorables<PickUpStorable>().FirstOrDefault() ;
      if ( pickUpStorable != null ) _pickUpModels = pickUpStorable.AllPickUpModelData ;
    }

    private void Button_Reference( object sender, RoutedEventArgs e )
    {
      const string fileName = "only_choose_folder_and_do_not_edit_file_name.xlsx" ;
      SaveFileDialog saveFileDialog = new SaveFileDialog { FileName = fileName, Filter = "Csv files (*.xlsx)|*.xlsx", InitialDirectory = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ) } ;

      if ( saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK ) return ;
      _path = Path.GetDirectoryName( saveFileDialog.FileName )! ;
      TbFolder.Text = _path ;
    }

    private void Button_Execute( object sender, RoutedEventArgs e )
    {
      if ( _fileNames.Any() && ! string.IsNullOrEmpty( _path ) ) {
        CreateOutputFile() ;
        DialogResult = true ;
        Close() ;
      }
      else {
        if ( ! _fileNames.Any() && string.IsNullOrEmpty( _path ) )
          MessageBox.Show( "Please select the output folder and file type.", "Warning" ) ;
        else if ( string.IsNullOrEmpty( _path ) )
          MessageBox.Show( "Please select the output folder.", "Warning" ) ;
        else if ( ! _fileNames.Any() )
          MessageBox.Show( "Please select the output file type.", "Warning" ) ;
      }
    }

    private void Button_Cancel( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    public class ListBoxItem
    {
      public string? TheText { get ; set ; }
      public bool TheValue { get ; set ; }
    }

    private void CreateCheckBoxList()
    {
      _fileTypes.Add( new ListBoxItem { TheText = SummaryFileType, TheValue = false } ) ;
      _fileTypes.Add( new ListBoxItem { TheText = ConfirmationFileType, TheValue = false } ) ;
      LbFileType.ItemsSource = _fileTypes ;

      _doconTypes.Add( new ListBoxItem { TheText = On, TheValue = true } ) ;
      _doconTypes.Add( new ListBoxItem { TheText = Off, TheValue = false } ) ;
      LbDocon.ItemsSource = _doconTypes ;
    }

    private void DoconItem_Checked( object sender, RoutedEventArgs e )
    {
      _fileNames = new List<string>() ;
      var radioButton = sender as RadioButton ;
      var fileTypes = _fileTypes.Where( f => f.TheValue == true ).Select( f => f.TheText ).ToList() ;
      var docon = radioButton!.Content.ToString() == On ? DoconOn : DoconOff ;
      foreach ( var fileType in fileTypes ) {
        string fileName = string.Empty ;
        switch ( fileType ) {
          case SummaryFileType :
            fileName = SummaryFileName ;
            break ;
          case ConfirmationFileType :
            fileName = ConfirmationFileName ;
            break ;
          default :
            break ;
        }

        if ( string.IsNullOrEmpty( fileName ) ) continue ;
        _fileNames.Add( docon + fileName ) ;
      }

      TbFileName.Text = _fileNames.Any() ? "\"" + string.Join( "\" \"", _fileNames ) + "\"" : string.Empty ;
    }

    private void FileType_Checked( object sender, RoutedEventArgs e )
    {
      var checkbox = sender as CheckBox ;
      var docon = _doconTypes.First().TheValue ? DoconOn : DoconOff ;
      switch ( checkbox!.Content.ToString() ) {
        case SummaryFileType :
          if ( ! _fileNames.Contains( docon + SummaryFileName ) )
            _fileNames.Add( docon + SummaryFileName ) ;
          break ;
        case ConfirmationFileType :
          if ( ! _fileNames.Contains( docon + ConfirmationFileName ) )
            _fileNames.Add( docon + ConfirmationFileName ) ;
          break ;
        default :
          break ;
      }

      TbFileName.Text = _fileNames.Any() ? "\"" + string.Join( "\" \"", _fileNames ) + "\"" : string.Empty ;
    }

    private void FileType_Unchecked( object sender, RoutedEventArgs e )
    {
      var checkbox = sender as CheckBox ;
      var docon = _doconTypes.First().TheValue ? DoconOn : DoconOff ;
      switch ( checkbox!.Content.ToString() ) {
        case SummaryFileType :
          if ( _fileNames.Contains( docon + SummaryFileName ) )
            _fileNames.Remove( docon + SummaryFileName ) ;
          break ;
        case ConfirmationFileType :
          if ( _fileNames.Contains( docon + ConfirmationFileName ) )
            _fileNames.Remove( docon + ConfirmationFileName ) ;
          break ;
        default :
          break ;
      }

      TbFileName.Text = _fileNames.Any() ? "\"" + string.Join( "\" \"", _fileNames ) + "\"" : string.Empty ;
    }

    private List<string> GetConstructionItemList()
    {
      var constructionItemList = new List<string>() ;
      foreach ( var pickUpModel in _pickUpModels.Where( pickUpModel => ! constructionItemList.Contains( pickUpModel.ConstructionItems ) && pickUpModel.EquipmentType == PickUpViewModel.ProductType.Conduit.GetFieldName() ) ) {
        constructionItemList.Add( pickUpModel.ConstructionItems ) ;
      }

      return constructionItemList ;
    }

    private void CreateOutputFile()
    {
      if ( ! _pickUpModels.Any() ) MessageBox.Show( "Don't have pick up data.", "Message" ) ;
      try {
        var constructionItemList = GetConstructionItemList() ;
        if ( ! constructionItemList.Any() ) constructionItemList.Add( DefaultConstructionItem ) ;
        foreach ( var fileName in _fileNames ) {
          XSSFWorkbook workbook = new XSSFWorkbook() ;

          Dictionary<string, XSSFCellStyle> xssfCellStyles = new Dictionary<string, XSSFCellStyle>
          {
            { "borderedCellStyle", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Center, NPOI.SS.UserModel.HorizontalAlignment.Center ) },
            { "noneBorderedCellStyle", CreateCellStyle( workbook, BorderStyle.None, BorderStyle.None, BorderStyle.None, BorderStyle.None, NPOI.SS.UserModel.VerticalAlignment.Center, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "bottomBorderedCellStyle", CreateCellStyle( workbook, BorderStyle.None, BorderStyle.None, BorderStyle.None, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Center, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "leftBottomBorderedCellStyle", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.None, BorderStyle.None, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Center, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "rightBottomBorderedCellStyle", CreateCellStyle( workbook, BorderStyle.None, BorderStyle.Thin, BorderStyle.None, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Center, NPOI.SS.UserModel.HorizontalAlignment.Right ) },
            { "leftAlignmentLeftRightBorderedCellStyle", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.None, BorderStyle.None, NPOI.SS.UserModel.VerticalAlignment.Center, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "leftRightBorderedCellStyle", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.None, BorderStyle.None, NPOI.SS.UserModel.VerticalAlignment.Center, NPOI.SS.UserModel.HorizontalAlignment.Right ) },
            { "exceptTopBorderedCellStyle", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.None, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Center, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "wrapTextBorderedCellStyle", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Center, NPOI.SS.UserModel.HorizontalAlignment.Left, true ) }
          } ;
          var headerNoneBorderedCellStyle = CreateCellStyle( workbook, BorderStyle.None, BorderStyle.None, BorderStyle.None, BorderStyle.None, NPOI.SS.UserModel.VerticalAlignment.Center, NPOI.SS.UserModel.HorizontalAlignment.Left ) ;
          XSSFFont myFont = (XSSFFont) workbook.CreateFont() ;
          myFont.FontHeightInPoints = 16 ;
          myFont.IsBold = true ;
          headerNoneBorderedCellStyle.SetFont( myFont ) ;
          xssfCellStyles.Add( "headerNoneBorderedCellStyle", headerNoneBorderedCellStyle ) ;

          if ( fileName.Contains( SummaryFileName ) )
            foreach ( var sheetName in constructionItemList ) {
              CreateSheet( SheetType.Summary, workbook, sheetName, xssfCellStyles ) ;
            }
          else if ( fileName.Contains( ConfirmationFileName ) )
            foreach ( var sheetName in constructionItemList ) {
              CreateSheet( SheetType.Confirmation, workbook, sheetName, xssfCellStyles ) ;
            }

          FileStream fs = new FileStream( _path + @"\" + fileName, FileMode.OpenOrCreate ) ;
          workbook.Write( fs ) ;

          workbook.Close() ;
          fs.Close() ;
        }

        MessageBox.Show( "Export pick-up output file successfully.", "Message" ) ;
      }
      catch ( Exception ex ) {
        MessageBox.Show( "Export file failed because " + ex, "Error message" ) ;
      }
    }

    private enum SheetType
    {
      Confirmation,
      Summary
    }

    private void CreateSheet( SheetType sheetType, IWorkbook workbook, string sheetName, IReadOnlyDictionary<string, XSSFCellStyle> xssfCellStyles )
    {
      List<string> levels = _document.GetAllElements<Level>().Select( l => l.Name ).ToList() ;
      var codeList = GetCodeList() ;
      var docon = _doconTypes.First().TheValue ? DoconOn : DoconOff ;

      ISheet sheet = workbook.CreateSheet( sheetName ) ;
      IRow row0, row2 ;
      int rowStart ;
      switch ( sheetType ) {
        case SheetType.Confirmation :
          sheet.SetColumnWidth( 1, 8000 ) ;
          sheet.SetColumnWidth( 2, 8000 ) ;
          sheet.SetColumnWidth( 3, 4000 ) ;
          sheet.SetColumnWidth( 4, 1500 ) ;
          sheet.SetColumnWidth( 5, 4000 ) ;
          sheet.SetColumnWidth( 7, 3000 ) ;
          sheet.SetColumnWidth( 16, 3000 ) ;
          rowStart = 0 ;
          foreach ( var level in levels ) {
            row0 = sheet.CreateRow( rowStart ) ;
            var row1 = sheet.CreateRow( rowStart + 1 ) ;
            row2 = sheet.CreateRow( rowStart + 2 ) ;
            CreateMergeCell( sheet, row0, rowStart, rowStart, 2, 6, docon, xssfCellStyles[ "bottomBorderedCellStyle" ] ) ;
            CreateCell( row0, 13, "縮尺 :", xssfCellStyles[ "bottomBorderedCellStyle" ] ) ;
            CreateCell( row0, 14, "", xssfCellStyles[ "bottomBorderedCellStyle" ] ) ;
            CreateCell( row0, 15, "階高 :", xssfCellStyles[ "bottomBorderedCellStyle" ] ) ;
            CreateCell( row0, 16, "", xssfCellStyles[ "bottomBorderedCellStyle" ] ) ;

            CreateCell( row1, 1, "【入力確認表】", xssfCellStyles[ "headerNoneBorderedCellStyle" ] ) ;
            CreateCell( row1, 2, "工事階層 :", xssfCellStyles[ "noneBorderedCellStyle" ] ) ;
            CreateMergeCell( sheet, row1, rowStart + 1, rowStart + 1, 3, 6, sheetName, xssfCellStyles[ "noneBorderedCellStyle" ] ) ;
            CreateCell( row1, 7, "図面番号 :", xssfCellStyles[ "noneBorderedCellStyle" ] ) ;
            CreateMergeCell( sheet, row1, rowStart + 1, rowStart + 1, 8, 9, level, xssfCellStyles[ "noneBorderedCellStyle" ] ) ;
            CreateCell( row1, 10, "階数 :", xssfCellStyles[ "noneBorderedCellStyle" ] ) ;
            CreateCell( row1, 12, "区間 :", xssfCellStyles[ "noneBorderedCellStyle" ] ) ;
            CreateMergeCell( sheet, row1, rowStart + 1, rowStart + 1, 13, 16, "", xssfCellStyles[ "noneBorderedCellStyle" ] ) ;

            CreateCell( row2, 1, "品名", xssfCellStyles[ "borderedCellStyle" ] ) ;
            CreateMergeCell( sheet, row2, rowStart + 2, rowStart + 2, 2, 3, "規格", xssfCellStyles[ "borderedCellStyle" ] ) ;
            CreateCell( row2, 4, "単位", xssfCellStyles[ "borderedCellStyle" ] ) ;
            CreateMergeCell( sheet, row2, rowStart + 2, rowStart + 2, 5, 15, "軌跡", xssfCellStyles[ "borderedCellStyle" ] ) ;
            CreateCell( row2, 16, "合計数量", xssfCellStyles[ "borderedCellStyle" ] ) ;

            rowStart += 3 ;
            foreach ( var code in codeList ) {
              var conduitPickUpModels = _pickUpModels.Where( p => p.ConstructionItems == sheetName && p.Specification2 == code && p.Floor == level && p.EquipmentType == PickUpViewModel.ProductType.Conduit.GetFieldName() ).GroupBy( x => x.ProductCode, ( key, p ) => new { ProductCode = key, PickUpModels = p.ToList() } ) ;
              foreach ( var conduitPickUpModel in conduitPickUpModels ) {
                rowStart = AddConfirmationPickUpRow( conduitPickUpModel.PickUpModels, sheet, rowStart, xssfCellStyles ) ;
              }
            }

            var lastRow = sheet.CreateRow( rowStart ) ;
            CreateCell( lastRow, 1, "", xssfCellStyles[ "borderedCellStyle" ] ) ;
            CreateMergeCell( sheet, lastRow, rowStart, rowStart, 2, 3, "", xssfCellStyles[ "borderedCellStyle" ] ) ;
            CreateCell( lastRow, 4, "", xssfCellStyles[ "borderedCellStyle" ] ) ;
            CreateMergeCell( sheet, lastRow, rowStart, rowStart, 5, 15, "", xssfCellStyles[ "borderedCellStyle" ] ) ;
            CreateCell( lastRow, 16, "", xssfCellStyles[ "borderedCellStyle" ] ) ;

            rowStart += 2 ;
          }

          break ;
        case SheetType.Summary :
          sheet.SetColumnWidth( 1, 500 ) ;
          sheet.SetColumnWidth( 2, 8000 ) ;
          sheet.SetColumnWidth( 3, 4000 ) ;
          row0 = sheet.CreateRow( 0 ) ;
          row2 = sheet.CreateRow( 2 ) ;
          var row3 = sheet.CreateRow( 3 ) ;
          CreateCell( row0, 2, "【拾い出し集計表】", xssfCellStyles[ "headerNoneBorderedCellStyle" ] ) ;
          CreateMergeCell( sheet, row0, 0, 0, 6, 7, docon, xssfCellStyles[ "bottomBorderedCellStyle" ] ) ;
          for ( var i = 7 ; i < 19 ; i++ ) {
            CreateCell( row0, i, "", xssfCellStyles[ "bottomBorderedCellStyle" ] ) ;
          }

          CreateCell( row0, 14, sheetName, xssfCellStyles[ "bottomBorderedCellStyle" ] ) ;

          CreateMergeCell( sheet, row2, 2, 2, 1, 3, "", xssfCellStyles[ "borderedCellStyle" ] ) ;
          CreateCell( row2, 4, "", xssfCellStyles[ "borderedCellStyle" ] ) ;
          Dictionary<int, string> levelColumns = new Dictionary<int, string>() ;
          var index = 5 ;
          foreach ( var level in levels ) {
            CreateCell( row2, index, level, xssfCellStyles[ "borderedCellStyle" ] ) ;
            levelColumns.Add( index, level ) ;
            CreateCell( row3, index, "", xssfCellStyles[ "borderedCellStyle" ] ) ;
            index++ ;
          }

          CreateCell( row2, index, "合計", xssfCellStyles[ "borderedCellStyle" ] ) ;

          CreateMergeCell( sheet, row3, 3, 3, 1, 3, "品名/規格", xssfCellStyles[ "borderedCellStyle" ] ) ;
          CreateCell( row3, 4, "単位", xssfCellStyles[ "borderedCellStyle" ] ) ;
          CreateCell( row3, index, "", xssfCellStyles[ "borderedCellStyle" ] ) ;

          rowStart = 4 ;
          foreach ( var code in codeList ) {
            var conduitPickUpModels = _pickUpModels.Where( p => p.ConstructionItems == sheetName && p.Specification2 == code && p.EquipmentType == PickUpViewModel.ProductType.Conduit.GetFieldName() ).GroupBy( x => x.ProductCode, ( key, p ) => new { ProductCode = key, PickUpModels = p.ToList() } ) ;
            foreach ( var conduitPickUpModel in conduitPickUpModels ) {
              rowStart = AddSummaryPickUpRow( conduitPickUpModel.PickUpModels, sheet, rowStart, levelColumns, index, xssfCellStyles ) ;
            }
          }

          break ;
        default :
          break ;
      }
    }

    private List<string> GetCodeList()
    {
      var codeList = new List<string>() ;
      foreach ( var pickUpModel in _pickUpModels.Where( pickUpModel => ! codeList.Contains( pickUpModel.Specification2 ) ) ) {
        codeList.Add( pickUpModel.Specification2 ) ;
      }

      return codeList ;
    }

    private int AddSummaryPickUpRow( List<PickUpModel> pickUpModels, ISheet sheet, int rowStart, IReadOnlyDictionary<int, string> levelColumns, int index, IReadOnlyDictionary<string, XSSFCellStyle> xssfCellStyles )
    {
      if ( ! pickUpModels.Any() ) return rowStart ;
      var pickUpModel = pickUpModels.First() ;
      var rowName = sheet.CreateRow( rowStart ) ;
      CreateMergeCell( sheet, rowName, rowStart, rowStart, 1, 3, pickUpModel.ProductName, xssfCellStyles[ "leftAlignmentLeftRightBorderedCellStyle" ] ) ;
      CreateMergeCell( sheet, rowName, rowStart, rowStart + 1, 4, 4, pickUpModel.Tani, xssfCellStyles[ "borderedCellStyle" ] ) ;

      rowStart++ ;
      var rowStandard = sheet.CreateRow( rowStart ) ;
      CreateCell( rowStandard, 1, "", xssfCellStyles[ "leftBottomBorderedCellStyle" ] ) ;
      CreateCell( rowStandard, 2, pickUpModel.Standard, xssfCellStyles[ "bottomBorderedCellStyle" ] ) ;
      CreateCell( rowStandard, 3, "", xssfCellStyles[ "rightBottomBorderedCellStyle" ] ) ;
      CreateCell( rowStandard, 4, "", xssfCellStyles[ "bottomBorderedCellStyle" ] ) ;

      double total = 0 ;
      for ( var i = 5 ; i < index ; i++ ) {
        double quantityFloor = 0 ;
        var level = levelColumns[ i ] ;
        foreach ( var item in pickUpModels.Where( item => item.Floor == level ) ) {
          double.TryParse( item.Quantity, out var quantity ) ;
          quantityFloor += quantity ;
        }

        CreateCell( rowName, i, quantityFloor == 0 ? string.Empty : Math.Round( quantityFloor, 2 ).ToString(), xssfCellStyles[ "leftRightBorderedCellStyle" ] ) ;
        CreateCell( rowStandard, i, "", xssfCellStyles[ "exceptTopBorderedCellStyle" ] ) ;
        total += quantityFloor ;
      }

      CreateCell( rowName, index, total == 0 ? string.Empty : Math.Round( total, 2 ).ToString(), xssfCellStyles[ "leftRightBorderedCellStyle" ] ) ;
      CreateCell( rowStandard, index, "", xssfCellStyles[ "exceptTopBorderedCellStyle" ] ) ;
      rowStart++ ;
      return rowStart ;
    }

    private List<string> GetPickUpNumbersList( List<PickUpModel> pickUpModels )
    {
      var pickUpNumberList = new List<string>() ;
      foreach ( var pickUpModel in pickUpModels.Where( pickUpModel => ! pickUpNumberList.Contains( pickUpModel.PickUpNumber ) ) ) {
        pickUpNumberList.Add( pickUpModel.PickUpNumber ) ;
      }

      return pickUpNumberList ;
    }

    private int AddConfirmationPickUpRow( List<PickUpModel> pickUpModels, ISheet sheet, int rowStart, IReadOnlyDictionary<string, XSSFCellStyle> xssfCellStyles )
    {
      if ( ! pickUpModels.Any() ) return rowStart ;
      var pickUpNumbers = GetPickUpNumbersList( pickUpModels ) ;
      var pickUpModel = pickUpModels.First() ;
      var row = sheet.CreateRow( rowStart ) ;
      CreateCell( row, 1, pickUpModel.ProductName, xssfCellStyles[ "leftBottomBorderedCellStyle" ] ) ;
      CreateCell( row, 2, pickUpModel.Standard, xssfCellStyles[ "leftBottomBorderedCellStyle" ] ) ;
      CreateCell( row, 3, "", xssfCellStyles[ "rightBottomBorderedCellStyle" ] ) ;
      CreateCell( row, 4, pickUpModel.Tani, xssfCellStyles[ "borderedCellStyle" ] ) ;

      double total = 0 ;
      Dictionary<string, int> trajectory = new Dictionary<string, int>() ;
      foreach ( var pickUpNumber in pickUpNumbers ) {
        double seenQuantity = 0 ;
        Dictionary<string, double> notSeenQuantities = new Dictionary<string, double>() ;
        var items = pickUpModels.Where( p => p.PickUpNumber == pickUpNumber ).ToList() ;
        foreach ( var item in items.Where( item => ! string.IsNullOrEmpty( item.Quantity ) ) ) {
          double.TryParse( item.Quantity, out var quantity ) ;
          if ( ! string.IsNullOrEmpty( item.Direction ) ) {
            if ( ! notSeenQuantities.Keys.Contains( item.Direction ) ) {
              notSeenQuantities.Add( item.Direction, 0 ) ;
            }

            notSeenQuantities[ item.Direction ] += quantity ;
          }
          else
            seenQuantity += quantity ;

          total += quantity ;
        }

        var number = _doconTypes.First().TheValue ? "[" + pickUpNumber + "]" : string.Empty ;
        var seenQuantityStr = seenQuantity > 0 ? Math.Round( seenQuantity, 2 ).ToString() : string.Empty ;
        var notSeenQuantityStr = string.Empty ;
        foreach ( var (_, value) in notSeenQuantities ) {
          notSeenQuantityStr += value > 0 ? " + ↓" + Math.Round( value, 2 ) : string.Empty ;
        }

        var key = "( " + seenQuantityStr + notSeenQuantityStr + " )" ;
        var itemKey = trajectory.FirstOrDefault( t => t.Key.Contains( key ) ).Key ;
        if ( string.IsNullOrEmpty( itemKey ) )
          trajectory.Add( number + key, 1 ) ;
        else {
          trajectory[ itemKey ]++ ;
        }
      }

      List<string> trajectoryStr = ( from item in trajectory select item.Value == 1 ? item.Key : item.Key + " x " + item.Value ).ToList() ;
      CreateMergeCell( sheet, row, rowStart, rowStart, 5, 15, string.Join( " + ", trajectoryStr ), xssfCellStyles[ "wrapTextBorderedCellStyle" ] ) ;
      CreateCell( row, 16, Math.Round( total, 2 ).ToString(), xssfCellStyles[ "rightBottomBorderedCellStyle" ] ) ;

      rowStart++ ;
      return rowStart ;
    }

    private void CreateCell( IRow currentRow, int cellIndex, string value, ICellStyle style )
    {
      ICell cell = currentRow.CreateCell( cellIndex ) ;
      cell.SetCellValue( value ) ;
      cell.CellStyle = style ;
    }

    private void CreateMergeCell( ISheet sheet, IRow currentRow, int firstRowIndex, int lastRowIndex, int firstCellIndex, int lastCellIndex, string value, ICellStyle style )
    {
      ICell cell = currentRow.CreateCell( firstCellIndex ) ;
      CellRangeAddress cellMerge = new CellRangeAddress( firstRowIndex, lastRowIndex, firstCellIndex, lastCellIndex ) ;
      sheet.AddMergedRegion( cellMerge ) ;
      cell.SetCellValue( value ) ;
      cell.CellStyle = style ;
      RegionUtil.SetBorderTop( style.BorderTop == BorderStyle.None ? 0 : 1, cellMerge, sheet ) ;
      RegionUtil.SetBorderBottom( style.BorderBottom == BorderStyle.None ? 0 : 1, cellMerge, sheet ) ;
      RegionUtil.SetBorderLeft( style.BorderLeft == BorderStyle.None ? 0 : 1, cellMerge, sheet ) ;
      RegionUtil.SetBorderRight( style.BorderRight == BorderStyle.None ? 0 : 1, cellMerge, sheet ) ;
    }

    private XSSFCellStyle CreateCellStyle( IWorkbook workbook, BorderStyle leftBorderStyle, BorderStyle rightBorderStyle, BorderStyle topBorderStyle, BorderStyle bottomBorderStyle, NPOI.SS.UserModel.VerticalAlignment verticalAlignment, NPOI.SS.UserModel.HorizontalAlignment horizontalAlignment, bool wrapText = false )
    {
      XSSFCellStyle borderedCellStyle = (XSSFCellStyle) workbook.CreateCellStyle() ;
      borderedCellStyle.BorderLeft = leftBorderStyle ;
      borderedCellStyle.BorderTop = topBorderStyle ;
      borderedCellStyle.BorderRight = rightBorderStyle ;
      borderedCellStyle.BorderBottom = bottomBorderStyle ;
      borderedCellStyle.VerticalAlignment = verticalAlignment ;
      borderedCellStyle.Alignment = horizontalAlignment ;
      borderedCellStyle.WrapText = wrapText ;
      return borderedCellStyle ;
    }
  }
}