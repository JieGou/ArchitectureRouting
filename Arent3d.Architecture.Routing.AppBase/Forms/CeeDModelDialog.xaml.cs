using System ;
using System.Collections.Generic ;
using System.Globalization ;
using System.IO ;
using System.Linq ;
using System.Text ;
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
using NPOI.SS.UserModel ;
using NPOI.XSSF.UserModel ;
using CellType = NPOI.SS.UserModel.CellType ;
using MessageBox = System.Windows.MessageBox ;
using Visibility = System.Windows.Visibility ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CeeDModelDialog : Window
  {
    private readonly Document _document ;
    private CeedViewModel? _allCeeDModels ;
    private CeedViewModel? _ceeDModelUsed ;
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
      _ceeDModelUsed = null ;
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
      var selectedItem = (CeedModel)DtGrid.SelectedValue ;
      SelectedSetCode = selectedItem.CeeDSetCode ;
      DialogResult = true ;
      Close() ;      
    }

    private void Button_Click( object sender, RoutedEventArgs e )
    {
      if ( _ceeDModelUsed != null && _ceeDModelUsed.CeedModels.Any() ) {
        CeedStorable ceeDStorable = _document.GetCeeDStorable() ;
        {
          try {
            using Transaction t = new Transaction( _document, "Save data" ) ;
            t.Start() ;
            ceeDStorable.CeedModelUsedData = _ceeDModelUsed.CeedModels ;
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
      if ( _allCeeDModels == null && _ceeDModelUsed == null ) return ;
      var ceeDViewModels = _ceeDModelUsed ?? _allCeeDModels ;
      if ( ceeDViewModels == null ) return ;
      if ( string.IsNullOrEmpty( _ceeDModelNumberSearch ) && string.IsNullOrEmpty( _modelNumberSearch ) ) {
        this.DataContext = ceeDViewModels ;
      }
      else {
        List<CeedModel> ceeDModels = new List<CeedModel>() ;
        switch ( string.IsNullOrEmpty( _ceeDModelNumberSearch ) ) {
          case false when ! string.IsNullOrEmpty( _modelNumberSearch ) :
            ceeDModels = ceeDViewModels.CeedModels.Where( c => c.CeeDModelNumber.Contains( _ceeDModelNumberSearch ) && c.ModelNumber.Contains( _modelNumberSearch ) ).ToList() ;
            break ;
          case false when string.IsNullOrEmpty( _modelNumberSearch ) :
            ceeDModels = ceeDViewModels.CeedModels.Where( c => c.CeeDModelNumber.Contains( _ceeDModelNumberSearch ) ).ToList() ;
            break ;
          case true when ! string.IsNullOrEmpty( _modelNumberSearch ) :
            ceeDModels = ceeDViewModels.CeedModels.Where( c => c.ModelNumber.Contains( _modelNumberSearch ) ).ToList() ;
            break ;
        }
        var ceeDModelsSearch = new CeedViewModel( ceeDViewModels.CeedStorable, ceeDModels ) ;
        this.DataContext = ceeDModelsSearch ;
      }
    }
    
    private void Button_SymbolRegistration( object sender, RoutedEventArgs e )
    {
      OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Csv files (*.xlsx)|*.xlsx|(*.csv)|*.csv", Multiselect = false } ;
      string filePath = string.Empty;
      if ( openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
        filePath = openFileDialog.FileName ;
      }

      if ( string.IsNullOrEmpty( filePath ) ) return ;
      var ceeDStorable = _document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( ceeDStorable == null ) return ;
      var modelNumberUsed = GetModelNumberUsed( filePath ) ;
      if ( ! modelNumberUsed.Any() ) return ;
      List<CeedModel> ceeDModelUsed = new List<CeedModel>() ;
      foreach ( var modelNumber in modelNumberUsed ) {
        var ceeDModels = ceeDStorable.CeedModelData.Where( c => c.ModelNumber.Contains( modelNumber ) ).Distinct().ToList() ;
        ceeDModelUsed.AddRange( ceeDModels ) ;
      }

      ceeDModelUsed = ceeDModelUsed.Distinct().ToList() ;
      _ceeDModelUsed = new CeedViewModel( ceeDStorable, ceeDModelUsed ) ;
      this.DataContext = _ceeDModelUsed ;
      CbCodeIsUsed.Visibility = Visibility.Visible ;
    }
    
    private void Button_LoadData( object sender, RoutedEventArgs e )
    {
      OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Csv files (*.xlsx)|*.xlsx", Multiselect = false } ;
      string filePath = string.Empty;
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

    private void LoadData( CeedStorable ceeDStorable )
    {
      var ceeDModelData = ceeDStorable.CeedModelUsedData.Any() ? ceeDStorable.CeedModelUsedData : ceeDStorable.CeedModelData ;
      if ( ! ceeDModelData.Any() ) return ;
      var viewModelUsed = new ViewModel.CeedViewModel( ceeDStorable, ceeDModelData ) ;
      this.DataContext = viewModelUsed ;
      _ceeDModelUsed = viewModelUsed ;
      CmbCeeDModelNumbers.ItemsSource = viewModelUsed.CeeDModelNumbers ;
      CmbModelNumbers.ItemsSource = viewModelUsed.ModelNumbers ;
      _allCeeDModels = ceeDStorable.CeedModelUsedData.Any() ? new ViewModel.CeedViewModel( ceeDStorable, ceeDStorable.CeedModelData ) : viewModelUsed ;
      CbCodeIsUsed.Visibility = ceeDStorable.CeedModelUsedData.Any() ? Visibility.Visible : Visibility.Hidden ;
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
              CeedModel ceeDModel = new CeedModel( ceeDModelNumbers[ k ], ceeDSetCode, generalDisplayDeviceSymbols, strModelNumbers, floorPlanSymbol, ceeDName ) ;
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
    
    private static List<string> GetModelNumberUsed( string path )
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
              if ( string.IsNullOrEmpty( strModelNumber )  ) continue ;
              var arrModelNumbers = strModelNumber.Split( '\n' ) ;
              foreach ( var modelNumber in arrModelNumbers ) {
                if ( ! string.IsNullOrEmpty( modelNumber ) && ! modelNumbers.Contains( modelNumber ) ) {
                  modelNumbers.Add( modelNumber );
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
              var modelNumber = values.Length > 1 ? values[1].Trim() : values[0].Trim() ;
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
    
    private void CodeIsUse_Checked( object sender, RoutedEventArgs e )
    {
      if ( _ceeDModelUsed == null ) return ;
        this.DataContext = _ceeDModelUsed ;
    }

    private void CodeIsUse_UnChecked( object sender, RoutedEventArgs e )
    {
      if ( _allCeeDModels == null ) return ;
      this.DataContext = _allCeeDModels ;
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