using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;
using System ;
using System.Collections.ObjectModel ;
using System.Collections.Specialized ;
using System.Globalization ;
using System.IO ;
using System.Linq ;
using System.Windows ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using NPOI.XSSF.UserModel ;
using NPOI.SS.UserModel ;
using NPOI.SS.Util ;
using BorderStyle = NPOI.SS.UserModel.BorderStyle ;
using CheckBox = System.Windows.Controls.CheckBox ;
using MessageBox = System.Windows.Forms.MessageBox ;
using RadioButton = System.Windows.Controls.RadioButton ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.StorableCaches ;
using MoreLinq ;
using MoreLinq.Extensions ;
using NPOI.OpenXmlFormats.Spreadsheet ;
using CellType = NPOI.SS.UserModel.CellType ;
using FillPattern = NPOI.SS.UserModel.FillPattern ;
using MarginType = NPOI.SS.UserModel.MarginType ;


namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class PickUpReportViewModel : NotifyPropertyChanged
  {
    private const string SummaryFileType = "拾い出し集計表" ;
    private const string ConfirmationFileType = "拾い根拠確認表" ;
    private string PickUpNumberOff => FileName + "OFF" ;
    private string PickUpNumberOn => FileName + "ON" ;
    private const string OnText = "ON" ;
    private const string OffText = "OFF" ;
    private const string OutputItemAll = "全項目出力" ;
    private const string OutputItemSelection = "出力項目選択" ;
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
    private const string Wire = "電線" ;
    private const float DefaultCharacterWidth = 7.001699924468994F ;
    
    private readonly Document _document ;
    private readonly List<HiroiMasterModel> _hiroiMasterModels ;
    private readonly List<HiroiSetMasterModel> _hiroiSetMasterNormalModels ;
    private readonly List<HiroiSetMasterModel> _hiroiSetMasterEcoModels ;
    private readonly List<HiroiSetCdMasterModel> _hiroiSetCdMasterNormalModels ;
    private readonly List<HiroiSetCdMasterModel> _hiroiSetCdMasterEcoModels ;
    private readonly List<DetailTableModel> _dataDetailTable ;

    public ObservableCollection<PickUpModel> PickUpModels { get ; set ; }
    public ObservableCollection<ListBoxItem> FileTypes { get ; set ; }
    public ObservableCollection<ListBoxItem> DoconTypes { get ; set ; }
    public ObservableCollection<ListBoxItem> OutputItems { get ; set ; }
    
    public ObservableCollection<ListBoxItem> CurrentSettingList { get ; set ; }
    public ObservableCollection<ListBoxItem> PreviousSettingList { get ; set ; }

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

    private List<string> _fileNames ;
    
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

    public RelayCommand GetSaveLocationCommand => new( GetSaveLocation ) ;
    public RelayCommand<Window> CancelCommand => new( Cancel ) ;
    public RelayCommand<Window> ExecuteCommand => new( Execute ) ;
    public RelayCommand SettingCommand => new( OutputItemsSelectionSetting ) ;
    public RelayCommand<Window> SetOptionCommand => new( SetOption ) ;
    
    
    public PickUpReportViewModel( Document document )
    {
      _document = document ;
      PickUpModels = new ObservableCollection<PickUpModel>() ;
      FileTypes = new ObservableCollection<ListBoxItem>() ;
      DoconTypes = new ObservableCollection<ListBoxItem>() ;
      OutputItems = new ObservableCollection<ListBoxItem>() ;
      CurrentSettingList = new ObservableCollection<ListBoxItem>() ;
      PreviousSettingList = new ObservableCollection<ListBoxItem>() ;
      _hiroiMasterModels = new List<HiroiMasterModel>() ;
      _hiroiSetMasterNormalModels = new List<HiroiSetMasterModel>() ;
      _hiroiSetMasterEcoModels = new List<HiroiSetMasterModel>() ;
      _hiroiSetCdMasterNormalModels = new List<HiroiSetCdMasterModel>() ;
      _hiroiSetCdMasterEcoModels = new List<HiroiSetCdMasterModel>() ;
      _dataDetailTable = new List<DetailTableModel>() ;
      
      var csvStorable = _document.GetAllStorables<CsvStorable>().FirstOrDefault() ;
      if ( csvStorable != null ) 
      {
        _hiroiMasterModels = csvStorable.HiroiMasterModelData ;
        _hiroiSetMasterNormalModels = csvStorable.HiroiSetMasterNormalModelData ;
        _hiroiSetMasterEcoModels = csvStorable.HiroiSetMasterEcoModelData ;
        _hiroiSetCdMasterNormalModels = csvStorable.HiroiSetCdMasterNormalModelData ;
        _hiroiSetCdMasterEcoModels = csvStorable.HiroiSetCdMasterEcoModelData ;
      }
      
      var detailSymbolStorable =  _document.GetDetailTableStorable() ;
      _dataDetailTable = detailSymbolStorable.DetailTableModelData ; ;


      _pathName = string.Empty ;
      _fileName = string.Empty ;
      _fileNames = new List<string>() ;
      CreateCheckBoxList() ;
      InitPickUpModels() ;
    }

    private void InitPickUpModels()
    {
      var pickUpStorable = _document.GetAllStorables<PickUpStorable>().FirstOrDefault() ;
      if ( pickUpStorable != null ) PickUpModels = new ObservableCollection<PickUpModel>( pickUpStorable.AllPickUpModelData ) ;
    }

    private void GetSaveLocation()
    {
      const string fileName = "フォルダを選択してください.xlsx" ;
      SaveFileDialog saveFileDialog = new SaveFileDialog { FileName = fileName, InitialDirectory = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ) } ;

      if ( saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK ) return ;
      PathName = Path.GetDirectoryName( saveFileDialog.FileName )! ;
    }

    private void Execute( Window window )
    {
      if ( _fileNames.Any() && ! string.IsNullOrEmpty( PathName ) && ! string.IsNullOrEmpty( FileName ) && PickUpModels.Any()  ) {
        CreateOutputFile() ;
        window.DialogResult = true ;
        window.Close() ;
      }
      else { 
        var errorStr = "Please select " ;
        var listError = new List<string>();
        if ( ! _fileNames.Any() ) listError.Add( "file type" ) ; 
        if ( string.IsNullOrEmpty( PathName ) ) listError.Add( "the output folder" ) ;
        if ( string.IsNullOrEmpty( FileName ) ) listError.Add( "the file name" ) ;
        for ( int i = 0 ; i < listError.Count ; i++ ) {
          if ( i != listError.Count - 1 ) errorStr += listError[ i ] + ( i == listError.Count - 2 ? " " : ", " ) ;
          else errorStr += $"and {listError[ i ]}." ;
        }
        if ( ! PickUpModels.Any() ) errorStr = "Don't have pick up data." ;
        MessageBox.Show( errorStr, "Warning" ) ;
      }
    }
    
    private void Cancel( Window window)
    {
      window.DialogResult = false ;
      window.Close() ;
    }
    
    private void CreateCheckBoxList()
    {
      // FileTypes
      FileTypes.Add( new ListBoxItem { TheText = SummaryFileType, TheValue = false } ) ;
      FileTypes.Add( new ListBoxItem { TheText = ConfirmationFileType, TheValue = false } ) ;

      // DoconTypes
      DoconTypes.Add( new ListBoxItem { TheText = OnText, TheValue = true } ) ;
      DoconTypes.Add( new ListBoxItem { TheText = OffText, TheValue = false } ) ;
      
      // OutputItems
      OutputItems.Add( new ListBoxItem { TheText = OutputItemAll, TheValue = true } );
      OutputItems.Add( new ListBoxItem { TheText = OutputItemSelection, TheValue = false } );

      //SettingList
      CreateSettingList() ;
    }

    private void CreateSettingList()
    {
      CurrentSettingList.Add( new ListBoxItem { TheText = LengthItem, TheValue = true } );
      CurrentSettingList.Add( new ListBoxItem { TheText = ConstructionMaterialItem, TheValue = true } );
      CurrentSettingList.Add( new ListBoxItem { TheText = EquipmentMountingItem, TheValue = false } );
      CurrentSettingList.Add( new ListBoxItem { TheText = WiringItem, TheValue = false } );
      CurrentSettingList.Add( new ListBoxItem { TheText = BoardItem, TheValue = false } );
      CurrentSettingList.Add( new ListBoxItem { TheText = InteriorRepairEquipmentItem, TheValue = true } );
      CurrentSettingList.Add( new ListBoxItem { TheText = OtherItem, TheValue = false } );

      PreviousSettingList = new ObservableCollection<ListBoxItem>( CurrentSettingList.Select( x => x.Copy() ).ToList() ) ;
    }
    
    public void OutputItemsChecked( object sender )
    {
      var radioButton = sender as RadioButton ;
      IsOutputItemsEnable = radioButton?.Content.ToString() == OutputItemSelection ;
    }

    public void DoconItemChecked( object sender )
    {
      _fileNames = new List<string>() ;
      var radioButton = sender as RadioButton ;
      var fileTypes = FileTypes.Where( f => f.TheValue == true ).Select( f => f.TheText ).ToList() ;
      var pickUpNumberStatus = radioButton!.Content.ToString() == OnText ? PickUpNumberOn : PickUpNumberOff ;
      foreach ( var fileType in fileTypes ) {
        string fileName = string.Empty ;
        switch ( fileType ) {
          case SummaryFileType :
            fileName = SummaryFileName ;
            break ;
          case ConfirmationFileType :
            fileName = ConfirmationFileName ;
            break ;
        }

        if ( string.IsNullOrEmpty( fileName ) ) continue ;
        _fileNames.Add( pickUpNumberStatus + fileName ) ;
      }
      
    }

    public void FileTypeChecked( object sender )
    {
      var checkbox = sender as CheckBox ;
      var pickUpNumberStatus = DoconTypes.First().TheValue ? PickUpNumberOn : PickUpNumberOff ;
      switch ( checkbox!.Content.ToString() ) {
        case SummaryFileType :
          if ( ! _fileNames.Contains( pickUpNumberStatus + SummaryFileName ) )
            _fileNames.Add( pickUpNumberStatus + SummaryFileName ) ;
          break ;
        case ConfirmationFileType :
          if ( ! _fileNames.Contains( pickUpNumberStatus + ConfirmationFileName ) )
            _fileNames.Add( pickUpNumberStatus + ConfirmationFileName ) ;
          break ;
      }
      
    }

    public void FileTypeUnchecked( object sender )
    {
      var checkbox = sender as CheckBox ;
      var pickUpNumberStatus = DoconTypes.First().TheValue ? PickUpNumberOn : PickUpNumberOff ;
      switch ( checkbox!.Content.ToString() ) {
        case SummaryFileType :
          if ( _fileNames.Contains( pickUpNumberStatus + SummaryFileName ) )
            _fileNames.Remove( pickUpNumberStatus + SummaryFileName ) ;
          break ;
        case ConfirmationFileType :
          if ( _fileNames.Contains( pickUpNumberStatus + ConfirmationFileName ) )
            _fileNames.Remove( pickUpNumberStatus + ConfirmationFileName ) ;
          break ;
      }
    }

    private string GetFileName(string fileName)
    {
      return string.IsNullOrEmpty( _fileName ) ? fileName : $"{_fileName}{fileName}" ;
    }

    private List<string> GetConstructionItemList()
    {
      var constructionItemList = new List<string>() ;
      foreach ( var pickUpModel in PickUpModels.Where( pickUpModel =>
                 ! constructionItemList.Contains( pickUpModel.ConstructionItems ) && pickUpModel.EquipmentType == PickUpViewModel.ProductType.Conduit.GetFieldName() ) ) {
        constructionItemList.Add( pickUpModel.ConstructionItems ) ;
      }

      return constructionItemList ;
    }

    private void CreateOutputFile()
    {
      GetPickModels() ;
      if ( ! PickUpModels.Any() ) return;
      try {
        var constructionItemList = GetConstructionItemList() ;
        if ( ! constructionItemList.Any() ) constructionItemList.Add( DefaultConstructionItem ) ;
        foreach ( var fileName in _fileNames ) {
          XSSFWorkbook workbook = new XSSFWorkbook() ;

          Dictionary<string, XSSFCellStyle> xssfCellStyles = new Dictionary<string, XSSFCellStyle>
          {
            { "borderedCellStyle", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Center ) },
            { "noneBorderedCellStyle", CreateCellStyle( workbook, BorderStyle.None, BorderStyle.None, BorderStyle.None, BorderStyle.None, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "bottomBorderedCellStyle", CreateCellStyle( workbook, BorderStyle.None, BorderStyle.None, BorderStyle.None, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "leftBottomBorderedCellStyle", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.None, BorderStyle.None, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "rightBottomBorderedCellStyle", CreateCellStyle( workbook, BorderStyle.None, BorderStyle.Thin, BorderStyle.None, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Right ) },
            { "leftAlignmentLeftRightBorderedCellStyle", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.None, BorderStyle.None, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "leftRightBorderedCellStyle", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.None, BorderStyle.None, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Right ) },
            { "exceptTopBorderedCellStyle", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.None, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "wrapTextBorderedCellStyle", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Left, false ) },
            { "borderedCellStyleMedium", CreateCellStyle( workbook, BorderStyle.Medium, BorderStyle.Medium, BorderStyle.Medium, BorderStyle.Medium, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Center ) },
            { "bottomBorderedCellStyleMedium", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.None, BorderStyle.Medium, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "topBorderedCellStyleMedium", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.Medium, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Center ) },
            { "leftBottomBorderedCellStyleMedium", CreateCellStyle( workbook, BorderStyle.Medium, BorderStyle.None, BorderStyle.None, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "rightBottomBorderedCellStyleMedium", CreateCellStyle( workbook, BorderStyle.None, BorderStyle.Medium, BorderStyle.None, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Center ) },
            { "leftAlignmentLeftRightBorderedCellStyleMedium", CreateCellStyle( workbook, BorderStyle.Medium, BorderStyle.Medium, BorderStyle.None, BorderStyle.None, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "leftRightBorderedCellStyleMedium", CreateCellStyle( workbook, BorderStyle.Medium, BorderStyle.Thin, BorderStyle.None, BorderStyle.None, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Center ) },
            { "exceptTopBorderedCellStyleMedium", CreateCellStyle( workbook, BorderStyle.Medium, BorderStyle.Medium, BorderStyle.None, BorderStyle.Medium, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "wrapTextBorderedCellStyleMedium", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Left, false ) },
            { "leftRightBottomBorderedCellStyleMedium", CreateCellStyle( workbook, BorderStyle.Medium, BorderStyle.Medium, BorderStyle.None, BorderStyle.Medium, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Right ) },
            { "leftRightBottomBorderedCellStyleMediumThin", CreateCellStyle( workbook, BorderStyle.Medium, BorderStyle.Medium, BorderStyle.None, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Right ) },
            { "leftRightTopBorderedCellStyleMedium", CreateCellStyle( workbook, BorderStyle.Medium, BorderStyle.Medium, BorderStyle.Medium, BorderStyle.None, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Center ) },
            { "topRightBorderedCellStyleMedium", CreateCellStyle( workbook, BorderStyle.Medium, BorderStyle.Medium, BorderStyle.Medium, BorderStyle.None, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Center ) },
            { "leftTopBorderedCellStyleMedium", CreateCellStyle( workbook, BorderStyle.Medium, BorderStyle.None, BorderStyle.Medium, BorderStyle.None, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Center ) },
            { "rightBorderedCellStyleMedium", CreateCellStyle( workbook, BorderStyle.None, BorderStyle.Medium, BorderStyle.None, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Center ) },
            { "rightBorderedCellStyleMediumDotted", CreateCellStyle( workbook, BorderStyle.None, BorderStyle.Medium, BorderStyle.None, BorderStyle.None, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Right ) },
            { "leftBorderedCellStyleMedium", CreateCellStyle( workbook, BorderStyle.Medium, BorderStyle.Thin, BorderStyle.None, BorderStyle.None, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "exceptTopBorderedCellStyleSummary", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.Dotted, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "exceptTopBorderedCellStyleSummaryMedium", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Medium, BorderStyle.Dotted, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "bottomCellStyleSummaryMedium", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.Dotted, BorderStyle.Medium, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "rightBottomCellStyleSummaryMedium", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Medium, BorderStyle.Dotted, BorderStyle.Medium, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "leftBottomBorderedCellStyleLastRow", CreateCellStyle( workbook, BorderStyle.Medium, BorderStyle.None, BorderStyle.None, BorderStyle.Medium, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "bottomBorderedCellStyleLastRow", CreateCellStyle( workbook, BorderStyle.None, BorderStyle.None, BorderStyle.None, BorderStyle.Medium, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Left ) },
            { "rightBottomBorderedCellStyleLastRow", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Medium, BorderStyle.None, BorderStyle.Medium, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Right ) }
          } ;
          var headerNoneBorderedCellStyle = CreateCellStyle( workbook, BorderStyle.None, BorderStyle.None, BorderStyle.None, BorderStyle.None, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Left ) ;
          XSSFFont myFont = (XSSFFont) workbook.CreateFont() ;
          myFont.FontHeightInPoints = 18 ;
          myFont.IsBold = true ;
          myFont.FontName = "ＭＳ ゴシック";
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

          var fileNameToOutPut = GetFileName(fileName) ;
          FileStream fs = new FileStream( PathName + @"\" + fileNameToOutPut, FileMode.OpenOrCreate ) ;
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
      var fileName = FileName ;
      ISheet sheet = workbook.CreateSheet( sheetName ) ;
      IRow row0, row2 ;
      int rowStart ;
      sheet.SetMargin(MarginType.BottomMargin ,0.078740157480315 );
      sheet.SetMargin(MarginType.TopMargin ,0.551181102362205);
      sheet.SetMargin(MarginType.LeftMargin ,0.275590551181102 );
      sheet.SetMargin(MarginType.RightMargin ,0.275590551181102 );
      sheet.SetMargin(MarginType.HeaderMargin ,0.196850393700787 );
      sheet.SetMargin(MarginType.FooterMargin ,0.196850393700787 );
      var printSetup = sheet.PrintSetup;
      printSetup.PaperSize = (short) 9 ;
      printSetup.FitWidth = 1; //fit width onto 1 page
      printSetup.FitHeight = 0; //don't care about height
      printSetup.Landscape = true;
      sheet.FitToPage = true;
      switch ( sheetType ) {
        case SheetType.Confirmation :
          sheet.SetColumnWidth( 0, GetWidth256Excel( 2.0F ) ) ;
          sheet.SetColumnWidth( 1, GetWidth256Excel( 27.86F )  ) ;
          sheet.SetColumnWidth( 2, GetWidth256Excel( 26F ) ) ;
          sheet.SetColumnWidth( 3, GetWidth256Excel( 9.86F ) ) ;
          sheet.SetColumnWidth( 4, GetWidth256Excel( 4.57F ) ) ;
          sheet.SetColumnWidth( 5, GetWidth256Excel( 8F )  ) ;
          sheet.SetColumnWidth( 7, GetWidth256Excel( 9.86F ) ) ;
          sheet.SetColumnWidth( 10, GetWidth256Excel( 9.5F ) ) ;
          sheet.SetColumnWidth( 13, GetWidth256Excel( 12.2F ) );
          sheet.SetColumnWidth( 15, GetWidth256Excel( 12.2F ) );
          sheet.SetColumnWidth( 16, GetWidth256Excel( 9.5F ) ) ;
          
          rowStart = 0 ;
          var view = _document.ActiveView ;
          var scale = view.Scale ;
          HeightSettingStorable settingStorables = _document.GetHeightSettingStorable() ;
          
          foreach ( var level in levels ) {
            if( PickUpModels.All( x => x.Floor != level ) ) continue;
            if( PickUpModels.Where( x => x.Floor == level ).All( p => p.ConstructionItems != sheetName ) ) continue;
            var height = settingStorables.HeightSettingsData.Values.FirstOrDefault( x => x.LevelName == level )?.Elevation ?? 0 ;
            row0 = sheet.CreateRow( rowStart ) ;
            row0.HeightInPoints = 13.5F;
            var row1 = sheet.CreateRow( rowStart + 1 ) ;
            row1.HeightInPoints = 22.5F;
            row2 = sheet.CreateRow( rowStart + 2 ) ;
            row2.HeightInPoints = 13.5F;
            CreateMergeCell( sheet, row0, rowStart, rowStart, 2, 6, fileName, xssfCellStyles[ "bottomBorderedCellStyle" ] ) ;
            CreateCell( row0, 13, $"縮尺:1/{scale}", xssfCellStyles[ "bottomBorderedCellStyle" ] ) ;
            CreateCell( row0, 14, "", xssfCellStyles[ "bottomBorderedCellStyle" ] ) ;
            CreateCell( row0, 15, $"階高:{Math.Round(height/1000,1)}ｍ", xssfCellStyles[ "bottomBorderedCellStyle" ] ) ;
            CreateCell( row0, 16, "", xssfCellStyles[ "bottomBorderedCellStyle" ] ) ;

            CreateCell( row1, 1, "【入力確認表】", xssfCellStyles[ "headerNoneBorderedCellStyle" ] ) ;
            CreateCell( row1, 2, "工事項目:", xssfCellStyles[ "noneBorderedCellStyle" ] ) ;
            CreateMergeCell( sheet, row1, rowStart + 1, rowStart + 1, 3, 6, sheetName, xssfCellStyles[ "noneBorderedCellStyle" ] ) ;
            CreateCell( row1, 7, "図面番号:", xssfCellStyles[ "noneBorderedCellStyle" ] ) ;
            CreateMergeCell( sheet, row1, rowStart + 1, rowStart + 1, 8, 9, "", xssfCellStyles[ "noneBorderedCellStyle" ] ) ;
            CreateCell( row1, 10, "階数:", xssfCellStyles[ "noneBorderedCellStyle" ] ) ;
            CreateCell( row1, 11, level, xssfCellStyles[ "noneBorderedCellStyle" ] ) ;
            CreateCell( row1, 12, "区間:", xssfCellStyles[ "noneBorderedCellStyle" ] ) ;
            CreateMergeCell( sheet, row1, rowStart + 1, rowStart + 1, 13, 16, "", xssfCellStyles[ "noneBorderedCellStyle" ] ) ;

            var space = "      " ;
            CreateCell( row2, 1, $"品{space}名", xssfCellStyles[ "borderedCellStyleMedium" ] ) ;
            CreateMergeCell( sheet, row2, rowStart + 2, rowStart + 2, 2, 3, $"規{space}格", xssfCellStyles[ "borderedCellStyleMedium" ], true ) ;
            CreateCell( row2, 4, "単位", xssfCellStyles[ "borderedCellStyleMedium" ] ) ;
            var nextSpace = "                " ;
            CreateMergeCell( sheet, row2, rowStart + 2, rowStart + 2, 5, 15, $"軌{nextSpace}跡", xssfCellStyles[ "borderedCellStyleMedium" ], true ) ;
            CreateCell( row2, 16, "合計数量", xssfCellStyles[ "borderedCellStyleMedium" ] ) ;

            rowStart += 3 ;
            List<KeyValuePair<string, List<PickUpModel>>> dictionaryDataPickUpModel = new List<KeyValuePair<string, List<PickUpModel>>>() ;
            
            foreach ( var code in codeList ) {
              var dataPickUpModels = PickUpModels
                .Where( p => p.ConstructionItems == sheetName && p.Specification2 == code && p.Floor == level )
                .GroupBy( x => x.ProductCode, ( key, p ) => new { ProductCode = key, PickUpModels = p.ToList() } ) ;
            
              foreach ( var dataPickUpModel in dataPickUpModels ) {
                if ( dictionaryDataPickUpModel.Any( l => l.Key == dataPickUpModel.ProductCode ) && ! IsTani( dataPickUpModel.PickUpModels.First() ) ) {
                  var dataPickUpModelExist = dictionaryDataPickUpModel.Single( x => x.Key == dataPickUpModel.ProductCode ) ;
                  dataPickUpModelExist.Value.AddRange( dataPickUpModel.PickUpModels );
                }
                else {
                  dictionaryDataPickUpModel.Add( new KeyValuePair<string, List<PickUpModel>>(dataPickUpModel.ProductCode, dataPickUpModel.PickUpModels) );
                }
              }
            }

            var dictionaryDataPickUpModelOrder = dictionaryDataPickUpModel.OrderBy( x => x.Value.First().Tani == "m" ? 1 : 2).ThenBy( c => c.Value.First().ProductName ).ThenBy( c => c.Value.First().Standard ) ; ;
            var maxNumberPickUp = FindMaxPickUpNumber( dictionaryDataPickUpModel.SelectMany(x=>x.Value).ToList()) ;
            foreach ( var dataPickUpModel in dictionaryDataPickUpModelOrder ) {
              rowStart = AddConfirmationPickUpRow( dataPickUpModel.Value, sheet, rowStart, xssfCellStyles, maxNumberPickUp ) ;
            }
            
            while ( rowStart % 61 != 0 ) {
              var rowTemp = sheet.CreateRow( rowStart ) ;
              rowTemp.HeightInPoints = 13.5F;
              CreateCell( rowTemp, 1, "", xssfCellStyles[ "leftBottomBorderedCellStyleMedium" ] ) ;
              CreateCell( rowTemp, 2, "", xssfCellStyles[ "leftBottomBorderedCellStyleMedium" ] ) ;
              CreateCell( rowTemp, 3, "", xssfCellStyles[ "rightBottomBorderedCellStyleMedium" ] ) ;
              CreateCell( rowTemp, 4, "", xssfCellStyles[ "rightBottomBorderedCellStyleMedium" ] ) ;
              CreateMergeCell( sheet, rowTemp, rowStart, rowStart, 5, 15, "", xssfCellStyles[ "leftBottomBorderedCellStyleMedium" ] ) ;
              CreateCell( rowTemp, 16,  "", xssfCellStyles[ "leftRightBottomBorderedCellStyleMediumThin" ] ) ;
             
              rowStart++ ;
            }

            var lastRow = sheet.CreateRow( rowStart ) ;
            CreateCell( lastRow, 1, "", xssfCellStyles[ "leftRightBottomBorderedCellStyleMedium" ] ) ;
            CreateMergeCell( sheet, lastRow, rowStart, rowStart, 2, 3, "", xssfCellStyles[ "leftRightBottomBorderedCellStyleMedium" ], true ) ;
            CreateCell( lastRow, 4, "", xssfCellStyles[ "leftRightBottomBorderedCellStyleMedium" ] ) ;
            CreateMergeCell( sheet, lastRow, rowStart, rowStart, 5, 15, "", xssfCellStyles[ "leftRightBottomBorderedCellStyleMedium" ], true ) ;
            CreateCell( lastRow, 16, "", xssfCellStyles[ "leftRightBottomBorderedCellStyleMedium" ] ) ;

            sheet.SetRowBreak( rowStart );
            rowStart += 1 ;
          }

          break ;
        case SheetType.Summary :
          sheet.SetColumnWidth( 0, 500 ) ;
          sheet.SetColumnWidth( 1, 500 ) ;
          sheet.SetColumnWidth( 2, 8000 ) ;
          sheet.SetColumnWidth( 4, 1300 ) ;
          row0 = sheet.CreateRow( 0 ) ;
          row2 = sheet.CreateRow( 2 ) ;
          var row1S = sheet.CreateRow( 1 ) ;
          row1S.HeightInPoints = 8.25F ;
          var row3 = sheet.CreateRow( 3 ) ;
          row3.HeightInPoints = 19.5F ;
          CreateCell( row0, 2, "【拾い出し集計表】", xssfCellStyles[ "headerNoneBorderedCellStyle" ] ) ;
          CreateMergeCell( sheet, row0, 0, 0, 6, 7, fileName, xssfCellStyles[ "bottomBorderedCellStyle" ] ) ;
          for ( var i = 7 ; i < 19 ; i++ ) {
            CreateCell( row0, i, "", xssfCellStyles[ "bottomBorderedCellStyle" ] ) ;
          }

          CreateCell( row0, 14, sheetName, xssfCellStyles[ "bottomBorderedCellStyle" ] ) ;

          CreateMergeCell( sheet, row2, 2, 2, 1, 3, "", CreateCellStyle( workbook, BorderStyle.Medium, BorderStyle.Thin, BorderStyle.Medium, BorderStyle.None, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Center ), true ) ;
          CreateCell( row2, 4, "", CreateCellStyle( workbook, BorderStyle.None, BorderStyle.Medium, BorderStyle.Medium, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Center ) ) ;
          Dictionary<int, string> levelColumns = new Dictionary<int, string>() ;
          var index = 5 ;
          foreach ( var level in levels ) {
            if(PickUpModels.All( x => x.Floor != level )) continue;
            CreateCell( row2, index, level, xssfCellStyles[ "topBorderedCellStyleMedium" ] ) ;
            levelColumns.Add( index, level ) ;
            CreateCell( row3, index, "", xssfCellStyles[ "bottomBorderedCellStyleMedium" ] ) ;
            index++ ;
          }

          if ( index < 19 ) {
            for ( int i = index + 1 ; i < 19 ; i++ ) {
              if ( i == 18 ) {
                CreateCell( row2, i, "", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Medium, BorderStyle.Medium, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Center ) ) ;
                CreateCell( row3, i, "", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Medium, BorderStyle.Thin, BorderStyle.Medium, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Center ) ) ;
              }
              else {
                CreateCell( row2, i, "", xssfCellStyles[ "topBorderedCellStyleMedium" ] ) ;
                CreateCell( row3, i, "", xssfCellStyles[ "bottomBorderedCellStyleMedium" ] ) ;
              }
            }
          }

          if ( index == 18 ) {
            CreateCell( row2, index, "合計", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Medium, BorderStyle.Medium, BorderStyle.Thin, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Center ) ) ;
          }
          else {
            CreateCell( row2, index, "合計", xssfCellStyles[ "topBorderedCellStyleMedium" ] ) ;
          }
          var spaceS = "  " ;
          CreateMergeCell( sheet, row3, 3, 3, 1, 3, $"品{spaceS}名 / 規{spaceS}格", CreateCellStyle( workbook, BorderStyle.Medium, BorderStyle.Thin, BorderStyle.Thin, BorderStyle.Medium, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Center ), true ) ;
          CreateCell( row3, 4, "単位", CreateCellStyle( workbook, BorderStyle.Thin, BorderStyle.Medium, BorderStyle.None, BorderStyle.Medium, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Center ) ) ;
          if ( index == 18 ) {
            CreateCell( row3, index, "", CreateCellStyle( workbook, BorderStyle.None, BorderStyle.Medium, BorderStyle.None, BorderStyle.Medium, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Center ) ) ;
          }
          else {
            CreateCell( row3, index, "", CreateCellStyle( workbook, BorderStyle.None, BorderStyle.Thin, BorderStyle.None, BorderStyle.Medium, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.HorizontalAlignment.Center ) ) ;
          }


          rowStart = 4 ;
          List<KeyValuePair<string, List<PickUpModel>>> dictionaryDataPickUpModelSummary = new List<KeyValuePair<string, List<PickUpModel>>>() ;
          foreach ( var code in codeList ) {
            var dataPickUpModels = PickUpModels
              .Where( p => p.ConstructionItems == sheetName && p.Specification2 == code )
              .GroupBy( x => x.ProductCode, ( key, p ) => new { ProductCode = key, PickUpModels = p.ToList() } ) ;
            foreach ( var dataPickUpModel in dataPickUpModels ) {
              if ( dictionaryDataPickUpModelSummary.Any( l => l.Key == dataPickUpModel.ProductCode ) && ! IsTani( dataPickUpModel.PickUpModels.First() ) ) {
                var dataPickUpModelExist = dictionaryDataPickUpModelSummary.Single( x => x.Key == dataPickUpModel.ProductCode ) ;
                dataPickUpModelExist.Value.AddRange( dataPickUpModel.PickUpModels );
              }
              else {
                dictionaryDataPickUpModelSummary.Add( new KeyValuePair<string, List<PickUpModel>>(dataPickUpModel.ProductCode, dataPickUpModel.PickUpModels) );
              }
            }
          }
          
          var dictionaryDataPickUpModelOrderSummary = dictionaryDataPickUpModelSummary.OrderBy( x => x.Value.First().Tani == "m" ? 1 : 2).ThenBy( c => c.Value.First().ProductName ).ThenBy( c => c.Value.First().Standard ).ToList() ;
          foreach ( var dataPickUpModel in dictionaryDataPickUpModelOrderSummary ) {
            if ( rowStart + 2 == (dictionaryDataPickUpModelOrderSummary.Count * 2 + 4)) {
              rowStart = AddSummaryPickUpRow( dataPickUpModel.Value, sheet, rowStart, levelColumns, index, xssfCellStyles, true ) ;
            }
            else {
              rowStart = AddSummaryPickUpRow( dataPickUpModel.Value, sheet, rowStart, levelColumns, index, xssfCellStyles ) ;
            }
          }
          
          break ;
      }
    }

    private List<string> GetCodeList()
    {
      var codeList = new List<string>() ;
      foreach ( var pickUpModel in PickUpModels.Where( pickUpModel => ! codeList.Contains( pickUpModel.Specification2 ) ) ) {
        codeList.Add( pickUpModel.Specification2 ) ;
      }

      return codeList ;
    }

    private int AddSummaryPickUpRow( 
      List<PickUpModel> pickUpModels, 
      ISheet sheet, 
      int rowStart, 
      IReadOnlyDictionary<int, string> levelColumns, 
      int index,
      IReadOnlyDictionary<string, XSSFCellStyle> xssfCellStyles,
      bool isLastRow = false
      )
    {
      if ( ! pickUpModels.Any() ) return rowStart ;
      var pickUpModel = pickUpModels.First() ;
      var rowName = sheet.CreateRow( rowStart ) ;
      rowName.HeightInPoints = 13.5F ;
      var isTani = IsTani( pickUpModel ) ;
      CreateMergeCell( sheet, rowName, rowStart, rowStart, 1, 3, pickUpModel.ProductName, xssfCellStyles[ "leftBorderedCellStyleMedium" ], true ) ;
      CreateMergeCell( sheet, rowName, rowStart, rowStart + 1, 4, 4, isTani ? "ｍ" : pickUpModel.Tani, xssfCellStyles[ "rightBorderedCellStyleMedium" ], true ) ;

      rowStart++ ;
      var rowStandard = sheet.CreateRow( rowStart ) ;
      rowStandard.HeightInPoints = 13.5F ;
      if ( isLastRow ) {
        CreateCell( rowStandard, 1, "", xssfCellStyles[ "leftBottomBorderedCellStyleLastRow" ] ) ;
        CreateCell( rowStandard, 2, pickUpModel.Standard, xssfCellStyles[ "bottomBorderedCellStyleLastRow" ] ) ;
        CreateCell( rowStandard, 3, "", xssfCellStyles[ "bottomBorderedCellStyleLastRow" ] ) ;
        CreateCell( rowStandard, 4, "", xssfCellStyles[ "rightBottomBorderedCellStyleLastRow" ] ) ;
      }
      else {
        CreateCell( rowStandard, 1, "", xssfCellStyles[ "leftBottomBorderedCellStyleMedium" ] ) ;
        CreateCell( rowStandard, 2, pickUpModel.Standard, xssfCellStyles[ "bottomBorderedCellStyle" ] ) ;
        CreateCell( rowStandard, 3, "", xssfCellStyles[ "rightBottomBorderedCellStyle" ] ) ;
        CreateCell( rowStandard, 4, "", xssfCellStyles[ "rightBottomBorderedCellStyleMedium" ] ) ;
      }
      
      double total = 0 ;
      for ( var i = 5 ; i < index ; i++ ) {
        double quantityFloor = 0 ;
        var level = levelColumns[ i ] ;
        quantityFloor = CalculateTotalByFloor( pickUpModels.Where( item => item.Floor == level ).ToList() ) ;
        CreateCell( rowName, i, quantityFloor == 0 ? string.Empty : Math.Round( quantityFloor, isTani ? 1 : 2 ).ToString(), xssfCellStyles[ "leftRightBorderedCellStyle" ] ) ;
        CreateCell( rowStandard, i, "", isLastRow ? xssfCellStyles[ "bottomCellStyleSummaryMedium" ] : xssfCellStyles[ "exceptTopBorderedCellStyleSummary" ] ) ;
        
        total += quantityFloor ;
      }

      if ( index != 18 ) 
      {
        CreateCell( rowName, index, total == 0 ? string.Empty : Math.Round( total, isTani ? 1 : 2 ).ToString(), xssfCellStyles[ "leftRightBorderedCellStyle" ] ) ;
        CreateCell( rowStandard, index, "", isLastRow ? xssfCellStyles[ "bottomCellStyleSummaryMedium" ] : xssfCellStyles[ "exceptTopBorderedCellStyleSummary" ] ) ;
      }
      else {
        CreateCell( rowName, index, total == 0 ? string.Empty : Math.Round( total, isTani ? 1 : 2 ).ToString(), xssfCellStyles[ "rightBorderedCellStyleMediumDotted" ] ) ;
        CreateCell( rowStandard, index, "", isLastRow ? xssfCellStyles[ "rightBottomCellStyleSummaryMedium" ] : xssfCellStyles[ "exceptTopBorderedCellStyleSummaryMedium" ] ) ;
      }
    

      if ( index < 19 ) {
        for ( int i = index + 1 ; i < 19 ; i++ ) {
          if ( i == 18 ) {
            CreateCell( rowName, i, "", xssfCellStyles[ "rightBorderedCellStyleMediumDotted" ] ) ;
            CreateCell( rowStandard, i, "", isLastRow ? xssfCellStyles[ "rightBottomCellStyleSummaryMedium" ] : xssfCellStyles[ "exceptTopBorderedCellStyleSummaryMedium" ] ) ;
          }
          else {
            CreateCell( rowName, i,  "" , xssfCellStyles[ "leftRightBorderedCellStyle" ] ) ;
            CreateCell( rowStandard, i, "", isLastRow ? xssfCellStyles[ "bottomCellStyleSummaryMedium" ] : xssfCellStyles[ "exceptTopBorderedCellStyleSummary" ] ) ;
          }
        }
      }
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
    private int AddConfirmationPickUpRow( List<PickUpModel> pickUpModels, ISheet sheet, int rowStart, IReadOnlyDictionary<string, XSSFCellStyle> xssfCellStyles, int maxPickUpNumber )
    {
      if ( ! pickUpModels.Any() ) return rowStart ;
      var pickUpNumbers = GetPickUpNumbersList( pickUpModels ) ;
      var pickUpModel = pickUpModels.First() ;
      var row = sheet.CreateRow( rowStart ) ;
      row.HeightInPoints = 13.5F ;
      var isTani = IsTani( pickUpModel ) ;
      double total = 0 ;
      Dictionary<string, int> trajectory = new Dictionary<string, int>() ;
      var routes = RouteCache.Get( DocumentKey.Get( _document ) ) ;
      var inforDisplays = GetInforDisplays( pickUpModels, routes ) ;
      var isMoreTwoWireBook = IsMoreTwoWireBook( pickUpModels ) ;
      CreateCell( row, 1, pickUpModel.ProductName, xssfCellStyles[ "leftBottomBorderedCellStyleMedium" ] ) ;
      CreateCell( row, 2, pickUpModel.Standard, xssfCellStyles[ "leftBottomBorderedCellStyleMedium" ] ) ;
      CreateCell( row, 3, "", xssfCellStyles[ "rightBottomBorderedCellStyleMedium" ] ) ;
      CreateCell( row, 4, isTani ? "ｍ" : pickUpModel.Tani, xssfCellStyles[ "rightBottomBorderedCellStyleMedium" ] ) ;
      foreach ( var pickUpNumber in pickUpNumbers ) {
        string stringNotTani = string.Empty ;
        Dictionary<string, double> notSeenQuantities = new Dictionary<string, double>() ;
        Dictionary<string, double> notSeenQuantitiesPullBox = new Dictionary<string, double>() ;
        var items = pickUpModels.Where( p => p.PickUpNumber == pickUpNumber ).ToList() ;
        var itemFirst = items.First() ;
        var wireBook = ( string.IsNullOrEmpty( itemFirst.WireBook ) || itemFirst.WireBook == "1" ) ? string.Empty : itemFirst.WireBook ;
        var itemsGroupByRoute = items.Where( item => ! string.IsNullOrEmpty( item.Quantity ) ).GroupBy( i => i.RouteNameRef ) ;
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

          foreach ( var item in itemGroupByRoute ) {
            double.TryParse( item.Quantity, out var quantity ) ;
            if ( ! string.IsNullOrEmpty( item.Direction ) ) {
              if ( isSegmentFromPowerToPullBox ) {
                if ( ! notSeenQuantitiesPullBox.Keys.Contains( item.Direction ) ) {
                  notSeenQuantitiesPullBox.Add( item.Direction, 0 ) ;
                }

                notSeenQuantitiesPullBox[ item.Direction ] += quantity ;
              }
              else {
                if ( ! notSeenQuantities.Keys.Contains( item.Direction ) ) {
                  notSeenQuantities.Add( item.Direction, 0 ) ;
                }

                notSeenQuantities[ item.Direction ] += quantity ;
              }
            }
            else {
              if ( ! isTani ) stringNotTani += string.IsNullOrEmpty( stringNotTani ) ? item.SumQuantity : $"＋{item.SumQuantity}" ;
              seenQuantity += quantity ;
            }

            totalBasedOnCreateTable += quantity ;
          }
          
          if ( seenQuantity > 0 ) {
            if ( isSegmentConnectedToPullBox ) {
              var countStr = string.Empty ;
              var inforDisplay = inforDisplays.SingleOrDefault( x => x.RouteNameRef == itemGroupByRoute.Key ) ;
              if ( inforDisplay != null && inforDisplay.IsDisplay) {
                countStr = ( inforDisplay.NumberDisplay == 1 ? string.Empty : $"×{inforDisplay.NumberDisplay}" ) + ( isMoreTwoWireBook ? $"×N" : string.IsNullOrEmpty(valueDetailTableStr) ? string.Empty: $"×{valueDetailTableStr}" ) ;
                inforDisplay.IsDisplay = false ;
                if ( isSegmentFromPowerToPullBox ) {
                  listSeenQuantityPullBox.Add( $"({Math.Round( seenQuantity, isTani ? 1 : 2 ).ToString( CultureInfo.InvariantCulture )}＋↓{Math.Round( notSeenQuantitiesPullBox.First().Value, isTani ? 1 : 2 )})" + countStr);
                }
                else {
                  listSeenQuantityPullBox.Add( Math.Round( seenQuantity, isTani ? 1 : 2 ).ToString( CultureInfo.InvariantCulture ) + countStr);
                }
              }
            }
            else {
              listSeenQuantity.Add( Math.Round( seenQuantity, isTani ? 1 : 2 ) ) ;
            }
          }
        }

        total += ! string.IsNullOrEmpty( wireBook ) ? Math.Round( totalBasedOnCreateTable, isTani ? 1 : 2 ) * int.Parse( wireBook ) : Math.Round( totalBasedOnCreateTable, isTani ? 1 : 2 ) ;

        var number = DoconTypes.First().TheValue && ! string.IsNullOrEmpty( pickUpNumber ) ? "[" + pickUpNumber + "]" : string.Empty ;
        var numberPullBox = DoconTypes.First().TheValue ? $"[{(maxPickUpNumber + 1)}]" : string.Empty;
        var seenQuantityStr = isTani ? string.Join( "＋", listSeenQuantity ) : string.Join("＋",stringNotTani.Split( '+' )) ;

        var seenQuantityPullBoxStr = string.Empty ;
        if ( listSeenQuantityPullBox.Any() ) {
          seenQuantityPullBoxStr = numberPullBox + string.Join($"＋{numberPullBox}", listSeenQuantityPullBox ) ;
        }
        
        var notSeenQuantityStr = string.Empty ;
        foreach ( var (_, value) in notSeenQuantities ) {
          notSeenQuantityStr += value > 0 ? "＋↓" + Math.Round( value, isTani ? 1 : 2 ) : string.Empty ;
        }

        var key = isTani ? ( "(" + seenQuantityStr + notSeenQuantityStr + ")" ) + (string.IsNullOrEmpty( valueDetailTableStr ) ? string.Empty : $"×{valueDetailTableStr}") + (string.IsNullOrEmpty( seenQuantityPullBoxStr ) ? string.Empty : $"＋{seenQuantityPullBoxStr}" )  : ( seenQuantityStr + notSeenQuantityStr ) ;
        var itemKey = trajectory.FirstOrDefault( t => t.Key.Contains( key ) ).Key ;

        if ( string.IsNullOrEmpty( itemKey ) )
          trajectory.Add( number + key, 1 ) ;
        else {
          trajectory[ itemKey ]++ ;
        }
      }
      
      List<string> trajectoryStr = ( from item in trajectory select item.Value == 1 ? item.Key : item.Key + "×" + item.Value ).ToList() ;
      int firstCellIndex = 5 ;
      int lastCellIndex = 15 ;
      int lengthOfCellMerge = GetWidthOfCellMerge( sheet, 5, 15 ) ;
      
      var valueOfCell = string.Empty ;
      var trajectoryStrCount = trajectoryStr.Count ;
      var count = 0 ;
      if ( trajectoryStrCount > 1 ) {
        for ( var i = 0 ; i < trajectoryStrCount ; i++ ) {
          valueOfCell += trajectoryStr[ i ] + (i == trajectoryStrCount - 1 ? "": "＋");
          if ( valueOfCell.Length * 2.5  < lengthOfCellMerge/256.0 && i < trajectoryStrCount - 1 ) continue;
          if ( count == 0 ) {
            CreateMergeCell( sheet, row, rowStart, rowStart, firstCellIndex, lastCellIndex, valueOfCell , xssfCellStyles[ "leftBottomBorderedCellStyleMedium" ] ) ;
            count++ ;
          }
          else {
            var rowTrajectory = sheet.CreateRow( ++rowStart ) ;
            rowTrajectory.HeightInPoints = 13.5F;
            CreateCell( rowTrajectory, 1, "", xssfCellStyles[ "leftBottomBorderedCellStyleMedium" ] ) ;
            CreateCell( rowTrajectory, 2, "", xssfCellStyles[ "leftBottomBorderedCellStyleMedium" ] ) ;
            CreateCell( rowTrajectory, 3, "", xssfCellStyles[ "rightBottomBorderedCellStyleMedium" ] ) ;
            CreateCell( rowTrajectory, 4, "", xssfCellStyles[ "rightBottomBorderedCellStyleMedium" ] ) ;
            CreateCell( rowTrajectory, 16, "", xssfCellStyles[ "leftRightBottomBorderedCellStyleMediumThin" ] ) ;
            CreateMergeCell( sheet, rowTrajectory, rowStart, rowStart, firstCellIndex, lastCellIndex, valueOfCell , xssfCellStyles[ "leftBottomBorderedCellStyleMedium" ] ) ;
          }

          valueOfCell = string.Empty ;
        }
        CreateCell( row, 16, Math.Round( total, isTani ? 1 : 2 ).ToString(), xssfCellStyles[ "leftRightBottomBorderedCellStyleMediumThin" ] ) ;
      }
      else {
        CreateMergeCell( sheet, row, rowStart, rowStart, firstCellIndex, lastCellIndex, string.Join( "＋", trajectoryStr ), xssfCellStyles[ "wrapTextBorderedCellStyle" ] ) ;
        CreateCell( row, 16, Math.Round( total, isTani ? 1 : 2 ).ToString(), xssfCellStyles[ "leftRightBottomBorderedCellStyleMediumThin" ] ) ;
      }
      
      rowStart++ ;
      return rowStart ;
    }

    private int GetWidthOfCellMerge( ISheet sheet, int firstCellIndex, int lastCellIndex )
    {
      int result = 0 ;
      for ( int i = firstCellIndex ; i <= lastCellIndex ; i++ ) {
        result += sheet.GetColumnWidth( i ) ;
      }

      return result ;
    }

    private void CreateCell( IRow currentRow, int cellIndex, string value, ICellStyle style )
    {
      ICell cell = currentRow.CreateCell( cellIndex ) ;
      style.DataFormat = (short)49 ; // format type is text
      cell.CellStyle = style ;
      cell.SetCellValue( value ) ;
    }

    private void CreateMergeCell( ISheet sheet, IRow currentRow, int firstRowIndex, int lastRowIndex, int firstCellIndex, int lastCellIndex, string value, ICellStyle style, bool isMediumBorder = false )
    {
      ICell cell = currentRow.CreateCell( firstCellIndex ) ;
      CellRangeAddress cellMerge = new CellRangeAddress( firstRowIndex, lastRowIndex, firstCellIndex, lastCellIndex ) ;
      sheet.AddMergedRegion( cellMerge ) ;
      style.DataFormat = (short)49 ; // format type is text
      cell.CellStyle = style ;
      cell.SetCellValue( value ) ;
      RegionUtil.SetBorderTop( style.BorderTop == BorderStyle.None ? 0 : style.BorderTop == BorderStyle.Thin ?  1 : isMediumBorder ? 2 : 1, cellMerge, sheet ) ;
      RegionUtil.SetBorderBottom( style.BorderBottom == BorderStyle.None ? 0 : style.BorderBottom == BorderStyle.Thin ?  1 : isMediumBorder ? 2 : 1, cellMerge, sheet ) ;
      RegionUtil.SetBorderLeft( style.BorderLeft == BorderStyle.None ? 0 : style.BorderLeft == BorderStyle.Thin ?  1 : isMediumBorder ? 2 : 1, cellMerge, sheet ) ;
      RegionUtil.SetBorderRight( style.BorderRight == BorderStyle.None ? 0 : style.BorderRight == BorderStyle.Thin ?  1 : isMediumBorder ? 2 : 1, cellMerge, sheet ) ;
    }

    private XSSFCellStyle CreateCellStyle( 
      IWorkbook workbook,
      BorderStyle leftBorderStyle,
      BorderStyle rightBorderStyle, 
      BorderStyle topBorderStyle, 
      BorderStyle bottomBorderStyle, 
      NPOI.SS.UserModel.VerticalAlignment verticalAlignment, 
      NPOI.SS.UserModel.HorizontalAlignment horizontalAlignment,
      bool wrapText = false )
    {
      XSSFCellStyle borderedCellStyle = (XSSFCellStyle) workbook.CreateCellStyle() ;
      borderedCellStyle.BorderLeft = leftBorderStyle ;
      borderedCellStyle.BorderTop = topBorderStyle ;
      borderedCellStyle.BorderRight = rightBorderStyle ;
      borderedCellStyle.BorderBottom = bottomBorderStyle ;
      borderedCellStyle.VerticalAlignment = verticalAlignment ;
      borderedCellStyle.Alignment = horizontalAlignment ;
      borderedCellStyle.WrapText = wrapText ;
      
      XSSFFont myFont = (XSSFFont) workbook.CreateFont() ;
      myFont.FontName = "ＭＳ 明朝";
      borderedCellStyle.SetFont( myFont );
      return borderedCellStyle ;
    }
    
    private void OutputItemsSelectionSetting()
    {
      var settingOutputPickUpReport = new SettingOutputPickUpReport( this ) ;
      settingOutputPickUpReport.ShowDialog();

      if ( settingOutputPickUpReport.DialogResult == false ) {
        CurrentSettingList = new ObservableCollection<ListBoxItem>( PreviousSettingList.Select(x => x.Copy()).ToList() ) ;
      }
      else {
        PreviousSettingList = new ObservableCollection<ListBoxItem>( CurrentSettingList.Select(x => x.Copy()).ToList() ) ;
      }
    }
    
    private void SetOption( Window window )
    {
      window.DialogResult = true ;
      window.Close() ;
    }

    private void GetPickModels()
    {
      if ( ! IsOutputItemsEnable )
        InitPickUpModels() ;
      else
        UpdatePickModels() ;
    }

    private void UpdatePickModels()
    {
      var settings = CurrentSettingList.Where( s => s.TheValue ).Select( s => s.TheText ) ;

      var newPickUpModels = PickUpModels
        .Where(p=> 
          _hiroiMasterModels.Any( h => 
            (int.Parse( h.Buzaicd ) ==  int.Parse( p.ProductCode.Split( '-' ).First())) 
            && (settings.Contains( h.Syurui )) )) ;

      PickUpModels = new ObservableCollection<PickUpModel>( newPickUpModels ) ;
    }

    private bool IsTani( PickUpModel pickUpModel )
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

    private int FindMaxPickUpNumber(List<PickUpModel> pickUpModels)
    {
      int result = int.MinValue ;
      foreach ( var pickUpModel in pickUpModels.Where( x=>! string.IsNullOrEmpty( x.PickUpNumber ) ) ) {
        int pickUpNumber = int.Parse( pickUpModel.PickUpNumber );
        if ( pickUpNumber > result ) result = pickUpNumber ;
      }

      return result ;
    }

    private List<InforDisplay> GetInforDisplays(List<PickUpModel> pickUpModels, RouteCache routes)
    {
      var routesNameRef = pickUpModels.Select( x => x.RouteNameRef ).Distinct() ;
      var inforDisplays = new List<InforDisplay>() ;
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
            inforDisplays.Add( new InforDisplay(true, idPullBox, 1, routeNameRef) );
          }
        }
      }

      return inforDisplays ;
    }

    private int GetWidth256Excel( float widthExcel )
    {
      return (int)Math.Round((widthExcel * DefaultCharacterWidth + 5) / DefaultCharacterWidth * 256);
    }
    
    private bool IsMoreTwoWireBook( List<PickUpModel> pickUpModels )
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

    private double CalculateTotalByFloor(List<PickUpModel> pickUpModels)
    {
      double result = 0 ;
      if ( pickUpModels.Count < 1 ) return result ;
      if ( IsTani( pickUpModels.First() ) ) {
        var pickUpModelsByNumber = PickUpModelByNumber( PickUpViewModel.ProductType.Conduit, pickUpModels ) ;
        foreach ( var pickUpModelByNumber in pickUpModelsByNumber ) {
          var wireBook = pickUpModelByNumber.WireBook ;
          if ( ! string.IsNullOrEmpty( wireBook ) ) {
            result += ( double.Parse( pickUpModelByNumber.Quantity ) * int.Parse( wireBook ) ) ;
          }
          else {
            result += double.Parse( pickUpModelByNumber.Quantity ) ;
          }
        }
      }
      else {
        foreach ( var item in pickUpModels ) {
          double.TryParse( item.Quantity, out var quantity ) ;
          result += quantity ;
        }
      }
     
      return result ;
    }
    
    private List<PickUpModel> PickUpModelByNumber( PickUpViewModel.ProductType productType, List<PickUpModel> pickUpModels )
    {
      List<PickUpModel> result = new() ;
      
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
    
    private List<PickUpModel> PickUpModelByProductCode( List<PickUpModel> pickUpModels )
    {
      List<PickUpModel> pickUpModelByProductCodes = new() ;
      
      var pickUpModelsByProductCode = pickUpModels.GroupBy( x => x.ProductCode.Split( '-' ).First() )
        .Select( g => g.ToList() ) ;
        
      foreach ( var pickUpModelByProductCode in pickUpModelsByProductCode ) {
        var pickUpModelsByConstructionItemsAndConstruction = pickUpModelByProductCode.GroupBy( x => ( x.ConstructionItems, x.Construction ) )
          .Select( g => g.ToList() ) ;
          
        foreach ( var pickUpModelByConstructionItemsAndConstruction in pickUpModelsByConstructionItemsAndConstruction ) {
          var sumQuantity = Math.Round(pickUpModelByConstructionItemsAndConstruction.Sum( p => Convert.ToDouble( p.Quantity ) ), 1) ;
            
          var pickUpModel = pickUpModelByConstructionItemsAndConstruction.FirstOrDefault() ;
          if ( pickUpModel == null ) 
            continue ;
            
          PickUpModel newPickUpModel = new(pickUpModel.Item, pickUpModel.Floor, pickUpModel.ConstructionItems, pickUpModel.EquipmentType, pickUpModel.ProductName, pickUpModel.Use, pickUpModel.UsageName, pickUpModel.Construction, pickUpModel.ModelNumber, pickUpModel.Specification, pickUpModel.Specification2, pickUpModel.Size, $"{sumQuantity}", pickUpModel.Tani, pickUpModel.Supplement, pickUpModel.Supplement2, pickUpModel.Group, pickUpModel.Layer, pickUpModel.Classification, pickUpModel.Standard, pickUpModel.PickUpNumber, pickUpModel.Direction, pickUpModel.ProductCode, pickUpModel.CeedSetCode, pickUpModel.DeviceSymbol, pickUpModel.Condition, pickUpModel.SumQuantity, pickUpModel.RouteName, null, pickUpModel.WireBook ) ;
          
          pickUpModelByProductCodes.Add( newPickUpModel ) ;
        }
      }

      return pickUpModelByProductCodes ;
    }
    
    public class ListBoxItem
    {
      public string? TheText { get ; set ; }
      public bool TheValue { get ; set ; }
    }

    private class InforDisplay
    {
      public bool IsDisplay { get ; set ; }
      public string? IdPullBox { get ; set ; }
      public int NumberDisplay { get ; set ; }
      public string? RouteNameRef { get ; set ; }

      public InforDisplay( bool isDisplay, string? idPullBox, int numberDisplay, string? routeNameRef )
      {
        IsDisplay = isDisplay ;
        IdPullBox = idPullBox??string.Empty ;
        NumberDisplay = numberDisplay ;
        RouteNameRef = routeNameRef??string.Empty ; ;
      }
    }
  }
}