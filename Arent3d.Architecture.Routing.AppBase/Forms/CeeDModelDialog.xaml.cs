using System ;
using System.Collections.Generic ;
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

      var images=GetListImages(  filePath ) ;
      MessageBox.Show( images.Count.ToString() ) ;
      
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

    //void ReadFileByOpenXML( string filePath )
    //{
      //StringBuilder sb = new StringBuilder() ;
      // using ( FileStream fs = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) ) {
      //   using ( SpreadsheetDocument doc = SpreadsheetDocument.Open( fs, false ) ) {
      //     WorkbookPart? workbookPart = doc.WorkbookPart ;
      //     if ( workbookPart != null ) {
      //       var sstpart = workbookPart.GetPartsOfType<SharedStringTablePart>().First() ;
      //       SharedStringTable sst = sstpart.SharedStringTable ;
      //
      //       WorksheetPart worksheetPart = workbookPart.WorksheetParts.First() ;
      //       Worksheet sheet = worksheetPart.Worksheet ;
      //
      //       var cells = sheet.Descendants<Cell>() ;
      //       var rows = sheet.Descendants<Row>() ;
      //
      //       sb.AppendLine( string.Format( "Row count = {0}", rows.LongCount() ) ) ;
      //       sb.AppendLine( string.Format( "Row count = {0}", cells.LongCount() ) ) ;

            // One way: go through each cell in the sheet
            // foreach (Cell cell in cells)
            // {
            //   if ((cell.DataType != null) && (cell.DataType == CellValues.SharedString))
            //   {
            //     if ( cell.CellValue != null ) {
            //       int ssid = int.Parse(cell.CellValue.Text);
            //       string str = sst.ChildElements[ssid].InnerText;
            //       result+=string.Format("Shared string {0}: {1}", ssid, str);
            //     }
            //   }
            //   else if (cell.CellValue != null)
            //   {
            //     result+=string.Format("Cell contents: {0}", cell.CellValue.Text);
            //   }
            // }

            // Or... via each row
            // for ( int i = 7 ; i < rows.Count() ; i++ ) {
            //   Row row = rows.ToList()[ i ] ;
            //   Cell c = row.Elements<Cell>().ToList()[ 6 ] ;
            //   if ((c.DataType != null) && (c.DataType == CellValues.SharedString))
            //   {
            //     if ( c.CellValue != null ) {
            //       int ssid = int.Parse(c.CellValue.Text);
            //       string str = sst.ChildElements[ssid].InnerText;
            //       if ( str.Contains( "又は" ) ) {
            //         sb.AppendLine(string.Format("Shared string shape{0}: {1}", ssid, str));  
            //       }
            //       
            //     }
            //   }
            //   else if (c.CellValue != null)
            //   {
            //     sb.AppendLine(string.Format("Cell contents: {0}", c.CellValue.Text));
            //   }
            // }
    //       }
    //     }
    //   }
    // }

    private List<Image> GetListImages( string filePath )
    {
      string workbookPath = filePath ;
      var _listImage = new List<Image>() ;
      Application? excelApp = new Microsoft.Office.Interop.Excel.Application() ;
      var excelWorkbook = excelApp.Workbooks.Open( workbookPath, Type.Missing, false, Type.Missing, Type.Missing, Type.Missing, false, XlPlatform.xlWindows, Type.Missing, true, false, Type.Missing, Type.Missing, Type.Missing, Type.Missing ) ;
      if ( excelWorkbook != null ) {
        Worksheet sheet = (Worksheet) excelWorkbook.Sheets[ "セットコード一覧表" ] ;

        if ( sheet == null ) return _listImage ;

        var shapes = new List<Microsoft.Office.Interop.Excel.Shape>() ;
        Range xlRange = sheet.UsedRange ;
        var endRow = xlRange.Rows.Count ;
        for ( var i = 7 ; i <= endRow ; i++ ) {
          Range cell = (Range) sheet.Cells[ i, 3 ] ;
          if ( cell == null ) continue ;
          var name = cell.Cells.Value2.ToString() ;
          var firstIndexGroup = i ;
          var nextName = cell.Cells.Value2.ToString() ;

          do {
            i++ ;
            if ( i > endRow ) break ;
            name = nextName ;
            // cell =  sheet.Cells[ i, 3 ].ToString() ;
            // if ( cell == null ) break ;
            nextName = cell.Cells.Value2.ToString() ;
          } while ( ! ( string.IsNullOrEmpty( name ) && ! string.IsNullOrEmpty( nextName ) ) ) ;
          //COPY SHAPE TO NEW SHEET

          //CONVERT SHARP TO IMAGE 
          for ( int j = 1 ; i <= sheet.Shapes.Count ; j++ ) {
            var shape = sheet.Shapes.Item( j ) ;
            shape.Copy() ;
            var image = Clipboard.GetImage() ;
            _listImage.Add( image ) ;
          }

          // foreach ( var shape in shapes ) {
          //   shape.Copy() ;
          //   var image = Clipboard.GetImage() ;
          //   _listImage.Add( image ) ;
          // }
          //DELETE TEMP SHEET
        }


        excelWorkbook.Save() ;
        excelWorkbook.Close() ;
        excelApp.Quit() ;


        if ( sheet != null )
          Marshal.ReleaseComObject( sheet ) ;

        if ( excelWorkbook != null )
          Marshal.ReleaseComObject( excelWorkbook ) ;
        if ( excelApp != null )
          Marshal.ReleaseComObject( excelApp ) ;
        excelApp = null ;
      }

      return _listImage ;
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


        var allFloorShapes = drawing.GetShapes().Where( x => ( (XSSFClientAnchor) x.GetAnchor() ).Col1 is 5 )
          .OrderBy( x => ( (XSSFClientAnchor) x.GetAnchor() ).Col1 ).ThenBy( x => ( (XSSFClientAnchor) x.GetAnchor() ).Row1 ) ;
        var allInstrumentationShapes = drawing.GetShapes().Where( x => ( (XSSFClientAnchor) x.GetAnchor() ).Col1 is 6 )
          .OrderBy( x => ( (XSSFClientAnchor) x.GetAnchor() ).Col1 ).ThenBy( x => ( (XSSFClientAnchor) x.GetAnchor() ).Row1 ) ;

        List<ImageBytes> imageBytesList = new List<ImageBytes>() ;
        foreach ( var shape in allFloorShapes ) {
          var clientAnchor = ( (XSSFClientAnchor) shape.GetAnchor() ) ;
          var position = ( (XSSFClientAnchor) shape.GetAnchor() ).Row1 ;
          byte[] imageByte = new byte[] { } ;
          //TODO convert shape to byte[]
          //1.
          // ObjectData inpPic = (ObjectData) shape ;
          // FileOutputStream out = new FileOutputStream("pict.jpg");
          //imageByte = inpPic.PictureData.Data ;
          //2.
          // BinaryFormatter bf = new BinaryFormatter();
          // using (MemoryStream ms = new MemoryStream())
          // {
          //   bf.Serialize(ms, shape);
          //   imageByte= ms.ToArray();
          // }
          //3.
          // shape.Copy() ;
          // if (Clipboard.ContainsImage())
          // {
          //   var bitmap = Clipboard.GetImage();
          //   if (bitmap==null) continue;
          //   JpegBitmapEncoder encoder = new JpegBitmapEncoder();
          //   using MemoryStream stream  = new MemoryStream() ;
          //   bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
          // encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
          // encoder.Save(stream);
          //   imageByte = stream.ToArray();
          // }
          imageBytesList.Add( new ImageBytes( position, imageByte ) ) ;
        }

        const int startRow = 7 ;
        var endRow = workSheet.LastRowNum ;
        for ( var i = startRow ; i <= endRow ; i++ ) {
          List<string> ceeDModelNumbers = new List<string>() ;
          List<string> ceeDSetCodes = new List<string>() ;
          List<string> modelNumbers = new List<string>() ;
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
          }

          var strModelNumbers = modelNumbers.Any() ? string.Join( "\n", modelNumbers ) : string.Empty ;
          if ( ! ceeDModelNumbers.Any() ) {
            CeedModel ceeDModel = new CeedModel( string.Empty, string.Empty, generalDisplayDeviceSymbols, strModelNumbers, floorPlanSymbol, ceeDName ) ;
            ceedModelData.Add( ceeDModel ) ;
          }
          else {
            for ( var k = 0 ; k < ceeDModelNumbers.Count ; k++ ) {
              var ceeDSetCode = ceeDSetCodes.Any() ? ceeDSetCodes[ k ] : string.Empty ;
              var imageByPosition = imageBytesList.Where( b => b.ImagePostion == i + 1 ).Select( c => c.ImagePostion ).ToList() ;
              CeedModel ceeDModel ;
              if ( imageByPosition is { Count: > 0 } ) {
                var positionImg = string.Empty ;
                foreach ( var pos in imageByPosition ) {
                  positionImg = positionImg + " " + pos ;
                }
                ceeDModel = new CeedModel( ceeDModelNumbers[ k ], ceeDSetCode, generalDisplayDeviceSymbols, strModelNumbers, positionImg, ceeDName ) ;
              }
              else {
                ceeDModel = new CeedModel( ceeDModelNumbers[ k ], ceeDSetCode, generalDisplayDeviceSymbols, strModelNumbers, floorPlanSymbol, ceeDName ) ;
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

  public class ImageBytes
  {
    public ImageBytes( int imagePostion, byte[] imageBytesList )
    {
      this.ImagePostion = imagePostion ;
      ImageByte = imageBytesList ;
    }

    public int ImagePostion { get ; set ; }
    public byte[] ImageByte { get ; set ; }
  }
}