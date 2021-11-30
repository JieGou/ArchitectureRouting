using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Windows ;
using System.Windows.Forms ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using NPOI.XSSF.UserModel ;
using NPOI.SS.UserModel;
using NPOI.SS.Util ;
using BorderStyle = NPOI.SS.UserModel.BorderStyle ;
using MessageBox = System.Windows.Forms.MessageBox ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class PickUpReportDialog : Window
  {
    private readonly Document _document ;
    private readonly List<ListBoxItem> _fileTypes ;
    private readonly List<ListBoxItem> _outputItems ;
    private List<ListBoxItem> _itemTypes ;
    private List<string> _fileTypesSelected ;
    private string _itemSelected ;
    private List<string> _itemsTypesSelected;
    private string path ;
    public PickUpReportDialog( Document document)
    {
      InitializeComponent() ;
      _document = document ;
      _fileTypes = new List<ListBoxItem>() ;
      _outputItems = new List<ListBoxItem>() ;
      _itemTypes = new List<ListBoxItem>() ;
      _fileTypesSelected = new List<string>() ;
      _itemSelected = string.Empty ;
      _itemsTypesSelected = new List<string>() ;
      path = string.Empty ;
      CreateCheckBoxList();
    }
    
    private void Button_Register( object sender, RoutedEventArgs e )
    {

    }
    
    private void Button_Delete( object sender, RoutedEventArgs e )
    {

    }
    
    private void Button_DeleteAll( object sender, RoutedEventArgs e )
    {

    }
    
    private void Button_Setting( object sender, RoutedEventArgs e )
    {
      ChangeValueItemType() ;
      var dialog = new PickUpItemSelectionDialog( _itemTypes ) ;

      dialog.ShowDialog() ;
      _itemsTypesSelected = dialog.ItemsTypesSelected;
    }
    
    private void Button_Reference( object sender, RoutedEventArgs e )
    {
      const string fileName = "file_name.xlsx" ;
      SaveFileDialog saveFileDialog = new SaveFileDialog { FileName = fileName, Filter = "Csv files (*.xlsx)|*.xlsx", InitialDirectory = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ) } ;

      if ( saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK ) return ;
      path = saveFileDialog.FileName ;
      TbFolder.Text = Path.GetDirectoryName( path ) ;
      TbFileName.Text = Path.GetFileName( path ) ;
    }

    private void Button_Execute( object sender, RoutedEventArgs e )
    {
      _fileTypesSelected = new List<string>() ;
      _itemSelected = string.Empty ;
      foreach ( var fileType in _fileTypes ) {
        if ( fileType.TheValue == true )
          _fileTypesSelected.Add( fileType.TheText! ) ;
      }

      foreach ( var outputItem in _outputItems ) {
        if ( outputItem.TheValue == true )
          _itemSelected = outputItem.TheText! ;
      }

      CreateOutputFile() ;
      //DialogResult = true ;
      //Close() ;
    }
    
    private void Button_Cancel( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
      Close() ;
    }
    
    public class ListBoxItem
    {
      public string? TheText { get; set; }
      public bool TheValue { get; set; }
    }

    private void CreateCheckBoxList()
    {
      _fileTypes.Add( new ListBoxItem { TheText = "拾い根拠確認表", TheValue = false } ) ;
      _fileTypes.Add( new ListBoxItem { TheText = "拾い出し集計表", TheValue = false } ) ;
      _fileTypes.Add( new ListBoxItem { TheText = "ユーザファイル", TheValue = true } ) ;
      LbFileType.ItemsSource = _fileTypes ;
      
      _outputItems.Add( new ListBoxItem { TheText = "全項目出力", TheValue = true } ) ;
      _outputItems.Add( new ListBoxItem { TheText = "出力項目選択", TheValue = false } ) ;
      LbOutputItem.ItemsSource = _outputItems ;
      
      _itemTypes.Add( new ListBoxItem { TheText = "長さ物", TheValue = true } ) ;
      _itemTypes.Add( new ListBoxItem { TheText = "工事部材", TheValue = true } ) ;
      _itemTypes.Add( new ListBoxItem { TheText = "機器取付", TheValue = false } ) ;
      _itemTypes.Add( new ListBoxItem { TheText = "結線", TheValue = false } ) ;
      _itemTypes.Add( new ListBoxItem { TheText = "盤搬入据付", TheValue = false } ) ;
      _itemTypes.Add( new ListBoxItem { TheText = "内装・補修・設備", TheValue = true } ) ;
      _itemTypes.Add( new ListBoxItem { TheText = "その他", TheValue = false } ) ;

      _itemsTypesSelected = new List<string>() { "長さ物", "工事部材", "内装・補修・設備" } ;
    }

    private void ChangeValueItemType()
    {
      foreach ( var itemType in _itemTypes ) {
        itemType.TheValue = _itemsTypesSelected.Contains( itemType.TheText! ) ? true : false ;
      }
    }

    private void CreateOutputFile()
    {
      try {
        XSSFWorkbook workbook = new XSSFWorkbook() ;

        CreateSheet( SheetType.Summary, workbook, "エアコンスイッチ工事" ) ;
        CreateSheet( SheetType.Confirmation, workbook, "自動制御設備工事" ) ;
        
        FileStream fs = new FileStream( path, FileMode.OpenOrCreate ) ;
        workbook.Write( fs );
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

    private void CreateSheet( SheetType sheetType, IWorkbook workbook, string sheetName )
    {
      List<string> levels = _document.GetAllElements<Level>().Select( l => l.Name ).ToList() ;

      XSSFCellStyle borderedCellStyle = CreateCellStyle( workbook, BorderStyle.Medium, BorderStyle.Medium, BorderStyle.Medium, NPOI.SS.UserModel.VerticalAlignment.Center ) ;
      XSSFCellStyle noneBorderedCellStyle = CreateCellStyle( workbook, BorderStyle.None, BorderStyle.None, BorderStyle.None, NPOI.SS.UserModel.VerticalAlignment.None ) ;
      XSSFCellStyle bottomBorderedCellStyle = CreateCellStyle( workbook, BorderStyle.None, BorderStyle.None, BorderStyle.Medium, NPOI.SS.UserModel.VerticalAlignment.None ) ;

      ISheet sheet = workbook.CreateSheet( sheetName ) ;
      var row0 = sheet.CreateRow( 0 ) ;
      var row1 = sheet.CreateRow( 1 ) ;
      var row2 = sheet.CreateRow( 2 ) ;
      var row3 = sheet.CreateRow( 3 ) ;
      switch ( sheetType ) {
        case SheetType.Confirmation :
          CreateMergeCell( sheet, row0, 0, 0, 2, 6, "ドーコンOFF", bottomBorderedCellStyle ) ;
          CreateCell( row0, 2, "ドーコンOFF", bottomBorderedCellStyle ) ;
          CreateCell( row0, 13, "縮尺 :", bottomBorderedCellStyle ) ;
          CreateCell( row0, 14, "", bottomBorderedCellStyle ) ;
          CreateCell( row0, 15, "階高 :", bottomBorderedCellStyle ) ;
          CreateCell( row0, 16, "", bottomBorderedCellStyle ) ;

          CreateCell( row1, 1, "入力確認表", noneBorderedCellStyle ) ;
          CreateCell( row1, 2, "工事階層 :", noneBorderedCellStyle ) ;
          CreateCell( row1, 4, sheetName, noneBorderedCellStyle ) ;
          CreateCell( row1, 7, "図面番号 :", noneBorderedCellStyle ) ;
          CreateCell( row1, 8, levels.First(), noneBorderedCellStyle ) ;
          CreateCell( row1, 10, "階数 :", noneBorderedCellStyle ) ;
          CreateCell( row1, 12, "区間 :", noneBorderedCellStyle ) ;
          
          CreateCell( row2, 1, "品名", borderedCellStyle ) ;
          CreateMergeCell( sheet, row2, 2, 2, 2, 3, "規格", borderedCellStyle ) ;
          CreateCell( row2, 4, "単位", borderedCellStyle ) ;
          CreateMergeCell( sheet, row2, 2, 2, 5, 15, "軌跡", borderedCellStyle ) ;
          CreateCell( row2, 16, "合計数量", borderedCellStyle ) ;

          break ;
        case SheetType.Summary :
          CreateCell( row0, 2, "拾い出し集計表", noneBorderedCellStyle ) ;
          CreateCell( row0, 6, "ドーコンOFF", bottomBorderedCellStyle ) ;
          for ( var i = 7; i < 19; i++ ) {
            CreateCell( row0, i, "", bottomBorderedCellStyle ) ;
          }
          CreateCell( row0, 14, sheetName, bottomBorderedCellStyle ) ;

          CreateMergeCell( sheet, row2, 2, 2, 1, 3, "", borderedCellStyle ) ;
          CreateCell( row2, 4, "", borderedCellStyle ) ;
          var index = 5 ;
          foreach ( var level in levels ) {
            CreateCell( row2, index, level, borderedCellStyle ) ;
            CreateCell( row3, index, "", borderedCellStyle ) ;
            index++ ;
          }
          CreateCell( row2, index, "合計", borderedCellStyle ) ;
          
          CreateMergeCell( sheet, row3, 3, 3, 1, 3, "品名/規格", borderedCellStyle ) ;
          CreateCell( row3, 4, "単位", borderedCellStyle ) ;
          CreateCell( row3, index, "", borderedCellStyle ) ;
          break ;
        default :
          break ;
      }
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
    }
    
    private XSSFCellStyle CreateCellStyle( IWorkbook workbook, BorderStyle borderStyle, BorderStyle topBorderStyle , BorderStyle bottomBorderStyle, NPOI.SS.UserModel.VerticalAlignment verticalAlignment )
    {
      XSSFCellStyle borderedCellStyle = (XSSFCellStyle) workbook.CreateCellStyle() ;
      borderedCellStyle.BorderLeft = borderStyle ;
      borderedCellStyle.BorderTop = topBorderStyle ;
      borderedCellStyle.BorderRight = borderStyle ;
      borderedCellStyle.BorderBottom = bottomBorderStyle ;
      borderedCellStyle.VerticalAlignment = verticalAlignment ;
      return borderedCellStyle ;
    }
  }
}