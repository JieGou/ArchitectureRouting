using System ;
using System.Collections.Generic ;
using System.Globalization ;
using System.IO ;
using System.Linq ;
using System.Text ;
using System.Windows ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;
using NPOI.SS.UserModel ;
using NPOI.XSSF.UserModel ;
using MessageBox = System.Windows.MessageBox ;
using CellType = NPOI.SS.UserModel.CellType ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CsvModelDialog : Window
  {
    private readonly Document _document ;
    private List<WiresAndCablesModel> _allWiresAndCablesModels ;
    private List<ConduitsModel> _allConduitModels ;
    private List<HiroiSetMasterModel> _allHiroiSetMasterNormalModels ;
    private List<HiroiSetMasterModel> _allHiroiSetMasterEcoModels ;
    private List<HiroiSetCdMasterModel> _allHiroiSetCdMasterNormalModels ;
    private List<HiroiSetCdMasterModel> _allHiroiSetCdMasterEcoModels ;
    private List<HiroiMasterModel> _allHiroiMasterModels ;
    private List<CeedModel> _ceeDModelData ;
    private bool _isLoadAll ;

    public CsvModelDialog( Document document )
    {
      InitializeComponent() ;

      _document = document ;
      _allWiresAndCablesModels = new List<WiresAndCablesModel>() ;
      _allConduitModels = new List<ConduitsModel>() ;
      _allHiroiSetMasterNormalModels = new List<HiroiSetMasterModel>() ;
      _allHiroiSetMasterEcoModels = new List<HiroiSetMasterModel>() ;
      _allHiroiSetCdMasterNormalModels = new List<HiroiSetCdMasterModel>() ;
      _allHiroiSetCdMasterEcoModels = new List<HiroiSetCdMasterModel>() ;
      _allHiroiMasterModels = new List<HiroiMasterModel>() ;
      _ceeDModelData = new List<CeedModel>() ;
    }

    private void Button_Save( object sender, RoutedEventArgs e )
    {
      CsvStorable csvStorable = _document.GetCsvStorable() ;
      {
        if ( _allWiresAndCablesModels.Any() )
          csvStorable.WiresAndCablesModelData = _allWiresAndCablesModels ;
        if ( _allConduitModels.Any() )
          csvStorable.ConduitsModelData = _allConduitModels ;
        if ( _allHiroiSetMasterNormalModels.Any() )
          csvStorable.HiroiSetMasterNormalModelData = _allHiroiSetMasterNormalModels ;
        if ( _allHiroiSetMasterEcoModels.Any() )
          csvStorable.HiroiSetMasterEcoModelData = _allHiroiSetMasterEcoModels ;
        if ( _allHiroiSetCdMasterNormalModels.Any() )
          csvStorable.HiroiSetCdMasterNormalModelData = _allHiroiSetCdMasterNormalModels ;
        if ( _allHiroiSetCdMasterEcoModels.Any() )
          csvStorable.HiroiSetCdMasterEcoModelData = _allHiroiSetCdMasterEcoModels ;
        if ( _allHiroiMasterModels.Any() )
          csvStorable.HiroiMasterModelData = _allHiroiMasterModels ;

        try {
          using Transaction t = new Transaction( _document, "Save data" ) ;
          t.Start() ;
          csvStorable.Save() ;
          t.Commit() ;
        }
        catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
          MessageBox.Show( "Save CSV Files Failed.", "Error Message" ) ;
          DialogResult = false ;
        }
      }
      CeedStorable ceeDStorable = _document.GetCeeDStorable() ;
      {
        if ( _ceeDModelData.Any() ) {
          ceeDStorable.CeedModelData = _ceeDModelData ;
          try {
            using Transaction t = new Transaction( _document, "Save CeeD data" ) ;
            t.Start() ;
            ceeDStorable.Save() ;
            t.Commit() ;
          }
          catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
          }
        }
      }

      DialogResult = true ;
      Close() ;
    }

    private void Button_LoadWiresAndCablesData( object sender, RoutedEventArgs e )
    {
      _allWiresAndCablesModels = new List<WiresAndCablesModel>() ;
      string filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      GetData( filePath, 2, ModelName.WiresAndCables ) ;
    }

    private void Button_LoadConduitsData( object sender, RoutedEventArgs e )
    {
      _allConduitModels = new List<ConduitsModel>() ;
      string filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      GetData( filePath, 2, ModelName.Conduits ) ;
    }

    private void Button_LoadHiroiSetMasterNormalData( object sender, RoutedEventArgs e )
    {
      _allHiroiSetMasterNormalModels = new List<HiroiSetMasterModel>() ;
      string filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      GetData( filePath, 0, ModelName.HiroiSetMasterNormal ) ;
    }

    private void Button_LoadHiroiSetMasterEcoData( object sender, RoutedEventArgs e )
    {
      _allHiroiSetMasterEcoModels = new List<HiroiSetMasterModel>() ;
      string filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      GetData( filePath, 0, ModelName.HiroiSetMasterEco ) ;
    }

    private void Button_LoadHiroiSetCdMasterNormalData( object sender, RoutedEventArgs e )
    {
      _allHiroiSetCdMasterNormalModels = new List<HiroiSetCdMasterModel>() ;
      string filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      GetData( filePath, 0, ModelName.HiroiSetCdMasterNormal ) ;
    }

    private void Button_LoadHiroiSetCdMasterEcoData( object sender, RoutedEventArgs e )
    {
      _allHiroiSetCdMasterEcoModels = new List<HiroiSetCdMasterModel>() ;
      string filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      GetData( filePath, 0, ModelName.HiroiSetCdMasterEco ) ;
    }

    private void Button_LoadHiroiMasterData( object sender, RoutedEventArgs e )
    {
      _allHiroiMasterModels = new List<HiroiMasterModel>() ;
      string filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      GetData( filePath, 0, ModelName.HiroiMaster ) ;
    }

    private string OpenFileDialog()
    {
      OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Csv files (*.csv)|*.csv", Multiselect = false } ;
      string filePath = string.Empty ;
      if ( openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
        filePath = openFileDialog.FileName ;
      }

      return filePath ;
    }

    private void GetData( string path, int startLine, ModelName modelName )
    {
      var checkFile = true ;
      const int wacColCount = 10 ;
      const int conduitColCount = 5 ;
      const int hsmColCount = 27 ;
      const int hsCdmColCount = 4 ;
      const int hmColCount = 12 ;
      try {
        using StreamReader reader = new StreamReader( path, Encoding.GetEncoding( "shift-jis" ), true ) ;
        List<string> lines = new List<string>() ;
        var startRow = 0 ;
        while ( ! reader.EndOfStream ) {
          var line = reader.ReadLine() ;
          if ( startRow > startLine ) {
            var values = line!.Split( ',' ) ;

            switch ( modelName ) {
              case ModelName.WiresAndCables :
                if ( values.Length < wacColCount ) checkFile = false ;
                else {
                  WiresAndCablesModel wiresAndCablesModel = new WiresAndCablesModel( values[ 0 ], values[ 1 ], values[ 2 ], values[ 3 ], values[ 4 ], values[ 5 ], values[ 6 ], values[ 7 ], values[ 8 ], values[ 9 ] ) ;
                  _allWiresAndCablesModels.Add( wiresAndCablesModel ) ;
                }

                break ;
              case ModelName.Conduits :
                if ( values.Length < conduitColCount ) checkFile = false ;
                else {
                  ConduitsModel conduitsModel = new ConduitsModel( values[ 0 ], values[ 1 ], values[ 2 ], values[ 3 ], values[ 4 ] ) ;
                  _allConduitModels.Add( conduitsModel ) ;
                }

                break ;
              case ModelName.HiroiSetMasterNormal :
                if ( values.Length < hsmColCount ) checkFile = false ;
                else {
                  HiroiSetMasterModel hiroiSetMasterNormalModel = new HiroiSetMasterModel( values[ 0 ], values[ 1 ], values[ 2 ], values[ 3 ], values[ 4 ], values[ 5 ], values[ 6 ], values[ 7 ], values[ 8 ], values[ 9 ], values[ 10 ], values[ 11 ], values[ 12 ], values[ 13 ], values[ 14 ], values[ 15 ], values[ 16 ], values[ 17 ], values[ 18 ], values[ 19 ], values[ 20 ], values[ 21 ], values[ 22 ], values[ 23 ], values[ 24 ], values[ 25 ], values[ 26 ] ) ;
                  _allHiroiSetMasterNormalModels.Add( hiroiSetMasterNormalModel ) ;
                }

                break ;
              case ModelName.HiroiSetMasterEco :
                if ( values.Length < hsmColCount ) checkFile = false ;
                else {
                  HiroiSetMasterModel hiroiSetMasterEcoModel = new HiroiSetMasterModel( values[ 0 ], values[ 1 ], values[ 2 ], values[ 3 ], values[ 4 ], values[ 5 ], values[ 6 ], values[ 7 ], values[ 8 ], values[ 9 ], values[ 10 ], values[ 11 ], values[ 12 ], values[ 13 ], values[ 14 ], values[ 15 ], values[ 16 ], values[ 17 ], values[ 18 ], values[ 19 ], values[ 20 ], values[ 21 ], values[ 22 ], values[ 23 ], values[ 24 ], values[ 25 ], values[ 26 ] ) ;
                  _allHiroiSetMasterEcoModels.Add( hiroiSetMasterEcoModel ) ;
                }

                break ;
              case ModelName.HiroiSetCdMasterNormal :
                if ( values.Length < hsCdmColCount ) checkFile = false ;
                else {
                  HiroiSetCdMasterModel hiroiSetCdMasterNormalModel = new HiroiSetCdMasterModel( values[ 0 ], values[ 1 ], values[ 2 ], values[ 3 ] ) ;
                  _allHiroiSetCdMasterNormalModels.Add( hiroiSetCdMasterNormalModel ) ;
                }

                break ;
              case ModelName.HiroiSetCdMasterEco :
                if ( values.Length < hsCdmColCount ) checkFile = false ;
                else {
                  HiroiSetCdMasterModel hiroiSetCdMasterEcoModel = new HiroiSetCdMasterModel( values[ 0 ], values[ 1 ], values[ 2 ], values[ 3 ] ) ;
                  _allHiroiSetCdMasterEcoModels.Add( hiroiSetCdMasterEcoModel ) ;
                }

                break ;
              case ModelName.HiroiMaster :
                if ( values.Length < hmColCount ) checkFile = false ;
                else {
                  HiroiMasterModel hiroiMasterModel = new HiroiMasterModel( values[ 0 ], values[ 1 ], values[ 2 ], values[ 3 ], values[ 4 ], values[ 5 ], values[ 6 ], values[ 7 ], values[ 8 ], values[ 9 ], values[ 10 ], values[ 11 ] ) ;
                  _allHiroiMasterModels.Add( hiroiMasterModel ) ;
                }

                break ;
              default :
                throw new ArgumentOutOfRangeException( nameof( modelName ), modelName, null ) ;
            }
          }

          if ( ! checkFile ) {
            break ;
          }

          startRow++ ;
        }

        reader.Close() ;
        if ( ! checkFile ) {
          MessageBox.Show( "Incorrect file format.", "Error Message" ) ;
        }
        else {
          if ( ! _isLoadAll ) {
            MessageBox.Show( "Load file successful.", "Result Message" ) ;
          }
        }
      }
      catch ( Exception ) {
        MessageBox.Show( "Load file failed.", "Error Message" ) ;
      }
    }

    private enum ModelName
    {
      WiresAndCables,
      Conduits,
      HiroiSetMasterNormal,
      HiroiSetMasterEco,
      HiroiSetCdMasterNormal,
      HiroiSetCdMasterEco,
      HiroiMaster
    }

    private void BtnLoadAll_OnClick( object sender, RoutedEventArgs e )
    {
      var dialog = new FolderBrowserDialog() ;
      dialog.ShowNewFolderButton = false ;
      dialog.ShowDialog() ;
      string[] fileNames = new[]
      {
        "hiroimaster.csv", "hiroisetcdmaster_normal.csv", "hiroisetcdmaster_eco.csv", "hiroisetmaster_eco.csv",
        "hiroisetmaster_normal.csv", "電線管一覧.csv", "電線・ケーブル一覧.csv", "【CeeD】セットコード一覧表.xlsx"
      } ;
      _isLoadAll = true ;
      foreach ( var fileName in fileNames ) {
        var path = Path.Combine( dialog.SelectedPath, fileName ) ;
        if ( File.Exists( path ) ) {
          switch ( fileName ) {
            case "hiroimaster.csv" :
              _allHiroiMasterModels = new List<HiroiMasterModel>() ;
              GetData( path, 0, ModelName.HiroiMaster ) ;
              break ;
            case "hiroisetcdmaster_normal.csv" :
              _allHiroiSetCdMasterNormalModels = new List<HiroiSetCdMasterModel>() ;
              GetData( path, 0, ModelName.HiroiSetCdMasterNormal ) ;
              break ;
            case "hiroisetcdmaster_eco.csv" :
              _allHiroiSetCdMasterEcoModels = new List<HiroiSetCdMasterModel>() ;
              GetData( path, 0, ModelName.HiroiSetCdMasterEco ) ;
              break ;
            case "hiroisetmaster_eco.csv" :
              _allHiroiSetMasterEcoModels = new List<HiroiSetMasterModel>() ;
              GetData( path, 0, ModelName.HiroiSetMasterEco ) ;
              break ;
            case "hiroisetmaster_normal.csv" :
              _allHiroiSetMasterNormalModels = new List<HiroiSetMasterModel>() ;
              GetData( path, 0, ModelName.HiroiSetMasterNormal ) ;
              break ;
            case "電線管一覧.csv" :
              _allConduitModels = new List<ConduitsModel>() ;
              GetData( path, 2, ModelName.Conduits ) ;
              break ;
            case "電線・ケーブル一覧.csv" :
              _allWiresAndCablesModels = new List<WiresAndCablesModel>() ;
              GetData( path, 2, ModelName.WiresAndCables ) ;
              break ;
            case "【CeeD】セットコード一覧表.xlsx" :
              _ceeDModelData = GetAllCeeDModelNumber( path ) ;
              break ;
          }
        }
      }

      MessageBox.Show( "Load files successful.", "Result Message" ) ;
      _isLoadAll = false ;
    }

    private static List<CeedModel> GetAllCeeDModelNumber( string path )
    {
      List<CeedModel> ceedModelData = new List<CeedModel>() ;

      try {
        FileStream fs = new FileStream( path, FileMode.Open, FileAccess.Read ) ;
        XSSFWorkbook wb = new XSSFWorkbook( fs ) ;
        ISheet workSheet = wb.NumberOfSheets < 2 ? wb.GetSheetAt( wb.ActiveSheetIndex ) : wb.GetSheetAt( 1 ) ;
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
          }

          var strModelNumbers = modelNumbers.Any() ? string.Join( "\n", modelNumbers ) : string.Empty ;
          if ( ! ceeDModelNumbers.Any() ) {
            CeedModel ceeDModel = new CeedModel( string.Empty, string.Empty, generalDisplayDeviceSymbols,
              strModelNumbers, floorPlanSymbol, ceeDName ) ;
            ceedModelData.Add( ceeDModel ) ;
          }
          else {
            for ( var k = 0 ; k < ceeDModelNumbers.Count ; k++ ) {
              var ceeDSetCode = ceeDSetCodes.Any() ? ceeDSetCodes[ k ] : string.Empty ;
              CeedModel ceeDModel = new CeedModel( ceeDModelNumbers[ k ], ceeDSetCode, generalDisplayDeviceSymbols,
                strModelNumbers, floorPlanSymbol, ceeDName ) ;
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

    private static string GetCellValue( ICell? cell )
    {
      string cellValue = string.Empty ;
      if ( cell == null ) return cellValue ;
      cellValue = cell.CellType switch
      {
        CellType.Blank => string.Empty,
        CellType.Numeric => DateUtil.IsCellDateFormatted( cell )
          ? cell.DateCellValue.ToString( CultureInfo.InvariantCulture )
          : cell.NumericCellValue.ToString( CultureInfo.InvariantCulture ),
        CellType.String => cell.StringCellValue,
        _ => cellValue
      } ;

      return cellValue ;
    }
  }
}
